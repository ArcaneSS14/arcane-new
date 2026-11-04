// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Content.Arcane.Common.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Arcane.Client.Serialization;

/// <summary>
/// Copies the components of every entity prototype once while the game is loading.
/// </summary>
/// <remarks>
/// The first copy of a type compiles its delegates on the main thread, which is what makes the first entity of each
/// kind so expensive. Without this, that work happens mid-round whenever a player walks into a new area.
/// </remarks>
public sealed partial class ComponentCopyWarmup
{
    /// <summary>
    /// Delegates are per type, so the same component only needs a few copies to reach the types of its nested fields.
    /// Copying every component of every prototype takes several seconds and gains almost nothing over this.
    /// </summary>
    private const int MaxCopiesPerComponent = 16;

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IDependencyCollection _dependencies = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ISerializationManager _serialization = default!;

    public ComponentCopyWarmup()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Starts the warm-up on a background thread, compiling everything can take several seconds.
    /// </summary>
    /// <remarks>
    /// Concurrent copies on the main thread are safe, the worst outcome is a delegate getting compiled twice.
    /// </remarks>
    public void Start()
    {
        if (!_cfg.GetCVar(ArcaneCCVars.ComponentCopyWarmup))
            return;

        var prototypes = _prototype.EnumeratePrototypes<EntityPrototype>().ToArray();
        Task.Run(() => Run(prototypes));
    }

    private void Run(EntityPrototype[] prototypes)
    {
        IoCManager.InitThread(_dependencies, replaceExisting: true);

        var sawmill = _log.GetSawmill("arcane.warmup");
        var stopwatch = Stopwatch.StartNew();
        var copied = 0;
        var copies = new Dictionary<string, int>();

        foreach (var prototype in prototypes)
        {
            foreach (var (name, entry) in prototype.Components)
            {
                var count = copies.GetValueOrDefault(name);
                if (count >= MaxCopiesPerComponent)
                    continue;

                copies[name] = count + 1;

                try
                {
                    // Hooks must stay off here: they aren't thread safe, and sprites would add this throwaway copy to
                    // the resource cache's list of sprites to set up. They also don't compile any delegates.
                    var target = _factory.GetComponent(name);
                    _serialization.CopyTo(entry.Component, ref target, skipHook: true, notNullableOverride: true);
                    copied += 1;
                }
                catch (Exception e)
                {
                    // This is only an optimization, the same copy will be retried when the entity is spawned.
                    sawmill.Debug($"Failed to warm up {name} of {prototype.ID}: {e.Message}");
                }
            }
        }

        sawmill.Debug($"Copied {copied} prototype components in {stopwatch.Elapsed.TotalMilliseconds:F0} ms");
    }
}
