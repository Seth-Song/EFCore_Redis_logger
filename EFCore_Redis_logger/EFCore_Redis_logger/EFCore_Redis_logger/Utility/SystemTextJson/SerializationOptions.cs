using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace EFCore_Redis_logger.Utility.SystemTextJson
{
    public static class SerializationOptions
    {
        static SerializationOptions()
        {
            Flexible = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                IgnoreNullValues = true,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        public static JsonSerializerOptions Flexible { get; private set; }
    }
}
