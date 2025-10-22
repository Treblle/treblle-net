using System;
using System.Reflection;

namespace Treblle.Net.Helpers
{
    public static class EnvironmentHelper
    {
        public static string GetCSharpVersion()
        {
            #if CSHARP_3_0
                                return "3";
            #elif CSHARP_4_0
                                return "4";
            #elif CSHARP_5_0
                                return "5";
            #elif CSHARP_6_0
                                return "6";
            #elif CSHARP_7_0
                                return "7.0";
            #elif CSHARP_7_1
                                return "7.1";
            #elif CSHARP_7_2
                                return "7.2";
            #elif CSHARP_7_3
                                return "7.3";
            #elif CSHARP_8_0
                                return "8";
            #elif CSHARP_9_0
                                return "9";
            #elif CSHARP_10_0
                                return "10";
            #else
                        return "";
            #endif
        }

        public static Language ExtractLanguageData()
        {
            var language = new Language();
            language.Name = "c#";
            language.Version = EnvironmentHelper.GetCSharpVersion();

            return language;
        }

        public static Os ExtractOsData()
        {
            var os = new Os();

            os.Name = Environment.OSVersion.ToString();
            os.Release = Environment.OSVersion.Version.ToString();
            os.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

            return os;
        }
    }
}
