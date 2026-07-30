---
paths:
  - "src/Tests/**/*"
---

# Testing Rules

Rules for tests in AMIS.

## Test Organization

```
src/Tests/
├── Architecture.Tests/    # Layering enforcement (mandatory)
├── {Module}.Tests/        # Module-specific tests
└── Generic.Tests/         # Shared utilities
```

## Naming Conventions

| Type | Pattern |
|------|---------|
| Test class | `{ClassUnderTest}Tests` |
| Test method | `{Method}_{Scenario}_{ExpectedResult}` |
| Test file | Same as class name |

## Test Structure

Always use Arrange-Act-Assert:

```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsId()
{
    // Arrange
    var command = new CreateProductCommand("Test", 10m);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.Id.ShouldNotBe(Guid.Empty);
}
```

## Required Tests

### For Handlers
- Happy path with valid input
- Edge cases (empty, null, boundary values)
- Persistence verified by reading back from the in-memory `DbContext` the handler was given
  (there is no repository to verify calls against)

### For Validators
- Each validation rule has a test
- Valid input passes
- Invalid input fails with correct property

### For Entities
- Factory method creates valid entity
- Invalid input throws appropriate exception
- Domain events raised correctly

## Libraries

These are the libraries actually referenced by all 12 test projects. **FluentAssertions and Moq are not
referenced anywhere — do not use `.Should()` or `Mock<T>`.**

- **xUnit** - test framework (12/12 projects)
- **Shouldly** - `x.ShouldBe(y)` assertions (12/12)
- **NSubstitute** - `Substitute.For<T>()` for dependencies (7/12)
- **AutoFixture** - object generation (6/12)
- **Microsoft.EntityFrameworkCore.InMemory** / **.Sqlite** - real `DbContext` in handler tests (6/12)
- **NetArchTest.Rules** - architecture tests (2/12)

## Architecture Tests

Architecture tests in `Architecture.Tests/` are mandatory and enforce:
- Module boundary isolation
- No cross-module internal dependencies
- Handlers/validators are sealed
- Contracts don't depend on implementations

These run on every build and PR.

