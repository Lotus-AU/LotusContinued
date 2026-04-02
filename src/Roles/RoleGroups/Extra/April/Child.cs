using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Managers.History.Events;
using Lotus.Roles.Interactions;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Victory.Conditions;
using UnityEngine;
using VentLib.Localization.Attributes;

namespace Lotus.Roles.RoleGroups.Extra.April;

public class Child: Crewmate, IAprilFoolsRole
{
    public override string GetRoleOutfitPath() => "RoleOutfits/Neutral/Jester".ToLower();

    [RoleAction(LotusActionType.PlayerDeath)]
    private void MyDeath()
    {
        if (Game.State is not GameState.Roaming) return;
        Players.GetAllPlayers().Do(p =>
        {
            if (p.IsAlive())
                MyPlayer.InteractWith(p,
                    new UnblockedInteraction(new FatalIntent(true,
                        () => new CustomDeathEvent(p, MyPlayer, Translations.EarDamageReason)), this));
        });
        Game.MatchData.GameHistory.SetCauseOfDeath(MyPlayer.PlayerId, new CustomDeathEvent(MyPlayer, null, Translations.EarDamageReason));
        ManualWin.Activate(MyPlayer, ReasonType.SoloWinner, 999);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => base
        .Modify(roleModifier)
        .RoleFlags(((IAprilFoolsRole)this).HideIfNotAprilFools(RoleFlags))
        .RoleColor(Color.white);

    [Localized(nameof(Child))]
    internal static class Translations
    {
        [Localized(nameof(EarDamageReason))] public static string EarDamageReason = "Ear Damage";
        [Localized(nameof(ScreamedDeathReason))] public static string ScreamedDeathReason = "Screamed";
    }
}