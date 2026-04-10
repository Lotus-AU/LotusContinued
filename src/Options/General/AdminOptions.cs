using System.Collections.Generic;
using System.Linq;
using Lotus;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.Options;
using Lotus.Patches.Network;
using Lotus.Roles.Builtins;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.IO;
using VentLib.Options.UI;
using VentLib.Utilities.Extensions;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class AdminOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    private static readonly Color _optionColor = GameMaster.GMColor;

    // ReSharper disable once InconsistentNaming
    public bool HostGM;
    public bool SpectatorMode;
    public float AutoHauntCooldown;

    public bool AutoKick;
    public bool KickPlayersWithoutFriendcodes;
    public int KickPlayersUnderLevel;
    public bool KickMobilePlayers;
    public bool EnableWhitelist;

    public int AutoStartPlayerThreshold;
    public int AutoStartMaxTime = -1;
    public int AutoStartGameCountdown;
    public bool AutoPlayAgain;

    public Cooldown AutoCooldown = new();
    public bool AutoStartEnabled;

    public AdminOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(AdminOptionTranslations.AdminTitle)
            .Color(_optionColor)
            .IsHeader(false)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean(false)
            .KeyName("Host GM", AdminOptionTranslations.HostGmText)
            .Color(_optionColor)
            .BindBool(b => HostGM = b)
            .IsHeader(true)
            .ShowSubOptionPredicate(v => (bool)v)
            .SubOption(sub => sub
                .KeyName("Spectator Mode", AdminOptionTranslations.SpectatorMode)
                .AddBoolean()
                .BindBool(b => SpectatorMode = b)
                .ShowSubOptionPredicate(v => (bool)v)
                .SubOption(sub2 => sub2
                    .KeyName("Auto Haunt Cooldown", AdminOptionTranslations.AutoHauntCooldown)
                    .AddFloatRange(2.5f, 60f, 2.5f, 9, "s")
                    .BindFloat(v => AutoHauntCooldown = v)
                    .Build())
                .Build())
            .Build());

        // TODO: repeat offenders
        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean()
            .Builder("Chat AutoKick", _optionColor)
            .Name(AdminOptionTranslations.AutoKickText)
            .BindBool(b => AutoKick = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .AddBoolean(false)
            .Builder("Kick Players Without Friendcode", _optionColor)
            .Name(AdminOptionTranslations.AutoKickNoFriendCodeText)
            .BindBool(b => KickPlayersWithoutFriendcodes = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Kick Players Under Level", _optionColor)
            .AddIntRange(1, 100, 1)
            .Value(v => v.Text(GeneralOptionTranslations.DisabledText).Value(0).Color(Color.red).Build())
            .Name(AdminOptionTranslations.AutoKickUnderLevel)
            .BindInt(i => KickPlayersUnderLevel = i)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Kick Mobile Players", _optionColor)
            .Name(AdminOptionTranslations.AutoKickMobile)
            .AddBoolean(false)
            .BindBool(b => KickMobilePlayers = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Enable Whitelist", _optionColor)
            .Name(AdminOptionTranslations.EnableWhitelist)
            .AddBoolean(false)
            .BindBool(b => EnableWhitelist = b)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Auto Start", _optionColor)
            .Name(AdminOptionTranslations.AutoStartText)
            .AddBoolean(false)
            .BindBool(b =>
            {
                AutoStartEnabled = b;
                if (GameStartManager.Instance != null && !b) GameStartManager.Instance.ResetStartState();
            })
            .ShowSubOptionPredicate(b => (bool)b)
            .SubOption(sub2 => sub2
                .AddIntRange(5, ModConstants.MaxPlayers, suffix: " " + AdminOptionTranslations.AutoStartSuffix)
                .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(-1).Color(Color.red).Build())
                .KeyName("Auto Start Player Threshold", AdminOptionTranslations.AutoStartPlayerThreshold)
                .IOSettings(io => io.UnknownValueAction = ADEAnswer.Allow)
                .BindInt(i => AutoStartPlayerThreshold = i)
                .Build())
            .SubOption(sub2 => sub2
                .AddIntRange(30, 540, ModConstants.MaxPlayers, 0, GeneralOptionTranslations.SecondsSuffix)
                .Value(v => v.Text(GeneralOptionTranslations.OffText).Value(-1).Color(Color.red).Build())
                .KeyName("Auto Start Max Wait Time", AdminOptionTranslations.AutoStartMaxWaitTime)
                .IOSettings(io => io.UnknownValueAction = ADEAnswer.Allow)
                .BindInt(i =>
                {
                    if (LobbyBehaviour.Instance == null) return;
                    AutoStartMaxTime = i;
                    if (i == -1) AutoCooldown.Finish();
                    else
                    {
                        AutoCooldown.SetDuration(i);
                        AutoCooldown.Start();
                    }
                    PlayerJoinPatch.CheckAutostart();
                })
                .Build())
            .SubOption(sub2 => sub2
                .AddIntRange(4, 20, 2, 3, GeneralOptionTranslations.SecondsSuffix)
                .KeyName("Auto Start Game Countdown", AdminOptionTranslations.AutoStartGameCountdown)
                .BindInt(i => AutoStartGameCountdown = i)
                .Build())
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Builder("Auto Play Again", _optionColor)
            .Name(AdminOptionTranslations.AutoPlayAgain)
            .AddBoolean()
            .BindBool(b => AutoPlayAgain = b)
            .Build());

        AllOptions.Where(o => !o.Attributes.ContainsKey("Title")).ForEach(o => GeneralOptions.StandardOptionManager.Register(o, VentLib.Options.OptionLoadMode.LoadOrCreate));
        PostInitialize();
    }

    [Localized("Admin")]
    private class AdminOptionTranslations
    {
        [Localized("SectionTitle")]
        public static string AdminTitle = "Host Options";

        [Localized("HostGM")]
        public static string HostGmText = "Host GM";

        [Localized("AutoKick")]
        public static string AutoKickText = "Chat Auto Kick";

        [Localized("AutoKickNoFriendcode")]
        public static string AutoKickNoFriendCodeText = "Kick Players w/o Friendcodes";

        [Localized("AutoKickLevel")]
        public static string AutoKickUnderLevel = "Kick Players Under Level";

        [Localized(nameof(AutoKickMobile))]
        public static string AutoKickMobile = "Kick Mobile Players";

        // Auto Start
        [Localized("AutoStart")]
        public static string AutoStartText = "Auto Start";

        [Localized(nameof(AutoStartPlayerThreshold))]
        public static string AutoStartPlayerThreshold = "Player Threshold";

        [Localized(nameof(AutoStartMaxWaitTime))]
        public static string AutoStartMaxWaitTime = "Maximum Wait Time";

        [Localized(nameof(AutoStartGameCountdown))]
        public static string AutoStartGameCountdown = "Game Countdown";

        [Localized("AutoStartOptionSuffix")]
        public static string AutoStartSuffix = "Players";

        [Localized(nameof(AutoPlayAgain))]
        public static string AutoPlayAgain = "Auto Play Again";

        [Localized(nameof(SpectatorMode))]
        public static string SpectatorMode = "Spectator Mode";

        [Localized(nameof(AutoHauntCooldown))]
        public static string AutoHauntCooldown = "Auto Haunt Cooldown";

        [Localized(nameof(EnableWhitelist))]
        public static string EnableWhitelist = "Enable Whitelist via Friendcode";
    }

}