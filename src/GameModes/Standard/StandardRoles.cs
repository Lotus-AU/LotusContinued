using System;
using System.Collections.Generic;
using System.Linq;
using Lotus.Roles;
using System.Reflection;
using Lotus.Roles.Builtins;
using Lotus.Roles.RoleGroups.Crew;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.Roles.RoleGroups.Madmates.Roles;
using Lotus.Roles.RoleGroups.Neutral;
using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.Roles.RoleGroups.Undead.Roles;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Roles.Subroles;
using Lotus.Roles.Subroles.Romantics;
using VentLib.Utilities.Extensions;
using Medium = Lotus.Roles.RoleGroups.Crew.Medium;
using Pirate = Lotus.Roles.RoleGroups.Neutral.Pirate;
using Lotus.src.Roles.Subroles;

namespace Lotus.GameModes.Standard;
public class StandardRoles : RoleHolder
{
    public override List<Action> FinishedCallbacks() => Callbacks;

    public new List<CustomRole> MainRoles { get; set; }

    public new List<CustomRole> ModifierRoles { get; set; }

    public new List<CustomRole> SpecialRoles { get; set; }

    public new List<CustomRole> AllRoles { get; set; }

    private static List<CustomRole> AddonRoles = new();

    public static List<Action> Callbacks { get; set; } = new List<Action>();

    public StaticRoles Static;
    public Modifiers Mods;
    public ExtraRoles Special;

    private static StandardRoleManager roleManager = null!;
    public static StandardRoles Instance = null!;

    public StandardRoles(Roles.Managers.RoleManager manager) : base(manager)
    {
        Instance = this;
        roleManager = (manager as StandardRoleManager)!;
        AllRoles = new List<CustomRole>();
        Static = new StaticRoles();
        Mods = new Modifiers();
        Special = new ExtraRoles();

        MainRoles = Static.GetType()
            .GetFields()
            .Select(f => (CustomRole)f.GetValue(Static)!)
            .ToList();

        ModifierRoles = Mods.GetType()
            .GetFields()
            .Select(f => (CustomRole)f.GetValue(Mods)!)
            .ToList();

        SpecialRoles = Special.GetType()
            .GetFields()
            .Select(f => (CustomRole)f.GetValue(Special)!)
            .ToList();

        List<CustomRole> realAllRoleList = MainRoles;
        realAllRoleList.AddRange(ModifierRoles);
        realAllRoleList.AddRange(SpecialRoles);
        realAllRoleList.AddRange(AllRoles);
        realAllRoleList.AddRange(AddonRoles);
        AllRoles = realAllRoleList;
        AllRoles.ForEach(r => r.Solidify());
    }

    public static void AddRole(CustomRole role) => AddonRoles.Add(role);

    internal void LinkEditor(Type editorType)
    {
        if (!editorType.IsAssignableTo(typeof(RoleEditor)))
            throw new ArgumentException("Editor Type MUST be a subclass of AbstractBaseRole.RoleEditor");
        Type roleType = editorType.BaseType!.DeclaringType!;
        bool isStatic = typeof(StaticRoles).GetFields().Any(f => f.FieldType == roleType);
        bool isExtra = typeof(ExtraRoles).GetFields().Any(f => f.FieldType == roleType);

        CustomRole role = roleManager.GetRoleFromType(roleType);
        ConstructorInfo editorCtor = editorType.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { roleType })!;
        RoleEditor editor = (RoleEditor)editorCtor.Invoke(new object?[] { role });
        CustomRole modified = (CustomRole)editor.StartLink();

        if (isStatic)
        {
            typeof(StaticRoles).GetField(roleType.Name)?.SetValue(Static, modified);
            MainRoles.Replace(role, modified);
        }

        if (isExtra)
        {
            typeof(ExtraRoles).GetField(roleType.Name)?.SetValue(Special, modified);
            SpecialRoles.Replace(role, modified);
        }

