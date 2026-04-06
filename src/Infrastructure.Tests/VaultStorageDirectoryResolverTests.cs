using FluentAssertions;

namespace Infrastructure.Tests;

public sealed class VaultStorageDirectoryResolverTests
{
    [Fact(DisplayName = "ResolveStorageDirectory should return the default path when override is empty")]
    [Trait("Category", "Unit")]
    public void ResolveStorageDirectoryShouldReturnTheDefaultPathWhenOverrideIsEmpty()
    {
        // Arrange
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalPass");

        // Act
        var resolvedPath = VaultStorageDirectoryResolver.ResolveStorageDirectory("   ");

        // Assert
        resolvedPath.Should().Be(Path.GetFullPath(expectedPath));
    }

    [Fact(DisplayName = "ResolveStorageDirectory should expand environment variables")]
    [Trait("Category", "Unit")]
    public void ResolveStorageDirectoryShouldExpandEnvironmentVariables()
    {
        // Arrange
        const string variableName = "LOCALPASS_STORAGE_TEST_ROOT";
        var overridePath = $"%{variableName}%\\Vault";
        var tempRoot = Path.Combine(Path.GetTempPath(), "LocalPass.Tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(variableName, tempRoot);

        try
        {
            // Act
            var resolvedPath = VaultStorageDirectoryResolver.ResolveStorageDirectory(overridePath);

            // Assert
            resolvedPath.Should().Be(Path.GetFullPath(Path.Combine(tempRoot, "Vault")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
