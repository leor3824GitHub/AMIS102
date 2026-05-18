namespace AMIS.Modules.FastReporting.Contracts.v1.Reports;

public sealed record ReportFileDto(
    byte[] Content,
    string ContentType,
    string FileName);
