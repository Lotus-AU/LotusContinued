using System.Linq;
using Lotus.API.Odyssey;
using Lotus.API.Player;
using Lotus.Chat;
using Lotus.Extensions;
using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.Internals;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.Internals.Trackers;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;
using Lotus.API.Vanilla.Meetings;

namespace Lotus.Roles.RoleGroups.Crew;

public class Tracker : Vanilla.Tracker
{
    private TrackBodyValue canTrackBodies;
    private bool canTrackUnreportableBodies;

    private Cooldown trackBodyCooldown;
    private Cooldown trackBodyDuration;

    [UIComponent(UI.Indicator)]
    public string DisplayDeadBodies()
    {
        if (canTrackBodies is TrackBodyValue.Never) return "";
        if (canTrackBodies is TrackBodyValue.OnPet && trackBodyDuration.IsReady()) return "";
        return Object.FindObjectsOfType<DeadBody>()
            .Where(db => canTrackUnreportableBodies || !Game.MatchData.UnreportableBodies.Contains(db.ParentId))
            .Select(db => RoleUtils.CalculateArrow(MyPlayer, db, Color.gray))
            .Fuse();
    }

    [RoleAction(LotusActionType.OnPet)]
    public void TrackDeadBodies()
    {
        if (canTrackBodies is not TrackBodyValue.OnPet) return;
        if (trackBodyCooldown.NotReady() || trackBodyDuration.NotReady()) return;
        trackBodyDuration.StartThenRun(() => trackBodyCooldown.Start());
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        AddTrackerOptions(base.RegisterOptions(optionStream))
            .SubOption(sub => sub.KeyName("Track Bodies", Translations.Options.CanTrackBodies)
                .Value(v => v.Text(GeneralOptionTranslations.OffText).Color(Color.red).Value(0).Build())
                .Value(v => v.Text(Translations.Options.OnPetText).Color(new Color(0.73f, 0.58f, 1f)).Value(1).Build())
                .Value(v => v.Text(GeneralOptionTranslations.AlwaysText).Color(Color.green).Value(2).Build())
                .BindInt(i => canTrackBodies = (TrackBodyValue)i)
                .ShowSubOptionPredicate(i => (int)i != 0)
                .SubOption(sub2 => sub2.KeyName("Can Track Unreportable Bodies", Translations.Options.CanTrackUnreportableBodies)
                    .BindBool(b => canTrackUnreportableBodies = b)
                    .AddOnOffValues()
                    .Build())
                .SubOption(sub2 => sub2.KeyName("Track Body Duration", Translations.Options.TrackBodyDuration)
                    .AddFloatRange(2.5f, 120f, 2.5f, 4, GeneralOptionTranslations.SecondsSuffix)
                    .BindFloat(trackBodyDuration.SetDuration)
                    .Build())
                .SubOption(sub2 => sub2.KeyName("Track Body Cooldown", Translations.Options.TrackBodyCooldown)
                    .AddFloatRange(0, 120f, 2.5f, 16, GeneralOptionTranslations.SecondsSuffix)
                    .BindFloat(trackBodyCooldown.SetDuration)
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            // .RoleColor(new Color(0.82f, 0.24f, 0.82f))
            .RoleAbilityFlags(RoleAbilityFlag.UsesPet);

    private static class Translations
    {
        public static class Options
        {

            [Localized(nameof(CanTrackBodies))]
            public static string CanTrackBodies = "Can Track Bodies";

            [Localized(nameof(CanTrackUnreportableBodies))]
            public static string CanTrackUnreportableBodies = "Can Track Unreportable Bodies";

            [Localized(nameof(TrackBodyCooldown))]
            public static string TrackBodyCooldown = "Track Body Cooldown";

            [Localized(nameof(TrackBodyDuration))]
            public static string TrackBodyDuration = "Track Body Duration";

            [Localized(nameof(OnPetText))]
            public static string OnPetText = "On Pet";
        }
    }

    private enum TrackBodyValue
    {
        Never,
        OnPet,
        Always
    }
}