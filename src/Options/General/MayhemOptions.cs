using System;
using System.Collections.Generic;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Lotus.GameModes.Normal.Standard;
using VentLib.Options;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class MayhemOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    private static readonly Color _optionColor = new(0.84f, 0.8f, 1f);

    public bool AllRolesCanVent;
    public bool CamoComms;

    public bool UseRandomSpawn => randomSpawnOn && Game.CurrentGameMode is NormalStandardGameMode;
    private bool randomSpawnOn;

    public MayhemOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(Translations.MayhemOptionTitle)
            .Color(_optionColor)
            .Build());

        AllOptions.Add(Builder("Random Spawn")
            .Name(Translations.RandomSpawnText)
            .BindBool(b => randomSpawnOn = b)
            .Build());

        // AllOptions.Add(Builder("Camo Comms")
        //     .Name(Translations.CamoCommText)
        //     .BindBool(b => CamoComms = b)
        //     .Build());

        PostInitialize();
    }

    private GameOptionBuilder Builder(string key) => new GameOptionBuilder().AddBoolean(false).Builder(key, _optionColor);

    [Localized("Mayhem")]
    public static class Translations
    {
        [Localized("SectionTitle")]
        public static string MayhemOptionTitle = "Mayhem Options";

        [Localized("RandomMaps")]
        public static string RandomMapModeText = "Enable Random Maps";

        [Localized("RandomSpawn")]
        public static string RandomSpawnText = "Random Spawn";

        [Localized("CamoComms")]
        public static string CamoCommText = "Camo Comms";

        [Localized("AllRolesCanVent")]
        public static string AllRolesVentText = "All Roles Can Vent";

        [Localized("Skeld")] public static string MapNameSkeld = "Skeld";
        [Localized("Mira")] public static string MapNameMira = "Mira";
        [Localized("Polus")] public static string MapNamePolus = "Polus";
        [Localized("Airship")] public static string MapNameAirship = "Airship";
    }
}