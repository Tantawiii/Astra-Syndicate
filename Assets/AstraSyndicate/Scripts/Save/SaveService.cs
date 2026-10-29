using System;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Save
{
    /// <summary>
    /// Save pipeline: serialize, then protect, then store; the reverse on load. When the primary copy
    /// cannot be read (corrupted, tampered, or rejected by the protector) the backup is used instead,
    /// so one bad write never costs the player their progress.
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveSerializer _serializer;
        private readonly ISaveStorage _storage;
        private readonly ISaveDataProtector _protector;
        private readonly IEventBus _events;

        public SaveService(ISaveSerializer serializer, ISaveStorage storage, ISaveDataProtector protector, IEventBus events)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _protector = protector ?? throw new ArgumentNullException(nameof(protector));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public bool Save<T>(string key, T data) where T : class
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                byte[] plain = _serializer.Serialize(data);
                _storage.Write(key, _protector.Protect(plain));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Could not save '{key}': {e.Message}");
                _events.Publish(new SaveFailedEvent(key, e.Message));
                return false;
            }

            _events.Publish(new SaveCompletedEvent(key));
            return true;
        }

        public bool TryLoad<T>(string key, out T data) where T : class
        {
            ReadResult primary = TryRead(key, SaveCopy.Primary, out data);
            if (primary == ReadResult.Success) return true;

            ReadResult backup = TryRead(key, SaveCopy.Backup, out data);
            if (backup == ReadResult.Success)
            {
                if (primary == ReadResult.Unreadable)
                {
                    DiscardPrimary(key);
                    Debug.LogWarning($"[Save] '{key}' was unreadable and has been restored from its backup.");
                    _events.Publish(new SaveLoadFailedEvent(key, recoveredFromBackup: true));
                }
                return true;
            }

            if (primary == ReadResult.Unreadable || backup == ReadResult.Unreadable)
            {
                Debug.LogWarning($"[Save] '{key}' could not be read (corrupted or tampered with).");
                _events.Publish(new SaveLoadFailedEvent(key, recoveredFromBackup: false));
            }
            return false;
        }

        public bool Exists(string key)
        {
            return _storage.Exists(key);
        }

        public void Delete(string key)
        {
            _storage.Delete(key);
        }

        // Drops a bad primary copy so the next write cannot rotate it over the good backup.
        private void DiscardPrimary(string key)
        {
            try
            {
                _storage.DeleteCopy(key, SaveCopy.Primary);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Could not remove the unreadable copy of '{key}': {e.Message}");
            }
        }

        private ReadResult TryRead<T>(string key, SaveCopy copy, out T data) where T : class
        {
            data = null;
            try
            {
                if (!_storage.TryRead(key, copy, out byte[] stored)) return ReadResult.Missing;
                if (!_protector.TryUnprotect(stored, out byte[] plain)) return ReadResult.Unreadable;

                data = _serializer.Deserialize<T>(plain);
                return data != null ? ReadResult.Success : ReadResult.Unreadable;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] {copy} copy of '{key}' is unreadable: {e.Message}");
                data = null;
                return ReadResult.Unreadable;
            }
        }

        private enum ReadResult
        {
            Missing,
            Unreadable,
            Success,
        }
    }
}
