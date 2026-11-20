using System.Linq;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using System.IO; // Arcane

namespace Content.Client.Changelog
{
    public sealed partial class ChangelogManager : IPostInjectInit
    {
        [Dependency] private ILogManager _logManager = default!;
        [Dependency] private IResourceManager _resource = default!;
        [Dependency] private ISerializationManager _serialization = default!;
        [Dependency] private IConfigurationManager _configManager = default!;

        private const string SawmillName = "changelog";
        public const string MainChangelogName = "Arcane"; // Arcane-Edit: Traumalog > Arcane

        private ISawmill _sawmill = default!;

        // Arcane-Start: published snapshots can assign different numeric IDs to the same PR.
        private const string ReadEntriesHeader = "arcane-changelog-read-v2";
        private readonly HashSet<string> _readEntryKeys = new();
        private HashSet<string> _entryKeys = new();
        private ResPath ReadEntriesPath => new($"/changelog_read_v2_{MainChangelogName}_{_configManager.GetCVar(CCVars.ServerId)}");

        private static string EntryKey(ChangelogEntry entry) =>
            string.IsNullOrWhiteSpace(entry.Url) ? $"id:{entry.Id}" : entry.Url;

        public bool IsRead(ChangelogEntry entry) => _readEntryKeys.Contains(EntryKey(entry));
        // Arcane-End

        public bool NewChangelogEntries { get; private set; }
        public int LastReadId { get; private set; }
        public int MaxId { get; private set; }

        public event Action? NewChangelogEntriesChanged;

        /// <summary>
        ///     Ran when the user opens ("read") the changelog,
        ///     stores the new ID to disk and clears <see cref="NewChangelogEntries"/>.
        /// </summary>
        /// <remarks>
        ///     <see cref="LastReadId"/> is NOT cleared
        ///     since that's used in the changelog menu to show the "since you last read" bar.
        /// </remarks>
        public void SaveNewReadId()
        {
            NewChangelogEntries = false;
            NewChangelogEntriesChanged?.Invoke();

            using var sw = _resource.UserData.OpenWriteText(new ($"/changelog_last_seen_{MainChangelogName}_{_configManager.GetCVar(CCVars.ServerId)}")); // Arcane-Edit: isolate read IDs from upstream journals

            sw.Write(MaxId.ToString());

            // Arcane-Start: keep the in-memory read snapshot for the window's unread divider.
            using TextWriter entriesWriter = _resource.UserData.OpenWriteText(ReadEntriesPath);
            entriesWriter.WriteLine(ReadEntriesHeader);
            foreach (var key in _readEntryKeys.Union(_entryKeys).OrderBy(key => key))
            {
                entriesWriter.WriteLine(key);
            }
            // Arcane-End
        }

        public async void Initialize()
        {
            // Open changelog purely to compare to the last viewed date.
            var changelogs = await LoadChangelog();
            UpdateChangelogs(changelogs);
            _configManager.OnValueChanged(CCVars.ServerId, OnServerIdCVarChanged);
        }

        private void UpdateChangelogs(List<Changelog> changelogs)
        {
            if (changelogs.Count == 0)
            {
                return;
            }

            var mainChangelogs = changelogs.Where(c => c.Name == MainChangelogName).ToArray();
            if (mainChangelogs.Length == 0)
            {
                _sawmill.Error($"No changelog file found in Resources/Changelog with name {MainChangelogName}");
                return;
            }

            var changelog = mainChangelogs[0]; // Arcane-Edit: use the named journal regardless of tab ordering
            if (mainChangelogs.Length > 1)
            {
                _sawmill.Error($"More than one file found in Resource/Changelog with name {MainChangelogName}");
            }

            if (changelog.Entries.Count == 0)
            {
                return;
            }

            MaxId = changelog.Entries.Max(c => c.Id);
            _entryKeys = changelog.Entries.Select(EntryKey).ToHashSet(); // Arcane

            CheckLastSeenEntry();
        }

