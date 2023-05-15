using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

            MaskFieldsFromJToken(jsonObject, blacklist, mask);

            var result = jsonObject.ToString();

            return result;
        }

        private static void MaskFieldsFromJToken(JToken token, string[] blacklist, string mask)
        {
            var container = token as JContainer;

            if (container == null)
            {
                return; // abort recursive
            }

            var removeList = new List<JToken>();
            foreach (var jToken in container.Children())
            {
                if (jToken is JProperty prop)
                {
                    var matching = blacklist.Any(item =>
                    {
                        return Regex.IsMatch(prop.Path, "(?<=\\.)(\\b" + item + "\\b)(?=\\.?)", RegexOptions.IgnoreCase);
                    });

                    if (matching)
                    {
                        removeList.Add(jToken);
                    }
                }

                // call recursive 
                MaskFieldsFromJToken(jToken, blacklist, mask);
            }

            // replace 
            foreach (var el in removeList)
            {
                var prop = (JProperty)el;

                prop.Value = mask;
            }
        }
    }
}
