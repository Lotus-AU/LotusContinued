using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using Lotus.Factions;
using Lotus.Factions.Crew;
using Lotus.Factions.Impostors;
using Lotus.Factions.Interfaces;
using Lotus.Factions.Undead;
using Lotus.GUI;
using Lotus.Options;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Overrides;
using Lotus.API.Stats;
using Lotus.Extensions;
using Lotus.Logging;
using Lotus.Roles.Builtins;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Interfaces;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization;
using VentLib.Options;
using VentLib.Options.UI;
using VentLib.Options.IO;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.API;
using Lotus.Addons;
using Lotus.Roles.Properties;
using Lotus.Roles.Subroles;
using Lotus.Managers;
using Lotus.Roles.RoleGroups.NeutralKilling;
using Lotus.Patches.Intro;
using Object = UnityEngine.Object;
using Lotus.Roles.Outfit;
using Lotus.Options.Patches;
using Lotus.Roles.GUI;
using VentLib.Options.Interfaces;

namespace Lotus.Roles;

// Some people hate using "Base" and "Abstract" in class names but I used both so now I'm a war-criminal :) -Tealeaf
// yes and I am one of them. especially the I thing too -Discussions
public abstract class AbstractBaseRole
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(AbstractBaseRole));

    private static Dictionary<IGameOptionTab, LotusAddon?> LastAddonAdded = new();

    public PlayerControl MyPlayer { get; protected set; } = null!;
    // public string Description => Localizer.Translate($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Description", assembly: DeclaringAssembly);
    public string Blurb => RoleFlags.HasFlag(RoleFlag.DoNotTranslate) ? "" : Localizer.Translate($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Blurb", assembly: DeclaringAssembly);

    public string RoleName
    {
        get
        {
            if (RoleFlags.HasFlag(RoleFlag.DoNotTranslate)) return OverridenRoleName ?? EnglishRoleName;
            string name = Localizer.Translate($"Roles.{EnglishRoleName.RemoveHtmlTags()}.RoleName", assembly: DeclaringAssembly, defaultValue: "N/A");
            return OverridenRoleName ?? (name == "N/A" ? EnglishRoleName : name);
        }
    }
    public IEnumerable<string> Aliases
    {
        get
        {
            if (RoleFlags.HasFlag(RoleFlag.DoNotTranslate)) return Array.Empty<string>();
            string aliases = Localizer.Translate($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Aliases", "", true, DeclaringAssembly, TranslationCreationOption.NothingIfNull);
            return aliases == "" ? Array.Empty<string>() : aliases.Split(",", StringSplitOptions.TrimEntries).Select(a => a.ToLowerInvariant());
        }
    }
    public string Description
    {
        get
        {
            if (RoleFlags.HasFlag(RoleFlag.DoNotTranslate)) return "";
            if (RoleOptions == null) return Localizer.Translate($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Description", assembly: DeclaringAssembly);
            string OptionText(Option opt)
            {
                object value = opt.GetValue();
                if (value is bool booleanValue && !booleanValue) return "";
                return "\n"
                    + Localizer.Translate($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Mutable.{opt.Key().Replace(" ", "")}", "", true, DeclaringAssembly, TranslationCreationOption.NothingIfNull)
                        .Formatted(opt.GetValueText())
                    + opt.Children.GetConditionally(opt.GetValue()).Select(o => OptionText(o))
                        .Fuse("");
            }
            return Localizer.Translate($"Roles.{EnglishRoleName.RemoveHtmlTags()}.Description", assembly: DeclaringAssembly) + "\n" + OptionText(RoleOptions).Trim();
        }
    }
    public string? OverridenRoleName;

    public RoleTypes RealRole => DesyncRole ?? VirtualRole;
    public RoleTypes? DesyncRole;
    public RoleTypes VirtualRole;
    public IFaction Faction { get; set; } = FactionInstances.Neutral;
    public SpecialType SpecialType = SpecialType.None;
    public Color RoleColor = Color.white;
    public ColorGradient? RoleColorGradient = null;
    public Func<AudioClip>? IntroSound = null;

    public int Chance { get; private set; }
    public int Count { get; private set; }
    public int AdditionalChance { get; private set; }
    public bool BaseCanVent = false;
    public int DisplayOrder = 500;

    public RoleAbilityFlag RoleAbilityFlags;
    public RoleFlag RoleFlags;
    public Assembly Assembly => this.GetType().Assembly;
    internal LotusAddon? Addon;

    internal virtual LotusRoleType LotusRoleType { get; set; } = LotusRoleType.Unknown;
    internal GameOption RoleOptions;
    internal readonly Assembly DeclaringAssembly;

    public virtual HashSet<Type> BannedModifiers() => new();
    public virtual RoleUIManager UIManager { get; } = new();
    public virtual List<Statistic> Statistics() => new();
    public RoleMetadata Metadata;

    public string EnglishRoleName { get; internal set; }
    internal Dictionary<LotusActionType, List<RoleAction>> RoleActions = new();

    protected List<GameOptionOverride> RoleSpecificGameOptionOverrides = new();
    /// <summary>
    /// Represents an ID for the role that should be unique to THE defining assembly
    /// </summary>
    public virtual ulong RoleID => this.GetType().SemiConsistentHash();
    protected AbstractBaseRole()
    {
        DeclaringAssembly = this.GetType().Assembly;
        //if (this is AbridgedRole2) return;
        this.EnglishRoleName = this.GetType().Name.Replace("CRole", "").Replace("Role", "");
        log.Debug($"AbstractBaseRole() Role Name: {EnglishRoleName}");
        CreateInstanceBasedVariables();

        // Why are we calling this? Modify may reference uncreated options, yet when setting up options developers may try to reference
        // RoleColor (which is white until after Modify)
        // To solve this we call Modify to TRY to set up the role color, crashing once it requires uncreated options
        // The modify call in the Solidify method is the "real" modify
        RoleModifier _;
        try
        {
            _ = Modify(new RoleModifier(this));
        }
        catch { }
        Metadata = new RoleMetadata();
    }

    public virtual void Solidify()
    {
        this.RoleSpecificGameOptionOverrides.Clear();

        GameOptionBuilder optionBuilder = GetGameOptionBuilder();

        LinkedRoles().ForEach(lRole =>
        {
            if (lRole.GetRoleType() is RoleType.DontShow)
            {
                Create();
                return;
            }
            optionBuilder.SubOption(_ =>
            {
                Create();
                return lRole.RoleOptions;
            });
            return;

            void Create()
            {

                lRole.RoleOptions = lRole.GetGameOptionBuilder().IsHeader(false).Build();
                lRole.Modify(new RoleModifier(lRole));
                lRole.RoleOptions.Color = lRole.RoleColor;
                lRole.SetupRoleActions();
            }
        });

        RoleOptions = optionBuilder.Build();
        Modify(new RoleModifier(this));
        RoleOptions.Color = RoleColor;

        if (RoleFlags.HasFlag(RoleFlag.DontRegisterOptions) || RoleOptions.GetValueText() == "N/A") goto finished;
        Option? percentageOption = RoleOptions.Children.FirstOrDefault(child => child.Name() == "Percentage", null!);
        if (percentageOption != null) this.Chance = (int)percentageOption.GetValue();
        else
        {
            DevLogger.Log($"Could not find Percentage Option for {EnglishRoleName}. Setting as 0.");
            this.Chance = 0;
        }

        if (!RoleFlags.HasFlag(RoleFlag.Hidden) && RoleOptions.Tab == null)
        {
            IGameOptionTab? outputTab = null;
            if (GetType() == typeof(Impostor)) outputTab = DefaultTabs.HiddenTab;
            else if (GetType() == typeof(Crewmate)) outputTab = DefaultTabs.HiddenTab;
            else if (GetType() == typeof(GuardianAngel)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Engineer)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Scientist)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Phantom)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Tracker)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Noisemaker)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Shapeshifter)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Detective)) outputTab = DefaultTabs.HiddenTab;
            // else if (GetType() == typeof(Viper)) outputTab = DefaultTabs.HiddenTab;
            else
            {
                if (this is GameMaster) { /*ignored*/ }
                else if (this is Subrole)
                    outputTab = DefaultTabs.MiscTab;
                else if (this.Faction is ImpostorFaction)
                    outputTab = DefaultTabs.ImpostorsTab;
                else if (this.Faction is Crewmates)
                    outputTab = DefaultTabs.CrewmateTab;
                else if (this.Faction is TheUndead || this.SpecialType is SpecialType.NeutralKilling or SpecialType.Neutral)
                    outputTab = DefaultTabs.NeutralTab;
                else
                    outputTab = DefaultTabs.MiscTab;
            }

            if (outputTab != null)
            {
                if (LastAddonAdded.GetValueOrDefault(outputTab) != Addon)
                    outputTab.AddOption(new GameOptionTitleBuilder()
                        .Title(Addon != null ? Addon.GetDisplayName() : ProjectLotus.ModName)
                        .IsHeader(true)
                        .Build()
                    );


                LastAddonAdded[outputTab] = Addon;
                RoleOptions.Tab = outputTab;
            }
        }
        RoleOptions.Register(GlobalRoleManager.RoleOptionManager, OptionLoadMode.LoadOrCreate);

    finished:

        SetupRoleActions();
    }

    private void SetupRoleActions()
    {
        Enum.GetValues<LotusActionType>().Do(action => this.RoleActions.Add(action, new List<RoleAction>()));
        this.GetType().GetMethods(AccessFlags.InstanceAccessFlags)
            .SelectMany(method => method.GetCustomAttributes<RoleActionAttribute>().Select(a => (a, method)))
            .Where(t => t.a.Subclassing || t.method.DeclaringType == this.GetType())
            .Select(t => new RoleAction(t.Item1, t.method, this))
            .Do(AddRoleAction);
    }

    public void AddRoleAction(RoleAction action)
    {
        List<RoleAction> currentActions = this.RoleActions.GetValueOrDefault(action.ActionType, new List<RoleAction>());

        log.Log(LogLevel.All, $"Registering Action {action.ActionType} => {action.Method.Name} (from: \"{action.Method.DeclaringType}\")", "RegisterAction");
        if (action.ActionType is LotusActionType.FixedUpdate &&
            currentActions.Count > 0)
            throw new ConstraintException("LotusActionType.FixedUpdate is limited to one per class. If you're inheriting a class that uses FixedUpdate you can add Override=METHOD_NAME to your annotation to override its Update method.");

        action.SetExecuter(this);
        if (action.Attribute.Subclassing || action.Method.DeclaringType == this.GetType())
            currentActions.Add(action);

        this.RoleActions[action.ActionType] = currentActions;
    }

    protected void CreateInstanceBasedVariables()
    {
        this.UIManager.SetBaseRole(this);
        this.GetType().GetFields(AccessFlags.InstanceAccessFlags | BindingFlags.FlattenHierarchy)
            .Where(f => f.GetCustomAttribute<NewOnSetupAttribute>() != null)
            .Select(f => new NewOnSetup(f, f.GetCustomAttribute<NewOnSetupAttribute>()!.UseCloneIfPresent))
            .ForEach(CreateAnnotatedFields);
        this.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            .Where(f => f.FieldType == typeof(Cooldown) || (f.FieldType.IsGenericType && typeof(Optional<>).IsAssignableFrom(f.FieldType.GetGenericTypeDefinition())))
            .Do(f =>
            {
                if (f.FieldType.GetCustomAttribute<NewOnSetupAttribute>() != null) CreateAnnotatedFields(new NewOnSetup(f, f.FieldType.GetCustomAttribute<NewOnSetupAttribute>()!.UseCloneIfPresent));
                else if (f.FieldType == typeof(Cooldown)) CreateCooldown(f);
                else CreateOptional(f);
            });
    }

    // lol this method is such a hack it's funny
    private static readonly List<(RoleAction, AbstractBaseRole)> EmptyActions = new();
    public IEnumerable<(RoleAction, AbstractBaseRole)> GetActions(LotusActionType actionType)
    {
        return !RoleActions.TryGetValue(actionType, out List<RoleAction>? actions)
            ? EmptyActions : actions.Select(action => (action, this));
    }

    private void CreateAnnotatedFields(NewOnSetup setupRules)
    {

        FieldInfo field = setupRules.FieldInfo;
        object? currentValue = field.GetValue(this);
        object newValue;

        if (setupRules.UseCloneIfPresent && currentValue is ICloneOnSetup cos)
        {
            field.SetValue(this, cos.CloneIndiscriminate());
            return;
        }

        MethodInfo? cloneMethod = field.FieldType.GetMethod("Clone", AccessFlags.InstanceAccessFlags, Array.Empty<Type>());
        if (currentValue == null || cloneMethod == null || !setupRules.UseCloneIfPresent)
            try
            {
                newValue = AccessTools.CreateInstance(field.FieldType);
            }
            catch (Exception e)
            {
                log.Exception(e);
                throw new ArgumentException($"Error during \"{nameof(NewOnSetup)}\" processing. Could not create instance with no-args constructor for type {field.FieldType}. (Field={field}, Role={EnglishRoleName})");
            }
        else
            try
            {
                newValue = cloneMethod.Invoke(currentValue, null)!;
            }
            catch (Exception e)
            {
                log.Exception(e);
                throw new ArgumentException($"Error during \"{nameof(NewOnSetup)}\" processing. Could not clone original instance for type {field.FieldType}. (Field={field}, Role={EnglishRoleName})");
            }
        if (newValue != null)
        {
            field.SetValue(this, newValue);
        }
    }

    private void CreateCooldown(FieldInfo fieldInfo)
    {
        Cooldown? value = (Cooldown)fieldInfo.GetValue(this);
        Cooldown setValue = value == null ? new Cooldown() : value.Clone();
        value?.TimeRemaining();
        fieldInfo.SetValue(this, setValue);
    }

    private void CreateOptional(FieldInfo fieldInfo)
    {
        ConstructorInfo GetConstructor(Type[] parameters) => AccessTools.Constructor(fieldInfo.FieldType, parameters);

        object? optional = fieldInfo.GetValue(this);
        ConstructorInfo constructor = GetOptionalConstructor(fieldInfo, optional == null);
        object? setValue = constructor.Invoke(optional == null ? Array.Empty<object>() : new[] { optional });
        fieldInfo.SetValue(this, setValue);
    }

    private ConstructorInfo GetOptionalConstructor(FieldInfo info, bool isNull)
    {
        if (isNull) return AccessTools.Constructor(info.FieldType, Array.Empty<Type>());
        return info.FieldType.GetConstructors().First(c =>
            c.GetParameters().SelectWhere(p => p.ParameterType, t => t!.IsGenericType).Any(tt =>
                tt!.GetGenericTypeDefinition().IsAssignableTo(typeof(Optional<>))));
    }

    /// <summary>
    /// This method is called when the role class is Instantiated (during role selection),
    /// thus allowing modifications to the specific player attached to this role
    /// </summary>
    /// <param name="player">The player assigned to this role</param>
    protected virtual void Setup(PlayerControl player) { }

    /// <summary>
    /// This method is called after the current role is finished cloning and setting up.
    /// A player is not passed here as MyPlayer is already set and should be used.
    /// </summary>
    protected virtual void PostSetup() { }

    /// <summary>
    /// This method is called when the options are generated to get the role type.
    /// Override this along with adding the RoleFlag for the Role.
    /// <br/>
    /// This is only used for Variation and Transformation Roles.
    /// </summary>
    /// <returns>Role Type</returns>
    public virtual RoleType GetRoleType() => RoleType.Normal;

    /// <summary>
    /// Forced method that allows CustomRoles to provide unique definitions for themselves
    /// </summary>
    /// <param name="roleModifier">Automatically supplied RoleModifier used for class specifications</param>
    /// <returns>Provided <b>OR</b> new RoleModifier</returns>
    protected abstract RoleModifier Modify(RoleModifier roleModifier);

    /// <summary>
    /// Roles that are "Linked" to this role. What this means:
    /// ANY role you put in here will have its options become a sub-option of this role.
    /// Additionally, you DO NOT (AND SHOULD NOT) register said role in your addon. The role will be automatically hooked in by being a member
    /// of this function. If you happen to do so probably nothing will go wrong, but it's undefined behaviour and should be avoided.
    /// <br/><br/>
    /// If you're looking to change the options from % chance to "Show" / "Hide" refer to <see cref="RoleFlags"/> on the child role.
    /// </summary>
    /// <returns>Linked Roles</returns>
    public virtual List<CustomRole> LinkedRoles() => [];

    /// <summary>
    /// Force the path for the Role Image or Role Outfit.
    /// </summary>
    protected virtual string ForceRoleImageDirectory() => "N/A";

    /// <summary>
    /// Using the resourceDirectory parameter, this code will search your mod's resources and see if it finds the file.<br/>
    /// If the corresponding file is a .png, it will add your image to the options.<br/>
    /// If the corresponding file is a .yaml, it will add your image to the options.<br/>
    /// You can override this function in your roles or patch this to use your own<br/>
    /// asset bundles or other methods of loading sprites.
    /// </summary>
    /// <param name="resourceDirectory">A string to the path of the image or outfit file.</param>
    /// <returns>A function which runs after VentFramework generates your option.</returns>
    protected virtual Action<RoleOptionIntializer.RoleOptionIntialized> SetupRoleOption(string resourceDirectory)
    {
        if (DeclaringAssembly == Assembly.GetExecutingAssembly()) return RoleOutfitLotus(resourceDirectory);

        if (AssetLoader.ResourceExists(resourceDirectory.EndsWith(".png") ? resourceDirectory : resourceDirectory + ".png", DeclaringAssembly))
        {
            if (!resourceDirectory.EndsWith(".png")) resourceDirectory += ".png";
            PersistentAssetLoader.RegisterSprite(EnglishRoleName + "RoleImage", resourceDirectory, 500, DeclaringAssembly);
            return roleOption => roleOption.RoleImage.sprite = PersistentAssetLoader.GetSprite(EnglishRoleName + "RoleImage");
        }
        if (AssetLoader.ResourceExists(resourceDirectory.EndsWith(".yaml") ? resourceDirectory : resourceDirectory + ".yaml", DeclaringAssembly))
        {
            if (!resourceDirectory.EndsWith(".yaml")) resourceDirectory += ".yaml";
            return roleOption =>
            {
                roleOption.RoleImage.enabled = false;
                PoolablePlayer fakePlayer = Object.Instantiate<PoolablePlayer>(DestroyableSingleton<HudManager>.Instance.IntroPrefab.PlayerPrefab, roleOption.RoleImage.transform);
                fakePlayer.name = "RoleOutfit";
                fakePlayer.transform.localPosition = new Vector3(0, -.2f, 0);
                fakePlayer.transform.localScale = Vector3.one;
                fakePlayer.enabled = true;
                fakePlayer.cosmetics.initialized = false;
                fakePlayer.cosmetics.EnsureInitialized(PlayerBodyTypes.Normal);
                OutfitFile outfit = OutfitFile.FromManifestFile(resourceDirectory, DeclaringAssembly);
                fakePlayer.UpdateFromPlayerOutfit(outfit.ToPlayerOutfit(), PlayerMaterial.MaskType.SimpleUI, outfit.ShowDead, false);
                if (outfit.ShowDead)
                {
                    fakePlayer.cosmetics.SetBodyAsGhost();
                    // Async.Schedule(() => fakePlayer.FindChild<Transform>("Cosmetics", true).position = new Vector3(.245f, .2f, 0), 0.1f);
                }
                fakePlayer.FindChild<Transform>("Names", true).gameObject.SetActive(false);
            };
        }
        // log we can't find it and default to an empty image
        string? debugMessage = $"Could not find RoleImage for {EnglishRoleName}. Defaulting to no image. Path (.png/.yaml): {resourceDirectory}";
        return roleOption =>
        {
            if (debugMessage != null)
            {
                log.Debug(debugMessage);
                debugMessage = null;
            }
            roleOption.RoleImage.enabled = false;
        };
    }

    /// <summary>
    /// This method gets the path to the target file to replace.
    /// </summary>
    public string GetRoleOutfitPath()
    {
        string resourceDirectory = ForceRoleImageDirectory();

        if (resourceDirectory != "N/A") return resourceDirectory;

        if (this is GameMaster) resourceDirectory = "gm";
        else if ((Faction is ImpostorFaction | SpecialType is SpecialType.Madmate) && this is not NeutralKillingBase &&
                 SpecialType is SpecialType.None) resourceDirectory = "Imposter/" + EnglishRoleName;
        else if (Faction is Crewmates && SpecialType is SpecialType.None) resourceDirectory = "Crew/" + EnglishRoleName;
        else if (Faction is Modifiers | RoleFlags.HasFlag(RoleFlag.IsSubrole)) resourceDirectory = "Modifiers/" + EnglishRoleName;
        else resourceDirectory = "Neutral/" + EnglishRoleName;

        resourceDirectory = "RoleOutfits/" + resourceDirectory;
        return resourceDirectory.ToLower();
    }

    private Action<RoleOptionIntializer.RoleOptionIntialized> RoleOutfitLotus(string resourceDirectory)
    {
        if (!resourceDirectory.EndsWith(".yaml")) resourceDirectory += ".yaml";
        return roleOption =>
        {
            roleOption.RoleImage.enabled = false;
            PoolablePlayer fakePlayer = Object.Instantiate<PoolablePlayer>(DestroyableSingleton<HudManager>.Instance.IntroPrefab.PlayerPrefab, roleOption.RoleImage.transform);
            fakePlayer.name = "RoleOutfit";
            fakePlayer.transform.localPosition = new Vector3(0, -.2f, 0);
            fakePlayer.transform.localScale = Vector3.one;
            fakePlayer.enabled = true;
            fakePlayer.cosmetics.initialized = false;
            fakePlayer.cosmetics.EnsureInitialized(PlayerBodyTypes.Normal);
            OutfitFile outfit = OutfitFile.FromAssetBundle(resourceDirectory, LotusAssets.Bundle);
            fakePlayer.UpdateFromPlayerOutfit(outfit.ToPlayerOutfit(), PlayerMaterial.MaskType.SimpleUI, outfit.ShowDead, false);
            if (outfit.ShowDead)
            {
                fakePlayer.cosmetics.SetBodyAsGhost();
                // Async.Schedule(() => fakePlayer.FindChild<Transform>("Cosmetics", true).position = new Vector3(.245f, .2f, 0), 0.1f);
            }
            fakePlayer.FindChild<Transform>("Names", true).gameObject.SetActive(false);
        };
    }

    public GameOptionBuilder GetGameOptionBuilder()
    {
        GameOptionBuilder b = GetBaseBuilder();

        if (GetRoleType() is not RoleType.Normal || RoleFlags.HasFlag(RoleFlag.RemoveRoleMaximum)) return RegisterOptions(b);
        b = b.SubOption(s => s
            .KeyName("Maximum", RoleTranslations.MaximumText)
            .AddIntRange(1, ModConstants.MaxPlayers)
            .Bind(val => this.Count = (int)val)
            .ShowSubOptionPredicate(v => 1 < (int)v)
            .SubOption(subsequent => subsequent
                .Name(RoleTranslations.SubsequentChanceText)
                .Key("Subsequent Chance")
                .AddIntRange(10, 100, 10, 0, "%")
                .BindInt(v => AdditionalChance = v)
                .Build())
            .Build());

        return RegisterOptions(b);
    }

    protected virtual GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream)
    {
        Localizer localizer = Localizer.Get(DeclaringAssembly);
        string qualifier = $"Roles.{EnglishRoleName}.RoleName";

        return optionStream
            .KeyName(EnglishRoleName, localizer.Translate(qualifier))
            .Description(localizer.Translate($"Roles.{EnglishRoleName}.Blurb"))
            .IsHeader(true);
    }

    [SuppressMessage("ReSharper", "RemoveRedundantBraces")]
    private GameOptionBuilder GetBaseBuilder()
    {
        if (!RoleFlags.HasFlag(RoleFlag.RemoveRolePercent) && GetRoleType() is RoleType.Normal)
        {
            return new GameOptionBuilder()
                .SetAsRoleOption(val => this.Chance = val, SetupRoleOption(GetRoleOutfitPath()), 0, 100, RoleFlags.HasFlag(RoleFlag.IncrementChanceByFives) ? 5 : 10, suffix: "%")
                .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
                // .AddIntRange(0, 100, RoleFlags.HasFlag(RoleFlag.IncrementChanceByFives) ? 5 : 10, suffix: "%")
                // .Value(0)
                // .BindInt(val => this.Chance = val)
                .ShowSubOptionPredicate(value => this.Chance > 0);
        }

        string onText = GeneralOptionTranslations.OnText;
        string offText = GeneralOptionTranslations.OffText;

        if (GetRoleType() is RoleType.Transformation)
        {
            onText = GeneralOptionTranslations.ShowText;
            offText = GeneralOptionTranslations.HideText;
        }
        DevLogger.Log($"Variation Role: {EnglishRoleName}");

        if (GetRoleType() is RoleType.Variation)
            return new GameOptionBuilder()
                .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
                // .Value(v => v.Text(onText).Value(true).Color(Color.cyan).Build())
                // .Value(v => v.Text(offText).Value(false).Color(Color.red).Build())
                // .ShowSubOptionPredicate(b => (bool)b);
                .AddIntRange(0, 100, RoleFlags.HasFlag(RoleFlag.IncrementChanceByFives) ? 5 : 10, suffix: "%")
                .BindInt(val => this.Chance = val)
                .ShowSubOptionPredicate(value => this.Chance > 0);
        else
            return new GameOptionBuilder()
                .IOSettings(io => io.UnknownValueAction = ADEAnswer.UseDefault)
                .Value(v => v.Text(onText).Value(true).Color(Color.cyan).Build())
                .Value(v => v.Text(offText).Value(false).Color(Color.red).Build())
                .ShowSubOptionPredicate(b => (bool)b);
    }


    public override string ToString()
    {
        return this.RoleName;
    }

    public class RoleModifier
    {
        private AbstractBaseRole myRole;

        public RoleModifier(AbstractBaseRole role)
        {
            this.myRole = role;
        }

        public RoleModifier DesyncRole(RoleTypes? desyncRole)
        {
            myRole.DesyncRole = desyncRole;
            return this;
        }

        public RoleModifier VanillaRole(RoleTypes vanillaRole)
        {
            myRole.VirtualRole = vanillaRole;
            return this;
        }

        public RoleModifier SpecialType(SpecialType specialType)
        {
            myRole.SpecialType = specialType;
            myRole.Metadata.Set(LotusKeys.AuxiliaryRoleType, specialType);
            return this;
        }

        public RoleModifier Faction(IFaction factions)
        {
            myRole.Faction = factions;
            return this;
        }

        public RoleModifier CanVent(bool canVent)
        {
            myRole.BaseCanVent = canVent;
            return this;
        }

        public RoleModifier IntroSound(RoleTypes roleType) => IntroSound(() => BeginCrewmatePatch.GetIntroSound(roleType)!);

        public RoleModifier IntroSound(Func<AudioClip> sound)
        {
            myRole.IntroSound = sound;
            return this;
        }

        public RoleModifier OptionOverride(Override option, object? value, Func<bool>? condition = null)
        {
            myRole.RoleSpecificGameOptionOverrides.Add(new GameOptionOverride(option, value, condition));
            return this;
        }

        public RoleModifier OptionOverride(Override option, Func<object> valueSupplier, Func<bool>? condition = null)
        {
            myRole.RoleSpecificGameOptionOverrides.Add(new GameOptionOverride(option, valueSupplier, condition));
            return this;
        }

        public RoleModifier OptionOverride(GameOptionOverride @override)
        {
            myRole.RoleSpecificGameOptionOverrides.Add(@override);
            return this;
        }

        public RoleModifier RoleName(string adjustedName)
        {
            myRole.EnglishRoleName = adjustedName;
            return this;
        }

        public RoleModifier RoleColor(string htmlColor)
        {
            if (ColorUtility.TryParseHtmlString(htmlColor, out Color color))
                myRole.RoleColor = color;
            else
                log.Fatal($"Could not parse HTML Code {htmlColor} for {myRole.EnglishRoleName}");
            return this;
        }

        public RoleModifier Gradient(ColorGradient colorGradient)
        {
            myRole.RoleColorGradient = colorGradient;
            return this;
        }

        public RoleModifier Gradient(params Color[] colors)
        {
            myRole.RoleColorGradient = new ColorGradient(colors);
            return this;
        }

        public RoleModifier RoleColor(Color color)
        {
            myRole.RoleColor = color;

            System.Drawing.Color dColor = System.Drawing.Color.FromArgb(
                Mathf.FloorToInt(color.a * 255),
                Mathf.FloorToInt(color.r * 255),
                Mathf.FloorToInt(color.g * 255),
                Mathf.FloorToInt(color.b * 255));

            DevLogger.Log($"ColoredName: {ColorMapper.GetNearestName(dColor)}");

            return this;
        }

        public RoleModifier RoleFlags(RoleFlag roleFlags, bool replace = false)
        {
            if (replace) myRole.RoleFlags = roleFlags;
            else myRole.RoleFlags |= roleFlags;

            bool alreadyReplaced = false;

            if (roleFlags.HasFlag(RoleFlag.IsSubrole))
            {
                AddRoleProperty(RoleProperty.IsModifier, replace);
                alreadyReplaced = true;
            }
            if (roleFlags.HasFlag(RoleFlag.CannotWinAlone))
                AddRoleProperty(RoleProperty.CannotWinAlone, alreadyReplaced ? false : replace);

            return this;
        }

        public RoleModifier RoleAbilityFlags(RoleAbilityFlag roleAbilityFlags, bool replace = false)
        {
            if (replace) myRole.RoleAbilityFlags = roleAbilityFlags;
            else myRole.RoleAbilityFlags |= roleAbilityFlags;

            if (roleAbilityFlags.HasFlag(RoleAbilityFlag.IsAbleToKill))
                myRole.Metadata.GetOrDefault(RoleProperties.Key, new RoleProperties()).Add(RoleProperty.IsAbleToKill);

            return this;
        }

        public RoleModifier AddRoleProperty(RoleProperty[] properties, bool replace = false)
        {
            if (replace) myRole.Metadata.Set(RoleProperties.Key, new RoleProperties());
            myRole.Metadata.GetOrDefault(RoleProperties.Key, new RoleProperties()).AddAll(properties);
            return this;
        }
        public RoleModifier AddRoleProperty(RoleProperty property, bool replace = false)
        {
            return AddRoleProperty([property], replace);
        }
    }
}