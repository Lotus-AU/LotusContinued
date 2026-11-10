using Hazel;
using VentLib.Networking.Interfaces;
using System;
using System.Reflection;

namespace Lotus.Managers.History.Events;

public interface IDeathEvent : IRecipientEvent, IRpcWritable//, IRpcReadable<IDeathEvent>
{
    internal static IDeathEvent ReadDeathEvent(MessageReader reader)
    {
        string qualifiedName;
        try
        {
            qualifiedName = reader.ReadString();
        }
        catch
        {
            qualifiedName = "N/A";
        }
        if (qualifiedName == "N/A") throw new InvalidOperationException("A death event's Write function wasn't setup to send a type name!");
        Type? targetType = Type.GetType(qualifiedName);
        if (targetType == null) throw new InvalidOperationException($"Unknown death event type '{qualifiedName}'.");
        object? targetEvent = Activator.CreateInstance(targetType, nonPublic: true);
        if (targetEvent is not IDeathEvent @event) throw new InvalidOperationException($"{qualifiedName} is not an IDeathEvent.");
        @event.Read(reader);
        return @event;
    }

    /// <summary>
    /// Non-descriptive name of the event IE "Exiled", "Murdered", "Suicide", etc
    /// </summary>
    /// <returns>Simple name of the event</returns>
    public string SimpleName();

    void Read(MessageReader reader);
}