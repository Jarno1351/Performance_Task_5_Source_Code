using System.Text;
using HubLink.Models;

namespace HubLink.Services;

public interface ICsvExporter
{
    string Build(IEnumerable<Shipment> shipments);
    Task WriteAsync(string path, IEnumerable<Shipment> shipments, CancellationToken ct = default);
}

/// <summary>RFC 4180-style CSV: header row, CRLF line ends, quoting for commas/quotes/newlines.</summary>
public sealed class CsvExporter : ICsvExporter
{
    public static readonly string[] Columns = { "TrackingNumber", "Origin", "Destination", "Status", "LastUpdatedUtc" };

    public string Build(IEnumerable<Shipment> shipments)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(",", Columns)).Append("\r\n");
        foreach (var s in shipments)
        {
            sb.Append(Escape(s.TrackingNumber)).Append(',')
              .Append(Escape(s.Origin)).Append(',')
              .Append(Escape(s.Destination)).Append(',')
              .Append(Escape(s.StatusText)).Append(',')
              .Append(Escape(s.LastUpdatedUtcText)).Append("\r\n");
        }
        return sb.ToString();
    }

    public static string Escape(string value)
    {
        bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                           || value.StartsWith(' ') || value.EndsWith(' ');
        return needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }

    public async Task WriteAsync(string path, IEnumerable<Shipment> shipments, CancellationToken ct = default)
    {
        var content = Build(shipments);
        var temp = path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temp, content, new UTF8Encoding(false), ct);
            File.Move(temp, path, overwrite: true);   // only appears at the target path once fully written
        }
        catch
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { /* ignore cleanup errors */ }
            throw;
        }
    }
}
