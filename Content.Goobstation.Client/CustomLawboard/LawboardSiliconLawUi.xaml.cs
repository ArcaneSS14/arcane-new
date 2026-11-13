// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.FixedPoint;
using Content.Shared.Silicons.Laws;
using Content.Goobstation.Shared.CustomLawboard;
using Robust.Client.UserInterface;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Goobstation.Client.CustomLawboard;

[GenerateTypedNameReferences]
public sealed partial class LawboardSiliconLawUi : FancyWindow
{
    [Dependency] private IFileDialogManager _fileDialog = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private ILogManager _logManager = default!;

    private const int MaxLawFileBytes = 128 * 1024;
    private static readonly FileDialogFilters LawFileFilters = new(new FileDialogFilters.Group("yml", "yaml"));
    private List<SiliconLaw> _laws = new();

    public event Action<List<SiliconLaw>, bool>? LawsChangedEvent;

    public LawboardSiliconLawUi()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this); // Arcane
        NewLawButton.OnPressed += _ => AddNewLaw();
        Save.OnPressed += _ => SaveLaws();

        Import.OnPressed += _ => ImportLaws(); // Arcane
        Export.OnPressed += _ => ExportLaws();
    }

    private void AddNewLaw()
    {
        var newLaw = new SiliconLaw();
        newLaw.Order = FixedPoint2.New(_laws.Count + 1);
        _laws.Add(newLaw);
        SetLaws(_laws);
    }

    public void SaveLaws()
    {
        Save.Disabled = true;
        Timer.Spawn(500, () => Save.Disabled = false); // This is so it feels like it did something. It's completely unnecessary but it adds some feedback

        ProcessLawsChanged(true);
    }

    public void ProcessLawsChanged(bool popup = false)
    {
        LawsChangedEvent?.Invoke(_laws, popup); // The popup is just for flavor. There's probably a better way to do this
    }

    public void SetLaws(List<SiliconLaw> sLaws, bool notify = true)
    {
        _laws = sLaws;
        LawContainer.RemoveAllChildren();

        foreach (var law in sLaws.OrderBy(l => l.Order))
        {
            var lawControl = new LawboardSiliconLawContainer();
            lawControl.SetLaw(law);
            lawControl.MoveLawDown += MoveLawDown;
            lawControl.MoveLawUp += MoveLawUp;
            lawControl.DeleteAction += DeleteLaw;
            lawControl.ChangeLawEvent += ChangeLaw;

            LawContainer.AddChild(lawControl);
        }

        NewLawButton.Disabled = sLaws.Count >= SharedCustomLawboardSystem.MaxLaws;

        if (notify)
            ProcessLawsChanged();
    }

    public void DeleteLaw(SiliconLaw law)
    {
        _laws.Remove(law);
        SetLaws(_laws);
    }

    public void MoveLawDown(SiliconLaw law)
    {
        if (_laws.Count == 0)
        {
            return;
        }

        var index = _laws.IndexOf(law);
        if (index == -1)
        {
            return;
        }

        _laws[index].Order += FixedPoint2.New(1);
        SetLaws(_laws);
    }

    public void MoveLawUp(SiliconLaw law)
    {
        if (_laws.Count == 0)
        {
            return;
        }

        var index = _laws.IndexOf(law);
        if (index == -1)
        {
            return;
        }

        _laws[index].Order += FixedPoint2.New(-1);
        SetLaws(_laws);
    }

    public void ChangeLaw(SiliconLaw law)
    {
        if (_laws.Count == 0)
        {
            return;
        }

        var index = _laws.IndexOf(law);
        if (index == -1)
        {
            return;
        }

        if (law.LawString.Length > SharedCustomLawboardSystem.MaxLawLength)
        {
            law.LawString = law.LawString[..SharedCustomLawboardSystem.MaxLawLength]; // Makes the law have a max length to prevent the Horrors of a 9000 character law.
        }

        _laws[index].LawString = law.LawString;
    }

    public List<SiliconLaw> GetLaws()
    {
        return _laws;
    }

    private async void ImportLaws()
    {
        SetFileButtonsDisabled(true);
        try
        {
            await using var file = await _fileDialog.OpenFile(LawFileFilters, FileAccess.Read);
            if (file == null || !IsOpen)
                return;

            // Parse only the small flat document format that the law editor exports.
            using var reader = new StreamReader(file, EncodingHelpers.UTF8);
            var text = new char[MaxLawFileBytes + 1];
            if (!file.CanSeek || file.Length > MaxLawFileBytes)
                throw new InvalidDataException("Lawboard file exceeds the size limit.");

            var length = await reader.ReadBlockAsync(text);
            if (!IsOpen)
                return;
            if (length > MaxLawFileBytes)
                throw new InvalidDataException("Lawboard file exceeds the size limit.");

            var contents = new string(text, 0, length);
            using var input = new StringReader(contents);
            var yaml = new YamlStream();
            yaml.Load(input);
            if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
                throw new InvalidDataException("Expected a single lawset mapping.");

            ValidateLawMapping(root);
            var lawset = _serialization.Read<SiliconLawset>(root.ToDataNodeCast<MappingDataNode>(), notNullableOverride: true);
            SetLaws(SharedCustomLawboardSystem.SanitizeLaws(lawset.Laws), notify: false);
            ProcessLawsChanged(true);
        }
        catch (Exception error)
        {
            _logManager.GetSawmill("customlawboard-ui").Warning($"Lawboard import failed: {error.Message}");
            SetFileError("custom-lawboard-import-failed");
        }
        finally
        {
            SetFileButtonsDisabled(false);
        }
    }

    private static void ValidateLawMapping(YamlMappingNode root)
    {
        var seen = new HashSet<YamlNode>(ReferenceEqualityComparer.Instance) { root };
        foreach (var (key, value) in root)
        {
            if (!seen.Add(key) || !seen.Add(value) || key is not YamlScalarNode keyName)
                throw new InvalidDataException("Invalid lawset field.");

            switch (keyName.Value)
            {
                case "obeysTo" when value is YamlScalarNode:
                    break;
                case "laws" when value is YamlSequenceNode sequence && sequence.Children.Count <= SharedCustomLawboardSystem.MaxLaws:
                    foreach (var entry in sequence.Children)
                    {
                        if (!seen.Add(entry) || entry is not YamlMappingNode law)
                            throw new InvalidDataException("Expected a flat mapping per law.");

                        foreach (var (lawKey, lawValue) in law)
                        {
                            if (!seen.Add(lawKey) || !seen.Add(lawValue) || lawKey is not YamlScalarNode name ||
                                name.Value is not ("lawString" or "order" or "lawIdentifierOverride" or "corrupted") ||
                                lawValue is not YamlScalarNode)
                                throw new InvalidDataException("Invalid law field.");
                        }
                    }
                    break;
                default:
                    throw new InvalidDataException("Unknown lawset field.");
            }
        }
    }

    private async void ExportLaws()
    {
        var lawset = new SiliconLawset { Laws = SharedCustomLawboardSystem.SanitizeLaws(_laws) };
        SetFileButtonsDisabled(true);
        try
        {
            if (await _fileDialog.SaveFile(LawFileFilters) is not { } file)
                return;

            await using var writer = new StreamWriter(file.fileStream);
            if (IsOpen)
                _serialization.WriteValue(lawset, notNullableOverride: true).Write(writer);
        }
        catch (Exception error)
        {
            _logManager.GetSawmill("customlawboard-ui").Warning($"Lawboard export failed: {error.Message}");
            SetFileError("custom-lawboard-export-failed");
        }
        finally
        {
            SetFileButtonsDisabled(false);
        }
    }

    private void SetFileButtonsDisabled(bool disabled)
    {
        if (Disposed)
            return;

        Import.Disabled = Export.Disabled = disabled;
        if (disabled)
            FileStatus.Visible = false;
    }

    private void SetFileError(LocId message)
    {
        if (!IsOpen)
            return;

        FileStatus.Text = Loc.GetString(message);
        FileStatus.Visible = true;
    }
}
