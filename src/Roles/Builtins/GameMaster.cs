using System.Collections.Generic;
using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.GUI;
using Lotus.Options;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Overrides;
using UnityEngine;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.Builtins;

public sealed class GameMaster : CustomRole, IPhantomRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(GameMaster));
    private byte lastHauntedPlayer = 255;
    private Cooldown hauntCooldown = null!;
    public static Color GMColor = new(1f, 0.4f, 0.4f);

    public override string GetRoleOutfitPath() => $"RoleOutfits/Extra/{EnglishRoleName}".ToLower();

    [RoleAction(LotusActionType.RoundStart, ActionFlag.WorksAfterDeath)]
    private void ExileGM(bool roundStart)
    {
        lastHauntedPlayer = 255;
        log.Debug($"is Spectator: {GeneralOptions.AdminOptions.SpectatorMode}");
        if (!roundStart)
        {
            if (GeneralOptions.AdminOptions.SpectatorMode && MyPlayer.Data.Role.IsDead) Async.Schedule(Reset, 1f);
            return;
        }
        MyPlayer.RpcExileV2(false);

        Players.GetPlayers().Where(p => p.PlayerId != MyPlayer.PlayerId)
            .SelectMany(p => p.NameModel().ComponentHolders())
            .ForEach(holders =>
                {
                    holders.AddListener(component => component.AddViewer(MyPlayer));
                    holders.Components().ForEach(components => components.AddViewer(MyPlayer));
                }
            );

        MyPlayer.NameModel().Render(force: true);
        if (GeneralOptions.AdminOptions.SpectatorMode)
        {
            // generates haunt minigame
            Async.Schedule(Reset, 1f);
        }
    }

    private void Reset()
    {
        HauntMenuMinigame minigame = DestroyableSingleton<HudManager>.Instance.GetComponentInChildren<HauntMenuMinigame>();
        if (minigame != null) return;
        if (MyPlayer.Data.Role.IsDead) MyPlayer.Data.Role.UseAbility();
        hauntCooldown.Finish();
    }

    [RoleAction(LotusActionType.ReportBody, ActionFlag.GlobalDetector | ActionFlag.WorksAfterDeath, priority: API.Priority.Last)]
    private void ShowChatOnMeeting()
    {
        if (!GeneralOptions.AdminOptions.SpectatorMode) return;
        Async.Schedule(() =>
        {
            if (MeetingHud.Instance == null) return;
            ChatController chat = DestroyableSingleton<HudManager>.Instance.Chat;
            if (!chat.IsOpenOrOpening) chat.Toggle();
        }, 3f);
    }

    [RoleAction(LotusActionType.FixedUpdate, ActionFlag.WorksAfterDeath)]
    private void OnUpdate()
    {
        if (!GeneralOptions.AdminOptions.SpectatorMode || hauntCooldown.NotReady()) return;
        hauntCooldown.Start(GeneralOptions.AdminOptions.AutoHauntCooldown);
        HauntMenuMinigame minigame = DestroyableSingleton<HudManager>.Instance.GetComponentInChildren<HauntMenuMinigame>();
        log.Debug($"Trying to set player for gm. {MyPlayer.Data.Role.IsDead}");
        if (minigame == null)
        {
            // try to generate it
            if (MyPlayer.Data.Role.IsDead) MyPlayer.Data.Role.UseAbility();
            minigame = DestroyableSingleton<HudManager>.Instance.GetComponentInChildren<HauntMenuMinigame>();
            if (minigame == null)
            {
                log.Debug("Could not find HauntMenuMinigame!");
                return;
            }
        }
        IEnumerable<PlayerControl> possibleTargets = Players.GetAlivePlayers().Where(p => p.PlayerId != lastHauntedPlayer);
        if (possibleTargets.Any())
        {
            PlayerControl target = possibleTargets.ToList().GetRandom();
            lastHauntedPlayer = target.PlayerId;
            log.Debug($"Setting haunt target: {target.name}");
            minigame.SetHauntTarget(target);
        }
        else log.Debug("Possble targets is equal to zero.");
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).Tab(DefaultTabs.HiddenTab);

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier
        .RoleName("GM")
        .RoleColor(GMColor)
        .Faction(FactionInstances.Neutral)
        .RoleFlags(RoleFlag.Hidden | RoleFlag.Unassignable | RoleFlag.CannotWinAlone)
        .OptionOverride(Override.AnonymousVoting, false);

    public bool IsCountedAsPlayer() => false;
}