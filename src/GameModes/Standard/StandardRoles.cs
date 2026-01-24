extern alias JBAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JBAnnotations::JetBrains.Annotations;
using Lotus.Roles;
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
using Lotus.Logging;

namespace Lotus.GameModes.Standard;
public class StandardRoles : RoleHolder
{
    public override List<Action> FinishedCallbacks() => Callbacks;

    public static List<Action> Callbacks { get; set; } = new List<Action>();

    public StaticRoles Static;
    public Modifiers Mods;
    public ExtraRoles Special;

    public static StandardRoles Instance = null!;

    public StandardRoles()
    {
        Instance = this;
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

        // add all roles
        AllRoles.AddRange(MainRoles);
        AllRoles.AddRange(ModifierRoles);
        AllRoles.AddRange(SpecialRoles);
        AllRoles.AddRange(AddonRoles);

        // avoid `Collection was modified` error
        var newRoles = new List<CustomRole>();
        AllRoles.ForEach(r => newRoles.AddRange(r.LinkedRoles()));
        AllRoles.AddRange(newRoles);

        // solidify every role to finish them off
        AllRoles.ForEach(r =>
        {
            if (!r.RoleFlags.HasFlag(RoleFlag.TransformationRole) &&
                !r.RoleFlags.HasFlag(RoleFlag.VariationRole)) r.Solidify();
        });
    }

    [UsedImplicitly]
    [Obsolete("This function no longer needs to be called by mods. PL will now auto add your roles to the gamemode you export them with.")]
    public static void AddRole(CustomRole role)
    {
        if (role.Addon == null || AddonRoles.Contains(role)) return;
        DevLogger.Log($"adding {role.EnglishRoleName} to Standard.");
        AddonRoles.Add(role);
        Instance.AllRoles.Add(role);
        role.Solidify();
        StandardGameMode.Instance.RoleManager.RegisterRole(role);
    }

    public override void AddAddonRole(CustomRole addonRole)
    {
        if (addonRole.Addon == null || AddonRoles.Contains(addonRole)) return;
        DevLogger.Log($"adding {addonRole.EnglishRoleName} to Standard.");
        AddonRoles.Add(addonRole);
        AllRoles.Add(addonRole);
        addonRole.Solidify();
        StandardGameMode.Instance.RoleManager.RegisterRole(addonRole);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
        public Viper Viper = new();
        public Warlock Warlock = new();
        public Witch Witch = new();
        public YinYanger YinYanger = new();

        /// Madmates

        public CrewPostor CrewPostor = new();
        public MadGuardian MadGuardian = new();
        public MadSnitch MadSnitch = new();
        public Madmate Madmate = new();
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
        public Detective Detective = new();
        public Dictator Dictator = new();
        public Doctor Doctor = new();
        public Duplicator Duplicator = new();
        public Engineer Engineer = new();
        public Escort Escort = new();
        public ExConvict ExConvict = new();
        public Genie Genie = new();
        public Herbalist Herbalist = new();
        public Investigator Investigator = new();
        public Jailor Jailor = new();
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
        public Turncoat Turncoat = new();
        public Vulture Vulture = new();
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Modifiers
    {
        public Bait Bait = new();
        public Bewilder Bewilder = new();
        public Deadly Deadly = new();
        public Diseased Diseased = new();
        public Flash Flash = new();
        public Guesser Guesser = new();
        public LastResort LastResort = new();
        public Nimble Nimble = new();
        public Oblivious Oblivious = new();
        public Rebellious Rebellious = new();
        public Rogue Rogue = new();
        public Romantic Romantic = new();
        public Sleuth Sleuth = new();
        public TieBreaker TieBreaker = new();
        public Torch Torch = new();
        public Unstoppable Unstoppable = new();
        public Watcher Watcher = new();
        public Workhorse Workhorse = new();
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ExtraRoles
    {
        public IllegalRole IllegalRole = new();
        public GameMaster GM = new();
        // public Debugger Debugger = new Debugger();
    }
}

