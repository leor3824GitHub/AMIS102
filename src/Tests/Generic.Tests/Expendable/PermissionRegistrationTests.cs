using System.Reflection;
using AMIS.Framework.Shared.Constants;
using AMIS.Modules.Expendable;
using Microsoft.Extensions.Hosting;

namespace Generic.Tests.Expendable;

public sealed class PermissionRegistrationTests
{
    [Fact]
    public void ConfigureServices_ShouldRegisterAllExpendablePermissions()
    {
        // Arrange
        var module = new ExpendableModule();
        var builder = Host.CreateApplicationBuilder();

        // Act
        module.ConfigureServices(builder);

        // Assert
        var registered = PermissionConstants.All.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var expected = GetConstStrings(typeof(ExpendableModuleConstants.Permissions)).ToList();

        expected.ShouldNotBeEmpty();
        expected.ShouldAllBe(permission => registered.Contains(permission));
    }

    private static IEnumerable<string> GetConstStrings(Type type)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        foreach (var field in type.GetFields(Flags))
        {
            if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            {
                var value = field.GetRawConstantValue() as string;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }
            }
        }

        foreach (var nested in type.GetNestedTypes(Flags))
        {
            foreach (var value in GetConstStrings(nested))
            {
                yield return value;
            }
        }
    }
}

