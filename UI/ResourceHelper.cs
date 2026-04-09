using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI;

/// <summary>
/// Builds resource strings from combat state. Centralizes formatting so
/// energy, stars, and any future resources are consistent everywhere.
/// </summary>
public static class ResourceHelper
{
    public static string GetResourceString(PlayerCombatState pcs)
    {
        var parts = new List<string>();
        parts.Add(Message.Localized("ui", "RESOURCE.ENERGY", new { current = pcs.Energy, max = pcs.MaxEnergy }).Resolve());
        if (pcs.Stars > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.STARS", new { amount = pcs.Stars }).Resolve());
        return string.Join(", ", parts);
    }
}
