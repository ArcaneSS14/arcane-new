// Arcane-Start
namespace Content.Server.GameTicking.Events;

/// <summary>Allows a content queue to exclude waiting connections from population-dependent checks.</summary>
[ByRefEvent]
public struct GetAdmittedPlayerCountEvent(int count)
{
    public int Count = count;
}
// Arcane-End
