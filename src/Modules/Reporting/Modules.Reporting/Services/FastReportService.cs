using System.Collections;
using AMIS.Modules.Reporting.Contracts.v1.Reports;
using FastReport;
using FastReport.Export.PdfSimple;

namespace AMIS.Modules.Reporting.Services;

/// <summary>
/// Wraps FastReport Open Source. Loads .frx templates from assembly embedded resources,
/// registers caller-supplied data sources, and exports to the requested format.
/// </summary>
public static class FastReportService
{
    public static async Task<ReportFileDto> GenerateAsync(
        string templateName,
        IEnumerable<ReportDataSource> sources,
        ReportFormat format = ReportFormat.Pdf,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var templateStream = LoadTemplate(templateName);
        using var report = new Report();

        report.Load(templateStream);

        foreach (var source in sources)
            report.RegisterData(source.Data, source.Name, maxNestingLevel: 2);

        await report.PrepareAsync(ct);

        return format switch
        {
            ReportFormat.Pdf => ExportToPdf(report, templateName),
            ReportFormat.Excel => throw new NotSupportedException(
                "Excel export is not yet enabled. " +
                "Add FastReport.OpenSource.Export.OoXML and implement Excel export."),
            _ => throw new NotSupportedException($"Report format '{format}' is not supported.")
        };
    }

    private static Stream LoadTemplate(string templateName)
    {
        var assembly = typeof(FastReportService).Assembly;
        var resourceName = $"AMIS.Modules.Reporting.Templates.{templateName}.frx";

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Report template '{templateName}' not found. " +
                $"Expected embedded resource '{resourceName}'. " +
                "Ensure the .frx file is in Templates/ with Build Action = EmbeddedResource.");
    }

    private static ReportFileDto ExportToPdf(Report report, string templateName)
    {
        using var output = new MemoryStream();
        using var export = new PDFSimpleExport();
        export.Export(report, output);
        return new ReportFileDto(output.ToArray(), "application/pdf", $"{templateName}.pdf");
    }
}

/// <summary>
/// Pairs a data source name (as defined in FastReport Designer) with its data collection.
/// </summary>
public sealed record ReportDataSource(string Name, IEnumerable Data);
