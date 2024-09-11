using System.Collections;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Extensions;
using Lotus.GUI.Name.Interfaces;
using Lotus.Roles;
using Lotus.Roles.Interfaces;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Operations;
using Lotus.RPC;
using Lotus.Server;
using Lotus.Options;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Debug.Profiling;
using VentLib.Utilities.Extensions;
using static VentLib.Utilities.Debug.Profiling.Profilers;
using VentLib.Networking.RPC;
using Lotus.GameModes.Standard;

namespace Lotus.Patches.Intro;


[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
class IntroDestroyPatch
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(IntroDestroyPatch));
    public static void Postfix(IntroCutscene __instance)
    {
        Profiler.Sample destroySample = Global.Sampler.Sampled();
        Game.State = GameState.Roaming;
        if (!AmongUsClient.Instance.AmHost) return;

        string pet = GeneralOptions.MiscellaneousOptions.AssignedPet;
        while (pet == "Random") pet = ModConstants.Pets.Values.ToList().GetRandom();
        log.Trace("Intro Scene Ending", "IntroCutscene");

        Profiler.Sample fullSample = Global.Sampler.Sampled("Setup ALL Players");
        Players.GetPlayers().ForEach(p =>
        {
            Profiler.Sample executeSample = Global.Sampler.Sampled("Execution Pregame Setup");
            Async.Execute(PreGameSetup(p, pet));
            if (ProjectLotus.AdvancedRoleAssignment)
                Async.Schedule(() =>
                {
                    p.PrimaryRole().Assign();
                    p.RpcResetAbilityCooldown();
                }, 0.175f);
            executeSample.Stop();
        });
        Async.Schedule(() => Players.GetPlayers().ForEach(p => Async.Execute(ReverseEngineeredRPC.UnshfitButtonTrigger(p))), NetUtils.DeriveDelay(2f));
        fullSample.Stop();

        Profiler.Sample propSample = Global.Sampler.Sampled("Propagation Sample");
        RoleOperations.Current.TriggerForAll(LotusActionType.RoundStart, null, true);
        propSample.Stop();

        Hooks.GameStateHooks.RoundStartHook.Propagate(new GameStateHookEvent(Game.MatchData, ProjectLotus.GameModeManager.CurrentGameMode));
        destroySample.Stop();
    }

    public static IEnumerator PreGameSetup(PlayerControl player, string pet)
    {
        if (player == null) yield break;

        Game.MatchData.RegenerateFrozenPlayers(player);

        if (player.GetVanillaRole().IsImpostor() && Game.CurrentGameMode is StandardGameMode)
        {
            float cooldown = GeneralOptions.GameplayOptions.GetFirstKillCooldown(player);
            log.Trace($"Fixing First Kill Cooldown for {player.name} (Cooldown={cooldown}s)", "Fix First Kill Cooldown");
            player.SetKillCooldown(cooldown);
        }

        if (GeneralOptions.MayhemOptions.RandomSpawn) Game.RandomSpawn.Spawn(player);

        // if (!ProjectLotus.AdvancedRoleAssignment) player.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3);
        yield return new WaitForSeconds(0.15f);
        if (player == null) yield break;

        NetworkedPlayerInfo playerData = player.Data;
        if (playerData == null) yield break;

        CustomRole role = player.PrimaryRole();
        if (role is not ITaskHolderRole taskHolder || !taskHolder.TasksApplyToTotal())
        {
            log.Trace($"Clearing Tasks For: {player.name}", "SyncTasks");
            playerData.Tasks?.Clear();
        }

        bool hasPet = !(player.cosmetics?.CurrentPet?.Data?.ProductId == "pet_EmptyPet");
        if (hasPet) log.Trace($"Player: {player.name} has pet: {player.cosmetics?.CurrentPet?.Data?.ProductId}. Skipping assigning pet: {pet}.", "PetAssignment");
        else if (player.AmOwner) player.SetPet(pet);
        else playerData.DefaultOutfit.PetId = pet;
        playerData.PlayerName = player.name;

        Players.SendPlayerData(playerData, autoSetName: false);
        yield return new WaitForSeconds(NetUtils.DeriveDelay(0.05f));
        if (player == null) yield break;

        if (!hasPet) player.CRpcShapeshift(player, false);

        INameModel nameModel = player.NameModel();
        if (SelectRolesPatch.desyncedIntroText.TryGetValue(player.PlayerId, out VentLib.Utilities.Collections.Remote<GUI.Name.Components.TextComponent>? value) && value != null)
        {
            value.Delete();
        }
        Players.GetPlayers().ForEach(p => nameModel.RenderFor(p, GameState.Roaming, force: true));
        player.SyncAll();
    }
}