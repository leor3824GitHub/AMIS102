using System;
using System.Collections.Generic;

namespace AMIS.Playground.Blazor.Components.Pages.Procurement;

public sealed class QuotationForm
{
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierAddress { get; set; }
    public string? TinNumber { get; set; }
    public DateTime? QuotationDateNullable { get; set; } = DateTime.Today;
    public string? DeliveryTerms { get; set; }
    public List<QLineItemRow> LineItems { get; set; } = new() { new QLineItemRow() };
}

public sealed class QLineItemRow
{
    public bool IsSelected { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = "pc";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}