using FluentAssertions;
using MermaidFlow.Application.Mermaid;
using Xunit;

namespace MermaidFlow.Application.Tests.Mermaid;

public class MermaidConstantsTests
{
    [Fact]
    public void MaxCodeLength_Is51200() => MermaidConstants.MaxCodeLength.Should().Be(51_200);
}