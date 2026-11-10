using Lotus.GUI;
using Lotus.GUI.Name;
using Lotus.Options;
using Lotus.Roles.Internals.Enums;
using Lotus.Roles.Internals.Attributes;
using Lotus.Roles.RoleGroups.Vanilla;
using Lotus.Utilities;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.UI;
using VentLib.Localization.Attributes;
using Lotus.Extensions;
using Lotus.Roles.GUI;
using Lotus.Roles.GUI.Interfaces;
using Lotus.RPC;
using VentLib;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Extensions;

namespace Lotus.Roles.RoleGroups.Impostors;

public class Miner : Impostor, IRoleUI
{
    private static readonly StandardLogger log = LoggerFactory.GetLogger<StandardLogger>(typeof(Miner));

    [UIComponent(UI.Cooldown)]
    private Cooldown minerAbilityCooldown;

    private Vector2 lastEnteredVentLocation = Vector2.zero;

    public RoleButton PetButton(IRoleButtonEditor petButton) => petButton
        .SetText(Translations.ButtonText)
        .BindCooldown(minerAbilityCooldown)
        .SetSprite(() => LotusAssets.LoadSprite("Buttons/Imp/miner_mine.png", 130, true));

    [RoleAction(LotusActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(LotusActionType.VentEntered)]
    private void EnterVent(Vent vent)
    {
        lastEnteredVentLocation = vent.transform.position;
    }

    [RoleAction(LotusActionType.OnPet)]
    public void MinerVentAction()
    {
        if (minerAbilityCooldown.NotReady()) return;
        minerAbilityCooldown.Start();
        if (MyPlayer.IsModded()) Vents.FindRPC((uint)ModCalls.UpdateMiner)?.Send([MyPlayer.OwnerId]);

        if (lastEnteredVentLocation == Vector2.zero) return;
        log.Trace($"{MyPlayer.Data.PlayerName}:{lastEnteredVentLocation}", "MinerTeleport");
        Utils.Teleport(MyPlayer.NetTransform, new Vector2(lastEnteredVentLocation.x, lastEnteredVentLocation.y + 0.3636f), this);
    }

    [ModRPC((uint)ModCalls.UpdateMiner, RpcActors.Host, RpcActors.NonHosts)]
    private static void RpcUpdateMiner()
    {
        Miner? miner = PlayerControl.LocalPlayer.PrimaryRole<Miner>();
        if (miner == null) return;
        miner.minerAbilityCooldown.Start();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).SubOption(sub =>
            sub.KeyName("Miner Ability Cooldown", Translations.Options.AbilityCooldown)
                .BindFloat(minerAbilityCooldown.SetDuration)
                .AddFloatRange(5, 50, 2.5f, 5, GeneralOptionTranslations.SecondsSuffix)
                .Build());

    [Localized(nameof(Miner))]
    public static class Translations
    {
        [Localized(nameof(ButtonText))]
        public static string ButtonText = "Mine";

        [Localized(ModConstants.Options)]
        public static class Options
        {
            [Localized(nameof(AbilityCooldown))]
            public static string AbilityCooldown = "Miner Ability Cooldown";
        }
    }
}