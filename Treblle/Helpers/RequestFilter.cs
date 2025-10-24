using System;
using System.Linq;
using System.Net.Http;

namespace Treblle.Net.Helpers
{
    /// <summary>
    /// Intelligent filtering to track only valid API requests, excluding static assets and non-API content
    /// </summary>
    internal static class RequestFilter
    {
        /// <summary>
        /// Content types that should be tracked by Treblle (API responses)
        /// Treblle only supports JSON-based response payloads or no response payload
        /// </summary>
        private static readonly string[] TrackedContentTypes = new[]
        {
            "application/json",
            "text/json",
            "application/vnd.api+json",
            "application/ld+json",
            "application/hal+json",
            "application/problem+json"
        };

        /// <summary>
        /// Content types that should NOT be tracked (static resources)
        /// </summary>
        private static readonly string[] ExcludedContentTypes = new[]
        {
            "text/html",
            "text/css",
            "text/javascript",
            "application/javascript",
            "application/x-javascript",
            "image/",
            "video/",
            "audio/",
            "font/",
            "application/font",
            "application/vnd.ms-fontobject",
            "application/x-font-ttf",
            "application/octet-stream"
        };

        /// <summary>
        /// Path patterns that should be automatically excluded (static resources, framework paths)
        /// </summary>
        private static readonly string[] DefaultExcludedPaths = new[]
        {
            "/swagger/*",
            "/swagger-ui/*",
            "/swagger-resources/*",
            "/webjars/*",
            "/assets/*",
            "/static/*",
            "/public/*",
            "/css/*",
            "/js/*",
            "/images/*",
            "/img/*",
            "/fonts/*",
            "/favicon.ico",
            "/robots.txt",
            "/sitemap.xml",
            "/_next/*",
            "/_nuxt/*",
            "/node_modules/*",
            "/.well-known/*"
        };

        /// <summary>
        /// File extensions that should be excluded (static assets)
        /// </summary>
        private static readonly string[] ExcludedExtensions = new[]
        {
            ".css",
            ".js",
            ".map",
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".svg",
            ".ico",
            ".woff",
            ".woff2",
            ".ttf",
            ".eot",
            ".mp4",
            ".mp3",
            ".webm",
            ".pdf",
            ".zip",
            ".rar",
            ".tar",
            ".gz",
            ".html",
            ".htm"
        };

        /// <summary>
        /// Determines if a request should be tracked based on path and extension
        /// </summary>
        public static bool ShouldTrackRequest(HttpRequestMessage request, string[] userExcludedPaths)
        {
            var path = request.RequestUri.AbsolutePath;

            // Check file extension first (fastest check)
            if (HasExcludedExtension(path))
            {
                return false;
            }

            // Check user-configured excluded paths
            if (userExcludedPaths != null && userExcludedPaths.Length > 0)
            {
                if (MatchesPathPattern(path, userExcludedPaths))
                {
                    return false;
                }
            }

            // Check default excluded paths (static resources)
            if (MatchesPathPattern(path, DefaultExcludedPaths))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if a response should be tracked based on content type
        /// </summary>
        public static bool ShouldTrackResponse(HttpResponseMessage response)
        {
            var contentType = response.Content?.Headers?.ContentType?.MediaType;

            if (string.IsNullOrEmpty(contentType))
            {
                // If no content type, assume it's an API response
                return true;
            }

            // Normalize content type
            var normalizedContentType = contentType.ToLowerInvariant();

            // Check if it's an explicitly excluded content type
            if (ExcludedContentTypes.Any(excluded =>
                normalizedContentType.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Check if it's a tracked content type
            if (TrackedContentTypes.Contains(normalizedContentType))
            {
                return true;
            }

            // For unknown content types, check if it looks like an API response
            var statusCode = (int)response.StatusCode;
            return (statusCode >= 200 && statusCode < 300) || // Success responses
                   (statusCode >= 400 && statusCode < 500) || // Client errors
                   statusCode == 500; // Server errors
        }

        /// <summary>
        /// Checks if path has a file extension that should be excluded
        /// </summary>
        private static bool HasExcludedExtension(string path)
        {
            return ExcludedExtensions.Any(ext =>
                path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Matches path against pattern array
        /// </summary>
        private static bool MatchesPathPattern(string path, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                // Exact match
                if (string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Wildcard match (e.g., "/admin/*")
                if (pattern.EndsWith("/*"))
                {
                    var prefix = pattern.Substring(0, pattern.Length - 2);
                    if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Segment match (e.g., "swagger" matches any path containing "swagger")
                if (path.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the default excluded paths for documentation purposes
        /// </summary>
        public static string[] GetDefaultExcludedPaths() => DefaultExcludedPaths;

        /// <summary>
        /// Gets the excluded file extensions for documentation purposes
        /// </summary>
        public static string[] GetExcludedExtensions() => ExcludedExtensions;
    }
}