        AllRoles.Replace(role, modified);
    }

    internal void RemoveEditor(Type editorType)
    {
        if (!editorType.IsAssignableTo(typeof(RoleEditor)))
            throw new ArgumentException("Editor Type MUST be a subclass of AbstractBaseRole.RoleEditor");
        Type roleType = editorType.BaseType!.DeclaringType!;
        bool isStatic = typeof(StaticRoles).GetFields().Any(f => f.FieldType == roleType);
        bool isExtra = typeof(ExtraRoles).GetFields().Any(f => f.FieldType == roleType);

        CustomRole role = roleManager.GetRoleFromType(roleType);
        RoleEditor editor = role.Editor!;

        if (isStatic)
        {
            typeof(StaticRoles).GetField(roleType.Name)?.SetValue(Static, editor.FrozenRole);
            MainRoles.Replace(role, (CustomRole)editor.FrozenRole);
        }

        if (isExtra)
        {
            typeof(ExtraRoles).GetField(roleType.Name)?.SetValue(Special, editor.FrozenRole);
            SpecialRoles.Replace(role, (CustomRole)editor.FrozenRole);
        }

        AllRoles.Replace(role, (CustomRole)editor.FrozenRole);
    }

    public class StaticRoles
    {
        /// Impostors

        //assassin
        //bomber
        public Assassin Assassin = new();
        public Blackmailer Blackmailer = new();
        public BountyHunter BountyHunter = new();
        public Camouflager Camouflager = new();
        public Consort Consort = new();
        public Creeper Creeper = new();
        public Disperser Disperser = new();
        public Escapist Escapist = new();
        public FireWorks FireWorks = new();
        public Freezer Freezer = new();
        public Grenadier Grenadier = new();
        public IdentityThief IdentityThief = new();
        public Impostor Impostor = new();
        public Janitor Janitor = new();
        public Mafioso Mafioso = new();
        public Mare Mare = new();
        public Mastermind Mastermind = new();
        public Miner Miner = new();
        public Ninja Ninja = new();
        public Phantom Phantom = new();
        public PickPocket PickPocket = new();
        public Puppeteer Puppeteer = new();
        //sidekick madmate
        //silencer
        public SerialKiller SerialKiller = new();
        public Shapeshifter Shapeshifter = new();
        public Sniper Sniper = new();
        public Swooper Swooper = new();
        public TimeThief TimeThief = new();
        //traitor
        public Vampire Vampire = new();
        public Warlock Warlock = new();
        public Witch Witch = new();
        public YinYanger YinYanger = new();

        /// Madmates

        public CrewPostor CrewPostor = new();
        public Madmate Madmate = new();
        public MadGuardian MadGuardian = new();
        public MadSnitch MadSnitch = new();
        public Parasite Parasite = new();

        /// Crewmates

        public Alchemist Alchemist = new();
        public Altruist Altruist = new();
        public Bastion Bastion = new();
        public Bodyguard Bodyguard = new();
        public Chameleon Chameleon = new();
        public Charmer Charmer = new();
        public Crewmate Crewmate = new();
        public Crusader Crusader = new();
        public Demolitionist Demolitionist = new();
        public Dictator Dictator = new();
        public Doctor Doctor = new();
        public Duplicator Duplicator = new();
        public Engineer Engineer = new();
        public Escort Escort = new();
        public ExConvict ExConvict = new();
        public Herbalist Herbalist = new();
        public Investigator Investigator = new();
        public Mayor Mayor = new();
        public Medic Medic = new();
        public Medium Medium = new();
        public Mystic Mystic = new();
        public Noisemaker Noisemaker = new();
        public Observer Observer = new();
        public Oracle Oracle = new();
        public Psychic Psychic = new();
        public Repairman Repairman = new();
        public Scientist Scientist = new();
        public Sheriff Sheriff = new();
        public Snitch Snitch = new();
        public Speedrunner Speedrunner = new();
        public Swapper Swapper = new();
        public Roles.RoleGroups.Crew.Tracker Tracker = new();
        public Transporter Transporter = new();
        public Trapster Trapster = new();
        public Veteran Veteran = new();
        public Vigilante Vigilante = new();

        /// Neutral Killing

        public AgiTater AgiTater = new();
        public Arsonist Arsonist = new();
        public BloodKnight BloodKnight = new();
        public Demon Demon = new();
        public Egoist Egoist = new();
        public Hitman Hitman = new();
        public Jackal Jackal = new();
        public Juggernaut Juggernaut = new();
        public Marksman Marksman = new();
        public Necromancer Necromancer = new();
        public Occultist Occultist = new();
        public Pelican Pelican = new();
        public PlagueBearer PlagueBearer = new();
        public Retributionist Retributionist = new();
        public Glitch Glitch = new();
        public Werewolf Werewolf = new();

        /// Neutral Non-Killing
        public Amalgamation Amalgamation = new();
        public Amnesiac Amnesiac = new();
        public Archangel Archangel = new();
        public Copycat Copycat = new();
        public Executioner Executioner = new();
        public Hacker Hacker = new();
        public Jester Jester = new();
        public Opportunist Opportunist = new();
        public Pirate Pirate = new();
        public Postman Postman = new();
        public SchrodingersCat SchrodingersCat = new();
        public Survivor Survivor = new();
        public Taskrunner Taskrunner = new();
        public Terrorist Terrorist = new();
        public Vulture Vulture = new();

        /// extra stuff
        public Guesser Guesser = new();
    }

    public class Modifiers
    {
        public Bait Bait = new();
        public Bewilder Bewilder = new();
        public Bloodlust Bloodlust = new();
        public Deadly Deadly = new();
        public Diseased Diseased = new();
        public Flash Flash = new();
        public Honed Honed = new();
        public Nimble Nimble = new();
        public LastResort LastResort = new();
        public Oblivious Oblivious = new();
        public Romantic Romantic = new();
        public Sleuth Sleuth = new();
        public TieBreaker TieBreaker = new();
        public Torch Torch = new();
        public Unstoppable Unstoppable = new();
        public Watcher Watcher = new();
        public Workhorse Workhorse = new();
    }

    public class ExtraRoles
    {
        public IllegalRole IllegalRole = new();
        public GameMaster GM = new();
        // public Debugger Debugger = new Debugger();

        // public CrewGuesser CrewGuesser = new CrewGuesser();
        // public ImpGuesser ImpGuesser = new ImpGuesser();
        // public NeutralKillerGuesser NeutralKillerGuesser = new NeutralKillerGuesser();
        // public NeutralGuesser NeutralGuesser = new NeutralGuesser();
    }
}

