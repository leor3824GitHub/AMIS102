using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.ValueObjects;

public sealed class PropertyNumberTests
{
    [Fact]
    public void Create_ProducesCoaFormattedString()
    {
        var pn = PropertyNumber.Create(2026, "07", "05", 42, "01");
        pn.Value.ShouldBe("2026-07-05-0042-01");
        pn.Year.ShouldBe(2026);
        pn.SubMajor.ShouldBe("07");
        pn.GlAccount.ShouldBe("05");
        pn.Serial.ShouldBe("0042");
        pn.Location.ShouldBe("01");
    }

    [Fact]
    public void Parse_RoundTrips_KnownCoaSample()
    {
        const string sample = "2026-07-05-0042-01";
        var pn = PropertyNumber.Parse(sample);
        pn.ToString().ShouldBe(sample);
    }

    [Theory]
    [InlineData("badformat")]
    [InlineData("2026-7-5-42-1")]
    [InlineData("2026-07-05-0042")]
    [InlineData("")]
    public void TryParse_RejectsMalformed(string input)
    {
        PropertyNumber.TryParse(input, out var pn).ShouldBeFalse();
        pn.ShouldBeNull();
    }
}

