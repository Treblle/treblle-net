using Newtonsoft.Json;
using System.Collections.Generic;

namespace Treblle.Net
{
    public class Os
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("release")]
        public string Release { get; set; }
        [JsonProperty("architecture")]
        public string Architecture { get; set; }
    }

    public class Server
    {
        [JsonProperty("ip")]
        public string Ip { get; set; } = "bogon";

        [JsonProperty("timezone")]
        public string Timezone { get; set; } = "UTC";

        [JsonProperty("software")]
        public string Software { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("os")]
        public Os Os { get; set; }
    }

    public class Language
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class Request
    {
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; } = "bogon";

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("user_agent")]
        public string UserAgent { get; set; } = "";

        [JsonProperty("method")]
        public string Method { get; set; } = "GET";

        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        [JsonProperty("body")]
        public object Body { get; set; } = new { };

        [JsonProperty("route_path")]
        public string RoutePath { get; set; }

        [JsonProperty("query")]
        public Dictionary<string, string> Query { get; set; } = new Dictionary<string, string>();
    }

    public class Error
    {
        [JsonProperty("source")]
        public string Source { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("file")]
        public string File { get; set; }
        [JsonProperty("line")]
        public int Line { get; set; }
    }

    public class Response
    {
        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        [JsonProperty("code")]
        public int Code { get; set; } = 200;

        [JsonProperty("size")]
        public double Size { get; set; } = 0;

        [JsonProperty("load_time")]
        public double LoadTime { get; set; } = 0;

        [JsonProperty("body")]
        public object Body { get; set; } = new { };
    }

    public class Data
    {
        [JsonProperty("server")]
        public Server Server { get; set; }

        [JsonProperty("language")]
        public Language Language { get; set; }

        [JsonProperty("request")]
        public Request Request { get; set; }

        [JsonProperty("response")]
        public Response Response { get; set; }

        [JsonProperty("errors")]
        public List<Error> Errors { get; set; } = new List<Error>();
    }

    public class TrebllePayload
    {
        [JsonProperty("api_key")]
        public string ApiKey { get; set; }

        [JsonProperty("sdk_token")]
        public string SdkToken { get; set; }

        [JsonProperty("sdk")]
        public string Sdk { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }
}
