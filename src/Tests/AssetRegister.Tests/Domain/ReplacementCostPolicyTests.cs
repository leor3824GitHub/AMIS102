using AMIS.Modules.AssetRegister.Domain.Services;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

public sealed class ReplacementCostPolicyTests
{
    private static readonly DateOnly SubjectAcquired = new(2024, 3, 15);

    [Fact]
    public void Select_NoObservation_ReturnsSubjectCost()
    {
        ReplacementCostPolicy.Select(45_000m, SubjectAcquired, latestSimilar: null)
            .ShouldBe(45_000m);
    }

    [Fact]
    public void Select_NewerSimilarAcquisition_ReturnsObservedCost()
    {
        var observed = new ReplacementCostPolicy.PriceObservation(52_000m, new DateOnly(2026, 1, 10));

        ReplacementCostPolicy.Select(45_000m, SubjectAcquired, observed)
            .ShouldBe(52_000m);
    }

    [Fact]
    public void Select_ObservationOlderThanSubject_ReturnsSubjectCost()
    {
        var observed = new ReplacementCostPolicy.PriceObservation(38_000m, new DateOnly(2022, 6, 1));

        ReplacementCostPolicy.Select(45_000m, SubjectAcquired, observed)
            .ShouldBe(45_000m);
    }

    [Fact]
    public void Select_SameDayObservation_ReturnsObservedCost()
    {
        var observed = new ReplacementCostPolicy.PriceObservation(47_500m, SubjectAcquired);

        ReplacementCostPolicy.Select(45_000m, SubjectAcquired, observed)
            .ShouldBe(47_500m);
    }

    [Fact]
    public void Select_NonPositiveObservedCost_ReturnsSubjectCost()
    {
        var observed = new ReplacementCostPolicy.PriceObservation(0m, new DateOnly(2026, 1, 10));

        ReplacementCostPolicy.Select(45_000m, SubjectAcquired, observed)
            .ShouldBe(45_000m);
    }

    [Fact]
    public void Select_NewerCheaperAcquisition_ReturnsObservedCost()
    {
        // Market prices can fall; §4.19 takes the current price, not the historical maximum.
        var observed = new ReplacementCostPolicy.PriceObservation(30_000m, new DateOnly(2026, 2, 1));

        ReplacementCostPolicy.Select(45_000m, SubjectAcquired, observed)
            .ShouldBe(30_000m);
    }
}
