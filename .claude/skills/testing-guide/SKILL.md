---
name: testing-guide
description: Write unit tests, integration tests, and architecture tests for AMIS features. Use when adding tests or understanding the testing strategy.
---

# Testing Guide

AMIS uses a layered testing strategy with architecture tests as guardrails.

## Test Project Structure

```
src/Tests/
├── Architecture.Tests/    # Enforces layering rules
├── Generic.Tests/         # Shared test utilities
├── Identity.Tests/        # Identity module tests
├── Multitenancy.Tests/    # Multitenancy module tests
└── Auditing.Tests/        # Auditing module tests
```

## Architecture Tests

Architecture tests enforce module boundaries and layering. They run on every build.

```csharp
public class ArchitectureTests
{
    [Fact]
    public void Modules_ShouldNot_DependOnOtherModules()
    {
        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Modules.Multitenancy")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Contracts_ShouldNot_DependOnImplementation()
    {
        var result = Types.InAssembly(typeof(UserDto).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Modules.Identity")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Handlers_ShouldBe_Sealed()
    {
        var result = Types.InAssembly(typeof(IdentityModule).Assembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Or()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
```

## Unit Test Patterns

### Handler Tests

> ⚠️ **Use the libraries the repo actually uses.** All 12 test projects use **xunit + Shouldly**; 7 add
> **NSubstitute** and 6 add **AutoFixture**. **No project references Moq or FluentAssertions** — do not
> write `.Should()` or `Mock<T>`, they will not compile. And there is no `IRepository<T>` to mock:
> handlers take the module `DbContext`, so give them a real one backed by EF InMemory or SQLite.

```csharp
using AutoFixture;
using NSubstitute;
using Shouldly;
using Xunit;

public sealed class Create{Entity}CommandHandlerTests
{
    private readonly ICurrentUser _currentUser;
    private readonly Fixture _fixture = new();

    public Create{Entity}CommandHandlerTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.GetTenant().Returns("test-tenant-id");
        _currentUser.GetUserId().Returns(Guid.NewGuid());
    }

    private static {Module}DbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<{Module}DbContext>()
            .UseInMemoryDatabase($"{Guid.NewGuid()}")   // unique name per test — no cross-test bleed
            .Options);

    [Fact]
    public async Task Handle_ValidCommand_Persists{Entity}()
    {
        // Arrange
        await using var dbContext = NewDbContext();
        var handler = new Create{Entity}CommandHandler(dbContext, _currentUser);
        var command = new Create{Entity}Command("Test", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.ShouldNotBe(Guid.Empty);
        var saved = await dbContext.{Entity}s.FindAsync(result.Id);
        saved.ShouldNotBeNull();
        saved!.Name.ShouldBe("Test");
        saved.CreatedBy.ShouldNotBeNullOrWhiteSpace();   // handler sets this by hand
    }
}
```

**Multi-tenant handlers** need a Finbuckle tenant context, not just a DbContext. Reuse the existing
harness rather than rebuilding one:
[MultiTenantTestHost.cs](../../../src/Tests/AssetRegister.Tests/Integration/MultiTenantTestHost.cs).

### Validator Tests

```csharp
public class Create{Entity}ValidatorTests
{
    private readonly Create{Entity}Validator _validator = new();

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var command = new Create{Entity}Command("", 99.99m);
        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NegativePrice_Fails()
    {
        var command = new Create{Entity}Command("Test", -1m);
        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Price");
    }

    [Theory]
    [InlineData("Valid Name", 10)]
    [InlineData("Another", 0.01)]
    public void Validate_ValidCommand_Passes(string name, decimal price)
    {
        var command = new Create{Entity}Command(name, price);
        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
```

### Entity Tests

```csharp
public class {Entity}Tests
{
    [Fact]
    public void Create_ValidInput_Creates{Entity}WithEvent()
    {
        var entity = {Entity}.Create("Test", 99.99m, "tenant-1");

        entity.Id.ShouldNotBe(Guid.Empty);
        entity.Name.ShouldBe("Test");
        entity.Price.ShouldBe(99.99m);
        entity.TenantId.ShouldBe("tenant-1");
        entity.DomainEvents.OfType<{Entity}CreatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Create_EmptyName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => {Entity}.Create("", 99.99m, "tenant-1"));
    }

    [Fact]
    public void UpdateDetails_ValidInput_UpdatesAndRaisesEvent()
    {
        var entity = {Entity}.Create("Original", 50m, "tenant-1");
        entity.ClearDomainEvents();

        entity.UpdateDetails("Updated", 75m, "New description");

        entity.Name.ShouldBe("Updated");
        entity.Price.ShouldBe(75m);
        entity.Description.ShouldBe("New description");
        entity.DomainEvents.OfType<{Entity}UpdatedEvent>().ShouldHaveSingleItem();
    }
}
```

## Running Tests

```bash
# Run all tests
dotnet test src/AMIS.Framework.slnx

# Run specific test project
dotnet test src/Tests/Architecture.Tests

# Run with coverage
dotnet test src/AMIS.Framework.slnx --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~Create{Entity}HandlerTests"
```

## Test Conventions

| Convention | Example |
|------------|---------|
| Test class name | `{ClassUnderTest}Tests` |
| Test method name | `{Method}_{Scenario}_{ExpectedResult}` |
| Arrange-Act-Assert | Always use this structure |
| One assertion concept | Multiple asserts OK if same concept |

## Key Rules

1. **Architecture tests are mandatory** - They enforce module boundaries
2. **Validators need tests** - Cover edge cases
3. **Handlers need tests** - Substitute collaborators, give the handler a real in-memory `DbContext`
4. **Entities need tests** - Test factory methods and domain logic
5. **Use Shouldly** - `x.ShouldBe(y)` syntax. **Not** FluentAssertions — it is not referenced anywhere
6. **Use NSubstitute for mocking** - `Substitute.For<T>()`. **Not** Moq — it is not referenced anywhere
7. **Use AutoFixture** for filling in irrelevant command properties (`_fixture.Build<T>().With(...)`)

