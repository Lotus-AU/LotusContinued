using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lotus.Factions.Interfaces;
using Lotus.Roles;
using Lotus.Extensions;
using Lotus.GameModes;
using Lotus.GameModes.Standard;
using VentLib.Utilities.Extensions;
using Version = VentLib.Version.Version;
using Lotus.Factions;

namespace Lotus.Addons;

public abstract class LotusAddon
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(LotusAddon));

    internal Dictionary<AbstractBaseRole, HashSet<IGameMode>> ExportedDefinitions { get; } = new();

    internal readonly List<IFaction> Factions = new();
    internal readonly List<IGameMode> GameModes = new();

    internal readonly Assembly BundledAssembly = Assembly.GetCallingAssembly();
    internal readonly ulong UUID;

    public abstract string Name { get; }
    public abstract Version Version { get; }

    public LotusAddon()
    {
        UUID = (ulong)HashCode.Combine(BundledAssembly.GetIdentity(false)?.SemiConsistentHash() ?? 0ul, Name.SemiConsistentHash());
    }

    /// <summary>
    /// Returns the name of this addon.
    /// </summary>
    /// <param name="fullName">Whether to return te name with the Assembly and version.</param>
    /// <returns>Name of Addon (string)</returns>
    internal string GetName(bool fullName = false) => !fullName
        ? Name
        : $"{BundledAssembly.FullName}::{Name}-{Version.ToSimpleName()}";

    /// <summary>
    /// Returns the display name of this addon. <br/>
    /// This is only shown in the options right now. Defaults to the Name parameter.
    /// </summary>
    /// <returns></returns>
    public virtual string GetDisplayName() => Name;

    /// <summary>
    /// First function called after loading the plugin. Your plugin code should go here.
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    /// This runs after <b>all</b> plugins were loaded. You can disable stuff if your addon conflicts with another one.
    /// </summary>
    /// <param name="addons">List of all Addons that were loaded.</param>
    public virtual void PostInitialize(List<LotusAddon> addons)
    {
    }

    /// <summary>
    /// Export your custom roles.
    /// </summary>
    /// <param name="roleDefinitions">List of roles to export.</param>
    /// <param name="baseGameModes">The gamemodes to export these roles in. Make sure they have been registered first.</param>
    public void ExportCustomRoles(IEnumerable<CustomRole> roleDefinitions, params Type[] baseGameModes)
    {
        if (baseGameModes.Length == 0) ExportCustomRoles(roleDefinitions, StandardGameMode.Instance);
        else ExportCustomRoles(roleDefinitions, baseGameModes.Select(gm => ProjectLotus.GameModeManager.GetGameMode(gm) ?? StandardGameMode.Instance).ToArray());
    }

    /// <summary>
    /// Export your custom roles.
    /// </summary>
    /// <param name="roleDefinitions">List of roles to export.</param>
    /// <param name="baseGameModes">The gamemodes to export these roles in. Make sure they have been registered first.</param>
    public void ExportCustomRoles(IEnumerable<CustomRole> roleDefinitions, params IGameMode[] baseGameModes)
    {
        IGameMode[] targetGameModes = ProjectLotus.GameModeManager.GameModes.Where(gm => baseGameModes.Any(bgm => bgm.GetType() == gm.GetType())).ToArray();
        roleDefinitions.ForEach(r =>
        {
            r.Addon = this;
            HashSet<IGameMode> iGameMode = ExportedDefinitions.GetOrCompute(r, () => new HashSet<IGameMode>());
            targetGameModes.ForEach(x =>
            {
                iGameMode.Add(x);
                x.RoleManager.RoleHolder.AddAddonRole(r);
            });
            log.Trace($"Exporting Role ({r.EnglishRoleName}) for {Name}");
        });
    }

    /// <summary>
    /// Export your custom gamemodes.
    /// </summary>
    /// <param name="gamemodes">List of gamemodes to export.</param>
    public void ExportGameModes(IEnumerable<IGameMode> gamemodes)
    {
        foreach (IGameMode gamemode in gamemodes)
        {
            log.Trace($"Exporting GameMode ({gamemode.Name}) for {Name}");
            // ProjectLotus.GameModeManager.AddGamemodeSettingToOptions(gamemode.MainTab().GetOptions());
            gamemode.RoleManager.RoleHolder.AllRoles.ForEach(r => r.Addon = this);
            GameModes.Add(gamemode);
            ProjectLotus.GameModeManager.GameModes.Add(gamemode);
        }
    }

    /// <summary>
    /// Export your custom gamemodes.
    /// </summary>
    /// <param name="gamemodes">All gamemodes passed through the function.</param>
    public void ExportGameModes(params IGameMode[] gamemodes) => ExportGameModes((IEnumerable<IGameMode>)gamemodes);

    /// <summary>
    /// Export your custom factions.
    /// </summary>
    /// <param name="factions">List of factions to export.</param>
    public void ExportFactions(IEnumerable<IFaction> factions)
    {
        // more will be done soon for factions, as we need to replicate them over as well. but this is all for now.
        factions.ForEach(f =>
        {
            log.Trace($"Exporting Faction ({f.Name()}) for {Name}");
            FactionInstances.AddonFactions[f.GetType()] = f;
            Factions.Add(f);
        });
    }

    /// <summary>
    /// Export your custom factions.
    /// </summary>
    /// <param name="factions">List of factions to export.</param>
    public void ExportFactions(params IFaction[] factions) => ExportFactions((IEnumerable<IFaction>)factions);

    public override string ToString() => GetName(true);

    public static bool operator ==(LotusAddon? addon1, LotusAddon? addon2) => addon1?.Equals(addon2) ?? addon2 is null;
    public static bool operator !=(LotusAddon? addon1, LotusAddon? addon2) => !addon1?.Equals(addon2) ?? addon2 is not null;

    public override bool Equals(object? obj)
    {
        if (this is null) return obj is null;
        if (obj is null) return this is null;
        if (obj is not LotusAddon addon) return false;
        return addon.UUID == UUID;
    }
}