        private void CheckLastSeenEntry()
        {
            // Arcane-Edit-Start: isolate journals and reset absent or damaged per-server read markers.
            var path = new ResPath($"/changelog_last_seen_{MainChangelogName}_{_configManager.GetCVar(CCVars.ServerId)}");
            LastReadId = _resource.UserData.TryReadAllText(path, out var lastReadIdText) &&
                         int.TryParse(lastReadIdText, out var lastReadId)
                ? Math.Max(0, lastReadId)
                : 0;
            // Arcane-Edit-End

            // Arcane-Edit-Start: a new file deliberately avoids guessing URL identities from old IDs.
            _readEntryKeys.Clear();
            if (_resource.UserData.TryReadAllText(ReadEntriesPath, out var readEntriesText))
            {
                using var entriesReader = new StringReader(readEntriesText);
                if (entriesReader.ReadLine() == ReadEntriesHeader)
                {
                    while (entriesReader.ReadLine() is { } line)
                    {
                        var key = line.Trim();
                        if (!string.IsNullOrEmpty(key))
                            _readEntryKeys.Add(key);
                    }
                }
            }

            NewChangelogEntries = !_entryKeys.IsSubsetOf(_readEntryKeys);
            // Arcane-Edit-End

            NewChangelogEntriesChanged?.Invoke();
        }

        private void OnServerIdCVarChanged(string newValue)
        {
            CheckLastSeenEntry();
        }

        public Task<List<Changelog>> LoadChangelog()
        {
            return Task.Run(() =>
            {
                var changelogs = new List<Changelog>();
                var directory = new ResPath("/Changelog");
                foreach (var file in _resource.ContentFindFiles(new ResPath("/Changelog/")))
                {
                    if (file.Directory != directory || file.Extension != "yml")
                        continue;

                    var yamlData = _resource.ContentFileReadYaml(file);

                    if (yamlData.Documents.Count == 0)
                        continue;

                    var node = yamlData.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
                    var changelog = _serialization.Read<Changelog>(node, notNullableOverride: true);
                    if (string.IsNullOrWhiteSpace(changelog.Name))
                        changelog.Name = file.FilenameWithoutExtension;

                    changelogs.Add(changelog);
                }

                changelogs.Sort((a, b) => a.Order.CompareTo(b.Order));
                return changelogs;
            });
        }

        public void PostInject()
        {
            _sawmill = _logManager.GetSawmill(SawmillName);
        }

        /// <summary>
        ///     Tries to return a human-readable version number from the build.json file
        /// </summary>
        public string GetClientVersion()
        {
            var fork = _configManager.GetCVar(CVars.BuildForkId);
            var version = _configManager.GetCVar(CVars.BuildVersion);

            // This trimming might become annoying if down the line some codebases want to switch to a real
            // version format like "104.11.3" while others are still using the git hashes
            if (version.Length > 7)
                version = version[..7];

            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(fork))
                return Loc.GetString("changelog-version-unknown");

            return Loc.GetString("changelog-version-tag",
                ("fork", fork),
                ("version", version));
        }

        [DataDefinition]
        public sealed partial class Changelog
        {
            /// <summary>
            ///     The name to use for this changelog.
            ///     If left unspecified, the name of the file is used instead.
            ///     Used during localization to find the user-displayed name of this changelog.
            /// </summary>
            [DataField("Name")]
            public string Name = string.Empty;

            /// <summary>
            ///     The individual entries in this changelog.
            ///     These are not kept around in memory in the changelog manager.
            /// </summary>
            [DataField("Entries")]
            public List<ChangelogEntry> Entries = new();

            /// <summary>
            ///     Whether or not this changelog will be displayed as a tab to non-admins.
            ///     These are still loaded by all clients, but not shown if they aren't an admin,
            ///     as they do not contain sensitive data and are publicly visible on GitHub.
            /// </summary>
            [DataField("AdminOnly")]
            public bool AdminOnly;

            /// <summary>
            ///     Used when ordering the changelog tabs for the user to see.
            ///     Larger numbers are displayed later, smaller numbers are displayed earlier.
            /// </summary>
            [DataField("Order")]
            public int Order;
        }

        [DataDefinition]
        public sealed partial class ChangelogEntry
        {
            [DataField]
            public int Id { get; private set; }

            [DataField]
            public string Author { get; private set; } = "";

            [DataField]
            public DateTime Time { get; private set; }

            [DataField]
            public List<ChangelogChange> Changes { get; private set; } = default!;

            /// <summary>
            ///     Labels attached to the related PR, passed on via the SS14.Changelog parser.
            /// </summary>
            [DataField]
            public List<string> Labels { get; private set; } = [];

            // Arcane-Start
            [DataField]
            public string Url { get; private set; } = "";
            // Arcane-End
        }

        [DataDefinition]
        public sealed partial class ChangelogChange
        {
            [DataField("type")]
            public ChangelogLineType Type { get; private set; }

            [DataField("message")]
            public string Message { get; private set; } = "";
        }

        public enum ChangelogLineType
        {
            Add,
            Remove,
            Fix,
            Tweak,
        }
    }
}
