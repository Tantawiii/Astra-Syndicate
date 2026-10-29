using System;
using System.IO;
using AstraSyndicate.Save;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    public class FileSaveStorageTests
    {
        private const string Key = "slot";

        private string _directory;
        private FileSaveStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "AstraSaveTests_" + Guid.NewGuid().ToString("N"));
            _storage = new FileSaveStorage(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void Write_ThenRead_ReturnsSameBytes()
        {
            _storage.Write(Key, new byte[] { 1, 2, 3 });

            Assert.IsTrue(_storage.TryRead(Key, SaveCopy.Primary, out byte[] bytes));
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bytes);
            Assert.IsFalse(_storage.TryRead(Key, SaveCopy.Backup, out _));
        }

        [Test]
        public void SecondWrite_KeepsPreviousVersionAsBackup()
        {
            _storage.Write(Key, new byte[] { 1 });
            _storage.Write(Key, new byte[] { 2 });

            _storage.TryRead(Key, SaveCopy.Primary, out byte[] primary);
            _storage.TryRead(Key, SaveCopy.Backup, out byte[] backup);
            CollectionAssert.AreEqual(new byte[] { 2 }, primary);
            CollectionAssert.AreEqual(new byte[] { 1 }, backup);
        }

        [Test]
        public void Write_LeavesNoTempFileBehind()
        {
            _storage.Write(Key, new byte[] { 1 });
            _storage.Write(Key, new byte[] { 2 });

            Assert.IsEmpty(Directory.GetFiles(_directory, "*.tmp"));
        }

        [Test]
        public void DeleteCopy_Primary_KeepsBackup()
        {
            _storage.Write(Key, new byte[] { 1 });
            _storage.Write(Key, new byte[] { 2 });

            _storage.DeleteCopy(Key, SaveCopy.Primary);

            Assert.IsFalse(_storage.TryRead(Key, SaveCopy.Primary, out _));
            Assert.IsTrue(_storage.TryRead(Key, SaveCopy.Backup, out _));
            Assert.IsTrue(_storage.Exists(Key));
        }

        [Test]
        public void Delete_RemovesEveryCopy()
        {
            _storage.Write(Key, new byte[] { 1 });
            _storage.Write(Key, new byte[] { 2 });

            _storage.Delete(Key);

            Assert.IsFalse(_storage.Exists(Key));
            Assert.IsEmpty(Directory.GetFiles(_directory));
        }

        [TestCase("../escape")]
        [TestCase("sub/dir")]
        [TestCase("")]
        public void InvalidKeys_AreRejected(string key)
        {
            Assert.Throws<ArgumentException>(() => _storage.Write(key, new byte[] { 1 }));
        }
    }
}
