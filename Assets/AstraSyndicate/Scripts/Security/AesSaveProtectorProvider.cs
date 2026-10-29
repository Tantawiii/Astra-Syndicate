using AstraSyndicate.Save;
using UnityEngine;

namespace AstraSyndicate.Security
{
    /// <summary>
    /// Assign in GameConfig to encrypt save files with <see cref="AesHmacSaveProtector"/> and a device-bound key.
    /// Another encryption module replaces it by providing its own SaveProtectorProvider asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Astra Syndicate/Security/AES Save Protector", fileName = "SaveProtector_AES")]
    public sealed class AesSaveProtectorProvider : SaveProtectorProvider
    {
        [Tooltip("Let the editor and development builds read saves written before encryption was enabled; they are " +
                 "re-saved encrypted. Release builds never accept unencrypted saves.")]
        [SerializeField] private bool migrateUnencryptedSavesInDevelopment = true;

        public override ISaveDataProtector CreateProtector()
        {
            bool development = Application.isEditor || Debug.isDebugBuild;
            return new AesHmacSaveProtector(DeviceKey.Derive(), acceptPlaintext: migrateUnencryptedSavesInDevelopment && development);
        }
    }
}
