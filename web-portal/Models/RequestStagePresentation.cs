using GTEK.FSM.Shared.Contracts.Vocabulary;

namespace GTEK.FSM.WebPortal.Models;

public static class RequestStagePresentation
{
    public static string GetLabel(RequestStage stage, bool isEscalated = false)
    {
        if (isEscalated)
        {
            return "Escalated";
        }

        return RequestLifecycleTerminology.GetDisplayLabel(stage.ToString());
    }

    public static string GetCssClass(RequestStage stage, bool isEscalated = false)
    {
        if (isEscalated)
        {
            return "escalated";
        }

        return stage switch
        {
            RequestStage.New => "new",
            RequestStage.Assigned => "assigned",
            RequestStage.InProgress => "in-progress",
            RequestStage.OnHold => "on-hold",
            RequestStage.Completed => "completed",
            RequestStage.Cancelled => "cancelled",
            _ => "unknown",
        };
    }

    public static string GetStatusIconClass(RequestStage stage, bool isEscalated = false)
    {
        if (isEscalated) return "icon-escalated";
        return stage switch
        {
            RequestStage.New => "icon-new",
            RequestStage.Assigned => "icon-assigned",
            RequestStage.InProgress => "icon-in-progress",
            RequestStage.OnHold => "icon-on-hold",
            RequestStage.Completed => "icon-completed",
            RequestStage.Cancelled => "icon-cancelled",
            _ => "icon-unknown",
        };
    }

    public static string? MapPipelineStage(RequestStage stage)
    {
        return stage switch
        {
            RequestStage.New => "New",
            RequestStage.Assigned => "Assigned",
            RequestStage.InProgress => "In Progress",
            RequestStage.OnHold => "On Hold",
            RequestStage.Completed => null,
            RequestStage.Cancelled => null,
            _ => null,
        };
    }

    public static string MapWorkspaceStage(RequestStage stage)
    {
        return GetLabel(stage);
    }
}
