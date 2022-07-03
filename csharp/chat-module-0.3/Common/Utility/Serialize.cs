using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Common.Utility
{
    public static class Serializer<T>
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = true, };

        public static string Serialize(T obj)
        {
            return JsonSerializer.Serialize(obj, Options);
        }
        public static T Deserialize(string str)
        {
            T? obj = JsonSerializer.Deserialize<T>(str);
            if (obj == null)
            {
                throw new JsonException($"파싱 실패\n{str}");
            }
            return obj;
        }
    }
}
