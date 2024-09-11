using Lotus.API.Odyssey;
using Lotus.API.Vanilla.Sabotages;
using Lotus.Patches.Systems;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Crew;
using Lotus.GUI.Name.Holders;
using Lotus.Logging;
using Lotus.Managers;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Subroles;
using Lotus.Utilities;
using UnityEngine;
using VentLib;
using VentLib.Localization.Attributes;
using VentLib.Networking.RPC;
using VentLib.Networking.RPC.Attributes;
using VentLib.Options.UI;
using VentLib.Options.IO;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Crew;

public class Mystic : Crewmate, ISubrole
{
    private static ModRPC reactorFlashRpc = Vents.FindRPC((uint)RoleRPC.ReactorFlash)!;

    private float flashDuration;
    private bool sendAudioAlert;
    private bool isSubrole;
    private bool restrictedToCrew;

    public string? Identifier() => "ī";

    protected override void PostSetup()
    {
        if (!isSubrole) return;
        MyPlayer.NameModel().GCH<CounterHolder>().RemoveLast();
    }

    public bool IsAssignableTo(PlayerControl player)
    {
        return !restrictedToCrew || player.PrimaryRole().Faction is Crewmates;
    }

    [RoleAction(LotusActionType.PlayerDeath, ActionFlag.GlobalDetector)]
    private void MysticAnyDeath(PlayerControl deadPlayer)
    {
        if (MyPlayer.Data.IsDead) return;
        if (deadPlayer.GetSubrole<Bait>() != null) return;


        Remote<GameOptionOverride> optionOverride = AddOverride(new GameOptionOverride(Override.CrewLightMod, 0f));
        SyncOptions();

        bool didReactorAlert = false;
        if (sendAudioAlert && SabotagePatch.CurrentSabotage?.SabotageType() is not SabotageType.Reactor)
        {
            didReactorAlert = true;
            if (!MyPlayer.IsModded()) RoleUtils.PlayReactorsForPlayer(MyPlayer);
            else if (MyPlayer.IsHost()) reactorFlashRpc.InvokeTrampoline(false);
            else reactorFlashRpc.Send(new[] { MyPlayer.GetClientId() }, false);
        }

        Async.Schedule(() => MysticRevertAlert(optionOverride, didReactorAlert), NetUtils.DeriveDelay(flashDuration));
    }

    private void MysticRevertAlert(Remote<GameOptionOverride> remote, bool didReactorAlert)
    {
        remote.Delete();
        SyncOptions();
        if (!didReactorAlert) return;
        if (!MyPlayer.IsModded()) RoleUtils.EndReactorsForPlayer(MyPlayer);
        else if (MyPlayer.IsHost()) reactorFlashRpc.InvokeTrampoline(true);
        else reactorFlashRpc.Send(new[] { MyPlayer.GetClientId() }, true);
    }

    [ModRPC(RoleRPC.ReactorFlash, invocation: MethodInvocation.ExecuteAfter)]
    private static void ReactorFlash(bool finish)
    {
        if (finish) HudManager.Instance.StopReactorFlash();
        else HudManager.Instance.StartReactorFlash();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Mystic is a Modifier", Translations.Options.MysticIsModifier)
                .BindBool(b =>
                {
                    isSubrole = b;
                    if (b) RoleFlags |= RoleFlag.IsSubrole;
                    else RoleFlags &= ~RoleFlag.IsSubrole;
                })
                .ShowSubOptionPredicate(b => (bool)b)
                .SubOption(sub2 => sub2.KeyName("Restricted to Crewmates", TranslationUtil.Colorize(Translations.Options.RestrictedToCrewmates, FactionInstances.Crewmates.Color))
                    .AddBoolean()
                    .BindBool(b => restrictedToCrew = b)
                    .Build())
                .AddBoolean(false)
                .Build())
            .SubOption(sub => sub
                .KeyName("Flash Duration", Translations.Options.FlashDuration)
                .Bind(v => flashDuration = (float)v)
                .AddFloatRange(0.5f, 1.5f, 0.1f, 0, GeneralOptionTranslations.SecondsSuffix)
                .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
                .Build())
            .SubOption(sub => sub
                .KeyName("Send Audio Alert", Translations.Options.SendAudioAlert)
                .BindBool(v => sendAudioAlert = v)
                .AddBoolean()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        RoleModifier modifier = base.Modify(roleModifier).RoleColor(new Color(0.3f, 0.6f, 0.9f));
        return isSubrole ? modifier.RoleFlags(RoleFlag.IsSubrole) : modifier;
    }

    [Localized(nameof(Mystic))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(MysticIsModifier))]
            public static string MysticIsModifier = "Mystic is a Modifier";

            [Localized(nameof(RestrictedToCrewmates))]
            public static string RestrictedToCrewmates = "Restricted to Crewmates::0";

            [Localized(nameof(FlashDuration))]
            public static string FlashDuration = "Flash Duration";

            [Localized(nameof(SendAudioAlert))]
            public static string SendAudioAlert = "Send Audio Alert";
        }
    }


}