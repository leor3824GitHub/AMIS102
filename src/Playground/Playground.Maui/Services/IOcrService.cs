namespace Playground.Maui.Services;

public interface IOcrService
{
    bool IsSupported { get; }

    Task<string?> RecognizeTextAsync(Stream image, CancellationToken ct = default);
}
