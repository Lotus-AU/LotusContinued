using System.Collections.Generic;
using System.Diagnostics;
using AmongUs.GameOptions;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Options;
using Lotus.Roles.Interactions.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Vanilla;

public class Noisemaker : Crewmate
{
    protected bool ImpostorsGetAlert;
    protected float AlertDuration;

    [RoleAction(Internals.Enums.LotusActionType.Interaction, priority: API.Priority.Last)]
    public virtual void SendGameOptionsOnDeath(Interaction interaction, ActionHandle handle)
    {
        if (interaction.Intent is not IKillingIntent || handle.IsCanceled) return;
        // we assume that this player IS going to die
        // last should only be used for kind of postfix code since stuff should've been canceled before this
        List<Remote<GameOptionOverride>> remoteOverrides = new();
        Players.GetAllPlayers().ForEach(p => remoteOverrides.Add(Game.MatchData.Roles.AddOverride(p.PlayerId, new GameOptionOverride(Override.NoiseImpGetAlert, ImpostorsGetAlert))));
        Players.GetAllPlayers().ForEach(p => remoteOverrides.Add(Game.MatchData.Roles.AddOverride(p.PlayerId, new GameOptionOverride(Override.NoiseAlertDuration, AlertDuration))));
        Game.SyncAll();
        Async.Schedule(() => remoteOverrides.RemoveAll(remote => remote.Delete()), 0.5f);
    }

    protected GameOptionBuilder AddNoisemakerOptions(GameOptionBuilder builder)
    {
        return builder.SubOption(sub => sub
                .KeyName("Impostors Get Alert", NoisemakerTranslations.Options.ImpostorsGetAlert)
                .AddBoolean()
                .BindBool(b => ImpostorsGetAlert = b)
                .Build())
            .SubOption(sub => sub
                .KeyName("Alert Duration", NoisemakerTranslations.Options.AlertDuration)
                .AddFloatRange(0, 120, .5f, 6, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(f => AlertDuration = f)
                .Build());
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream)
    {
        try
        {
            var callingMethod = Mirror.GetCaller();
            var callingType = callingMethod?.DeclaringType;

            if (callingType == null)
            {
                return base.RegisterOptions(optionStream);
            }
            if (callingType == typeof(AbstractBaseRole)) return AddNoisemakerOptions(base.RegisterOptions(optionStream));
            else return base.RegisterOptions(optionStream);
        }
        catch
        {
            return base.RegisterOptions(optionStream);
        }
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .VanillaRole(RoleTypes.Noisemaker)
            .OptionOverride(Override.NoiseAlertDuration, () => AlertDuration)
            .OptionOverride(Override.NoiseImpGetAlert, () => ImpostorsGetAlert);

    [Localized(nameof(Noisemaker))]
    public static class NoisemakerTranslations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(ImpostorsGetAlert))]
            public static string ImpostorsGetAlert = "Killers Get Alert";

            [Localized(nameof(AlertDuration))]
            public static string AlertDuration = "Alert Duration";
        }
    }
}