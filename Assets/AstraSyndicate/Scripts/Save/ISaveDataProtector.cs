namespace AstraSyndicate.Save
{
    /// <summary>
    /// Security hook applied to every save file. <see cref="Protect"/> runs after serialization and
    /// before the bytes reach storage; <see cref="TryUnprotect"/> runs after reading and before
    /// deserialization. This is the integration point for the save encryption module.
    /// </summary>
    /// <remarks>
    /// Implementations must round-trip exactly: TryUnprotect(Protect(x)) yields x. They should
    /// return false (never throw) for data that was tampered with or cannot be decrypted; the
    /// save service then falls back to the backup copy.
    /// </remarks>
    public interface ISaveDataProtector
    {
        byte[] Protect(byte[] plainBytes);

        /// <summary>Returns false if the data was tampered with, corrupted, or cannot be decrypted.</summary>
        bool TryUnprotect(byte[] protectedBytes, out byte[] plainBytes);
    }
}
