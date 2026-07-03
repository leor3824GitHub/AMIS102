namespace AMIS.Maui.Features.Shared;

// How a property number reached the capture pipeline. Consumers that care about scan-vs-typed
// (e.g. Count records an `isScanned` flag) map this; consumers that don't (Scan) ignore it.
public enum PropertyInputSource
{
    Barcode,
    Ocr,
    Manual,
    Serial
}
