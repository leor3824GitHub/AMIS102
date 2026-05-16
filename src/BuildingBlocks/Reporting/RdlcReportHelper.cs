using System.Collections;
using System.Reflection;
using Microsoft.Reporting.NETCore;

namespace AMIS.Framework.Reporting;

public static class RdlcReportHelper
{
    public static byte[] Render(
        Assembly assembly,
        string resourceName,
        IEnumerable<(string Name, IEnumerable Data)> dataSources,
        string format = "PDF",
        string? pageWidth = null,
        string? pageHeight = null,
        string margin = "0.5cm")
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded report resource '{resourceName}' not found.");

        var report = new LocalReport();
        report.LoadReportDefinition(stream);

        foreach (var (name, data) in dataSources)
            report.DataSources.Add(new ReportDataSource(name, data));

        var deviceInfo = pageWidth is not null && pageHeight is not null
            ? $"""
               <DeviceInfo>
                 <OutputFormat>{format}</OutputFormat>
                 <PageWidth>{pageWidth}</PageWidth>
                 <PageHeight>{pageHeight}</PageHeight>
                 <MarginTop>{margin}</MarginTop>
                 <MarginBottom>{margin}</MarginBottom>
                 <MarginLeft>{margin}</MarginLeft>
                 <MarginRight>{margin}</MarginRight>
               </DeviceInfo>
               """
            : null;

        return report.Render(format, deviceInfo);
    }
}
