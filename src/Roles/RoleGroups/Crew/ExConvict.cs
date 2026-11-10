using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.GUI.Name.Holders;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using Lotus.API;
using Lotus.API.Odyssey;
using Lotus.Extensions;
using Lotus.Options;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.Roles.RoleGroups.Impostors;
using Lotus.RPC;
using UnityEngine;
using VentLib;
using VentLib.Options.UI;
using VentLib.Utilities;
using VentLib.Localization.Attributes;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Crew;

public class ExConvict : Crewmate, IRoleUI
{
    private Vector2? location;
    private bool clearMarkAfterMeeting;


    [UIComponent(UI.Cooldown)] private Cooldown canEscapeCooldown;

    [UIComponent(UI.Cooldown)] private Cooldown canMarkCooldown;

    [UIComponent(UI.Text)]
    private string TpIndicator() => canEscapeCooldown.IsReady() && location != null
        ? Color.cyan.Colorize("Press Pet to Escape")
        : "";

    public RoleButton PetButton(IRoleButtonEditor editor) => UpdatePetButton(editor);

    protected override void PostSetup()
    {
        CooldownHolder cooldownHolder = MyPlayer.NameModel().GetComponentHolder<CooldownHolder>();
        cooldownHolder.Get(1).SetPrefix("Escape In: ").SetTextColor(Color.cyan);
    }

    [RoleAction(LotusActionType.OnPet)]
    private void PetAction()
    {
        if (location == null) TryMarkLocation();
        else TryEscape();
    }

    [RoleAction(LotusActionType.RoundStart)]
    private void ClearMark()
    {
        if (clearMarkAfterMeeting)
        {
            location = null;
            if (MyPlayer.AmOwner) UpdatePetButton(UIManager.PetButton);
            else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateExConvict)?.Send([MyPlayer.OwnerId], false, false);
        }
    }

    private void TryMarkLocation()
    {
        if (canMarkCooldown.NotReady()) return;
        location = MyPlayer.GetTruePosition();
        canEscapeCooldown.Start();
        if (MyPlayer.AmOwner) UpdatePetButton(UIManager.PetButton);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateExConvict)?.Send([MyPlayer.OwnerId], true, true);
    }

    private void TryEscape()
    {
        if (canEscapeCooldown.NotReady() || location == null) return;
        Utils.Teleport(MyPlayer.NetTransform, location.Value, this);
        location = null;
        canMarkCooldown.Start();
        if (MyPlayer.AmOwner) UpdatePetButton(UIManager.PetButton);
        else if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateExConvict)?.Send([MyPlayer.OwnerId], false, true);
    }

    [ModRPC((uint)ModCalls.UpdateExConvict, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcSendUpdateButton(bool hasLocation, bool doCooldown)
    {
        ExConvict? exConvict = PlayerControl.LocalPlayer.PrimaryRole<ExConvict>();
        if (exConvict == null) return; // Should never be null but just in case.
        exConvict.location = hasLocation ? Vector2.zero : null; // Doesn't really matter about what we set as this value isn't ever read by the client.
        exConvict.UpdatePetButton(exConvict.UIManager.PetButton);
        if (!doCooldown) return;
        if (hasLocation) exConvict.canEscapeCooldown.Start();
        else exConvict.canMarkCooldown.Start();
    }

    private RoleButton UpdatePetButton(IRoleButtonEditor editor) => location == null
        ? editor
            .SetText(Escapist.Translations.MarkButtonText)
            .BindCooldown(canMarkCooldown)
            .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/ex-convict_mark.png", 130, true))
        : editor
            .SetText(Escapist.Translations.EscapeButtonText)
            .BindCooldown(canEscapeCooldown)
            .SetSprite(() => LotusAssets.LoadSprite("Buttons/Crew/ex-convict_teleport.png", 130, true));

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .KeyName("Cooldown After Mark", Translations.Options.MarkCooldown)
                .AddFloatRange(0, 60, 2.5f, 2, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(canEscapeCooldown.SetDuration)
                .Build())
            .SubOption(sub => sub
                .KeyName("Cooldown After Escape", Translations.Options.EscapeCooldown)
                .AddFloatRange(0, 180, 2.5f, 16, GeneralOptionTranslations.SecondsSuffix)
                .BindFloat(canMarkCooldown.SetDuration)
                .Build())
            .SubOption(sub => sub
                .KeyName("Clear Mark After Meeting", Translations.Options.ClearAfterMeeting)
                .AddBoolean()
                .BindBool(b => clearMarkAfterMeeting = b)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.43f, 0f, 0.2f)).RoleAbilityFlags(RoleAbilityFlag.UsesPet);

    [Localized(nameof(ExConvict))]
    public static class Translations
    {
        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(MarkCooldown))]
            public static string MarkCooldown = "Cooldown After Mark";

            [Localized(nameof(EscapeCooldown))]
            public static string EscapeCooldown = "Cooldown After Escape";

            [Localized(nameof(ClearAfterMeeting))]
            public static string ClearAfterMeeting = "Clear Mark After Meeting";
        }
    }
}