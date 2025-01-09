using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Treblle.Net
{
    public static class JsonMasker
    {
        public static string Mask(this string json, string[] blacklist, string mask)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            if (blacklist == null || !blacklist.Any())
            {
                return json;
            }

            var jsonObject = (JObject)JsonConvert.DeserializeObject(json);
            var blacklistSet = new HashSet<string>(blacklist);

            MaskFieldsFromJToken(jsonObject, blacklistSet, mask);

            var result = jsonObject.ToString();

            return result;
        }

        private static void MaskFieldsFromJToken(JToken token, HashSet<string> blacklist, string mask)
        {
            if (token is null || !(token is JContainer container))
            {
                return;
            }

            foreach (var jToken in container.Children())
            {
                if (jToken is JProperty prop)
                {
                    if (blacklist.Contains(prop.Name))
                    {
                        prop.Value = mask;
                    }
                }

                MaskFieldsFromJToken(jToken, blacklist, mask);
            }
        }

    }
}
