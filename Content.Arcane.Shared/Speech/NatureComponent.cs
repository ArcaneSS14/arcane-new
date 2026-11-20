using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Arcane.Shared.Speech;

[RegisterComponent]
public sealed partial class NatureComponent : Component
{
    [DataField]
    public ProtoId<TagPrototype>? emoteTag;

    [DataField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? newSounds;

    public bool AddedTag;
}
