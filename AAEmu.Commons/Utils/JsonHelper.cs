using System;
using Newtonsoft.Json;

namespace AAEmu.Commons.Utils
{
    public class JsonHelper
    {
        public static T DeserializeObject<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        public static bool TryDeserializeObject<T>(string json, out T result, out Exception error)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = new ArgumentException("NullOrWhiteSpace", "json");
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                result = default;
                error = e;
                return false;
            }

            error = null;
            return result != null; // TODO Checking value of 'result' for null will always return false when generic type is instantiated with a value type.
        }
    }
}
