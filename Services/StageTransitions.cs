using Aihrly.Models;

namespace Aihrly.Services;

public static class StageTransitions
{
    private static readonly Dictionary<ApplicationStage, HashSet<ApplicationStage>> Valid = new()
    {
        [ApplicationStage.Applied]    = [ApplicationStage.Screening, ApplicationStage.Rejected],
        [ApplicationStage.Screening]  = [ApplicationStage.Interview, ApplicationStage.Rejected],
        [ApplicationStage.Interview]  = [ApplicationStage.Offer,     ApplicationStage.Rejected],
        [ApplicationStage.Offer]      = [ApplicationStage.Hired,     ApplicationStage.Rejected],
        [ApplicationStage.Hired]      = [],
        [ApplicationStage.Rejected]   = [],
    };

    public static bool IsValid(ApplicationStage from, ApplicationStage to) =>
        Valid.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool IsTerminal(ApplicationStage stage) =>
        stage is ApplicationStage.Hired or ApplicationStage.Rejected;
}
