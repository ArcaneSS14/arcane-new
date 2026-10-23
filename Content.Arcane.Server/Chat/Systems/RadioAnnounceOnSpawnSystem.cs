using Content.Arcane.Server.Chat.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Robust.Shared.Map;

namespace Content.Arcane.Server.Chat.Systems;

public sealed partial class RadioAnnounceOnSpawnSystem : EntitySystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioAnnounceOnSpawnComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, RadioAnnounceOnSpawnComponent comp, MapInitEvent args)
    {
        var entityName = MetaData(uid).EntityName;
        var message = Loc.GetString(comp.Message, ("name", entityName));

        var componentName = Loc.GetString(comp.Sender);
        var sender = Spawn(null, MapCoordinates.Nullspace);

        _metaData.SetEntityName(sender, componentName);

        AddComp<RadioMicrophoneComponent>(sender);

        foreach (var channelId in comp.AnnouncementChannels)
        {
            _radio.SendRadioMessage(sender, message, ProtoMan.Index(channelId), sender, escapeMarkup: false);
        }

        QueueDel(sender);
    }
}
