namespace Content.Shared.StationRecords;

public sealed partial record GeneralStationRecord
{
    /// <summary>
    /// Custom display species name of this character, empty when none.
    /// </summary>
    [DataField]
    public string CustomSpeciesName = string.Empty;
}
