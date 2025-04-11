using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Treblle.Net.Helpers;

namespace Treblle.Net.Masking
{

    public static class JsonMasker
    {
        static List<DefaultStringMasker> maskers = null;

        public static string Mask(this string json, Dictionary<string, string> maskingMap, string mask)
        {
            if (string.IsNullOrWhiteSpace(json) || maskingMap.Count == 0)
            {
                return json;
            }

            var jsonObject = JsonConvert.DeserializeObject(json) as JObject;
            if (jsonObject == null)
            {
                return json;
            }

            if (maskers == null)
            {
                loadMaskers();
            }

            MaskFieldsFromJToken(jsonObject, maskingMap, mask, new List<string>());

            return jsonObject.ToString();
        }

        private static void MaskFieldsFromJToken(JToken token, Dictionary<string, string> maskingMap, string mask, List<string> path)
        {
            if (token == null || !(token is JContainer container))
            {
                return;
            }

            foreach (var jToken in container.Children())
            {
                if (jToken is JProperty prop)
                {
                    var currentPath = string.Join(".", path.Concat(new[] { prop.Name }));

                    if (prop.Value is JArray array)
                    {
                        for (int i = 0; i < array.Count; i++)
                        {
                            var item = array[i];

                            if (item is JContainer)
                            {
                                MaskFieldsFromJToken(item, maskingMap, mask, path.Concat(new[] { prop.Name, i.ToString() }).ToList());
                            }
                            else if (item is JValue value)
                            {
                                MaskArrayElementIfNeeded(array, i, value, maskingMap, mask, currentPath);
                            }
                        }
                    }
                    else if (prop.Value is JContainer)
                    {
                        MaskFieldsFromJToken(prop.Value, maskingMap, mask, path.Concat(new[] { prop.Name }).ToList());
                    }
                    else if (prop.Value != null)
                    {
                        bool isValueMasked = false;
                        foreach (KeyValuePair<string, string> map in maskingMap)
                        {

                            if (shouldMaskPath(map.Key, currentPath))
                            {
                                DefaultStringMasker masker = maskers.Where(obj => obj.GetType().Name == map.Value)
                                    .SingleOrDefault();

                                if (masker != null)
                                {
                                    prop.Value = masker.Mask(prop.Value.ToString());
                                    isValueMasked = true;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine($"Could not resolve masker for field {currentPath}");
                                }
                            }

                            // if the value is not masked go over mapping once again to check if value matches any pattern
                            if (!isValueMasked)
                            {
                                foreach (DefaultStringMasker masker in maskers)
                                {
                                    if (masker.IsPatternMatch(prop.Value.ToString()))
                                    {
                                        prop.Value = masker.Mask(prop.Value.ToString());
                                        break;
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        private static void MaskArrayElementIfNeeded(JArray array, int index, JValue value, Dictionary<string, string> maskingMap, string mask, string currentPath)
        {
            foreach (var map in maskingMap)
            {
                if (shouldMaskPath(map.Key, currentPath))
                {
                    var masker = maskers.FirstOrDefault(x => x.GetType().Name == map.Value);
                    if (masker != null)
                    {
                        array[index] = masker.Mask(value.ToString());
                        return;
                    }
                }
            }
        }

        private static bool shouldMaskPath(string sensitiveWord, string path)
        {
            sensitiveWord = sensitiveWord.ToLower();
            path = path.ToLower();
            return sensitiveWord.Contains(".")
                ? (path.Contains(sensitiveWord) || (sensitiveWord.EndsWith("*") && path.Contains(sensitiveWord.Substring(0, sensitiveWord.Length - 1))))
                : (path.Equals(sensitiveWord) || path.Contains($".{sensitiveWord}"));
        }

        private static void loadMaskers()
        {
            maskers = new List<DefaultStringMasker>();
            var allMaskerTypes = AssemblyHelper.GetClassesDerivedFromType(typeof(IStringMasker));

            foreach (var type in allMaskerTypes)
            {
                DefaultStringMasker instance = (DefaultStringMasker)AssemblyHelper.CreateInstance(type);
                maskers.Add(instance);
            }
        }

    }
}