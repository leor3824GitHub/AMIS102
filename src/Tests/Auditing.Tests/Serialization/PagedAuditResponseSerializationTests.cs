using System.Text.Json;
using System.Text.Json.Serialization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Auditing.Contracts;
using AMIS.Modules.Auditing.Contracts.Dtos;
using Shouldly;
using Xunit;

namespace AMIS.Modules.Auditing.Tests.Serialization;

/// <summary>
/// Reproduces the Blazor "Could not deserialize the response body stream"
/// failure to verify enum properties round-trip as numbers under the API's
/// global JsonStringEnumConverter.
/// </summary>
public class PagedAuditResponseSerializationTests
{
    private static JsonSerializerOptions ApiOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    [Fact]
    public void AuditSummaryDto_SerializesEnumsAsNumbers_UnderGlobalStringEnumConverter()
    {
        var dto = new AuditSummaryDto
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            EventType = AuditEventType.Security,
            Severity = AuditSeverity.Information,
            Tags = AuditTag.Authentication | AuditTag.Authorization,
            Source = "test"
        };

        var json = JsonSerializer.Serialize(dto, ApiOptions());

        json.ShouldContain("\"eventType\":2", customMessage: $"actual JSON: {json}");
        json.ShouldContain("\"severity\":3", customMessage: $"actual JSON: {json}");
        json.ShouldContain("\"tags\":96", customMessage: $"actual JSON: {json}");
    }

    [Fact]
    public void PagedResponse_OfAuditSummary_RoundTrips()
    {
        var page = new PagedResponse<AuditSummaryDto>
        {
            Items = new[]
            {
                new AuditSummaryDto
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = DateTime.UtcNow,
                    EventType = AuditEventType.EntityChange,
                    Severity = AuditSeverity.Warning,
                    Tags = AuditTag.PiiMasked
                }
            },
            PageNumber = 1,
            PageSize = 25,
            TotalCount = 1,
            TotalPages = 1
        };

        var json = JsonSerializer.Serialize(page, ApiOptions());

        json.ShouldContain("\"totalCount\":1");
        json.ShouldContain("\"hasNext\":false");
        json.ShouldContain("\"hasPrevious\":false");
        json.ShouldNotContain("\"EntityChange\"", customMessage: $"enums must serialize as numbers; JSON: {json}");
    }
}

