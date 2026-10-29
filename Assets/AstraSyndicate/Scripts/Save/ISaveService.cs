namespace AstraSyndicate.Save
{
    /// <summary>Stores data objects locally under string keys. Fully offline: no server involved.</summary>
    public interface ISaveService
    {
        /// <summary>Saves <paramref name="data"/> under <paramref name="key"/>. Returns false if the write failed.</summary>
        bool Save<T>(string key, T data) where T : class;

        /// <summary>
        /// Loads the data saved under <paramref name="key"/>. Falls back to the backup copy when the
        /// latest copy is corrupted or tampered with. Returns false if nothing readable exists.
        /// </summary>
        bool TryLoad<T>(string key, out T data) where T : class;

        bool Exists(string key);

        void Delete(string key);
    }
}
