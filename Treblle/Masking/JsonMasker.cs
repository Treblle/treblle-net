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
        private static List<DefaultStringMasker> maskers = null;
        private static Dictionary<string, DefaultStringMasker> maskersByType = null;
        private static readonly object maskerLock = new object();

        /// <summary>
        /// Masks sensitive fields in a JSON payload containing RawJsonString objects
        /// This method deserializes RawJsonString objects only once during masking
        /// </summary>
        public static string MaskPayload(this string json, Dictionary<string, string> maskingMap, string mask)
        {
            if (string.IsNullOrWhiteSpace(json) || maskingMap.Count == 0)
            {
                return json;
            }

            try
            {
                if (maskers == null)
                {
                    loadMaskers();
                }

                // Deserialize the full payload
                var jsonObject = JsonConvert.DeserializeObject<JObject>(json);
                if (jsonObject == null)
                {
                    DebugLogger.LogWarning("Failed to deserialize JSON for masking - returning original");
                    return json;
                }

                // Process the entire tree, handling RawJsonString objects
                MaskFieldsFromJToken(jsonObject, maskingMap, mask, new List<string>(), 0);

                // Serialize with custom settings to handle RawJsonString
                return JsonConvert.SerializeObject(jsonObject);
            }
            catch (JsonException ex)
            {
                DebugLogger.LogError("JSON masking (invalid JSON)", ex);
                return json; // Return original on JSON errors
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JSON masking", ex);
                return json; // Return original on any masking error - never crash host
            }
        }

        public static string Mask(this string json, Dictionary<string, string> maskingMap, string mask)
        {
            if (string.IsNullOrWhiteSpace(json) || maskingMap.Count == 0)
            {
                return json;
            }

            try
            {
                var jsonObject = JsonConvert.DeserializeObject(json) as JObject;
                if (jsonObject == null)
                {
                    return json;
                }

                if (maskers == null)
                {
                    loadMaskers();
                }

                MaskFieldsFromJToken(jsonObject, maskingMap, mask, new List<string>(), 0);

                return jsonObject.ToString();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JSON masking", ex);
                return json; // Return original on any masking error
            }
        }

        private const int MaxNestingDepth = 50; // Prevent stack overflow on deeply nested JSON

        private static void MaskFieldsFromJToken(JToken token, Dictionary<string, string> maskingMap, string mask, List<string> path, int depth)
        {
            if (token == null || !(token is JContainer container))
            {
                return;
            }

            // Prevent stack overflow on deeply nested JSON
            if (depth > MaxNestingDepth)
            {
                DebugLogger.LogWarning($"Max nesting depth ({MaxNestingDepth}) exceeded in JSON masking - skipping deeper levels");
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
                                MaskFieldsFromJToken(item, maskingMap, mask, path.Concat(new[] { prop.Name, i.ToString() }).ToList(), depth + 1);
                            }
                            else if (item is JValue value)
                            {
                                MaskArrayElementIfNeeded(array, i, value, maskingMap, mask, currentPath);
                            }
                        }
                    }
                    else if (prop.Value is JContainer)
                    {
                        MaskFieldsFromJToken(prop.Value, maskingMap, mask, path.Concat(new[] { prop.Name }).ToList(), depth + 1);
                    }
                    else if (prop.Value != null)
                    {
                        bool isValueMasked = false;
                        var propValueStr = prop.Value.ToString();

                        // First pass: Check if field name matches masking map
                        foreach (KeyValuePair<string, string> map in maskingMap)
                        {
                            if (shouldMaskPath(map.Key, currentPath))
                            {
                                // Use cached masker lookup by type name
                                if (maskersByType.TryGetValue(map.Value, out var masker))
                                {
                                    prop.Value = masker.Mask(propValueStr);
                                    isValueMasked = true;
                                    break; // Exit early - field name matched
                                }
                                else
                                {
                                    DebugLogger.LogWarning($"Could not resolve masker for field {currentPath}");
                                }
                            }
                        }

                        // Second pass: Only if field name didn't match, check if value matches any pattern
                        // This reduces expensive regex operations
                        if (!isValueMasked)
                        {
                            foreach (DefaultStringMasker masker in maskers)
                            {
                                if (masker.IsPatternMatch(propValueStr))
                                {
                                    prop.Value = masker.Mask(propValueStr);
                                    break; // Exit early - pattern matched
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
                    // Use cached masker lookup by type name
                    if (maskersByType.TryGetValue(map.Value, out var masker))
                    {
                        array[index] = masker.Mask(value.ToString());
                        return;
                    }
                }
            }
        }

        private static bool shouldMaskPath(string sensitiveWord, string path)
        {
            // Use case-insensitive comparison without allocating new strings
            if (sensitiveWord.Contains("."))
            {
                // Nested path matching
                return path.IndexOf(sensitiveWord, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       (sensitiveWord.EndsWith("*") &&
                        path.IndexOf(sensitiveWord.Substring(0, sensitiveWord.Length - 1), StringComparison.OrdinalIgnoreCase) >= 0);
            }
            else
            {
                // Simple field name matching
                return path.Equals(sensitiveWord, StringComparison.OrdinalIgnoreCase) ||
                       path.IndexOf($".{sensitiveWord}", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private static void loadMaskers()
        {
            // Double-check locking pattern for thread safety
            if (maskers == null)
            {
                lock (maskerLock)
                {
                    if (maskers == null)
                    {
                        var tempMaskers = new List<DefaultStringMasker>();
                        var tempMaskersByType = new Dictionary<string, DefaultStringMasker>();
                        var allMaskerTypes = AssemblyHelper.GetClassesDerivedFromType(typeof(IStringMasker));

                        foreach (var type in allMaskerTypes)
                        {
                            DefaultStringMasker instance = (DefaultStringMasker)AssemblyHelper.CreateInstance(type);
                            tempMaskers.Add(instance);
                            // Cache by type name for O(1) lookup
                            tempMaskersByType[type.Name] = instance;
                        }

                        // Assign atomically after full initialization
                        maskersByType = tempMaskersByType;
                        maskers = tempMaskers;
                    }
                }
            }
        }

    }
}