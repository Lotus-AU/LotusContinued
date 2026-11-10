using System.Linq;
using Hazel;
using InnerNet;

namespace Lotus.API.Reactive.HookEvents;

public class PlayerTaskHookEvent: INetworkedHookEvent
{
    public PlayerControl Player;
    public NormalPlayerTask? PlayerTask;

    PlayerTaskHookEvent()
    {

    }

    public PlayerTaskHookEvent(PlayerControl player, NormalPlayerTask? playerTask)
    {
        Player = player;
        PlayerTask = playerTask;
    }

    public void Read(MessageReader reader)
    {
        Player = reader.ReadNetObject<PlayerControl>();
        if (!reader.ReadBoolean())
        {
            PlayerTask = null;
            return;
        }

        uint taskId = reader.ReadUInt32();
        var taskArray = Player.Data.Tasks.ToArray().ToList();
        NetworkedPlayerInfo.TaskInfo? foundTaskIndex = taskArray.FirstOrDefault(t => t.Id == taskId);
        if (foundTaskIndex == null) return;
        NormalPlayerTask foundTask = ShipStatus.Instance.GetTaskById((byte)taskId);
        if (foundTask) PlayerTask = foundTask;
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        writer.WriteNetObject(Player);
        if (PlayerTask )
        {
            writer.Write(true);
            writer.Write(PlayerTask!.Id);
        } else writer.Write(false);
    }
}