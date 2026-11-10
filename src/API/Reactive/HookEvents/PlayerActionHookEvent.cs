using Hazel;
using InnerNet;
using Lotus.Roles.Internals;
using Lotus.Extensions;
using Lotus.Roles.Operations;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerActionHookEvent : INetworkedHookEvent
{
    public PlayerControl Source;
    public RoleAction Action;
    public object[] Params;

    PlayerActionHookEvent()
    {

    }

    public PlayerActionHookEvent(PlayerControl source, RoleAction action, object[] parameters)
    {
        Source = source;
        Action = action;
        Params = parameters;
    }

    public void Trigger()
    {
        ActionHandle handle = ActionHandle.NoInit();
        RoleOperations.Current.TriggerFor(new[] { Source }, Action.ActionType, Source, handle, Params);
    }


    public void Read(MessageReader reader)
    {
        Source = reader.ReadNetObject<PlayerControl>();

        int itemCount = reader.ReadInt32();
        for (int i = 0; i < itemCount; i++)
        {
            // reader.read
        }
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.Write(Source);
        int totalItemCount = 0;

        writer.Write(totalItemCount);
    }
}