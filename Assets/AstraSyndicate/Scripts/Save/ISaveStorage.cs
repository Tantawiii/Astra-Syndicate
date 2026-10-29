namespace AstraSyndicate.Save
{
    /// <summary>
    /// Raw byte storage for save keys. Every write keeps the previous primary copy as the backup,
    /// so a corrupted write can always be recovered from.
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>Writes a new primary copy; the current primary copy (if any) becomes the backup.</summary>
        void Write(string key, byte[] bytes);

        bool TryRead(string key, SaveCopy copy, out byte[] bytes);

        /// <summary>True if any copy of the key exists.</summary>
        bool Exists(string key);

        /// <summary>Deletes every copy of the key.</summary>
        void Delete(string key);

        /// <summary>Deletes one copy, e.g. a corrupted primary that must not be rotated into the backup.</summary>
        void DeleteCopy(string key, SaveCopy copy);
    }
}
