using Hazel;
using Lotus.API.Odyssey;
using Lotus.Roles;
using Lotus.API.Player;
using Lotus.Extensions;
using VentLib.Utilities.Optionals;

namespace Lotus.Managers.History.Events;

public class DeathEvent : IDeathEvent
{
    private FrozenPlayer? deadPlayer;
    private CustomRole playerRole;
    private Optional<FrozenPlayer> killer;
    private Optional<CustomRole> killerRole;

    private Timestamp timestamp = new();

    public DeathEvent()
    {

    }

    public DeathEvent(PlayerControl deadPlayer, PlayerControl? killer)
    {
        this.deadPlayer = Game.MatchData.GetFrozenPlayer(deadPlayer);
        playerRole = deadPlayer.PrimaryRole();
        this.killer = Optional<FrozenPlayer>.Of(Game.MatchData.GetFrozenPlayer(killer));
        this.killerRole = this.killer.Map(p => Optional<CustomRole>.Of(p.MyPlayer.PrimaryRole()).OrElse(p.MainRole));
    }

    public virtual void Read(MessageReader reader)
    {
        if (reader.ReadBoolean())
            deadPlayer = new FrozenPlayer().Read(reader);
        playerRole = GlobalRoleManager.Instance.GetRoleFromId(reader.ReadInt32());
        if (reader.ReadBoolean())
            killer = Optional<FrozenPlayer>.NonNull(new FrozenPlayer().Read(reader));
        else
            killer = Optional<FrozenPlayer>.Null();

        if (reader.ReadBoolean())
            killerRole = Optional<CustomRole>.NonNull(GlobalRoleManager.Instance.GetRoleFromId(reader.ReadInt32()));
        else
            killerRole = Optional<CustomRole>.Null();
    }

    public virtual void Write(MessageWriter writer)
    {
        writer.Write(GetType().AssemblyQualifiedName);
        if (deadPlayer != null)
        {
            writer.Write(true);
            deadPlayer.Write(writer);
        } else writer.Write(false);
        playerRole.Write(writer);

        if (killer.Exists())
        {
            writer.Write(true);
            killer.Get().Write(writer);
        } else writer.Write(false);
        if (killerRole.Exists())
        {
            writer.Write(true);
            killerRole.Get().Write(writer);
        } else writer.Write(false);

    }

    public FrozenPlayer Player() => deadPlayer!;

    public Optional<CustomRole> RelatedRole() => Optional<CustomRole>.NonNull(playerRole);

    public Timestamp Timestamp() => timestamp;

    public bool IsCompletion() => true;

    public virtual string Message()
    {
        string baseMessage = $"{Game.GetName(deadPlayer)} was {SimpleName().ToLower()}";
        return killer.Transform(klr => baseMessage + $" by {klr.Name}.", () => baseMessage + ".");
    }

    public Optional<FrozenPlayer> Instigator() => killer;

    public Optional<CustomRole> InstigatorRole() => killerRole;

    public virtual string SimpleName() => ModConstants.DeathNames.Killed;
}