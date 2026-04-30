using Aihrly.Models;
using Aihrly.Services;

namespace Aihrly.Tests.Unit;

public class StageTransitionTests
{
    [Fact]
    public void ValidTransition_Applied_To_Screening_ReturnsTrue()
    {
        var result = StageTransitions.IsValid(ApplicationStage.Applied, ApplicationStage.Screening);
        Assert.True(result);
    }

    [Fact]
    public void ValidTransition_Applied_To_Rejected_ReturnsTrue()
    {
        var result = StageTransitions.IsValid(ApplicationStage.Applied, ApplicationStage.Rejected);
        Assert.True(result);
    }

    [Fact]
    public void InvalidTransition_Applied_To_Hired_ReturnsFalse()
    {
        var result = StageTransitions.IsValid(ApplicationStage.Applied, ApplicationStage.Hired);
        Assert.False(result);
    }

    [Fact]
    public void InvalidTransition_Applied_To_Interview_ReturnsFalse()
    {
        var result = StageTransitions.IsValid(ApplicationStage.Applied, ApplicationStage.Interview);
        Assert.False(result);
    }

    [Fact]
    public void TerminalStage_Hired_IsTerminal_ReturnsTrue()
    {
        var result = StageTransitions.IsTerminal(ApplicationStage.Hired);
        Assert.True(result);
    }

    [Fact]
    public void TerminalStage_Rejected_IsTerminal_ReturnsTrue()
    {
        var result = StageTransitions.IsTerminal(ApplicationStage.Rejected);
        Assert.True(result);
    }

    [Fact]
    public void NonTerminalStage_Screening_IsTerminal_ReturnsFalse()
    {
        var result = StageTransitions.IsTerminal(ApplicationStage.Screening);
        Assert.False(result);
    }
}
