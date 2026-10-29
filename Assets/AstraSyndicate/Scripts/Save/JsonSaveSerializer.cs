using System.Text;
using UnityEngine;

namespace AstraSyndicate.Save
{
    /// <summary>
    /// JsonUtility-based serializer. Save data classes must be [Serializable] and keep their state
    /// in public or [SerializeField] fields (use lists instead of dictionaries).
    /// </summary>
    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        private readonly bool _prettyPrint;

        public JsonSaveSerializer(bool prettyPrint = false)
        {
            _prettyPrint = prettyPrint;
        }

        public byte[] Serialize<T>(T data) where T : class
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, _prettyPrint));
        }

        public T Deserialize<T>(byte[] bytes) where T : class
        {
            return JsonUtility.FromJson<T>(Encoding.UTF8.GetString(bytes));
        }
    }
}
