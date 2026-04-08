using FluentAssertions;

using System.Linq;

namespace Infrastructure.Tests;

public sealed class StrongPasswordGeneratorTests
{
    [Fact(DisplayName = "Generate should create a 20 character password with all required character classes")]
    [Trait("Category", "Unit")]
    public void GenerateShouldCreateAPasswordWithAllRequiredCharacterClasses()
    {
        var password = StrongPasswordGenerator.Generate();

        password.Should().HaveLength(20);
        password.Any(char.IsUpper).Should().BeTrue();
        password.Any(char.IsLower).Should().BeTrue();
        password.Any(char.IsDigit).Should().BeTrue();
        password.Any(character => !char.IsLetterOrDigit(character)).Should().BeTrue();
    }

    [Fact(DisplayName = "Generate should reject lengths smaller than the required character class count")]
    [Trait("Category", "Unit")]
    public void GenerateShouldRejectLengthsSmallerThanTheRequiredCharacterClassCount()
    {
        var action = () => StrongPasswordGenerator.Generate(3);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
