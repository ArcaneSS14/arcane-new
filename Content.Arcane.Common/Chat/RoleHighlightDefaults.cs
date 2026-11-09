namespace Content.Arcane.Common.Chat;

public static class RoleHighlightDefaults
{
    /// <summary>
    /// Builds the locale key from a job ID.
    /// </summary>
    public static string GetKey(string jobId) => $"highlights-{jobId.Replace(' ', '-').ToLowerInvariant()}";

    public static string Normalize(string value) => string.Join(
        '\n', value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
