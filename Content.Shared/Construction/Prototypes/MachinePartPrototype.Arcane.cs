// Arcane-Start
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Prototypes;

[Prototype]
public sealed partial class MachinePartPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public EntProtoId StockPartPrototype = string.Empty;
}
// Arcane-End
