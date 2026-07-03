using AMIS.Maui.Features.Asset;
using AMIS.Maui.Features.Shared;
using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Scan;

// Asset lookup: any input source (barcode / OCR / manual / serial) resolves to a property number,
// then navigates to the asset detail page. All the capture logic lives in PropertyCaptureViewModel;
// this subclass supplies only the terminal action (read/navigate).
public sealed partial class ScanViewModel(IOcrService ocr, IApiClient api)
    : PropertyCaptureViewModel(api, ocr)
{
    protected override Task HandleResolvedPropertyNoAsync(string propertyNo, PropertyInputSource source) =>
        Shell.Current.GoToAsync($"{nameof(AssetDetailPage)}?PropertyNo={Uri.EscapeDataString(propertyNo)}");
}
