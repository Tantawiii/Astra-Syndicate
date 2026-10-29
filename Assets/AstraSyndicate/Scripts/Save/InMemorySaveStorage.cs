using System.Collections.Generic;

namespace AstraSyndicate.Save
{
    /// <summary>Volatile storage for tests and tools. Mirrors FileSaveStorage's backup behaviour.</summary>
    public sealed class InMemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, byte[]> _primary = new Dictionary<string, byte[]>();
        private readonly Dictionary<string, byte[]> _backup = new Dictionary<string, byte[]>();

        public void Write(string key, byte[] bytes)
        {
            if (_primary.TryGetValue(key, out byte[] previous)) _backup[key] = previous;
            _primary[key] = (byte[])bytes.Clone();
        }

        public bool TryRead(string key, SaveCopy copy, out byte[] bytes)
        {
            if (Copies(copy).TryGetValue(key, out byte[] stored))
            {
                bytes = (byte[])stored.Clone();
                return true;
            }

            bytes = null;
            return false;
        }

        public bool Exists(string key)
        {
            return _primary.ContainsKey(key) || _backup.ContainsKey(key);
        }

        public void Delete(string key)
        {
            _primary.Remove(key);
            _backup.Remove(key);
        }

        public void DeleteCopy(string key, SaveCopy copy)
        {
            Copies(copy).Remove(key);
        }

        /// <summary>Replaces stored bytes without rotating copies, e.g. to simulate corruption in tests.</summary>
        public void SetRaw(string key, SaveCopy copy, byte[] bytes)
        {
            Copies(copy)[key] = bytes;
        }

        private Dictionary<string, byte[]> Copies(SaveCopy copy)
        {
            return copy == SaveCopy.Primary ? _primary : _backup;
        }
    }
}
