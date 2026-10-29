namespace AstraSyndicate.Save
{
    /// <summary>
    /// Development default: stores plain, readable JSON. Ship builds should assign an encrypting
    /// <see cref="SaveProtectorProvider"/> in GameConfig instead.
    /// </summary>
    public sealed class PassthroughDataProtector : ISaveDataProtector
    {
        public byte[] Protect(byte[] plainBytes)
        {
            return plainBytes;
        }

        public bool TryUnprotect(byte[] protectedBytes, out byte[] plainBytes)
        {
            plainBytes = protectedBytes;
            return protectedBytes != null;
        }
    }
}
