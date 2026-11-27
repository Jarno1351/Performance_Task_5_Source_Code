using HubLink.Models;

namespace HubLink.Services;

public enum FailureKind { Configuration, Unreachable, Timeout, HttpStatus, InvalidResponse }

/// <summary>A retrieval failure with a message that is safe to show to the user.</summary>
public sealed class ShipmentServiceException : Exception
{
    public FailureKind Kind { get; }
    public ShipmentServiceException(FailureKind kind, string message, Exception? inner = null)
        : base(message, inner) => Kind = kind;
}

public interface IShipmentService
{
    /// <summary>Human-readable request URL (for display only).</summary>
    string RequestDescription { get; }
    /// <exception cref="ShipmentServiceException">On any retrieval/parsing failure.</exception>
    Task<ShipmentResult> GetShipmentsAsync(CancellationToken cancellationToken = default);
}
