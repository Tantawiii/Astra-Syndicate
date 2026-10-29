using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AstraSyndicate.Security
{
    /// <summary>
    /// Derives a save key bound to this device, so save files copied to another device do not load.
    /// </summary>
    /// <remarks>
    /// The key is derived from <see cref="SystemInfo.deviceUniqueIdentifier"/>, which is enough to stop save
    /// editing and save sharing, but not a determined attacker on the same device. A platform keystore
    /// (Android Keystore, iOS Keychain, DPAPI) is the place for stronger protection.
    /// The derivation inputs must never change, or existing saves become unreadable.
    /// </remarks>
    public static class DeviceKey
    {
        private const string Purpose = "AstraSyndicate.FleetWars.SaveKey.v1";
        private const int Iterations = 10000;
        private const int KeySize = 32;
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Astra Syndicate save salt v1");

        public static byte[] Derive()
        {
            return Derive(SystemInfo.deviceUniqueIdentifier);
        }

        /// <summary>Key for an explicit device id (tests, tooling).</summary>
        public static byte[] Derive(string deviceId)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes($"{deviceId}|{Purpose}", Salt, Iterations))
            {
                return pbkdf2.GetBytes(KeySize);
            }
        }
    }
}
