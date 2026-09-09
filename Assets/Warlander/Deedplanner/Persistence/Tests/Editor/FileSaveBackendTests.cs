using System;
using System.IO;
using NUnit.Framework;
using Warlander.Deedplanner.Persistence.Compression;

namespace Warlander.Deedplanner.Persistence.Tests
{
    public class FileSaveBackendTests
    {
        [Test]
        public void SaveAsync_WriteFailureFaultsTask()
        {
            string missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var picker = new FakeMapSavePicker
            {
                Path = Path.Combine(missingDirectory, "deed.MAP")
            };
            var backend = new FileSaveBackend(new GzipByteCompressor(), picker);

            var task = backend.SaveAsync("payload", "deed");
            Assert.That(task.IsCompleted, Is.False);
            Assert.DoesNotThrow(picker.Succeed);

            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.InnerException, Is.TypeOf<DirectoryNotFoundException>());
        }

        [Test]
        public void SaveAsync_PickerOpenFailureFaultsTask()
        {
            var picker = new FakeMapSavePicker { Opens = false };
            var backend = new FileSaveBackend(new GzipByteCompressor(), picker);

            var task = backend.SaveAsync("payload", "deed");

            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        private class FakeMapSavePicker : IMapSavePicker
        {
            private Action<string[]> _onSuccess;

            public bool Opens = true;
            public string Path;

            public bool Show(Action<string[]> onSuccess, Action onCancel, string suggestedName)
            {
                _onSuccess = onSuccess;
                return Opens;
            }

            public void Succeed()
            {
                _onSuccess(new[] { Path });
            }
        }
    }
}
