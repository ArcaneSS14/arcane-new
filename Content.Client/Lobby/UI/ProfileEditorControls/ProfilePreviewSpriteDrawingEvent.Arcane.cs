namespace Content.Client.Lobby.UI.ProfileEditorControls;

/// <summary>
/// Lets client visual systems prepare the preview entity for its actual render direction.
/// </summary>
[ByRefEvent]
public readonly record struct ProfilePreviewSpriteDrawingEvent(Direction? Direction, Angle Rotation);
