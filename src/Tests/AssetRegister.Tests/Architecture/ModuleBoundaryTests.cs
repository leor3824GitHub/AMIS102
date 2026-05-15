using System.Reflection;
using AMIS.Modules.AssetRegister;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Architecture;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Module_DoesNotReference_OtherModuleImplementations()
    {
        var asm = typeof(AssetRegisterModule).Assembly;
        var bannedPrefixes = new[]
        {
            "AMIS.Modules.AssetManagement",
            "AMIS.Modules.ProcurementAcquisition",
            "AMIS.Modules.ProcurementPlanning",
            "AMIS.Modules.Finance",
            "AMIS.Modules.Vehicle",
            "AMIS.Modules.Expendable",
            "AMIS.Modules.MasterData",
        };

        var referenced = asm.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        foreach (var banned in bannedPrefixes)
        {
            referenced.ShouldNotContain(banned,
                customMessage: $"AssetRegister implementation must not reference '{banned}'.");
        }
    }

    [Fact]
    public void Module_OnlyContractsRefAllowed_ProcurementAcquisition()
    {
        // ProcurementAcquisition.Contracts is the single allowed cross-module reference,
        // for the inbound AssetIARAcceptedEvent.
        var asm = typeof(AssetRegisterModule).Assembly;
        var referenced = asm.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        referenced.ShouldContain("AMIS.Modules.ProcurementAcquisition.Contracts");
        referenced.ShouldNotContain("AMIS.Modules.ProcurementAcquisition",
            customMessage: "Only ProcurementAcquisition.Contracts may be referenced, not the implementation assembly.");
    }

    [Fact]
    public void AggregateRoots_HaveNoPublicSetters_OnDomainState()
    {
        var asm = typeof(AssetRegisterModule).Assembly;
        var aggregateRootNames = new[]
        {
            "AssetRegistry",
            "PropertyAccountability",
            "PropertyIssuanceReport",
            "PhysicalCountSession",
            "PropertyIncidentReport",
            "UnserviceablePropertyReport",
        };

        // Audit-trail and EF-required setters that we deliberately allow on roots.
        var allowedPublicSetters = new HashSet<string>(StringComparer.Ordinal)
        {
            "CreatedOnUtc", "CreatedBy", "LastModifiedOnUtc", "LastModifiedBy",
            "DeletedOnUtc", "DeletedBy", "IsDeleted",
            "Version", // concurrency token; EF needs to write
        };

        foreach (var name in aggregateRootNames)
        {
            var type = asm.GetTypes().FirstOrDefault(t => t.Name == name);
            type.ShouldNotBeNull($"Aggregate root '{name}' not found in module assembly.");

            var offenders = type!.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is { IsPublic: true })
                .Where(p => !allowedPublicSetters.Contains(p.Name))
                .Select(p => p.Name)
                .ToList();

            offenders.ShouldBeEmpty(
                $"Aggregate root '{name}' must not expose public setters on domain state. Offenders: {string.Join(", ", offenders)}");
        }
    }
}

