using FluentAssertions;
using MermaidFlow.Application.Common.Interfaces;
using MermaidFlow.Application.Mermaid;
using MermaidFlow.Application.Mermaid.Commands.RenderMermaid;
using MermaidFlow.Domain.Mermaid;
using Moq;
using Xunit;

namespace MermaidFlow.Application.Tests.Mermaid;

public class RenderMermaidCommandValidatorTests
{
    private readonly Mock<IThemeRepository> _themeRepositoryMock = new();
    private readonly RenderMermaidCommandValidator _validator;

    public RenderMermaidCommandValidatorTests()
    {
        _themeRepositoryMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) =>
                name is "default" or "dark" or "forest" or "neutral"
                    ? new Theme { Id = Guid.NewGuid(), Name = name, IsActive = true, CreatedAt = DateTime.UtcNow }
                    : null);

        _validator = new RenderMermaidCommandValidator(_themeRepositoryMock.Object);
    }

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(CreateCommand("graph TD\nA --> B", "default"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_EmptyCode_Fails(string? code)
    {
        var result = await _validator.ValidateAsync(CreateCommand(code!, "default"));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("default")]
    [InlineData("dark")]
    [InlineData("forest")]
    [InlineData("neutral")]
    public async Task Validate_AllowedThemes_Passes(string theme)
    {
        var result = await _validator.ValidateAsync(CreateCommand("graph TD\nA --> B", theme));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("light")]
    public async Task Validate_InvalidTheme_Fails(string theme)
    {
        var result = await _validator.ValidateAsync(CreateCommand("graph TD\nA --> B", theme));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(MermaidConstants.MaxCodeLength, true)]
    [InlineData(MermaidConstants.MaxCodeLength + 1, false)]
    public async Task Validate_CodeLength_Passes(int length, bool shouldPass)
    {
        var result = await _validator.ValidateAsync(CreateCommand(new string('a', length), "default"));
        result.IsValid.Should().Be(shouldPass);
    }

    private static RenderMermaidCommand CreateCommand(string code, string theme) => new(code, theme);
}