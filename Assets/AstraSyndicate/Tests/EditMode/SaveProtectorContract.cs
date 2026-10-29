using System;
using System.Text;
using AstraSyndicate.Save;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    /// <summary>
    /// What every secure <see cref="ISaveDataProtector"/> must guarantee. An encryption module proves its
    /// implementation by subclassing this fixture and returning its protector from <see cref="CreateProtector"/>
    /// (reference this test assembly, AstraSyndicate.Tests.EditMode, from the module's own test assembly).
    /// </summary>
    public abstract class SaveProtectorContract
    {
        private static readonly byte[] SampleSave = Encoding.UTF8.GetBytes(
            "{\"schemaVersion\":1,\"lastMissionId\":\"ch01_m02\",\"missions\":[{\"missionId\":\"ch01_m01\",\"completed\":true}]}");

        protected abstract ISaveDataProtector CreateProtector();

        [Test]
        public void RoundTrip_RestoresTheOriginalBytes()
        {
            AssertRoundTrip(SampleSave);
        }

        [Test]
        public void RoundTrip_WorksForEmptyData()
        {
            AssertRoundTrip(Array.Empty<byte>());
        }

        [Test]
        public void RoundTrip_WorksForLargeData()
        {
            var large = new byte[1 << 20];
            new Random(7).NextBytes(large);
            AssertRoundTrip(large);
        }

        [Test]
        public void ProtectedBytes_DoNotRevealTheSave()
        {
            byte[] protectedBytes = CreateProtector().Protect(SampleSave);

            Assert.IsFalse(Contains(protectedBytes, Encoding.UTF8.GetBytes("ch01_m02")), "Save contents must not be readable in the stored file.");
        }

        [TestCase(0)]
        [TestCase(0.5f)]
        [TestCase(1)]
        public void ChangedByte_IsRejected(float position)
        {
            ISaveDataProtector protector = CreateProtector();
            byte[] protectedBytes = protector.Protect(SampleSave);
            int index = Math.Min(protectedBytes.Length - 1, (int)(position * protectedBytes.Length));
            protectedBytes[index] ^= 0x20;

            Assert.IsFalse(protector.TryUnprotect(protectedBytes, out _), $"Changing byte {index} must be detected.");
        }

        [Test]
        public void TruncatedData_IsRejected()
        {
            ISaveDataProtector protector = CreateProtector();
            byte[] protectedBytes = protector.Protect(SampleSave);
            Array.Resize(ref protectedBytes, protectedBytes.Length - 1);

            Assert.IsFalse(protector.TryUnprotect(protectedBytes, out _));
        }

        [Test]
        public void Garbage_IsRejectedWithoutThrowing()
        {
            var garbage = new byte[96];
            new Random(11).NextBytes(garbage);
            ISaveDataProtector protector = CreateProtector();

            Assert.IsFalse(protector.TryUnprotect(garbage, out _));
            Assert.IsFalse(protector.TryUnprotect(Array.Empty<byte>(), out _));
            Assert.IsFalse(protector.TryUnprotect(null, out _));
        }

        private void AssertRoundTrip(byte[] original)
        {
            ISaveDataProtector protector = CreateProtector();

            Assert.IsTrue(protector.TryUnprotect(protector.Protect(original), out byte[] restored));
            CollectionAssert.AreEqual(original, restored);
        }

        private static bool Contains(byte[] data, byte[] pattern)
        {
            for (int start = 0; start <= data.Length - pattern.Length; start++)
            {
                int matched = 0;
                while (matched < pattern.Length && data[start + matched] == pattern[matched]) matched++;
                if (matched == pattern.Length) return true;
            }
            return false;
        }
    }
}
