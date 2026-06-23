using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using Hazel;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Factions;
using Lotus.Factions.Neutrals;
using Lotus.GUI;
using Lotus.GUI.Counters;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Components;
using Lotus.GUI.Name.Holders;
using Lotus.GUI.Name.Interfaces;
using Lotus.Options;
using Lotus.Roles.Overrides;
using Lotus.Extensions;
using Lotus.Factions.Impostors;
using Lotus.Logging;
using Lotus.Options.Roles;
using Lotus.Roles.Interfaces;
using VentLib.Localization.Attributes;
using VentLib.Networking.Interfaces;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using Lotus.API;
using VentLib.Networking;
using Lotus.Managers;
using System.Text.RegularExpressions;
using Lotus.API.Reactive;
using Lotus.API.Reactive.HookEvents;
using Lotus.Managers.History.Events;
using Lotus.Roles.Managers.Interfaces;
using Lotus.Utilities;
using Lotus.Roles.RoleGroups.Crew;
using VentLib;
using VentLib.Networking.RPC.Interfaces;
using VentLib.Utilities.Optionals;

namespace Lotus.Roles;

[Localized("Roles")]
public abstract class CustomRole : AbstractBaseRole, IRpcSendable<CustomRole>
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(CustomRole));

    public string GlobalRoleID => $"${Addon?.UUID ?? 0}~{RoleID}";
    protected HashSet<Type> RelatedRoles = new();

    internal Remote<TextComponent>? desyncedIntroText;

    static CustomRole()
    {
        AbstractConstructors.Register(typeof(CustomRole), r => GlobalRoleManager.Instance.GetRoleFromId(r.ReadInt32()));
    }

    public virtual bool CanVent() => BaseCanVent && !RoleAbilityFlags.HasFlag(RoleAbilityFlag.CannotVent);

    public virtual void HandleDisconnect() { }

    public Relation Relationship(PlayerControl player) => Relationship(player.PrimaryRole());

    public virtual Relation Relationship(CustomRole role)
    {
        if (this.Faction is Neutral && role.Faction is Neutral)
            return Options.RoleOptions.NeutralOptions.NeutralTeamingMode switch
            {
                NeutralTeaming.All => Relation.FullAllies,
                NeutralTeaming.KillersNeutrals => SpecialType == role.SpecialType ? Relation.FullAllies : Relation.None,
                NeutralTeaming.SameRole => RelatedRoles.Contains(role.GetType()) ? Relation.FullAllies : Relation.None,
                _ => Relation.None
            };

        return Faction.Relationship(role);
    }

    private RemoteList<GameOptionOverride> currentOverrides = new();

    /// <summary>
    /// Utilized for "live" instances of the class AKA when the game is actually being played
    /// </summary>
    /// <returns>Shallow clone of this class (except for certain fields such as roleOptions being a deep clone)</returns>
    public CustomRole Instantiate(PlayerControl player)
    {
        CustomRole cloned = Clone();
        cloned.RelatedRoles.Add(this.GetType());
        cloned.MyPlayer = player;

        CreateInstanceBasedVariables();
        cloned.UIManager.SetBaseRole(cloned);

        cloned.Setup(player);
        cloned.SetupUI2(player.NameModel());
        player.NameModel().Render(force: true);

        cloned.PostSetup();
        return cloned;
    }

    public CustomRole Clone()
    {
        CustomRole cloned = (CustomRole)this.MemberwiseClone();
        cloned.RoleSpecificGameOptionOverrides = new();
        cloned.currentOverrides = new();
        cloned.RelatedRoles = new HashSet<Type>(cloned.RelatedRoles);
        cloned.Modify(new RoleModifier(cloned));
        return cloned;
    }

    public bool IsEnabled() => this.Chance > 0 && (this.Count > 0 | this.RoleFlags.HasFlag(RoleFlag.RemoveRoleMaximum));

    /// <summary>
    /// Adds a GameOverride that continuously modifies this instances game options until removed
    /// </summary>
    /// <param name="optionOverride">Override to apply whenever SyncOptions is called</param>
    public Remote<GameOptionOverride> AddOverride(GameOptionOverride optionOverride) => currentOverrides.Add(optionOverride);

    public GameOptionOverride? GetOverride(Override overrideType) => GetRoleOverrides().LastOrDefault(o => o.Option == overrideType);
    /// <summary>
    /// Removes a continuous GameOverride
    /// </summary>
    /// <param name="optionOverride">Override to remove</param>
    protected void RemoveOverride(GameOptionOverride optionOverride) => currentOverrides.Remove(optionOverride);
    /// <summary>
    /// Removes a continuous GameOverride
    /// </summary>
    /// <param name="override">Override type to remove</param>
    protected void RemoveOverride(Override @override) => currentOverrides.RemoveAll(o => o.Option == @override);

    public virtual List<GameOptionOverride> GetRoleOverrides(IEnumerable<GameOptionOverride>? newOverrides = null)
    {
        DevLogger.Log($"(GetRoleOverrides) My player: {(MyPlayer == null ? "MyPlayer is null." : MyPlayer.name)}");
        List<GameOptionOverride> thisList = [];

        thisList.AddRange(MatchData.GetGlobalOverrides()); // Global Overrides have last priority
        thisList.AddRange(RoleSpecificGameOptionOverrides); // Role overrides from Modify have third priority
        thisList.AddRange(currentOverrides);  // Overrides added during game have second priority
        thisList.AddRange(Game.MatchData.Roles.GetOverrides(MyPlayer.PlayerId)); // Player specific overrides have the highest priority

        if (newOverrides != null) thisList.AddRange(newOverrides);

        return thisList;
    }

    // Useful for shorthand delegation
    public void SyncOptions() => SyncOptions(null);

    // ReSharper disable once MethodOverloadWithOptionalParameter
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public void SyncOptions(IEnumerable<GameOptionOverride>? newOverrides = null, bool official = false, bool parameterOverridesAreAbsolute = false)
    {
        if (MyPlayer == null || !AmongUsClient.Instance.AmHost) return;

        IEnumerable<GameOptionOverride>? overrides = newOverrides;
        if (!parameterOverridesAreAbsolute || overrides == null) overrides = GetRoleOverrides(newOverrides);

        IGameOptions modifiedOptions = DesyncOptions.GetModifiedOptions(overrides);
        if (official) RpcV3.Immediate(PlayerControl.LocalPlayer.NetId, RpcCalls.SyncSettings).Write(modifiedOptions).Send(MyPlayer.GetClientId());
        DesyncOptions.SyncToPlayer(modifiedOptions, MyPlayer);
    }

    public Optional<LastPlayerInfo> Assign(bool sendAllPlayers = false)
    {
        if (!AmongUsClient.Instance.AmHost) return Optional<LastPlayerInfo>.Null();
        bool isStartOfGame = Game.State is GameState.InIntro or GameState.InLobby;
        List<PlayerControl> players = Players.GetAllPlayers().ToList();

        PlayerControl[] alliedPlayers = players.Where(p => Relationship(p) is Relation.FullAllies).ToArray();
        HashSet<byte> alliedPlayerIds = alliedPlayers.Where(Faction.CanSeeRole).Select(p => p.PlayerId).ToHashSet();
        log.Debug($"CustomRole.Assign({sendAllPlayers}, {isStartOfGame}) for {MyPlayer.name}. {EnglishRoleName} - {RealRole}");

        var teamInfo = Game.MatchData.VanillaRoleTracker.ResetInfo(MyPlayer.PlayerId);
        PlayerControl myListPlayer = players.First(p => p.PlayerId == MyPlayer.PlayerId);
        players.Remove(myListPlayer);
        teamInfo.MyRole = RealRole;

        var writer = RpcV3.Mass(SendOption.Reliable);
        bool shouldSendRpc = !MyPlayer.AmOwner;

        PlayerControl? lastPlayer = null;
        if (!sendAllPlayers)
        {
            if (!players.Any())
            {
                lastPlayer = MyPlayer;
                shouldSendRpc = false;
            }
            else
            {
                lastPlayer = players.Last();
                players.Remove(lastPlayer);
            }
        }

        RoleTypes sentRole = RealRole;
        if (!MyPlayer.IsAlive() && !isStartOfGame) sentRole = sentRole.GhostEquivalent();

        if (MyPlayer.AmOwner) MyPlayer.SetRole(sentRole, ProjectLotus.AdvancedRoleAssignment);
        else
            writer
                .Start(MyPlayer.NetId, RpcCalls.SetRole)
                .Write((ushort)sentRole)
                .Write(ProjectLotus.AdvancedRoleAssignment)
                .End();

        if (lastPlayer == MyPlayer) return Optional<LastPlayerInfo>.NonNull(new LastPlayerInfo(MyPlayer, MyPlayer, RealRole));

        foreach (PlayerControl player in players)
        {
            RoleTypes targetRoleForMyPlayer = GetRoleTypeForMyPlayer(player);
            // log.Debug($"Assigning {targetRoleForMyPlayer} for {player.name}.");
            if (MyPlayer.AmOwner) player.SetRole(targetRoleForMyPlayer, ProjectLotus.AdvancedRoleAssignment);
            else writer
                    .Start(player.NetId, RpcCalls.SetRole)
                    .Write((ushort)targetRoleForMyPlayer)
                    .Write(ProjectLotus.AdvancedRoleAssignment)
                    .End();
            teamInfo.AddPlayer(player.PlayerId, targetRoleForMyPlayer.IsImpostor());
        }

        if (shouldSendRpc) writer.Send(MyPlayer.GetClientId());
        ShowRoleToTeammates(alliedPlayers);

        return sendAllPlayers
            ? Optional<LastPlayerInfo>.Null()
            : Optional<LastPlayerInfo>.NonNull(new LastPlayerInfo(MyPlayer, lastPlayer!, GetRoleTypeForMyPlayer(lastPlayer!)));

        RoleTypes GetRoleTypeForMyPlayer(PlayerControl target)
        {
            RoleTypes outputRole;
            CustomRole targetRole = target.PrimaryRole();
            if (targetRole.RealRole.IsCrewmate()) outputRole = targetRole.RealRole; // If their role is crewmate, then we can see their real role.
            else if (alliedPlayerIds.Contains(target.PlayerId)) outputRole = targetRole.RealRole; // If they are an allied impostor, then we can see their real role.
            else if (targetRole.Faction.GetType() == typeof(ImpostorFaction) && RealRole.IsCrewmate()) outputRole = targetRole.RealRole; // If they are impostor, and we are crewmate, than we can see their real role.
            else outputRole = RoleTypes.Crewmate; // This must mean they are a desynced impostor and not on our team or we cant see their role. So we should see them as crewmate.

            return target.IsAlive() || isStartOfGame ? outputRole : outputRole.GhostEquivalent();
        }
    }

    public virtual CustomRole ChangeRoleTo(CustomRole newRole, bool getCleanRole = true, bool revertUI = true)
    {
        if (Game.MatchData.Roles.GetMainRole(MyPlayer.PlayerId) != this)
            throw new NotSupportedException("Cannot call 'ChangeRoleTo' when the current role is not the player's main role.");
        log.Debug($"Changing {MyPlayer.name}'s role from {EnglishRoleName} to {newRole.EnglishRoleName}. (a role called this method.)");
        if (getCleanRole) newRole = IRoleManager.Current.GetCleanRole(newRole);

        HashSet<byte> oldAllies = Players.GetAllPlayers()
            .Where(p => Relationship(p) is Relation.FullAllies)
            .Where(Faction.CanSeeRole)
            .Select(p => p.PlayerId)
            .ToHashSet();

        if (revertUI)
        {
            if (MyPlayer.AmOwner) UIManager.RevertEverything();
            else if (MyPlayer.IsModded()) Vents.FindRPC((uint)RoleRPC.RevertRoleUI)!.Send([MyPlayer.OwnerId]);
        }
        Game.CurrentGameMode.Assign(MyPlayer, newRole);
        newRole = MyPlayer.PrimaryRole();
        newRole.Assign(true);
        Game.MatchData.GameHistory.AddEvent(new RoleChangeEvent(MyPlayer, newRole, this));

        if (revertUI)
        {
            if (MyPlayer.AmOwner) newRole.UIManager.ForceUpdate();
            else if (MyPlayer.IsModded()) Vents.FindRPC((uint)RoleRPC.ForceRoleUIUpdate)!.Send([MyPlayer.OwnerId]);
        }

        HashSet<byte> newAllies = Players.GetAllPlayers()
            .Where(p => newRole.Relationship(p) is Relation.FullAllies)
            .Where(newRole.Faction.CanSeeRole)
            .Select(p => p.PlayerId)
            .ToHashSet();

        var teamInfo = Game.MatchData.VanillaRoleTracker.GetInfo(MyPlayer.PlayerId);

        RoleTypes crewRole = MyPlayer.IsAlive() ? RoleTypes.Crewmate : RoleTypes.CrewmateGhost;
        RoleTypes impRole = MyPlayer.IsAlive() ? newRole.RealRole : newRole.RealRole.GhostEquivalent();

        // Now we need to send updated Role RPCs for our old allies, and send new Role RPCs for our new allies.
        oldAllies.ForEach(id =>
        {
            if (newAllies.Contains(id) || id == MyPlayer.PlayerId) return;
            teamInfo.AddVanillaCrewmate(id);
            PlayerControl? player = Utils.GetPlayerById(id);
            if (player == null) return;
            MyPlayer.RpcSetRoleDesync(crewRole, player);
        });
        Players.GetAllRoles().ForEach(role =>
        {
            if (role.Faction.GetType() != typeof(ImpostorFaction)) return;
            PlayerControl player = role.MyPlayer;
            byte id = player.PlayerId;
            if (newAllies.Contains(id)) return;

            if (newRole.RealRole.IsCrewmate())
            {
                if (this.RealRole.IsCrewmate()) return;
                // we turned from an imp role/NK to crew
                // now we must see these players as an Impostor.
                teamInfo.AddVanillaImpostor(id);
                MyPlayer.RpcSetRoleDesync(newRole.RealRole, player);
                player.RpcSetRoleDesync(role.RealRole, MyPlayer);
                return;
            }

            // we are imp.
            // we must now see these players as crew
            teamInfo.AddVanillaCrewmate(id);
            MyPlayer.RpcSetRoleDesync(crewRole, player);

            // set them as crewmate for us so we can interact with them.
            player.RpcSetRoleDesync(RoleTypes.Crewmate, MyPlayer);
        });
        teamInfo.Impostors.ForEach(id =>
        {
            if (newAllies.Contains(id)) return;
            PlayerControl? player = Utils.GetPlayerById(id);
            if (player == null) return;
            if (player.PrimaryRole().Faction.GetType() == typeof(ImpostorFaction) && newRole.RealRole.IsCrewmate())
                // if they are imp, and we are crew
                // its fine that they are impostors for us.
                return;

            // they are imp and our role is impostor.
            // set them to crew and make sure they see us as crew
            teamInfo.AddVanillaCrewmate(MyPlayer.PlayerId);
            if (player == null) return;
            MyPlayer.RpcSetRoleDesync(crewRole, player);

            // set them as crewmate for us so we can interact with them.
            player.RpcSetRoleDesync(RoleTypes.Crewmate, MyPlayer);
        });
        newAllies.ForEach(id =>
        {
            if (id == MyPlayer.PlayerId) return;
            teamInfo.AddVanillaImpostor(id);
            PlayerControl? player = Utils.GetPlayerById(id);
            if (player == null) return;
            MyPlayer.RpcSetRoleDesync(impRole, player);
            player.NameModel().GetComponentHolder<RoleHolder>().ForEach(rc => rc.AddViewer(MyPlayer));
        });
        ShowRoleToTeammates(newAllies.Select(Utils.PlayerById).Where(op => op.Exists()).Select(op => op.Get()));

        Game.MatchData.RegenerateFrozenPlayers(MyPlayer);
        return newRole;
    }

    public virtual void Revive()
    {
        Hooks.PlayerHooks.PlayerRevivedHook.Propagate(new PlayerHookEvent(MyPlayer));
        log.Debug($"Reviving {MyPlayer.GetNameWithRole()}. (a role called this function.)");

        PlayerControl[] alliedPlayers = Players.GetAllPlayers().Where(p => Relationship(p) is Relation.FullAllies).ToArray();
        HashSet<byte> alliedPlayerIds = alliedPlayers.Where(Faction.CanSeeRole).Select(p => p.PlayerId).ToHashSet();

        foreach (PlayerControl player in Players.GetAllPlayers())
        {
            if (player.Data.Disconnected) continue;
            RoleTypes outputRole = GetMyRoleForTarget(player);
            MyPlayer.RpcSetRoleDesync(outputRole, player);
        }

        Game.MatchData.RegenerateFrozenPlayers(MyPlayer);

        return;
        RoleTypes GetMyRoleForTarget(PlayerControl target)
        {
            RoleTypes outputRole;
            if (MyPlayer.PlayerId == target.PlayerId) outputRole = RealRole; // We can ofc see our role.
            else if (alliedPlayerIds.Contains(target.PlayerId)) outputRole = RealRole; // If they are an allied impostor, then they can see our role.
            else if (Faction.GetType() == typeof(ImpostorFaction) && target.PrimaryRole().RealRole.IsCrewmate()) outputRole = RealRole; // If we are impostor, and they are crewmate, than they can see our real role.
            else outputRole = RoleTypes.Crewmate; // This must mean they are a desynced impostor not on our team. So they should see us as crewmate.

            return outputRole;
        }
    }

    public virtual void RefreshKillCooldown(PlayerControl? target = null, IEnumerable<GameOptionOverride>? overrides = null)
    {
        if (MyPlayer == null || !AmongUsClient.Instance.AmHost) return;
        if (target == null) target = MyPlayer;

        SyncOptions(overrides);
        if (MyPlayer.IsHost())
        {
            target.ShowFailedMurder();
            MyPlayer.SetKillTimer(GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown) / 2f);
            return;
        }
        RpcV3.Immediate(MyPlayer.NetId, RpcCalls.MurderPlayer).Write(target).Write((int)MurderResultFlags.FailedProtected).Send(MyPlayer.GetClientId());
    }

    // During intro scene to set team name and role info for non-modded clients and skip the rest.
    // Note: When Neutral is based on the Crewmate role then it is impossible to display the info for it
    // code taken from tohe
    public virtual string GetRoleIntroString()
    {
        if (MyPlayer.IsHost() || MyPlayer.IsModded()) return "";
        //Get role info font size based on the length of the role info
        int GetInfoSize(string RoleInfo)
        {
            RoleInfo = Regex.Replace(RoleInfo, "<[^>]*>", "");
            RoleInfo = Regex.Replace(RoleInfo, "{[^}]*}", "");

            var BaseFontSize = 200;
            int BaseFontSizeMin = 100;

            BaseFontSize -= 3 * RoleInfo.Length;
            if (BaseFontSize < BaseFontSizeMin)
                BaseFontSize = BaseFontSizeMin;
            return BaseFontSize;
        }

        string IconText = "<color=#ffffff>|</color>";
        string Font = "<font=\"VCR SDF\" material=\"VCR Black Outline\">";
        string SelfTeamName = $"<size=450%>{IconText} {Font}{this.Faction.Color.Colorize(this.Faction.Name())}</font> {IconText}</size><size=900%>\n \n</size>\r\n";
        string SelfRoleName = $"<size=185%>{Font}{this.RoleColor.Colorize(this.RoleName)}</font></size>";
        string SelfSubRolesName = Utils.GetSubRolesText(MyPlayer.PlayerId);
        string SeerRealName = MyPlayer.name;
        string SelfName = this.RoleColor.Colorize(SeerRealName);
        string RoleInfo = $"<size=25%>\n</size><size={GetInfoSize(this.Blurb)}%>{Font}{this.RoleColor.Colorize(this.Blurb)}</font></size>";
        string RoleNameUp = "<size=1350%>\n\n\n\n\n</size>";

        string finalString = $"{SelfTeamName}{SelfRoleName}{SelfSubRolesName}\r\n{RoleInfo}{RoleNameUp}";
        return finalString;
    }

    private void ShowRoleToTeammates(IEnumerable<PlayerControl> allies)
    {
        // Currently only impostors can show each other their roles
        RoleHolder roleHolder = MyPlayer.NameModel().GetComponentHolder<RoleHolder>();
        if (roleHolder.Count == 0)
        {
            log.Warn("Error Showing Roles to Allies. Role Component does not exist.", "CustomRole::ShowRoleToTeammates");
            return;
        }
        allies.Where(Faction.CanSeeRole).ForEach(a =>
        {
            log.Trace($"Showing Role {EnglishRoleName} to {a.name}", "ShowRoleToTeammates");
            roleHolder.ForEach(rc => rc.AddViewer(a));
        });
    }

    private void SetupUI2(INameModel nameModel)
    {
        GameState[] gameStates = [GameState.InIntro, GameState.Roaming, GameState.InMeeting];

        if (this is ISubrole subrole && this.RoleFlags.HasFlag(RoleFlag.IsSubrole))
        {
            // if (subrole.Identifier() != null) nameModel.GetComponentHolder<SubroleHolder>().Add(new SubroleComponent(subrole, gameStates, viewers: MyPlayer));
            // else nameModel.GetComponentHolder<RoleHolder>().Add(new RoleComponent(this, gameStates, ViewMode.Additive, MyPlayer));
            nameModel.GetComponentHolder<SubroleHolder>().Add(new SubroleComponent(subrole, gameStates, viewers: MyPlayer));
        }
        else
        {
            nameModel.GetComponentHolder<RoleHolder>()
                .Add(new RoleComponent(this, gameStates, ViewMode.Overriden, MyPlayer));
            desyncedIntroText = nameModel.GetComponentHolder<TextHolder>()
                .Add(new TextComponent(GetRoleIntroString(), [GameState.InIntro], ViewMode.Absolute, MyPlayer));
        }
        SetupUiFields(nameModel);
        SetupUiMethods(nameModel);
    }

    private void SetupUiFields(INameModel nameModel)
    {
        this.GetType().GetFields(AccessFlags.InstanceAccessFlags)
            .Where(f => f.GetCustomAttribute<UIComponent>() != null)
            .Reverse()
            .ForEach(f =>
            {
                UIComponent uiComponent = f.GetCustomAttribute<UIComponent>()!;
                object? value = f.GetValue(this);
                switch (uiComponent.Component)
                {
                    case UI.Name:
                        if (value is not string s) throw new ArgumentException($"Values for \"{nameof(UI.Name)}\" must be string. (Got: {value?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<NameHolder>().Add(new NameComponent(s, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Role:
                        if (value is not CustomRole cr) throw new ArgumentException($"Values for \"{nameof(UI.Role)}\" must be {nameof(CustomRole)}. (Got: {value?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<RoleHolder>().Add(new RoleComponent(cr, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    /*case UI.Subrole:
                        if (value is not Subrole sr) throw new ArgumentException($"Values for \"{nameof(UI.Subrole)}\" must be {nameof(Subrole)}. (Got: {value?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<SubroleHolder>().Add(new SubroleComponent(sr, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;*/
                    case UI.Cooldown:
                        if (value is not Cooldown cd) throw new ArgumentException($"Values for \"{nameof(UI.Cooldown)}\" must be {nameof(Cooldown)}. (Got: {value?.GetType()}) in role: {EnglishRoleName}");
                        log.Fatal($"Loading Cooldown Field: {cd} for {this}");
                        nameModel.GetComponentHolder<CooldownHolder>().Add(new CooldownComponent(cd, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Counter:
                        if (value is not ICounter counter) throw new ArgumentException($"Values for \"{nameof(UI.Counter)}\" must be {nameof(ICounter)}. (Got: {value?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<CounterHolder>().Add(new CounterComponent(counter, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Indicator:
                        if (value is not string ind) throw new ArgumentException($"Values for \"{nameof(UI.Indicator)}\" must be string. (Got: {value?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(ind, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Text:
                        if (value is not string txt) throw new ArgumentException($"Values for \"{nameof(UI.Indicator)}\" must be string. (Got: {value?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<TextHolder>().Add(new TextComponent(txt, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Component: {uiComponent.Component} is not a valid component in role: {EnglishRoleName}");
                }
            });
    }

    private void SetupUiMethods(INameModel nameModel)
    {
        GameState[] gameStates = { GameState.InIntro, GameState.Roaming, GameState.InMeeting };
        this.GetType().GetMethods(AccessFlags.InstanceAccessFlags)
            .Where(m => m.GetCustomAttribute<UIComponent>() != null)
            .Reverse()
            .ForEach(m =>
            {
                UIComponent uiComponent = m.GetCustomAttribute<UIComponent>()!;
                if (m.GetParameters().Length > 0) throw new ConstraintException($"Methods marked by {nameof(UIComponent)} must have no parameters");

                Func<string> supplier = () => m.Invoke(this, null)?.ToString() ?? "N/A";
                switch (uiComponent.Component)
                {
                    case UI.Name:
                        nameModel.GetComponentHolder<NameHolder>().Add(new NameComponent(new LiveString(supplier), uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Role:
                        nameModel.GetComponentHolder<RoleHolder>().Add(new RoleComponent(new LiveString(supplier), uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Indicator:
                        nameModel.GetComponentHolder<IndicatorHolder>().Add(new IndicatorComponent(new LiveString(supplier), uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Text:
                        nameModel.GetComponentHolder<TextHolder>().Add(new TextComponent(new LiveString(supplier), uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Cooldown:
                        object? CooldownSupplier() => m.Invoke(this, null);
                        var obj = CooldownSupplier();
                        if (obj is not Cooldown) throw new ArgumentException($"Values for \"{nameof(UI.Cooldown)}\" must be {nameof(Cooldown)}. (Got: {obj?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<CooldownHolder>().Add(new CooldownComponent(() => (Cooldown)m.Invoke(this, null)!, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Counter:
                        object? CounterSupplier() => m.Invoke(this, null);
                        var counterObj = CounterSupplier();
                        if (counterObj is string)
                        {
                            nameModel.GetComponentHolder<CounterHolder>().Add(new CounterComponent(new LiveString(() => (string)m.Invoke(this, null)!), uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                            break;
                        }
                        if (counterObj is not ICounter) throw new ArgumentException($"Values for \"{nameof(UI.Counter)}\" must be {nameof(ICounter)}. (Got: {counterObj?.GetType()}) in role: {EnglishRoleName}");
                        nameModel.GetComponentHolder<CounterHolder>().Add(new CounterComponent(() => (ICounter)m.Invoke(this, null)!, uiComponent.GameStates, uiComponent.ViewMode, viewers: MyPlayer));
                        break;
                    case UI.Subrole:
                    default:
                        throw new ArgumentOutOfRangeException($"Component: {uiComponent.Component} is not a valid component");
                }
            });
    }

    // will need to work on this an actually implement a serialize and deserialize.
    public CustomRole Read(MessageReader reader)
    {
        // string qualifier = reader.ReadString();
        // DevLogger.Log($"Qualifier: {qualifier}");
        // return null;
        // return ProjectLotus.RoleManager.GetRole(qualifier);
        return GlobalRoleManager.Instance.GetRoleFromId(reader.ReadInt32());
    }

    public void Write(MessageWriter writer)
    {
        /*string qualifier = ProjectLotus.RoleManager.GetIdentifier(this);
        DevLogger.Log($"Qualifier: {qualifier}");
        writer.Write(qualifier);*/
        writer.Write(GlobalRoleManager.Instance.GetRoleId(this));
    }


    public static bool operator ==(CustomRole? a, CustomRole? b)
    {
        if (a is null) return b is null;
        return a.Equals(b);
    }

    public static bool operator !=(CustomRole? a, CustomRole? b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not CustomRole role) return false;
        return role.GetType() == this.GetType();
    }
}

/// <summary>
/// Information on the last player's role for what the seer should see them as.
/// </summary>
public class LastPlayerInfo(PlayerControl seer, PlayerControl target, RoleTypes targetRoleForSeer)
{
    public PlayerControl Seer { get; } = seer;
    public PlayerControl Target { get; } = target;
    public RoleTypes TargetRoleForSeer { get; } = targetRoleForSeer;
}