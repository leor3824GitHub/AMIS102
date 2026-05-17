using System.Collections;
using System.Data;
using System.Reflection;
using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using FastReport;
using FastReport.Data;
using FastReport.Export.PdfSimple;

namespace AMIS.Modules.FastReporting.Services;

/// <summary>
/// Boilerplate wrapper for FastReport.OpenSource. Loads a <c>.frx</c> template from an
/// assembly's embedded resources, registers caller-supplied data sources, runs optional
/// configuration hooks (page setup, band binding), and exports to PDF.
///
/// Handlers should call <see cref="GenerateAsync"/> rather than reimplementing the
/// load → register → enable → prepare → export sequence. Use the <c>configureReport</c>
/// hook for page-level tweaks (paper size, hiding objects) and <c>configureDataBindings</c>
/// for band-level fixes (e.g. explicit <see cref="DataBand.DataSource"/> rebinding).
/// </summary>
public static class FastReportService
{
    public static async Task<ReportFileDto> GenerateAsync(
        Assembly assembly,
        string templateName,
        IReadOnlyList<ReportDataSource> sources,
        ReportFormat format = ReportFormat.Pdf,
        Action<Report>? configureReport = null,
        Action<Report>? configureDataBindings = null,
        string? fileName = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(sources);

        ct.ThrowIfCancellationRequested();

        await using var templateStream = LoadTemplate(assembly, templateName);
        using var report = new Report();
        report.Load(templateStream);

        configureReport?.Invoke(report);

        foreach (var src in sources)
            RegisterAndEnable(report, src);

        configureDataBindings?.Invoke(report);

        await report.PrepareAsync(ct).ConfigureAwait(false);

        return format switch
        {
            ReportFormat.Pdf => ExportToPdf(report, fileName ?? templateName),
            ReportFormat.Excel => throw new NotSupportedException(
                "Excel export is not enabled. Add FastReport.OpenSource.Export.OoXML and " +
                "extend FastReportService with an Excel exporter."),
            _ => throw new NotSupportedException($"Report format '{format}' is not supported.")
        };
    }

    private static void RegisterAndEnable(Report report, ReportDataSource src)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(src.Name);
        ArgumentNullException.ThrowIfNull(src.Data);

        // FastReport's RegisterData has separate overloads for DataTable and IEnumerable.
        // Dispatch by runtime type — passing a DataTable as IEnumerable iterates DataRows
        // generically and loses column metadata.
        switch (src.Data)
        {
            case DataTable table:
                report.RegisterData(table, src.Name);
                break;
            case DataSet set:
                report.RegisterData(set, src.Name);
                break;
            case IEnumerable enumerable:
                report.RegisterData(enumerable, src.Name, maxNestingLevel: src.MaxNestingLevel);
                break;
            default:
                throw new ArgumentException(
                    $"Data source '{src.Name}' has unsupported type '{src.Data.GetType()}'. " +
                    "Pass an IEnumerable, a DataTable, or a DataSet.",
                    nameof(src));
        }

        // Business-object data sources are disabled by default; many .frx layouts reference
        // them only from TextObjects on title/summary bands. Enabling unconditionally lets
        // [DataSource.Field] expressions resolve regardless of the band wiring.
        var ds = report.GetDataSource(src.Name);
        if (ds is not null)
            ds.Enabled = true;
    }

    private static Stream LoadTemplate(Assembly assembly, string templateName)
    {
        var resourceName = templateName.EndsWith(".frx", StringComparison.OrdinalIgnoreCase)
            ? templateName
            : $"{assembly.GetName().Name}.Templates.{templateName}.frx";

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"FastReport template '{templateName}' not found. " +
                $"Expected embedded resource '{resourceName}'. " +
                "Ensure the .frx file is in Templates/ with Build Action = EmbeddedResource.");
    }

    private static ReportFileDto ExportToPdf(Report report, string fileName)
    {
        using var output = new MemoryStream();
        using var export = new PDFSimpleExport();
        export.Export(report, output);

        var name = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.pdf";

        return new ReportFileDto(output.ToArray(), "application/pdf", name);
    }
}

/// <summary>
/// Pairs a data source name (matching the FastReport Designer DataSource name) with its
/// data — accepts <see cref="IEnumerable"/>, <see cref="DataTable"/>, or <see cref="DataSet"/>.
/// </summary>
public sealed record ReportDataSource(string Name, object Data, int MaxNestingLevel = 2);
