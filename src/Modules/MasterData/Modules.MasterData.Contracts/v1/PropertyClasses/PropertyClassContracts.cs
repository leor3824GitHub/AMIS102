using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record PropertyClassItemDto(
    Guid Id,
    string ClassCode,
    string ItemCode,
    string Name,
    string? Description,
    bool IsActive);

public sealed record PropertyClassDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<PropertyClassItemDto> Items);

/// <summary>Flat summary used for search / autocomplete dropdowns.</summary>
public sealed record PropertyClassSummaryDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

// ── Queries ───────────────────────────────────────────────────────────────────

/// <summary>Returns all active property classes with their items (tree view).</summary>
public sealed record GetPropertyClassTreeQuery() : IQuery<IReadOnlyList<PropertyClassDto>>;

/// <summary>Returns a single property class including its items.</summary>
public sealed record GetPropertyClassByIdQuery(Guid Id) : IQuery<PropertyClassDto?>;

/// <summary>
/// Returns all active items across all classes, or filtered by ClassCode.
/// Used in autocomplete dropdowns when selecting classification for a PPE item.
/// </summary>
public sealed record GetPropertyClassItemsQuery(string? ClassCode = null)
    : IQuery<IReadOnlyList<PropertyClassItemDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public sealed record CreatePropertyClassCommand(
    string Code,
    string Name,
    string? Description = null) : ICommand<Guid>;

public sealed record UpdatePropertyClassCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive) : ICommand;

public sealed record CreatePropertyClassItemCommand(
    Guid PropertyClassId,
    string ItemCode,
    string Name,
    string? Description = null) : ICommand<Guid>;

public sealed record UpdatePropertyClassItemCommand(
    Guid Id,
    string ItemCode,
    string Name,
    string? Description,
    bool IsActive) : ICommand;

