using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.ValueObjects;

public sealed class PropertyNumberTests
{
    [Fact]
    public void Create_AcceptsNfaLocalFormat()
    {
        var pn = PropertyNumber.Create("2026-NFA-00B-07-DSK-001");
        pn.Value.ShouldBe("2026-NFA-00B-07-DSK-001");
    }

    [Fact]
    public void Create_TrimsAndUppercases()
    {
        var pn = PropertyNumber.Create("  2026-nfa-00b-07-dsk-001  ");
        pn.Value.ShouldBe("2026-NFA-00B-07-DSK-001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyOrWhitespace(string input)
    {
        Should.Throw<ArgumentException>(() => PropertyNumber.Create(input));
    }

    [Fact]
    public void Create_RejectsValuesLongerThanMaxLength()
    {
        var tooLong = new string('A', PropertyNumber.MaxLength + 1);
        Should.Throw<ArgumentException>(() => PropertyNumber.Create(tooLong));
    }

    [Fact]
    public void Parse_IsAliasForCreate()
    {
        const string raw = "2026-NFA-00B-07-DSK-001";
        PropertyNumber.Parse(raw).Value.ShouldBe(raw);
    }

    [Fact]
    public void TryParse_ReturnsFalseForEmpty()
    {
        PropertyNumber.TryParse("", out var pn).ShouldBeFalse();
        pn.ShouldBeNull();
    }

    [Fact]
    public void TryParse_ReturnsTrueAndPopulatesForValidInput()
    {
        PropertyNumber.TryParse("2026-NFA-00B-07-DSK-001", out var pn).ShouldBeTrue();
        pn.ShouldNotBeNull();
        pn!.Value.ShouldBe("2026-NFA-00B-07-DSK-001");
    }
}
