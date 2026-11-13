namespace Content.Arcane.Shared.Achievements;

/// <summary>Temporary play bodies, such as arena avatars, must not earn action achievements.</summary>
[RegisterComponent]
public sealed partial class NoAchievementsComponent : Component;
