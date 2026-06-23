#if ANDROID
using Android.Gms.Tasks;
using Android.Graphics;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;
using CancellationToken = System.Threading.CancellationToken;
using Task = System.Threading.Tasks.Task;
using GmsTask = Android.Gms.Tasks.Task;
#endif

#if IOS
using System.Text;
using CoreGraphics;
using Foundation;
using UIKit;
using Vision;
#endif

#if WINDOWS
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
#endif

namespace Playground.Maui.Services;

public sealed class OcrService : IOcrService
{
    public bool IsSupported =>
#if ANDROID || IOS || WINDOWS
        true;
#else
        false;
#endif

    public async Task<string?> RecognizeTextAsync(Stream image, CancellationToken ct = default)
    {
        if (image is null) return null;

#if ANDROID
        return await RecognizeAndroidAsync(image, ct).ConfigureAwait(false);
#elif IOS
        return await RecognizeIOSAsync(image, ct).ConfigureAwait(false);
#elif WINDOWS
        return await RecognizeWindowsAsync(image, ct).ConfigureAwait(false);
#else
        await Task.CompletedTask;
        return null;
#endif
    }

#if ANDROID
    private static Task<string?> RecognizeAndroidAsync(Stream image, CancellationToken ct)
    {
        var bitmap = BitmapFactory.DecodeStream(image);
        if (bitmap is null) return Task.FromResult<string?>(null);

        var inputImage = InputImage.FromBitmap(bitmap, 0);
        var recognizer = TextRecognition.GetClient(TextRecognizerOptions.DefaultOptions);

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ct.Register(() => tcs.TrySetCanceled(ct));

        var ocrTask = recognizer.Process(inputImage);
        ocrTask.AddOnSuccessListener(new OcrSuccessListener(result =>
        {
            var text = (result as Text)?.GetText();
            tcs.TrySetResult(text);
        }));
        ocrTask.AddOnFailureListener(new OcrFailureListener(_ => tcs.TrySetResult(null)));

        return tcs.Task;
    }

    private sealed class OcrSuccessListener(Action<Java.Lang.Object?> onSuccess)
        : Java.Lang.Object, IOnSuccessListener
    {
        public void OnSuccess(Java.Lang.Object? result) => onSuccess(result);
    }

    private sealed class OcrFailureListener(Action<Java.Lang.Exception> onFailure)
        : Java.Lang.Object, IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception e) => onFailure(e);
    }
#endif

#if IOS
    private static Task<string?> RecognizeIOSAsync(Stream image, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        image.CopyTo(ms);
        var data = NSData.FromArray(ms.ToArray());
        using var uiImage = UIImage.LoadFromData(data);
        if (uiImage?.CGImage is null) return Task.FromResult<string?>(null);

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        ct.Register(() => tcs.TrySetCanceled(ct));

        var request = new VNRecognizeTextRequest((req, error) =>
        {
            if (error is not null)
            {
                tcs.TrySetResult(null);
                return;
            }
            var sb = new StringBuilder();
            if (req.GetResults<VNRecognizedTextObservation>() is { } observations)
            {
                foreach (var obs in observations)
                {
                    var top = obs.TopCandidates(1).FirstOrDefault();
                    if (top is not null) sb.AppendLine(top.String);
                }
            }
            tcs.TrySetResult(sb.ToString());
        })
        {
            RecognitionLevel = VNRequestTextRecognitionLevel.Accurate,
            UsesLanguageCorrection = false
        };

        // Run handler off the UI thread; the completion delegate fulfils tcs.
        _ = Task.Run(() =>
        {
            try
            {
                using var handler = new VNImageRequestHandler(uiImage.CGImage!, new NSDictionary());
                handler.Perform([request], out _);
            }
            catch
            {
                tcs.TrySetResult(null);
            }
        }, ct);

        return tcs.Task;
    }
#endif

#if WINDOWS
    private static async Task<string?> RecognizeWindowsAsync(Stream image, CancellationToken ct)
    {
        var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (ocrEngine is null) return null;

        var randomAccessStream = new InMemoryRandomAccessStream();
        await image.CopyToAsync(randomAccessStream.AsStreamForWrite(), ct).ConfigureAwait(false);
        randomAccessStream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
        var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
        return ocrResult?.Text;
    }
#endif
}
