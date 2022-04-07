using System.Text.Json;

namespace EFCore_Redis_logger.Utility.SystemTextJson
{
    public class SystemTextJsonSerializer
    {
        public T Deserialize<T>(string serializedObject)
        {
            return JsonSerializer.Deserialize<T>(serializedObject, SerializationOptions.Flexible);
        }

        /// <inheritdoc/>
        public string Serialize(object item)
        {
            return JsonSerializer.Serialize(item, SerializationOptions.Flexible);
        }
    }
}
