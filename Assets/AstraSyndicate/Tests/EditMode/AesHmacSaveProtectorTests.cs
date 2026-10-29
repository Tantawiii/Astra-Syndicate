using System;
using System.Linq;
using System.Text;
using AstraSyndicate.Save;
using AstraSyndicate.Security;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    /// <summary>The reference protector passes the shared contract.</summary>
    public class AesHmacSaveProtectorContractTests : SaveProtectorContract
    {
        protected override ISaveDataProtector CreateProtector()
        {
            return new AesHmacSaveProtector(AesHmacSaveProtectorTests.Key);
        }
    }

    public class AesHmacSaveProtectorTests
    {
        internal static readonly byte[] Key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        private static readonly byte[] OtherKey = Enumerable.Range(0, 32).Select(i => (byte)(255 - i)).ToArray();
        private static readonly byte[] PlainSave = Encoding.UTF8.GetBytes("{\"number\":7}");

        [Test]
        public void SameSave_EncryptsDifferentlyEachTime()
        {
            var protector = new AesHmacSaveProtector(Key);

            byte[] first = protector.Protect(PlainSave);
            byte[] second = protector.Protect(PlainSave);

            CollectionAssert.AreNotEqual(first, second, "A random IV must make every write unique.");
            Assert.IsTrue(protector.TryUnprotect(second, out _));
        }

        [Test]
        public void AnotherKey_CannotReadTheSave()
        {
            byte[] protectedBytes = new AesHmacSaveProtector(Key).Protect(PlainSave);

            Assert.IsFalse(new AesHmacSaveProtector(OtherKey).TryUnprotect(protectedBytes, out _));
        }

        [Test]
        public void UnencryptedSave_IsRejectedByDefault()
        {
            Assert.IsFalse(new AesHmacSaveProtector(Key).TryUnprotect(PlainSave, out _));
        }

        [Test]
        public void UnencryptedSave_IsAcceptedWhileMigrating()
        {
            Assert.IsTrue(new AesHmacSaveProtector(Key, acceptPlaintext: true).TryUnprotect(PlainSave, out byte[] plain));
            CollectionAssert.AreEqual(PlainSave, plain);
        }

        [Test]
        public void MigratingProtector_StillRejectsTamperedEncryptedSaves()
        {
            var protector = new AesHmacSaveProtector(Key, acceptPlaintext: true);
            byte[] protectedBytes = protector.Protect(PlainSave);
            protectedBytes[protectedBytes.Length / 2] ^= 0x01;

            Assert.IsFalse(protector.TryUnprotect(protectedBytes, out _));
        }

        [Test]
        public void ShortKey_IsRefused()
        {
            Assert.Throws<ArgumentException>(() => new AesHmacSaveProtector(new byte[8]));
        }

        [Test]
        public void DeviceKey_IsStablePerDevice_AndDiffersBetweenDevices()
        {
            byte[] key = DeviceKey.Derive("device-a");

            Assert.AreEqual(32, key.Length);
            CollectionAssert.AreEqual(key, DeviceKey.Derive("device-a"));
            CollectionAssert.AreNotEqual(key, DeviceKey.Derive("device-b"));
        }
    }
}
