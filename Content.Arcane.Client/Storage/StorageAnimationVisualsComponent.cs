using Robust.Client.Graphics;

namespace Content.Arcane.Client.Storage;

/// <summary>
/// Opts compatible storage artwork into a reversible, client-only door transition.
/// </summary>
[RegisterComponent, Access(typeof(StorageAnimationSystem))]
public sealed partial class StorageAnimationVisualsComponent : Component
{
    /// <summary>Only these RSI paths may use this geometry, including after repainting.</summary>
    [DataField(required: true)]
    public HashSet<ResPath> Sprites = [];

    /// <summary>Only these open states contain a separate compatible door.</summary>
    [DataField(required: true)]
    public HashSet<string> OpenStates = [];

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0.2);

    /// <summary>Hinge in sprite-local metres, before the owner's sprite offset.</summary>
    [DataField]
    public Vector2 Hinge = new(-6.5f / 32f, 0);

    /// <summary>Geometry overrides for differently sized crate artwork.</summary>
    [DataField]
    public Dictionary<ResPath, Vector2> Hinges = [];

    /// <summary>Row separating lid tint from stationary body tint in composite overlay states.</summary>
    [DataField]
    public Dictionary<ResPath, int> OverlayCutoffs = [];

    /// <summary>Composite mouth artwork: rows below this point stay on the body.</summary>
    [DataField]
    public Dictionary<ResPath, int> BodyCutoffs = [];

    public Vector2 ResolvedHinge;
    public int OverlayCutoff;
    public EntityUid? FixedOverlay;
    public int BodyCutoff;
    public int? HiddenBaseLayer;
    public Texture? FrontTexture;
    public Texture? BackTexture;

    [DataField]
    public float OpenAngle = 140f;

    [DataField]
    public float Squish = 0.2f;

    /// <summary>Lift a crate lid instead of swinging a closet door sideways.</summary>
    [DataField]
    public bool Lid;

    public bool AppearanceInitialized;
    public bool Open;
    public bool Active;
    public float Phase;
    public EntityUid? Door;
    public int DoorLayer;
    public string ClosedState = string.Empty;
    public string OpenState = string.Empty;
    public readonly List<StorageAnimationLayer> Layers = [];
}

public readonly record struct StorageAnimationLayer(int Index, Vector2 Offset, Angle Rotation, Vector2 Scale, bool Visible, bool BackOnly = false);
