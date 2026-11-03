namespace Content.Arcane.Common.Sponsors;

/// <summary>
/// Optional sponsor info window. Implement it and register it in IoC to show the sponsor button in character setup.
/// </summary>
public interface ISponsorWindowCreator
{
    public void OpenWindow();
}
