using NUnit.Framework;

namespace Warlander.Deedplanner.Editing.Tests
{
    public class CommandManagerTests
    {
        private sealed class Value
        {
            public int Current;
        }

        private sealed class SetValue : IReversibleCommand
        {
            private readonly Value _value;
            private readonly int _before;
            private readonly int _after;

            public SetValue(Value value, int after)
            {
                _value = value;
                _before = value.Current;
                _after = after;
            }

            public void Execute() => _value.Current = _after;
            public void Undo() => _value.Current = _before;
            public void DisposeUndo() { }
            public void DisposeRedo() { }
        }

        [Test]
        public void PendingActionBlocksUndoUntilFinished()
        {
            var history = new CommandManager(10);
            var value = new Value();
            history.AddToActionAndExecute(new SetValue(value, 1));
            history.FinishAction();
            history.AddToActionAndExecute(new SetValue(value, 2));
            history.Undo();
            Assert.That(value.Current, Is.EqualTo(2));
            history.FinishAction();
            history.Undo();
            Assert.That(value.Current, Is.EqualTo(1));
            history.Undo();
            Assert.That(value.Current, Is.Zero);
        }

        [Test]
        public void PendingActionBlocksRedoUntilCancelled()
        {
            var history = new CommandManager(10);
            var value = new Value();
            history.AddToActionAndExecute(new SetValue(value, 1));
            history.FinishAction();
            history.Undo();
            history.AddToActionAndExecute(new SetValue(value, 2));
            history.Redo();
            Assert.That(value.Current, Is.EqualTo(2));
            history.UndoAction();
            history.Redo();
            Assert.That(value.Current, Is.EqualTo(1));
        }

        [Test]
        public void NestedLiveEditsReleaseHistoryExactlyOnce()
        {
            var history = new CommandManager(10);
            var value = new Value();
            history.AddToActionAndExecute(new SetValue(value, 1));
            history.FinishAction();
            var first = history.SuspendHistory();
            var second = history.SuspendHistory();
            first.Dispose();
            first.Dispose();
            history.Undo();
            Assert.That(value.Current, Is.EqualTo(1));
            second.Dispose();
            history.Undo();
            Assert.That(value.Current, Is.Zero);
        }
    }
}
