namespace Content.Arcane.Client.Medical.Defibrillator;


public sealed class DefibSwitchAnimation(AnimatedTextureRect texture, bool enabled)
{
    private const float SwitchDuration = 0.165f;

    public AnimatedTextureRect Texture { get; } = texture;
    public bool Enabled { get; } = enabled;
    public float TimeLeft { get; set; } = SwitchDuration;
    public bool IsFinished => TimeLeft <= 0f;
}
