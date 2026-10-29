namespace Content.Arcane.Server.Doors;

/// <summary>
/// Seals the extra tiles of a multi-tile airlock with invisible airtight entities that follow the door's state.
/// The offsets are local, before rotation; a 2-tile door drawn with offset 0.5,0 covers +1 local X.
/// </summary>
[RegisterComponent]
public sealed partial class MultiTileAirlockAirtightComponent : Component
{
    [DataField]
    public List<Vector2i> TileOffsets = new() { new Vector2i(1, 0) };

    [DataField]
    public EntProtoId Phantom = "PhantomAirtight";

    [ViewVariables]
    public List<EntityUid> Phantoms = new();
}
