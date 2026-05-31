using AMIS.Framework.Shared.Constants;

namespace AMIS.Modules.QuestPdfReporting;

public static class QuestPdfReportingModuleConstants
{
    public const string ModuleName = "QuestPdfReporting";

    public static readonly IReadOnlyList<AmisPermission> Permissions =
    [
        new("View QuestPDF Expendable Reports",      "View", "QuestPdfReporting.Expendable"),
        new("View QuestPDF Vehicle Reports",         "View", "QuestPdfReporting.Vehicle"),
        new("View QuestPDF Asset Management Reports","View", "QuestPdfReporting.AssetManagement"),
    ];
}
