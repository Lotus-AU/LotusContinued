using Hazel;
using InnerNet;
using Lotus.Managers;
using Lotus.Roles;
using UnityEngine;
using VentLib.Utilities.Optionals;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerTeleportedHookEvent: PlayerHookEvent
{
    public Vector2 OriginalLocation;
    public Vector2 NewLocation;

    public Optional<CustomRole> Initiator;

    PlayerTeleportedHookEvent()
    {

    }

    public PlayerTeleportedHookEvent(PlayerControl player, Vector2 originalLocation, Vector2 newLocation, CustomRole? initiatorRole) : base(player)
    {
        OriginalLocation = originalLocation;
        NewLocation = newLocation;
        Initiator = Optional<CustomRole>.Of(initiatorRole);
    }

    public override void Read(MessageReader reader)
    {
        Player =  reader.ReadNetObject<PlayerControl>();
        OriginalLocation = new Vector2(
            reader.ReadSingle(),
            reader.ReadSingle());

        NewLocation = new Vector2(
            reader.ReadSingle(),
            reader.ReadSingle());

        if (reader.ReadBoolean())
            Initiator = Optional<CustomRole>.NonNull(GlobalRoleManager.Instance.GetRoleFromId(reader.ReadInt32()));
        else
            Initiator = Optional<CustomRole>.Null();
    }

    public override void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Player);
        writer.Write(OriginalLocation.x);
        writer.Write(OriginalLocation.y);
        writer.Write(NewLocation.x);
        writer.Write(NewLocation.y);

        if (Initiator.Exists())
        {
            writer.Write(true);
            Initiator.Get().Write(writer);
        } else writer.Write(false);
    }
}