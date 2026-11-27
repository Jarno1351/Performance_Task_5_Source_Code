using System.Globalization;

namespace HubLink.Models;

public enum ShipmentStatus { Pending, InTransit, Delivered, Exception }

/// <summary>Application-level shipment, produced from the API's JSON.</summary>
public sealed class Shipment
{
    public required string TrackingNumber { get; init; }
    public required string BranchCode { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required ShipmentStatus Status { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
    /// <summary>Timestamp exactly as received from the API (used for CSV export).</summary>
    public required string LastUpdatedUtcText { get; init; }

    public string StatusText => Status.ToString();
    public string LastUpdatedDisplay =>
        LastUpdated.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}

public sealed record ShipmentResult(string BranchCode, IReadOnlyList<Shipment> Shipments);
