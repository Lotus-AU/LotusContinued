using System.Collections.Generic;
using UnityEngine;
using VentLib.Localization.Attributes;

namespace Lotus;

[Localized("Constants")]
public static class ModConstants
{
    public static Color HColor1 = new(0.03f, 0.53f, 0.01f);
    public static Color HColor2 = new(0.71f, 0.33f, 0f);

    public const int MaxPlayers = 15;

    public const byte PackedGameDataTo = 26;

    // Minimum distance for arrow to show (versus dot)
    public const float ArrowActivationMin = 3;

    public const float DynamicNameTimeBetweenRenders = 0.25f;

    public const double RoleFixedUpdateCooldown = 0.25;

    public const float DeriveDelayMultiplier = 0.0003f;
    public const float DeriveDelayFlatValue = 0.4f;

    public const int RecursiveDepthLimit = 20;

    public const float MaxPlayerSpeed = 3f;

    public const string Options = "Options";

    public static string[] ColorNames = new[]
    {
        "Red", "Blue", "Green", "Pink", "Orange", "Yellow", "Black", "White", "Purple", "Brown", "Cyan", "Lime",
        "Maroon", "Rose", "Banana", "Gray", "Tan", "Coral"
    };


    public const string Infinity = "∞";

    public const string DiscordInvite = "https://discord.gg/projectlotus";

    public const string LinkTree = "https://linktr.ee/lotusau";

    public const string WebsiteLink = "https://lotusau.top/";

    public const string SupportEmail = "theta@eps.lol";

    public static class Palette
    {
        public static Color InfinityColor = new(0.77f, 0.71f, 1f);

        public static Color ModifierColor = new(0.44f, 0.27f, 0.81f);

        public static Color NeutralColor = new(1f, 0.67f, 0.11f);
        public static Color PassiveColor = new(1f, 0.87f, 0.91f);
        public static Color KillingColor = new(1f, 0.27f, 0.18f);
        public static Color MadmateColor = new(0.73f, 0.18f, 0.02f);
        public static Color CrewmateColor = new(0.55f, 1f, 1f);


        public static Color GeneralColor1 = new(0.15f, 0.27f, 0.33f);
        public static Color GeneralColor2 = new(0.16f, 0.62f, 0.56f);
        public static Color GeneralColor3 = new(0.91f, 0.77f, 0.42f);
        public static Color GeneralColor4 = new(0.96f, 0.64f, 0.38f);
        public static Color GeneralColor5 = new(0.91f, 0.44f, 0.32f);


        public static Color GlobalColor = new(1f, 0.61f, 0.33f);
        public static Color InvalidUsage = new(1f, 0.67f, 0.11f);
        public static Color WinnerColor = new(1f, 0.83f, 0.24f);

        public static Color UndeadBlue = new(0.33f, 0.46f, 0.76f);
    }

    [Localized("DeathNames")]
    public static class DeathNames
    {
        [Localized(nameof(Killed))]
        public static string Killed = "Killed";

        [Localized(nameof(Suicide))]
        public static string Suicide = "Suicide";

        [Localized(nameof(Exiled))]
        public static string Exiled = "Exiled";

        [Localized(nameof(Bombed))]
        public static string Bombed = "Bombed";

        [Localized(nameof(Bitten))]
        public static string Bitten = "Bitten";

        [Localized(nameof(Cursed))]
        public static string Cursed = "Cursed";

        [Localized(nameof(Incinerated))]
        public static string Incinerated = "Incinerated";

        [Localized(nameof(Misfired))]
        public static string Misfired = "Misfired";

        [Localized(nameof(Parried))]
        public static string Parried = "Parried";

        [Localized(nameof(Sniped))]
        public static string Sniped = "Sniped";

        [Localized(nameof(Executed))]
        public static string Executed = "Executed";

        [Localized(nameof(Guessed))]
        public static string Guessed = "Guessed";
    }

    public static class Localization
    {
        public const string Misc = "Misc";
        public const string WinConditions = "WinConditions";
    }

    public static Dictionary<string, string> Pets = new()
    {
        { "Random", "Random" },
        { "Bedcrab", "pet_Bedcrab" },
        { "Breb", "pet_BredPet" },
        { "Brant", "pet_YuleGoatPet" },
        { "Bushfriend", "pet_Bush" },
        { "Charles Chopper", "pet_Charles" },
        { "Chewie", "pet_ChewiePet" },
        { "Clank", "pet_clank" },
        { "Coalton", "pet_coaltonpet" },
        { "Deitied Guy", "pet_Cube" },
        { "Doggy", "pet_Doggy" },
        { "E. Rose", "pet_Ellie" },
        { "Frankendog", "pet_frankendog" },
        { "Ghost", "pet_D2GhostPet" },
        { "Glitch Pet", "pet_test" },
        { "Guilty Spark", "pet_GuiltySpark" },
        { "H. Stickmin", "pet_Stickmin" },
        { "Hammy", "pet_HamPet" },
        { "Hampton", "pet_Hamster" },
        { "Headslug", "pet_Alien" },
        { "Poro", "pet_poro" },
        { "Magmate", "pet_Lava" },
        { "Crewmate", "pet_Crewmate" },
        { "Pouka", "pet_D2PoukaPet" },
        { "Ro-Bot", "pet_Robot" },
        { "Snowball", "pet_Snow" },
        { "Squig", "pet_Squig" },
        { "Nugget", "pet_nuggetPet" },
        { "Toppat Chopper", "pet_Charles_Red" },
        { "UFO", "pet_UFO" },
        { "Worm", "pet_D2WormPet" },
    };
}