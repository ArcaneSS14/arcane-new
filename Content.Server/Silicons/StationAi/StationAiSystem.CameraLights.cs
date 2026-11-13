using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.Player;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SlimPoweredLightSystem _cameraLights = default!;

    private const float CameraLightRange = 8f;
    private const float CameraLightInterval = 0.25f;
    private readonly HashSet<EntityUid> _cameraLightObservers = new();
    private readonly HashSet<Entity<SurveillanceCameraComponent>> _nearbyCameras = new();
    private HashSet<EntityUid> _litCameras = new();
    private HashSet<EntityUid> _requestedCameras = new();
    private float _cameraLightElapsed;

    private void InitializeCameraLights()
    {
        SubscribeLocalEvent<StationAiHeldComponent, ToggleStationAiCameraLightsEvent>(OnToggleCameraLights);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCameraLightRoundCleanup);
    }

    private void OnToggleCameraLights(Entity<StationAiHeldComponent> entity, ref ToggleStationAiCameraLightsEvent args)
    {
        if (args.Handled || args.Performer != entity.Owner || !TryGetCore(entity, out var core) ||
            !TryGetHeld(core, out var held) || held != entity.Owner || !_mobState.IsAlive(entity))
            return;

        args.Handled = true;
        var enabled = _cameraLightObservers.Add(entity.Owner);
        if (!enabled)
            _cameraLightObservers.Remove(entity.Owner);

        _actions.SetToggled(args.Action.AsNullable(), enabled);
        UpdateCameraLights();
    }

    private void RemoveCameraLightObserver(EntityUid observer)
    {
        if (_cameraLightObservers.Remove(observer))
            UpdateCameraLights();
    }

    private void OnCameraLightRoundCleanup(RoundRestartCleanupEvent args)
    {
        _cameraLightObservers.Clear();
        UpdateCameraLights();
        _cameraLightElapsed = 0f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_cameraLightObservers.Count == 0 && _litCameras.Count == 0)
            return;

        _cameraLightElapsed += frameTime;
        if (_cameraLightElapsed < CameraLightInterval)
            return;

        _cameraLightElapsed = 0f;
        UpdateCameraLights();
    }

    private void UpdateCameraLights()
    {
        _requestedCameras.Clear();
        foreach (var observer in _cameraLightObservers)
        {
            if (TerminatingOrDeleted(observer) || !_mobState.IsAlive(observer) || !HasComp<ActorComponent>(observer) ||
                !TryGetCore(observer, out var core) || core.Comp?.RemoteEntity is not { } eye ||
                !TryComp(eye, out TransformComponent? eyeTransform) || eyeTransform.GridUid is not { } grid ||
                _station.GetOwningStation(core.Owner) is not { } station || _station.GetOwningStation(eye) != station)
                continue;

            _nearbyCameras.Clear();
            _lookup.GetEntitiesInRange(eyeTransform.Coordinates, CameraLightRange, _nearbyCameras, LookupFlags.Static);
            foreach (var camera in _nearbyCameras)
            {
                if (!TerminatingOrDeleted(camera) && Transform(camera).GridUid == grid)
                    _requestedCameras.Add(camera);
            }
        }

        foreach (var camera in _litCameras)
        {
            if (!_requestedCameras.Contains(camera) && !TerminatingOrDeleted(camera))
                _cameraLights.SetAiIllumination(camera, false);
        }

        foreach (var camera in _requestedCameras)
        {
            if (!_litCameras.Contains(camera))
                _cameraLights.SetAiIllumination(camera, true);
        }

        (_litCameras, _requestedCameras) = (_requestedCameras, _litCameras);
    }
}
