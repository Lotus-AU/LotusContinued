using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Lotus.API;
using Lotus.API.Player;
using Lotus.Extensions;
using Lotus.Factions;
using Lotus.Factions.Impostors;
using Lotus.Roles;
using Lotus.Utilities;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Harmony.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.API.Odyssey;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.Roles.Overrides;
using UnityEngine;
using VentLib.Networking.RPC.Interfaces;
using VentLib.Utilities.Attributes;

namespace Lotus.Patches.Actions;

// phantom is very broken right now...
// but there are some uses.
// we shouldn't have to fix your code
// pls do a proper fix innersloth

// code sampled from Endless Host Roles (https://github.com/Gurge44/EndlessHostRoles/blob/main/Patches/PhantomRolePatch.cs)

[LoadStatic]
public static class PhantomActionsPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(PhantomActionsPatch));
    private static readonly string PhantomActionsPatchHookKey = nameof(PhantomActionsPatchHookKey);
    private static readonly Dictionary<byte, string> PetsList = [];
    private static readonly List<byte> InvisiblePlayers = [];


    private static int _ignoreSetInvisibilityPatch;

    public static bool IsInvisible(this PlayerControl player) => InvisiblePlayers.Contains(player.PlayerId);

    static PhantomActionsPatch()
    {
        Hooks.GameStateHooks.GameStartHook.Bind(PhantomActionsPatchHookKey, _ =>
        {
            InvisiblePlayers.Clear();
            PetsList.Clear();
        });
        Hooks.GameStateHooks.RoundEndHook.Bind(PhantomActionsPatchHookKey, OnMeetingCalled);
        Hooks.GameStateHooks.RoundStartHook.Bind(PhantomActionsPatchHookKey, _ =>
        {
            InvisiblePlayers.Clear();
            PetsList.Clear();
        });
    }

    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.CmdCheckVanish))]
    public static bool CmdCheckVanishPrefix(PlayerControl __instance, float maxDuration)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            __instance.CheckVanish();
            return false;
        }

        __instance.SetRoleInvisibility(true);
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.CheckVanish, SendOption.Reliable, AmongUsClient.Instance.HostId);
        messageWriter.Write(maxDuration);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        return false;
    }

    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.CmdCheckAppear))]
    public static bool CmdCheckAppearPrefix(PlayerControl __instance, bool shouldAnimate)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            __instance.CheckAppear(shouldAnimate);
            return false;
        }

        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.CheckAppear, SendOption.Reliable, AmongUsClient.Instance.HostId);
        messageWriter.Write(shouldAnimate);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        return false;
    }

    [QuickPrefix(typeof(PhantomRole), nameof(PhantomRole.UseAbility))]
    public static bool UseAbility(PhantomRole __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.Player.IsAlive() || !__instance.Player.AmOwner || !__instance.Player.moveable || Minigame.Instance || __instance.IsCoolingDown || __instance.fading) return false;
        Func<RoleEffectAnimation, bool> roleEffectAnimation = x => x.effectType == RoleEffectAnimation.EffectType.Vanish_Charge;
        if (__instance.Player.currentRoleAnimations.Find(roleEffectAnimation) || __instance.Player.walkingToVent || __instance.Player.inMovingPlat) return false;
        if (__instance.isInvisible)
        {
            __instance.MakePlayerVisible(true, true);
            return false;
        }
        DestroyableSingleton<HudManager>.Instance.AbilityButton.SetSecondImage(__instance.Ability);
        DestroyableSingleton<HudManager>.Instance.AbilityButton.OverrideText(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PhantomAbilityUndo,
            new Il2CppReferenceArray<Il2CppSystem.Object>(0)));
        __instance.Player.CmdCheckVanish(AUSettings.PhantomDuration());
        return false;
    }

    [QuickPostfix(typeof(PlayerControl), nameof(PlayerControl.SetRoleInvisibility))]
    public static void OnChangeVisibility(PlayerControl __instance, bool isActive, bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (_ignoreSetInvisibilityPatch > 0)
        {
            _ignoreSetInvisibilityPatch--;
            log.Debug($"Ignoring `PlayerControl.SetRoleInvisibility` call.");
            return;
        }
        PlayerControl phantom = __instance;

        log.Debug($"{phantom.name} {(isActive ? "is going invisible as Phantom." : "is appearing as Phantom.")}");
        Hooks.PlayerHooks.PlayerPhantomHook.Propagate(new PlayerPhantomHookEvent(__instance, isActive));

        List<byte> alliedPlayerIds = Players.GetPlayers()
            .Where(p => phantom.Relationship(p) is Relation.FullAllies)
            .Where(phantom.PrimaryRole().Faction.CanSeeRole)
            .Select(p => p.PlayerId)
            .ToList();
        bool isImpFaction = phantom.PrimaryRole().Faction.GetType() == typeof(ImpostorFaction);

        var writer = RpcV3.Mass(SendOption.Reliable);
        if (isActive)
        {
            InvisiblePlayers.Add(phantom.PlayerId);
            writer
                .Start(phantom.NetId, RpcCalls.SetRole)
                .Write((ushort)RoleTypes.Phantom)
                .Write(ProjectLotus.AdvancedRoleAssignment)
                .End()
                .Start(phantom.NetId, RpcCalls.CheckVanish)
                .Write(0)
                .End();
        }
        else
        {
            InvisiblePlayers.Remove(phantom.PlayerId);
            if (phantom.inVent)
            {
                int ventId = phantom.GetLastEnteredVent();
                phantom.MyPhysics.BootFromVent(ventId);
                writer.Start(phantom.MyPhysics.NetId, 34)
                    .WritePacked(ventId)
                    .End();
            }

            writer
                .Start(phantom.NetId, RpcCalls.SetRole)
                .Write((ushort)RoleTypes.Phantom)
                .Write(ProjectLotus.AdvancedRoleAssignment)
                .End();
            writer
                .Start(phantom.NetId, RpcCalls.CheckAppear)
                .Write(shouldAnimate)
                .End();
        }

        bool hasRanFinish = false;

        foreach (PlayerControl otherPlayer in Players.GetAllPlayers())
        {
            if (phantom.PlayerId == otherPlayer.PlayerId) continue; // skip player going invis
            CustomRole role = otherPlayer.PrimaryRole();
            if (role.RealRole.IsCrewmate() && isImpFaction) continue; // they are crewmate, and we are imp so we are phantom for them
            if (alliedPlayerIds.Contains(otherPlayer.PlayerId)) continue; // if we are allied than continue

            if (isActive) // is vanishing
            {
                log.Info($"Desync-vanishing {phantom.name} for {otherPlayer.GetNameWithRole()}.");
                if (otherPlayer.AmOwner) // host check
                {
                    _ignoreSetInvisibilityPatch += 1;
                    phantom.SetRole(RoleTypes.Phantom, ProjectLotus.AdvancedRoleAssignment);
                    phantom.SetRoleInvisibility(true, true, true);
                }
                else writer.Send(otherPlayer.OwnerId);

                if (hasRanFinish) continue;
                Async.Schedule(() => FinishVanishDesync(phantom, isImpFaction, alliedPlayerIds), 1.2f);
            }
            else // is returning
            {
                log.Info($"Desync-appearing {phantom.name} for {otherPlayer.GetNameWithRole()}.");
                if (otherPlayer.AmOwner) // host check
                {
                    _ignoreSetInvisibilityPatch += 1;
                    phantom.StartCoroutine(phantom.CoSetRole(RoleTypes.Phantom, ProjectLotus.AdvancedRoleAssignment));
                    phantom.SetRoleInvisibility(false, true, true);
                }
                else writer.Send(otherPlayer.OwnerId);

                if (hasRanFinish) continue;
                // Async.Schedule(() => StartAppearDesync(phantom, isImpFaction, alliedPlayerIds), .01f);
                Async.Schedule(() => FinishAppearDesync(phantom, isImpFaction, alliedPlayerIds), 1.8f);
            }
            hasRanFinish = true;
        }
    }

    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.CheckVanish))]
    public static bool InterceptVanish(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        PlayerControl phantom = __instance;
        log.Info($"CheckVanish - {phantom.GetNameWithRole()}");

        ActionHandle handle = ActionHandle.NoInit();
        RoleOperations.Current.Trigger(LotusActionType.Vanish, phantom, handle);
        if (handle.IsCanceled)
        {
            if (phantom.AmOwner)
            {
                DestroyableSingleton<HudManager>.Instance.AbilityButton.SetFromSettings(phantom.Data.Role.Ability);
                phantom.SetKillTimer(GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown));
                phantom.Data.Role.SetCooldown();
                phantom.PrimaryRole().UIManager.ForceUpdate();
                return false;
            }

            RpcV3.Immediate(phantom.NetId, RpcCalls.SetRole).Write((ushort)RoleTypes.Phantom).Write(true).Send(phantom.GetClientId());
            Async.Schedule(phantom.RpcResetAbilityCooldown, NetUtils.DeriveDelay(0.2f));
            // Async.Schedule(() => phantom.SetKillCooldown(phantom.PrimaryRole().GetOverride(Override.KillCooldown)?.GetValue() as float? ?? AUSettings.KillCooldown()), NetUtils.DeriveDelay(0.2f));

            return false;
        }

        return true;
    }

    [QuickPrefix(typeof(PlayerControl), nameof(PlayerControl.CheckAppear))]
    public static void InterceptAppear(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        PlayerControl phantom = __instance;
        log.Info($"CheckAppear - {phantom.GetNameWithRole()}");
    }

    private static void FinishVanishDesync(PlayerControl phantom, bool isImpFaction, List<byte> alliedPlayerIds)
    {
        phantom.Data.DefaultOutfit.PetSequenceId += 10;
        var sender = RpcV3.Mass(SendOption.Reliable);

        string petId = phantom.Data.DefaultOutfit.PetId;
        if (petId != "")
        {
            PetsList[phantom.PlayerId] = petId;
            sender.Start(phantom.NetId, RpcCalls.SetPetStr)
                .Write("")
                .Write(phantom.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .End();
        }

        sender
            .Start(phantom.NetId, RpcCalls.Exiled)
            .End();


        foreach (PlayerControl otherPlayer in Players.GetAllPlayers())
        {
            if (Game.State is GameState.InMeeting || phantom.PlayerId == otherPlayer.PlayerId) continue;
            CustomRole role = otherPlayer.PrimaryRole();
            if (role.RealRole.IsCrewmate() && isImpFaction) continue;
            if (alliedPlayerIds.Contains(otherPlayer.PlayerId)) continue;

            if (otherPlayer.AmOwner)
            {
                phantom.SetPet("");

                // Would cause client to be half-alive (they would be a ghost and can't walk through walls. so we're just not even going to deal with that.
                // phantom.Exiled();
                phantom.invisibilityAlpha = 0;
                phantom.shouldAppearInvisible = true;
                phantom.Visible = false;
                phantom.cosmetics.SetPhantomRoleAlpha(0);
            } else sender.Send(otherPlayer.OwnerId);
        }
    }

    private static void StartAppearDesync(PlayerControl phantom, bool shouldAnimate)
    {
        var writer = RpcV3.Mass()
            .Start(phantom.NetId, RpcCalls.CheckAppear)
            .Write(shouldAnimate)
            .End();
        phantom.CheckAppear(shouldAnimate);
        writer.Send();
    }

    private static void FinishAppearDesync(PlayerControl phantom, bool isImpFaction, List<byte> alliedPlayerIds)
    {
        bool changePet = PetsList.TryGetValue(phantom.PlayerId, out var petId);

        var writer = RpcV3.Mass()
            .Start(phantom.NetId, RpcCalls.SetRole)
            .Write((ushort)RoleTypes.Crewmate) // set crewmate as we are a desynced & non-allied impostor
            .Write(ProjectLotus.AdvancedRoleAssignment)
            .End();

        if (changePet)
            writer
                .Start(phantom.NetId, RpcCalls.SetPetStr)
                .Write(petId ?? "")
                .Write(phantom.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .End();

        foreach (PlayerControl otherPlayer in Players.GetAllPlayers())
        {
            if (Game.State is GameState.InMeeting || phantom == null || otherPlayer.PlayerId == phantom.PlayerId) continue;
            CustomRole role = otherPlayer.PrimaryRole();
            if (role.RealRole.IsCrewmate() && isImpFaction || alliedPlayerIds.Contains(otherPlayer.PlayerId)) continue;


            if (otherPlayer.AmOwner)
            {
                phantom.SetRole(RoleTypes.Crewmate, ProjectLotus.AdvancedRoleAssignment);
                if (changePet) phantom.SetPet(petId);
            }
            else writer.Send(otherPlayer.OwnerId);
        }
    }

    private static void OnMeetingCalled(GameStateHookEvent _)
    {
        if (InvisiblePlayers.Count == 0) return;
        List<PlayerControl> invisiblePhantoms = InvisiblePlayers
            .Select(Utils.PlayerById)
            .Where(op => op.Exists())
            .Select(op => op.Get())
            .ToList();
        if (invisiblePhantoms.Count == 0)
        {
            InvisiblePlayers.Clear();
            return;
        }

        foreach (PlayerControl phantom in invisiblePhantoms)
        {
            if (!phantom.IsAlive())
            {
                InvisiblePlayers.Remove(phantom.PlayerId);
                PetsList.Remove(phantom.PlayerId);
                continue;
            }

            List<byte> alliedPlayerIds = Players.GetPlayers()
                .Where(p => phantom.Relationship(p) is Relation.FullAllies)
                .Where(phantom.PrimaryRole().Faction.CanSeeRole)
                .Select(p => p.PlayerId)
                .ToList();
            bool isImpFaction = phantom.PrimaryRole().Faction.GetType() == typeof(ImpostorFaction);
            log.Debug($"Force making {phantom.name} visible for meeting.");

            foreach (PlayerControl otherPlayer in Players.GetAllPlayers())
            {
                if (otherPlayer.PlayerId == phantom.PlayerId) continue;
                CustomRole role = otherPlayer.PrimaryRole();
                if (role.RealRole.IsCrewmate() && isImpFaction || alliedPlayerIds.Contains(otherPlayer.PlayerId)) continue;
                log.Debug($"making {phantom.name} visible for {otherPlayer.name}.");
                Async.Execute(CoRevertInvisible(phantom, otherPlayer));
            }
            InvisiblePlayers.Remove(phantom.PlayerId);
        }
    }

    private static IEnumerator CoRevertInvisible(PlayerControl phantom, PlayerControl target)
    {
        phantom.RpcSetRoleDesync(RoleTypes.Crewmate, target);
        yield return new WaitForSeconds(NetUtils.DeriveDelay(1f));
        phantom.RpcSetRoleDesync(RoleTypes.Phantom, target);
        yield return new WaitForSeconds(NetUtils.DeriveDelay(1f));
        if (target.AmOwner)
        {
            _ignoreSetInvisibilityPatch += 1;
            phantom.SetRoleInvisibility(false, false, true);
        } else RpcV3.Immediate(phantom.NetId, RpcCalls.StartAppear).Write(false).Send(target.GetClientId());
        yield return new WaitForSeconds(NetUtils.DeriveDelay(1f));
        phantom.RpcSetRoleDesync(RoleTypes.Crewmate, target);
        if (PetsList.TryGetValue(phantom.PlayerId, out var petId))
        {
            if (target.AmOwner) phantom.SetPet(petId);
            else RpcV3.Immediate(phantom.NetId, RpcCalls.SetPetStr).Write(petId).Write(phantom.GetNextRpcSequenceId(RpcCalls.SetPetStr)).Send(target.GetClientId());
        }
    }
}