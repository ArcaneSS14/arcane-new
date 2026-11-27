using Content.Arcane.Common.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Events;
using Robust.Server;
using Robust.Shared.Configuration;

namespace Content.Arcane.Server.ServerRestart;

public sealed partial class RoundEndServerRestartSystem : EntitySystem
{
    [Dependency] private IBaseServer _baseServer = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private bool _restartOnRoundEnd;
    private int _rounds = 0;
    private int _restartOn;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Subs.CVar(_cfg, ArcaneCVars.RestartServerOnRoundEnd, OnRestartServerOnRoundEnd, true);
        Subs.CVar(_cfg, ArcaneCVars.RoundsToRestart, OnRoundsToRestart, true);
    }

    private void OnRestartServerOnRoundEnd(bool value)
    {
        _restartOnRoundEnd = value;
    }

    private void OnRoundsToRestart(int value)
    {
        _restartOn = value;
    }

    private void OnRoundStarted(RoundStartingEvent args)
    {
        _rounds++;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        if (!_restartOnRoundEnd)
            return;

        if (_rounds == 0)
            return;

        if (_rounds < _restartOn)
            return;

        _baseServer.Shutdown("The round has ended, the server is restarting.");
    }
}
