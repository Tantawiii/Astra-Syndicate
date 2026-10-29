using System;
using System.Collections.Generic;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using AstraSyndicate.Security;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    /// <summary>The save pipeline with the AES protector plugged into the hook, end to end.</summary>
    public class EncryptedSaveTests
    {
        private const string Key = "campaign_progress";

        private readonly List<SaveLoadFailedEvent> _loadFailures = new List<SaveLoadFailedEvent>();
        private InMemorySaveStorage _storage;
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _loadFailures.Clear();
            _storage = new InMemorySaveStorage();
            _events = new EventBus();
            _events.Subscribe<SaveLoadFailedEvent>(e => _loadFailures.Add(e));
        }

        [Test]
        public void EncryptedSave_RoundTrips()
        {
            SaveService saves = CreateSaves(new AesHmacSaveProtector(AesHmacSaveProtectorTests.Key));
            saves.Save(Key, new Sample { number = 42 });

            Assert.IsTrue(saves.TryLoad(Key, out Sample loaded));
            Assert.AreEqual(42, loaded.number);
        }

        [Test]
        public void StoredFile_IsNotPlainJson()
        {
            CreateSaves(new AesHmacSaveProtector(AesHmacSaveProtectorTests.Key)).Save(Key, new Sample { number = 42 });

            Assert.IsTrue(_storage.TryRead(Key, SaveCopy.Primary, out byte[] stored));
            Assert.AreNotEqual((byte)'{', stored[0]);
        }

        [Test]
        public void TamperedSave_IsRestoredFromTheBackup_AndReported()
        {
            SaveService saves = CreateSaves(new AesHmacSaveProtector(AesHmacSaveProtectorTests.Key));
            saves.Save(Key, new Sample { number = 1 });
            saves.Save(Key, new Sample { number = 2 });
            _storage.TryRead(Key, SaveCopy.Primary, out byte[] stored);
            stored[stored.Length / 2] ^= 0x01;
            _storage.SetRaw(Key, SaveCopy.Primary, stored);

            Assert.IsTrue(saves.TryLoad(Key, out Sample loaded));
            Assert.AreEqual(1, loaded.number);
            Assert.AreEqual(1, _loadFailures.Count);
            Assert.IsTrue(_loadFailures[0].RecoveredFromBackup);
        }

        [Test]
        public void SaveFromBeforeEncryption_IsMigratedOnTheNextSave()
        {
            CreateSaves(new PassthroughDataProtector()).Save(Key, new Sample { number = 7 });

            SaveService migrating = CreateSaves(new AesHmacSaveProtector(AesHmacSaveProtectorTests.Key, acceptPlaintext: true));
            Assert.IsTrue(migrating.TryLoad(Key, out Sample loaded), "The old plain save must still load while migrating.");
            migrating.Save(Key, loaded);

            SaveService strict = CreateSaves(new AesHmacSaveProtector(AesHmacSaveProtectorTests.Key));
            Assert.IsTrue(strict.TryLoad(Key, out Sample reloaded), "After the next save the file must be encrypted.");
            Assert.AreEqual(7, reloaded.number);
        }

        private SaveService CreateSaves(ISaveDataProtector protector)
        {
            return new SaveService(new JsonSaveSerializer(), _storage, protector, _events);
        }

        [Serializable]
        public class Sample
        {
            public int number;
        }
    }
}
