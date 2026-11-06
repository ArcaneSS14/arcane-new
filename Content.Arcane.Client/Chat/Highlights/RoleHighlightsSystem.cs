using System.Globalization;
using System.IO;
using System.Linq;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Roles;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Content.Arcane.Common.Chat;

namespace Content.Arcane.Client.Chat.Highlights;

public sealed partial class RoleHighlightsSystem : EntitySystem
{
    public const int MaxHighlightsLength = 128;

    private const int CurrentVersion = 1;
    private const int MaxSavedJobs = 256;
    private const long MaxFileSize = 64 * 1024;

    private static readonly ResPath HighlightsPath = new("/highlights.yml");
    private static readonly ResPath BackupPath = new("/highlights.backup.yml");

    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private ISerializationManager _serialization = default!;

    private Dictionary<ProtoId<JobPrototype>, string> _highlights = new();
    private bool _persistenceWritable;
    private RoleHighlightEditorWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetRoleHighlightsEvent>(OnGetRoleHighlights);
        SubscribeLocalEvent<OpenRoleHighlightEditorEvent>((ref OpenRoleHighlightEditorEvent ev) => OnOpenRoleHighlightEditor(ev));

        RecoverInterruptedSave();
    }

    public override void Shutdown()
    {
        _window?.Close();
        _window = null;
        base.Shutdown();
    }

    private void OnGetRoleHighlights(ref GetRoleHighlightsEvent args)
    {
        if (_highlights.TryGetValue(args.Job, out var highlights))
            args.Highlights = highlights;
    }

    private void OnOpenRoleHighlightEditor(OpenRoleHighlightEditorEvent args)
    {
        if (!_prototypes.TryIndex(args.Job, out var job))
            return;

        _window?.Close();

        _highlights.TryGetValue(args.Job, out var savedHighlights);
        var window = new RoleHighlightEditorWindow(
            job,
            GetDefaultHighlights(args.Job),
            savedHighlights,
            highlights => SaveHighlights(args.Job, highlights));

        _window = window;
        window.OnClose += () =>
        {
            if (ReferenceEquals(_window, window))
                _window = null;
        };
        window.OpenCentered();
    }

    private string GetDefaultHighlights(ProtoId<JobPrototype> job)
    {
        var key = RoleHighlightDefaults.GetKey(job.Id);
        if (!Loc.TryGetString(key, out var highlights))
            return string.Empty;

        return RoleHighlightDefaults.Normalize(highlights);
    }

    /// <summary>Persists the highlight override for a job without discarding other jobs.</summary>
    public RoleHighlightsSaveResult SaveHighlights(ProtoId<JobPrototype> job, string value)
    {
        if (!_persistenceWritable || !_prototypes.HasIndex<JobPrototype>(job))
            return RoleHighlightsSaveResult.Failed;

        if (!TryNormalize(value, out var normalized))
            return RoleHighlightsSaveResult.TooLong;

        var updated = new Dictionary<ProtoId<JobPrototype>, string>(_highlights);
        if (string.IsNullOrEmpty(normalized))
            updated.Remove(job);
        else
            updated[job] = normalized;

        try
        {
            WriteHighlights(updated);
            _highlights = updated;
            return RoleHighlightsSaveResult.Success;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save role highlights: {e}");
            return RoleHighlightsSaveResult.Failed;
        }
    }

    private LoadResult LoadHighlights(ResPath path, out Dictionary<ProtoId<JobPrototype>, string> loaded)
    {
        loaded = new();
        try
        {
            if (!_resources.UserData.Exists(path))
                return LoadResult.Missing;

            using var stream = _resources.UserData.OpenRead(path);
            if (stream.Length > MaxFileSize)
                throw new InvalidDataException($"Role highlights file exceeds {MaxFileSize} bytes.");

            using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
            var documents = DataNodeParser.ParseYamlStream(reader).ToArray();
            if (documents.Length != 1)
                throw new InvalidDataException("Role highlights file must contain exactly one YAML document.");

            if (documents[0].Root is not MappingDataNode root)
                throw new InvalidDataException("Role highlights file root must be a mapping.");

            var savedHighlights = ReadSavedHighlights(root);
            if (savedHighlights.Count > MaxSavedJobs)
                throw new InvalidDataException($"Role highlights file contains more than {MaxSavedJobs} jobs.");

            foreach (var (id, value) in savedHighlights)
            {
                if (!_prototypes.HasIndex<JobPrototype>(id) || !TryNormalize(value, out var normalized))
                    continue;

                if (!string.IsNullOrEmpty(normalized))
                    loaded[id] = normalized;
            }

            return LoadResult.Success;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load role highlights from {path}: {e}");
            loaded.Clear();
            return LoadResult.Failed;
        }
    }

    private Dictionary<string, string> ReadSavedHighlights(MappingDataNode root)
    {
        if (root.TryGet("version", out _))
        {
            var saved = _serialization.Read<SavedRoleHighlights>(root, notNullableOverride: true);
            if (saved.Version != CurrentVersion)
                throw new InvalidDataException($"Unsupported role highlights version {saved.Version}.");

            return saved.Highlights;
        }

        // Goob PR #1116 used a sequence with Key/Value fields and comma-separated values.
        if (!root.TryGet("highlights", out var legacyNode))
            throw new InvalidDataException("Role highlights file has no version or highlights data.");

        var legacy = _serialization.Read<LegacyRoleHighlight[]>(legacyNode, notNullableOverride: false);
        var migrated = new Dictionary<string, string>();
        foreach (var entry in legacy)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
                continue;

            migrated[entry.Key] = entry.Value.Replace(", ", "\n", StringComparison.Ordinal);
        }

        return migrated;
    }

    private void WriteHighlights(Dictionary<ProtoId<JobPrototype>, string> highlights)
    {
        var data = new SavedRoleHighlights
        {
            Version = CurrentVersion,
            Highlights = highlights.ToDictionary(entry => entry.Key.Id, entry => entry.Value),
        };
        var node = _serialization.WriteValue(data, alwaysWrite: true, notNullableOverride: true);
        var temporaryPath = new ResPath($"/highlights.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var writer = _resources.UserData.OpenWriteText(temporaryPath))
            {
                node.Write(writer);
                writer.Flush();
            }

            ReplaceHighlightsFile(temporaryPath);
        }
        finally
        {
            if (_resources.UserData.Exists(temporaryPath))
                _resources.UserData.Delete(temporaryPath);
        }
    }

    private void ReplaceHighlightsFile(ResPath temporaryPath)
    {
        var hadOriginal = _resources.UserData.Exists(HighlightsPath);
        if (hadOriginal)
        {
            if (_resources.UserData.Exists(BackupPath))
                _resources.UserData.Delete(BackupPath);
            _resources.UserData.Rename(HighlightsPath, BackupPath);
        }

        try
        {
            _resources.UserData.Rename(temporaryPath, HighlightsPath);
        }
        catch
        {
            if (hadOriginal &&
                !_resources.UserData.Exists(HighlightsPath) &&
                _resources.UserData.Exists(BackupPath))
            {
                _resources.UserData.Rename(BackupPath, HighlightsPath);
            }

            throw;
        }

        if (_resources.UserData.Exists(BackupPath))
        {
            try
            {
                _resources.UserData.Delete(BackupPath);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to remove role highlights backup: {e}");
            }
        }
    }

    private void RecoverInterruptedSave()
    {
        var primary = LoadHighlights(HighlightsPath, out var loaded);
        if (primary == LoadResult.Success)
        {
            _highlights = loaded;
            _persistenceWritable = true;
            try
            {
                if (_resources.UserData.Exists(BackupPath))
                    _resources.UserData.Delete(BackupPath);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to remove role highlights backup: {e}");
            }
            return;
        }

        var backup = LoadHighlights(BackupPath, out loaded);
        if (backup == LoadResult.Success)
        {
            _highlights = loaded;
            _persistenceWritable = primary == LoadResult.Missing || BackupInvalidFile();
            return;
        }

        if (backup == LoadResult.Failed)
            return;

        _persistenceWritable = primary == LoadResult.Missing || BackupInvalidFile();
    }

    private bool BackupInvalidFile()
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            _resources.UserData.Rename(
                HighlightsPath,
                new ResPath($"/highlights.invalid-{timestamp}.yml"));
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to back up invalid role highlights file: {e}");
            return false;
        }
    }

    private enum LoadResult : byte
    {
        Missing,
        Success,
        Failed,
    }

    private static bool TryNormalize(string value, out string normalized)
    {
        normalized = string.Empty;
        if (value.Length > MaxHighlightsLength)
            return false;

        var lines = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        normalized = string.Join('\n', lines);
        return normalized.Length <= MaxHighlightsLength;
    }

    [DataDefinition]
    public sealed partial class SavedRoleHighlights
    {
        [DataField("version")]
        public int Version { get; set; }

        [DataField("highlights")]
        public Dictionary<string, string> Highlights { get; set; } = new();
    }

    [DataDefinition]
    public sealed partial class LegacyRoleHighlight
    {
        [DataField("Key")]
        public string? Key { get; set; }

        [DataField("Value")]
        public string? Value { get; set; }
    }
}

public enum RoleHighlightsSaveResult : byte
{
    Success,
    TooLong,
    Failed,
}
