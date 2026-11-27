using System.Globalization;
using System.Net;
using System.Text.Json;
using HubLink.Models;

namespace HubLink.Services;

/// <summary>All HTTP communication and JSON interpretation lives here (no UI code).</summary>
public sealed class ShipmentApiClient : IShipmentService
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ShipmentApiClient(HttpClient http, AppSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public string RequestDescription
    {
        get { try { return BuildUri().ToString(); } catch (ShipmentServiceException) { return "(invalid configuration)"; } }
    }

    private Uri BuildUri()
    {
        var url = _settings.BaseUrl.Trim().TrimEnd('/') + "/" + _settings.EndpointPath.Trim().TrimStart('/')
                  + "?branchCode=" + Uri.EscapeDataString(_settings.BranchCode.Trim());
        var extra = _settings.ExtraQuery.Trim().TrimStart('?', '&');
        if (extra.Length > 0) url += "&" + extra;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ShipmentServiceException(FailureKind.Configuration,
                $"The configured service address is not a valid http(s) URL: \"{url}\". Check appsettings.json.");
        return uri;
    }

    public async Task<ShipmentResult> GetShipmentsAsync(CancellationToken cancellationToken = default)
    {
        var uri = BuildUri();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        HttpStatusCode status;
        string reason;
        string body;
        try
        {
            using var response = await _http.GetAsync(uri, linked.Token).ConfigureAwait(false);
            status = response.StatusCode;
            reason = response.ReasonPhrase ?? status.ToString();
            body = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new ShipmentServiceException(FailureKind.Timeout,
                $"The service did not respond within {_settings.TimeoutSeconds} seconds. Check the connection and try again.");
        }
        catch (HttpRequestException ex)
        {
            throw new ShipmentServiceException(FailureKind.Unreachable,
                $"Could not reach the shipment service at {_settings.BaseUrl}. Make sure the service is running and the address is correct. ({ex.Message})", ex);
        }

        if ((int)status < 200 || (int)status > 299)
        {
            var detail = TryReadServerError(body);
            throw new ShipmentServiceException(FailureKind.HttpStatus,
                $"The service returned an error: HTTP {(int)status} ({reason})." + (detail != null ? $" Message: {detail}" : ""));
        }

        return Parse(body);
    }

    private static string? TryReadServerError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
                return e.GetString();
        }
        catch (JsonException) { }
        return null;
    }

    // ---- JSON -> application models ----
    private sealed class ResponseDto { public string? BranchCode { get; set; } public List<ShipmentDto?>? Shipments { get; set; } }
    private sealed class ShipmentDto
    {
        public string? TrackingNumber { get; set; }
        public string? BranchCode { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public string? Status { get; set; }
        public string? LastUpdatedUtc { get; set; }
    }

    private ShipmentResult Parse(string body)
    {
        ResponseDto? dto;
        try { dto = JsonSerializer.Deserialize<ResponseDto>(body, JsonOptions); }
        catch (JsonException ex)
        {
            throw Invalid("the response body is not valid JSON", ex);
        }

        if (dto?.Shipments is null || string.IsNullOrWhiteSpace(dto.BranchCode))
            throw Invalid("the response does not contain the expected branchCode and shipments fields");

        var expected = _settings.BranchCode.Trim();
        if (dto.BranchCode != expected)
            throw Invalid($"the response is for branch \"{dto.BranchCode}\" but \"{expected}\" was requested");

        var list = new List<Shipment>(dto.Shipments.Count);
        for (int i = 0; i < dto.Shipments.Count; i++)
        {
            var s = dto.Shipments[i];
            if (s is null || string.IsNullOrWhiteSpace(s.TrackingNumber) || string.IsNullOrWhiteSpace(s.Origin) || string.IsNullOrWhiteSpace(s.Destination) ||
                string.IsNullOrWhiteSpace(s.LastUpdatedUtc) || s.BranchCode != expected)
                throw Invalid($"shipment #{i + 1} is missing required fields or belongs to another branch");

            ShipmentStatus? st = s.Status switch
            {
                "Pending" => ShipmentStatus.Pending,
                "InTransit" => ShipmentStatus.InTransit,
                "Delivered" => ShipmentStatus.Delivered,
                "Exception" => ShipmentStatus.Exception,
                _ => null
            };
            if (st is null) throw Invalid($"shipment {s.TrackingNumber} has an unknown status \"{s.Status}\"");

            if (!DateTimeOffset.TryParse(s.LastUpdatedUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var when))
                throw Invalid($"shipment {s.TrackingNumber} has an invalid lastUpdatedUtc value");

            list.Add(new Shipment
            {
                TrackingNumber = s.TrackingNumber!,
                BranchCode = s.BranchCode!,
                Origin = s.Origin,
                Destination = s.Destination,
                Status = st.Value,
                LastUpdated = when,
                LastUpdatedUtcText = s.LastUpdatedUtc!
            });
        }
        return new ShipmentResult(dto.BranchCode, list);
    }

    private static ShipmentServiceException Invalid(string detail, Exception? inner = null) =>
        new(FailureKind.InvalidResponse,
            $"The service responded, but the data could not be used: {detail}. No shipments were loaded.", inner);
}
