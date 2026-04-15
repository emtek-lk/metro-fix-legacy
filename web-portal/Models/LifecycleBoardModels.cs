namespace GTEK.FSM.WebPortal.Models;

public enum LifecycleTransitionDirection
{
    Forward,
    Backflow,
    SameStage,
}

public sealed record LifecycleTransitionCardModel(
    string FromStatus,
    string ToStatus,
    bool IsEnabled,
    LifecycleTransitionDirection Direction,
    string BadgeLabel,
    string DirectionIndicator,
    string Key,
    bool IsImmutable = false);

public sealed record LifecycleColumnModel(
    string Status,
    IReadOnlyList<LifecycleTransitionCardModel> Transitions);
