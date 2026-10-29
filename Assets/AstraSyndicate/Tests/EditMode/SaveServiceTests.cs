using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AstraSyndicate.Tests
{
    public class SaveServiceTests
    {
        private const string Key = "slot";

        private readonly List<SaveLoadFailedEvent> _loadFailures = new List<SaveLoadFailedEvent>();
        private InMemorySaveStorage _storage;
        private ChecksumProtector _protector;
        private EventBus _events;
        private SaveService _saves;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemorySaveStorage();
            _protector = new ChecksumProtector();
            _events = new EventBus();
            _loadFailures.Clear();
            _events.Subscribe<SaveLoadFailedEvent>(e => _loadFailures.Add(e));
            _saves = new SaveService(new JsonSaveSerializer(), _storage, _protector, _events);
        }

        [Test]
        public void SaveThenLoad_RoundTripsData()
        {
            Assert.IsTrue(_saves.Save(Key, new Sample { number = 7, text = "fleet" }));

            Assert.IsTrue(_saves.TryLoad(Key, out Sample loaded));
            Assert.AreEqual(7, loaded.number);
            Assert.AreEqual("fleet", loaded.text);
            Assert.IsTrue(_saves.Exists(Key));
        }

        [Test]
        public void Save_PassesBytesThroughTheProtector()
        {
            _saves.Save(Key, new Sample { number = 1 });

            Assert.AreEqual(1, _protector.ProtectCalls);
            Assert.IsTrue(_storage.TryRead(Key, SaveCopy.Primary, out byte[] stored));
            Assert.IsTrue(_protector.TryUnprotect(stored, out _), "Stored bytes must be the protector's output.");
        }

        [Test]
        public void TryLoad_MissingKey_ReturnsFalseWithoutReportingFailure()
        {
            Assert.IsFalse(_saves.TryLoad(Key, out Sample loaded));

            Assert.IsNull(loaded);
            Assert.AreEqual(0, _loadFailures.Count);
        }

        [Test]
        public void TryLoad_TamperedPrimary_RecoversFromBackup()
        {
            _saves.Save(Key, new Sample { number = 1 });
            _saves.Save(Key, new Sample { number = 2 });
            Tamper(SaveCopy.Primary);

            Assert.IsTrue(_saves.TryLoad(Key, out Sample loaded));

            Assert.AreEqual(1, loaded.number);
            Assert.AreEqual(1, _loadFailures.Count);
            Assert.IsTrue(_loadFailures[0].RecoveredFromBackup);
        }

        [Test]
        public void Save_AfterRecovery_KeepsTheGoodBackup()
        {
            _saves.Save(Key, new Sample { number = 1 });
            _saves.Save(Key, new Sample { number = 2 });
            Tamper(SaveCopy.Primary);
            _saves.TryLoad(Key, out Sample _);

            _saves.Save(Key, new Sample { number = 3 });
            Tamper(SaveCopy.Primary);

            Assert.IsTrue(_saves.TryLoad(Key, out Sample loaded));
            Assert.AreEqual(1, loaded.number, "The corrupted copy must never replace the good backup.");
        }

        [Test]
        public void TryLoad_BothCopiesTampered_ReturnsFalseAndReportsFailure()
        {
            _saves.Save(Key, new Sample { number = 1 });
            _saves.Save(Key, new Sample { number = 2 });
            Tamper(SaveCopy.Primary);
            Tamper(SaveCopy.Backup);

            Assert.IsFalse(_saves.TryLoad(Key, out Sample _));

            Assert.AreEqual(1, _loadFailures.Count);
            Assert.IsFalse(_loadFailures[0].RecoveredFromBackup);
        }

        [Test]
        public void TryLoad_UnparseablePrimary_RecoversFromBackup()
        {
            var saves = new SaveService(new JsonSaveSerializer(), _storage, new PassthroughDataProtector(), _events);
            saves.Save(Key, new Sample { number = 1 });
            saves.Save(Key, new Sample { number = 2 });
            _storage.SetRaw(Key, SaveCopy.Primary, new byte[] { 0x7B, 0x7B, 0x7B });

            Assert.IsTrue(saves.TryLoad(Key, out Sample loaded));
            Assert.AreEqual(1, loaded.number);
        }

        [Test]
        public void Delete_RemovesAllCopies()
        {
            _saves.Save(Key, new Sample { number = 1 });
            _saves.Save(Key, new Sample { number = 2 });

            _saves.Delete(Key);

            Assert.IsFalse(_saves.Exists(Key));
            Assert.IsFalse(_saves.TryLoad(Key, out Sample _));
        }

        [Test]
        public void Save_WhenStorageFails_ReturnsFalseAndPublishesFailure()
        {
            var failures = new List<SaveFailedEvent>();
            _events.Subscribe<SaveFailedEvent>(e => failures.Add(e));
            var saves = new SaveService(new JsonSaveSerializer(), new FailingStorage(), new PassthroughDataProtector(), _events);

            LogAssert.Expect(LogType.Error, new Regex("disk full"));
            Assert.IsFalse(saves.Save(Key, new Sample()));

            Assert.AreEqual(1, failures.Count);
        }

        private void Tamper(SaveCopy copy)
        {
            Assert.IsTrue(_storage.TryRead(Key, copy, out byte[] bytes));
            bytes[0] ^= 0x5A;
            _storage.SetRaw(Key, copy, bytes);
        }

        [Serializable]
        public class Sample
        {
            public int number;
            public string text;
        }

        /// <summary>Stand-in for the encryption module: appends a checksum and rejects data that no longer matches it.</summary>
        private sealed class ChecksumProtector : ISaveDataProtector
        {
            public int ProtectCalls { get; private set; }

            public byte[] Protect(byte[] plainBytes)
            {
                ProtectCalls++;
                var output = new byte[plainBytes.Length + 1];
                Array.Copy(plainBytes, output, plainBytes.Length);
                output[plainBytes.Length] = Checksum(plainBytes, plainBytes.Length);
                return output;
            }

            public bool TryUnprotect(byte[] protectedBytes, out byte[] plainBytes)
            {
                plainBytes = null;
                if (protectedBytes == null || protectedBytes.Length == 0) return false;

                int length = protectedBytes.Length - 1;
                if (protectedBytes[length] != Checksum(protectedBytes, length)) return false;

                plainBytes = new byte[length];
                Array.Copy(protectedBytes, plainBytes, length);
                return true;
            }

            private static byte Checksum(byte[] data, int length)
            {
                int sum = 0;
                for (int i = 0; i < length; i++) sum += data[i];
                return (byte)(sum & 0xFF);
            }
        }

        private sealed class FailingStorage : ISaveStorage
        {
            public void Write(string key, byte[] bytes) => throw new IOException("disk full");

            public bool TryRead(string key, SaveCopy copy, out byte[] bytes)
            {
                bytes = null;
                return false;
            }

            public bool Exists(string key) => false;

            public void Delete(string key)
            {
            }

            public void DeleteCopy(string key, SaveCopy copy)
            {
            }
        }
    }
}
