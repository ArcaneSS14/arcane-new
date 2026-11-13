// Arcane-Start
namespace Content.Goobstation.Server.Blob;

/// <summary>Raised by the lifecycle owner before it queues cleanup of the core and blob rule.</summary>
[ByRefEvent]
public readonly record struct BlobCoreShutdownEvent;
// Arcane-End
