// MIT License

// Copyright (c) 2026 Vecortys (vecortys@gmail.com)

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Content.Arcane.Common.Salvage;
using Content.Shared.Body;
using Content.Shared.Gibbing;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Salvage.Fulton;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Trauma.Shared.Effects;

namespace Content.Arcane.Shared.InfinityDorm;

public sealed partial class SharedInfinityDormSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SparksSystem _sparks = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly ProtoId<TagPrototype> InfiniteDormItemTag = "InfiniteDormItem";
    private static readonly ProtoId<TagPrototype> InfiniteDormItemBlockTag = "InfiniteDormItemBlock";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfinityDormExitComponent, InteractHandEvent>(OnExitInteract);
        SubscribeLocalEvent<InfinityDormExitComponent, InfinityDormExitDoAfterEvent>(OnExitDoAfter);
        SubscribeLocalEvent<InfinityDormVisitorComponent, ComponentInit>(OnVisitorInit);
        SubscribeLocalEvent<InfinityDormVisitorComponent, DidEquipHandEvent>(OnDidEquipHandEvent);

        SubscribeLocalEvent<EntParentChangedMessage>(OnVisitorParentChanged);
        SubscribeLocalEvent<InfinityDormVisitorComponent, BeforeFultonedEvent>(OnBeforeFultoned);
    }

    private void OnExitInteract(EntityUid uid, InfinityDormExitComponent component, InteractHandEvent args)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.TimeToExit, new InfinityDormExitDoAfterEvent(), uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnExitDoAfter(EntityUid uid, InfinityDormExitComponent component, InfinityDormExitDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<InfinityDormComponent>(_transform.GetParentUid(uid), out var dormComp))
            return;

        if (HasInfiniteDormItem(args.User))
        {
            _chat.TrySendInGameICMessage(uid, Loc.GetString("infinity-dorm-warning-blocked-items"), InGameICChatType.Speak, true);
            return;
        }

        RemoveInfiniteDormTags(args.User);
        RemComp<InfinityDormVisitorComponent>(args.User);

        _sparks.DoSparks(Transform(args.User).Coordinates, args.User, 3, 10);

        var teleporterTransform = Transform(dormComp.ConnectedTeleporter);
        _transform.SetMapCoordinates(args.User, _transform.GetMapCoordinates(teleporterTransform));

        _sparks.DoSparks(Transform(args.User).Coordinates, args.User, 3, 10);

    }

    private void OnVisitorInit(EntityUid uid, InfinityDormVisitorComponent component, ComponentInit args)
    {
        MarkInitItems(uid);
    }

    private void OnDidEquipHandEvent(EntityUid uid, InfinityDormVisitorComponent component, DidEquipHandEvent args)
    {
        if (args.Handled)
            return;

        if (_tag.HasTag(args.Equipped, InfiniteDormItemBlockTag))
            return;

        _tag.AddTag(args.Equipped, InfiniteDormItemTag);

        args.Handled = true;
    }

    private void OnVisitorParentChanged(ref EntParentChangedMessage args)
    {
        if (_net.IsClient)
            return;

        if (HasComp<GhostComponent>(args.Entity))
            return;

        // The old GibBody only affected bodies; Gib also destroys ordinary items.
        if (!HasComp<BodyComponent>(args.Entity))
            return;

        if (!HasComp<MapGridComponent>(_transform.GetParentUid(args.Entity)))
            return;

        var isDorm = HasComp<InfinityDormComponent>(_transform.GetParentUid(args.Entity));
        if (isDorm && HasComp<InfinityDormVisitorComponent>(args.Entity))
            return;

        if (!HasInfiniteDormItem(args.Entity) && !isDorm)
            return;

        _gibbing.Gib(args.Entity);
    }

    private void OnBeforeFultoned(EntityUid uid, InfinityDormVisitorComponent component, BeforeFultonedEvent args)
    {
        args.Cancel();
    }

    private void MarkInitItems(EntityUid user)
    {
        if (!TryComp<ContainerManagerComponent>(user, out var manager))
            return;

        foreach (var container in manager.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                _tag.AddTag(contained, InfiniteDormItemBlockTag);
                MarkInitItems(contained);
            }
        }
    }

    private bool HasInfiniteDormItem(EntityUid user)
    {
        if (!TryComp<ContainerManagerComponent>(user, out var manager))
            return false;

        foreach (var container in manager.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (_tag.HasTag(contained, InfiniteDormItemTag))
                    return true;

                if (HasInfiniteDormItem(contained))
                    return true;
            }
        }

        return false;
    }

    private void RemoveInfiniteDormTags(EntityUid user)
    {
        if (!TryComp<ContainerManagerComponent>(user, out var manager))
            return;

        foreach (var container in manager.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                _tag.RemoveTag(contained, InfiniteDormItemBlockTag);
                RemoveInfiniteDormTags(contained);
            }
        }
    }
}
