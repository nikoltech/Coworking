using Coworking.Domain.Common.StateMachine;

namespace Coworking.UnitTests.StateMachine;

public class LifecycleTests
{
    private enum Step { First, Second, Third }

    private static StateGraph<Step> Graph() =>
        StateGraph<Step>.Create()
            .From(Step.First, Step.Second)
            .From(Step.Second, Step.Third)
            .Build();

    [Fact]
    public void CanMove_ToCurrentState_IsTrue()
    {
        var lifecycle = new Lifecycle<Step>(Step.First, Graph());

        Assert.True(lifecycle.CanMove(Step.First));
    }

    [Fact]
    public void MoveToCurrentState_KeepsStateAndHistoryUnchanged()
    {
        var lifecycle = new Lifecycle<Step>(Step.First, Graph());

        lifecycle.MoveTo(Step.First);

        Assert.Equal(Step.First, lifecycle.Current);
        Assert.Empty(lifecycle.History);
    }

    [Fact]
    public void MoveToUnreachableState_Throws()
    {
        var lifecycle = new Lifecycle<Step>(Step.First, Graph());

        Assert.Throws<InvalidTransitionException<Step>>(() => lifecycle.MoveTo(Step.Third));
    }

    [Fact]
    public void MoveToAllowedState_RecordsHistory()
    {
        var lifecycle = new Lifecycle<Step>(Step.First, Graph());

        lifecycle.MoveTo(Step.Second);

        Assert.Equal(Step.Second, lifecycle.Current);
        Assert.Single(lifecycle.History);
    }
}
