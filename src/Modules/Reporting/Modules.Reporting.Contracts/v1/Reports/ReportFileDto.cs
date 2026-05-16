namespace AMIS.Modules.Reporting.Contracts.v1.Reports;

public sealed record ReportFileDto(
    byte[] Content,
    string ContentType,
    string FileName);
