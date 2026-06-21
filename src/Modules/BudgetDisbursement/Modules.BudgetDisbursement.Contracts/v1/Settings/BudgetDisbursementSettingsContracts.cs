using Mediator;

namespace AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;

public sealed record BudgetDisbursementSettingsDto(
    bool WatermarkSignedCopies,
    // DV signatory overrides — null means "fall back to org profile"
    string? DvSectionAName,
    string? DvSectionADesignation,
    string? DvSectionBName,
    string? DvSectionBDesignation,
    string? DvSectionCName,
    string? DvSectionCDesignation,
    // BUR signatory overrides — null means "fall back to org profile"
    string? BurSectionAName,
    string? BurSectionADesignation,
    string? BurSectionBName,
    string? BurSectionBDesignation);

public sealed record GetBudgetDisbursementSettingsQuery : IQuery<BudgetDisbursementSettingsDto>;

public sealed record UpdateBudgetDisbursementSettingsCommand(
    bool WatermarkSignedCopies,
    string? DvSectionAName,
    string? DvSectionADesignation,
    string? DvSectionBName,
    string? DvSectionBDesignation,
    string? DvSectionCName,
    string? DvSectionCDesignation,
    // BUR signatory overrides
    string? BurSectionAName,
    string? BurSectionADesignation,
    string? BurSectionBName,
    string? BurSectionBDesignation) : ICommand;
