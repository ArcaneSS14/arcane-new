using Content.Arcane.Client.JoinQueue;
using Content.IntegrationTests.Fixtures;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class QueueMiniGameInputTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task TypingInAnotherControlDoesNotStartMoveOrShootTheGame()
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var input = Client.ResolveDependency<IInputManager>();
            using var game = new InputProbe();
            using var text = new LineEdit();
            ui.StateRoot.AddChild(game);
            ui.StateRoot.AddChild(text);
            text.GrabKeyboardFocus();
            try
            {
                input.KeyDown(Key(Keyboard.Key.Space));
                game.Advance();
                Assert.That(game.IsStarted, Is.False, "Typing a space must not start the game.");

                text.ReleaseKeyboardFocus();
                game.Advance();
                Assert.That(game.IsStarted, Is.False, "A space held while leaving the text field must not start the game.");

                input.KeyUp(Key(Keyboard.Key.Space));
                game.Advance();
                input.KeyDown(Key(Keyboard.Key.Space));
                game.Advance();
                Assert.That(game.IsStarted, Is.True);

                text.GrabKeyboardFocus();
                input.KeyDown(Key(Keyboard.Key.A));
                game.Advance();
                Assert.That(game.MoveLeft, Is.False, "Typing A must not move the player.");
                Assert.That(game.Shooting, Is.False, "Typing a space must not fire.");

                text.ReleaseKeyboardFocus();
                game.Advance();
                Assert.That(game.MoveLeft, Is.True, "Gameplay input must resume after the text field loses focus.");
                Assert.That(game.Shooting, Is.True);
            }
            finally
            {
                input.KeyUp(Key(Keyboard.Key.A));
                input.KeyUp(Key(Keyboard.Key.Space));
                text.ReleaseKeyboardFocus();
            }
        });
    }

    private static KeyEventArgs Key(Keyboard.Key key) => new(key, false, false, false, false, false, 0);

    private sealed class InputProbe : QueueMiniGameControl
    {
        public InputProbe() => IoCManager.InjectDependencies(this);

        public bool IsStarted => Started;
        public bool MoveLeft { get; private set; }
        public bool Shooting { get; private set; }
        public override int Score => 0;
        public override int Lives => 1;
        public override int Wave => 0;
        protected override bool IsGameFinished => false;
        protected override void ResetGame() { }
        protected override void DrawGame(DrawingHandleScreen handle) { }

        protected override void UpdateRunningGame(FrameEventArgs args, bool space)
        {
            MoveLeft = IsKeyDown(Keyboard.Key.A);
            Shooting = space;
        }

        public void Advance() => FrameUpdate(new FrameEventArgs(0.01f));
    }
}
