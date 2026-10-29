namespace AstraSyndicate.Save
{
    /// <summary>Converts save data objects to bytes and back.</summary>
    public interface ISaveSerializer
    {
        byte[] Serialize<T>(T data) where T : class;

        T Deserialize<T>(byte[] bytes) where T : class;
    }
}
