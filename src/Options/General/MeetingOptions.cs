using System.Collections.Generic;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.UI;

namespace Lotus.Options.General;

[Localized(ModConstants.Options)]
public class MeetingOptions: LotusOptionHolder
{
    public override OptionManager OptionManager => GeneralOptions.StandardOptionManager;

    private static readonly Color _optionColor = new(0.27f, 0.75f, 1f);

    public int MeetingButtonPool = -1;
    public bool SyncMeetingButtons => MeetingButtonPool != -1;
    public ResolveTieMode ResolveTieMode;
    public SkipVoteMode NoVoteMode;

    public MeetingOptions()
    {
        AllOptions.Add(new GameOptionTitleBuilder()
            .Title(MeetingOptionTranslations.SectionTitle)
            .Color(_optionColor)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Color(Color.red).Value(-1).Build())
            .AddIntRange(1, 30)
            .Builder("Single Meeting Pool", _optionColor)
            .IsHeader(true)
            .Name(MeetingOptionTranslations.SingleMeetingPool)
            .BindInt(i => MeetingButtonPool = i)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Color(Color.red).Value(0).Build())
            .Value(v => v.Text(MeetingOptionTranslations.RandomPlayer).Color(ModConstants.Palette.InfinityColor).Value(1).Build())
            .Value(v => v.Text(MeetingOptionTranslations.KillAll).Color(ModConstants.Palette.GeneralColor4).Value(2).Build())
            .Builder("Resolve Tie Mode", _optionColor)
            .Name(MeetingOptionTranslations.ResolveTieMode)
            .BindInt(i => ResolveTieMode = (ResolveTieMode)i)
            .Build());

        AllOptions.Add(new GameOptionBuilder()
            .Value(v => v.Text(GeneralOptionTranslations.OffText).Color(Color.red).Value(0).Build())
            .Value(v => v.Text(MeetingOptionTranslations.RandomVote).Color(ModConstants.Palette.InfinityColor).Value(1).Build())
            .Value(v => v.Text(MeetingOptionTranslations.ReverseVote).Color(new Color(0.55f, 0.73f, 1f)).Value(2).Build())
            .Value(v => v.Text(MeetingOptionTranslations.ExplodeOnSkip).Color(new Color(1f, 0.4f, 0.2f)).Value(3).Build())
            .Builder("No Vote Mode", _optionColor)
            .Name(MeetingOptionTranslations.SkipVoteMode)
            .BindInt(i => NoVoteMode = (SkipVoteMode)i)
            .Build());

        PostInitialize();
    }

    private GameOptionBuilder Builder(string key) => new GameOptionBuilder().Key(key).Color(_optionColor);

    public static class MeetingOptionTranslations
    {
        [Localized(nameof(ButtonsRemainingMessage))]
        public static string ButtonsRemainingMessage = "There are {0} emergency buttons remaining.";

        [Localized(nameof(RandomPlayer))]
        public static string RandomPlayer = "Random Player";

        [Localized(nameof(KillAll))]
        public static string KillAll = "Kill All";

        [Localized(nameof(MeetingOptions))]
        public static string SectionTitle = "Meeting Options";

        [Localized(nameof(SingleMeetingPool))]
        public static string SingleMeetingPool = "Single Meeting Pool";

        [Localized(nameof(ResolveTieMode))]
        public static string ResolveTieMode = "Resolve Tie Mode";

        [Localized(nameof(SkipVoteMode))]
        public static string SkipVoteMode = "No Vote Mode";

        [Localized(nameof(ExplodeOnSkip))]
        public static string ExplodeOnSkip = "Explode";

        [Localized(nameof(ReverseVote))]
        public static string ReverseVote = "Self";

        [Localized(nameof(NegateVote))]
        public static string NegateVote = "Negate";

        [Localized(nameof(RandomVote))]
        public static string RandomVote = "Random";
    }
}

public enum ResolveTieMode
{
    None,
    Random,
    KillAll
}

public enum SkipVoteMode
{
    None,
    Random,
    Reverse,
    Explode
}