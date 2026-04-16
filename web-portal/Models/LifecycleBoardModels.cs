namespace GTEK.FSM.WebPortal.Models;

public enum LifecycleTransitionDirection
{
    Forward,
    Backflow,
    SameStage,
}

public sealed record LifecycleTransitionCardModel(
    string FromStageId,
    string ToStageId,
    string FromStatus,
    string ToStatus,
    bool IsEnabled,
    LifecycleTransitionDirection Direction,
    string BadgeLabel,
    string DirectionIndicator,
    string Key,
    bool IsImmutable = false);

public sealed record LifecycleColumnModel(
    string StageId,
    string Status,
    IReadOnlyList<LifecycleTransitionCardModel> Transitions);
