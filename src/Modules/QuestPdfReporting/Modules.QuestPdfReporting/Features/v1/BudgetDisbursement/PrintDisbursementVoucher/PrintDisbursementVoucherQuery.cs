using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintDisbursementVoucher;

/// <summary>
/// Renders a Disbursement Voucher (DV) as a PDF, following the NFA Disbursement Voucher form layout
/// (Mode of Payment, Payee/Responsibility Center, Particulars/Amount, Certified/Approved/Received boxes).
/// </summary>
public sealed record PrintDisbursementVoucherQuery(
    Guid DvId,
    string PaperSize = "a4",
    string Orientation = "portrait",
    double Margin = 14d) : IQuery<byte[]>;
