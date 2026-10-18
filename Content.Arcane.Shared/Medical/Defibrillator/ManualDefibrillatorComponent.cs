namespace Content.Arcane.Shared.Medical.Defibrillator;

[RegisterComponent]
public sealed partial class ManualDefibrillatorComponent : Component
{
    [DataField]
    public int Mode = 3;

    [DataField]
    public Dictionary<int, float> BpmZapMode = new()
    {
        { 1, -80f }, { 2, -50f }, { 3, 150f }, { 4, 200f },
    };

    [DataField]
    public Dictionary<int, float> BpmZapFlatlineMode = new()
    {
        { 1, 0f }, { 2, 0f }, { 3, 200f }, { 4, 300f },
    };
}
