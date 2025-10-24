# Treblle .NET SDK

[![Treblle API Intelligence](https://github.com/user-attachments/assets/b268ae9e-7c8a-4ade-95da-b4ac6fce6eea)](https://treblle.com)

[Website](http://treblle.com/) • [Documentation](https://docs.treblle.com/) • [Pricing](https://treblle.com/pricing)

Treblle is an API intelligence platform that helps developers, teams and organizations understand their APIs from a single integration point.

---

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
  - [Required Settings](#required-settings)
  - [Optional Settings](#optional-settings)
  - [Complete Configuration Example](#complete-configuration-example)
- [Advanced Features](#advanced-features)
  - [Data Masking](#data-masking)
  - [Path Exclusion](#path-exclusion)
  - [Debug Mode](#debug-mode)
  - [Load Balancing](#load-balancing)
- [Performance & Reliability](#performance--reliability)
- [Troubleshooting](#troubleshooting)
- [Support](#support)
- [License](#license)

---

## Features

✅ **Automatic API Monitoring** - Track all endpoints with a single line of code
✅ **Smart Request Filtering** - Automatically excludes static assets and non-API traffic
✅ **Sensitive Data Masking** - Built-in protection for passwords, credit cards, emails, SSN, and more
✅ **Zero Performance Impact** - Optimized async operations, compiled regex, connection pooling
✅ **Production Hardened** - Comprehensive error handling ensures SDK never crashes your API
✅ **Load Balanced** - Automatic distribution across multiple Treblle endpoints
✅ **Highly Configurable** - Flexible path exclusion, custom field masking, debug mode
✅ **No Dependencies** - Works seamlessly with existing ASP.NET Web API applications

---

## Requirements

- **.NET Framework 4.6.2** or higher (supports 4.6.2, 4.7.x, 4.8, and 4.8.1)
- **ASP.NET Web API 5.2.7+**
- **Newtonsoft.Json 13.0.3+**

> **Note**: For WCF services, please use the separate [Treblle.Net.Wcf](https://www.nuget.org/packages/Treblle.Net.Wcf) package.

### Why .NET Framework 4.6.2?

This SDK requires .NET Framework 4.6.2 as the minimum version to ensure:
- ✅ **Security**: TLS 1.2 enabled by default for secure API communication
- ✅ **Compatibility**: Wide enterprise support with long-term Microsoft lifecycle
- ✅ **Performance**: Modern async/await and HttpClient improvements
- ✅ **Reliability**: Stable foundation for production API monitoring

---

## Installation

### Via NuGet Package Manager Console

```powershell
Install-Package Treblle.Net
```

### Via .NET CLI

```bash
dotnet add package Treblle.Net
```

### Via NuGet Package Manager UI

1. Right-click on your project in Solution Explorer
2. Select "Manage NuGet Packages"
3. Search for "Treblle.Net"
4. Click "Install"

During installation, you'll be prompted to enter your **API Key** and **SDK Token**. You can find these in your [Treblle Dashboard](https://platform.treblle.com).

---

## Quick Start

### Step 1: Configure Your Credentials

Add your Treblle credentials to `Web.config`:

```xml
<configuration>
  <appSettings>
    <add key="Treblle:ApiKey" value="{Your_API_Key}" />
    <add key="Treblle:SdkToken" value="{Your_SDK_Token}" />
  </appSettings>
</configuration>
```

> **Get your credentials**: Visit your [Treblle Dashboard](https://platform.treblle.com) → Select your project → Copy SDK Token and API Key

### Step 2: Register the Handler (Automatic Tracking - Recommended)

Simply register the Treblle handler in your `WebApiConfig.cs` and **all API endpoints will be tracked automatically**:

```csharp
using Treblle.Net;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // Add Treblle handler - tracks all endpoints automatically
        config.MessageHandlers.Add(new TreblleHandler());

        // Your existing Web API configuration
        config.MapHttpAttributeRoutes();
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );
    }
}
```

That's it! All your API endpoints are now being monitored. 🎉

### Alternative: Manual Tracking (Legacy)

For fine-grained control, you can use the `[Treblle]` attribute on specific controllers or methods:

```csharp
[Treblle]
public class ProductsController : ApiController
{
    public IHttpActionResult Get()
    {
        return Ok(_productService.GetAll());
    }
}
```

---

## Configuration

### Required Settings

These two settings are **required** for the SDK to function:

```xml
<configuration>
  <appSettings>
    <add key="Treblle:ApiKey" value="{Your_API_Key}" />
    <add key="Treblle:SdkToken" value="{Your_SDK_Token}" />
  </appSettings>
</configuration>
```

### Optional Settings

All optional configuration settings:

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Treblle:ExcludedPaths` | string | (none) | Comma-separated paths to exclude from tracking |
| `Treblle:AdditionalFieldsToMask` | string | (none) | Comma-separated field names to mask |
| `Treblle:DisableMasking` | bool | `false` | **⚠️ Development only**: Disable data masking |
| `Treblle:Debug` | bool | `false` | Enable detailed debug logging |

### Complete Configuration Example

```xml
<configuration>
  <appSettings>
    <!-- Required -->
    <add key="Treblle:ApiKey" value="{Your_API_Key}" />
    <add key="Treblle:SdkToken" value="{Your_SDK_Token}" />

    <!-- Optional -->
    <add key="Treblle:ExcludedPaths" value="/health,/admin/*,/internal/*" />
    <add key="Treblle:AdditionalFieldsToMask" value="apiKey,authToken,internalId" />
    <add key="Treblle:Debug" value="false" />

    <!-- ⚠️ Development/Testing Only -->
    <!-- <add key="Treblle:DisableMasking" value="false" /> -->
  </appSettings>
</configuration>
```

---

## Advanced Features

### Data Masking

The SDK **automatically masks sensitive fields** before sending data to Treblle. This happens on your server before any data leaves your infrastructure.

#### Default Masked Fields

The following fields are automatically masked:

**Authentication & Secrets:**
- `password`, `pwd`
- `secret`
- `password_confirmation`, `passwordConfirmation`

**Financial Data:**
- `cc`, `card_number`, `cardNumber`, `ccv`
- `credit_score`, `creditScore`

**Personal Information:**
- `email`
- `ssn`
- `account.*` (all account fields)

**Nested Fields:**
- `user.email`, `user.dob`, `user.password`, `user.ss`
- `user.payments.cc`

#### Masking Strategies

Different field types use different masking strategies:

| Field Type | Strategy | Example |
|------------|----------|---------|
| Passwords/Secrets | Complete masking | `*****` |
| Credit Cards | Last 4 digits visible | `****-****-****-1234` |
| Emails | Partial masking | `j***@example.com` |
| SSN | Last 4 digits visible | `***-**-1234` |
| Dates | Year masked | `12/25/****` |

#### Custom Field Masking

Add your own fields to mask:

```xml
<add key="Treblle:AdditionalFieldsToMask" value="apiKey: DefaultStringMasker,authToken: DefaultStringMasker,dob: DateMasker" />
```

**Available Maskers:**
- `DefaultStringMasker` - Complete masking
- `CreditCardMasker` - Credit card number masking
- `EmailMasker` - Email address masking
- `SocialSecurityMasker` - SSN masking
- `DateMasker` - Date masking

**Simple format** (uses DefaultStringMasker):
```xml
<add key="Treblle:AdditionalFieldsToMask" value="customSecret,apiToken,internalId" />
```

#### ⚠️ Disabling Masking (Development/Testing Only)

```xml
<add key="Treblle:DisableMasking" value="true" />
```

**Warning**: This sends all data (passwords, credit cards, emails, etc.) in **plain text**. Only use in development/testing environments.

---

### Path Exclusion

#### Automatic Exclusions

The SDK **automatically excludes** common non-API requests:

**File Extensions:**
- `.css`, `.js`, `.map`
- `.jpg`, `.jpeg`, `.png`, `.gif`, `.svg`, `.ico`
- `.woff`, `.woff2`, `.ttf`, `.eot`
- `.mp4`, `.mp3`, `.webm`
- `.pdf`, `.zip`, `.rar`, `.tar`, `.gz`
- `.html`, `.htm`

**Default Paths:**
- `/swagger/*`, `/swagger-ui/*`, `/swagger-resources/*`
- `/assets/*`, `/static/*`, `/public/*`
- `/css/*`, `/js/*`, `/images/*`, `/img/*`, `/fonts/*`
- `/favicon.ico`, `/robots.txt`, `/sitemap.xml`
- `/_next/*`, `/_nuxt/*`, `/node_modules/*`
- `/.well-known/*`

**Content Types:**
- HTML, CSS, JavaScript
- Images, videos, audio
- Fonts
- Binary files

#### Custom Path Exclusions

Exclude specific endpoints using pattern matching:

```xml
<add key="Treblle:ExcludedPaths" value="/health,/admin/*,internal,/debug/info" />
```

**Pattern Matching:**

| Pattern | Behavior | Example |
|---------|----------|---------|
| `/health` | Exact match | Excludes only `/health` |
| `/admin/*` | Wildcard match | Excludes all paths starting with `/admin/` |
| `internal` | Segment match | Excludes any path containing `internal` |
| `/api/v1/status,/api/v2/status` | Multiple patterns | Comma-separated list |

**Examples:**
- `/health` → Matches `/health` only
- `/admin/*` → Matches `/admin/users`, `/admin/settings`, etc.
- `swagger` → Matches `/swagger`, `/api/swagger`, `/v1/swagger/docs`, etc.
- `/api/internal/*,/debug/*` → Matches both patterns

---

### Debug Mode

Enable debug mode to see detailed logging of SDK operations:

```xml
<add key="Treblle:Debug" value="true" />
```

#### Debug Output Includes:

**Configuration Status:**
```
[TREBLLE DEBUG] === TREBLLE CONFIGURATION ===
[TREBLLE DEBUG] ✅ SDK Token: ab12****ef78
[TREBLLE DEBUG] ✅ API Key: cd34****gh90
[TREBLLE DEBUG] 🔧 Debug Mode: ENABLED
```

**Request Tracking:**
```
[TREBLLE DEBUG] 🚀 Request Started: POST /api/users
[TREBLLE DEBUG] 🔒 Applied data masking to 12 fields
[TREBLLE DEBUG] 📊 Payload size: 2.34 KB
[TREBLLE DEBUG] ✅ Request Completed: POST /api/users - Status: 201 - Load Time: 155ms
[TREBLLE DEBUG] 📤 Payload sent to Treblle - Response: 200
```

**Skipped Requests:**
```
[TREBLLE DEBUG] ⏭️ Request skipped: Missing SDK Token or API Key configuration
[TREBLLE DEBUG] ⏭️ Request skipped: Filtered out (static asset)
[TREBLLE DEBUG] ⏭️ Request skipped: Response content type not tracked (text/html)
```

**Error Tracking:**
```
[TREBLLE DEBUG] ❌ Error in request capture: NullReferenceException
[TREBLLE DEBUG] ❌ Error in payload sending: HTTP 401: Unauthorized
```

**Important:** Disable debug mode in production for optimal performance:
```xml
<add key="Treblle:Debug" value="false" />
<!-- Or simply remove the setting entirely -->
```

---

### Load Balancing

The SDK **automatically distributes requests** across multiple Treblle endpoints for improved reliability and performance:

- **Three Endpoints**:
  - `rocknrolla.treblle.com`
  - `punisher.treblle.com`
  - `sicario.treblle.com`
- **Random Selection**: Each request is sent to a randomly selected endpoint
- **Thread-Safe**: Uses proper locking for concurrent requests
- **No Configuration Required**: Works automatically out of the box
- **Geographic Distribution**: Improves reliability and reduces latency

You don't need to configure anything - load balancing happens automatically!

---

## Performance & Reliability

### Performance Optimizations

The SDK is built for **zero-impact production use**:

✅ **Async/Await Throughout** - Non-blocking I/O operations
✅ **Connection Pooling** - Reuses HTTP connections (90-second keep-alive)
✅ **Compiled Regex Patterns** - 10-50x faster than runtime compilation
✅ **Pre-allocated Dictionaries** - Reduces memory allocations
✅ **Smart Masking** - Skips unnecessary pattern matching when field names match
✅ **Lazy Initialization** - Maskers loaded only when needed
✅ **O(1) Masker Lookup** - Dictionary-based type resolution
✅ **Optimized JSON Processing** - Single deserialization during masking

### Reliability Features

The SDK is **production-hardened** to never crash your API:

✅ **Comprehensive Error Handling** - All operations wrapped in try-catch
✅ **Graceful Degradation** - Failures log but don't prevent tracking
✅ **Original Exception Preservation** - API errors always re-thrown correctly
✅ **Stack Overflow Protection** - Max depth limits on recursive operations
✅ **Circular Reference Handling** - Prevents infinite serialization loops
✅ **Thread-Safe Initialization** - Double-check locking for concurrent requests
✅ **Automatic Resource Cleanup** - Proper disposal of HTTP responses
✅ **DoS Protection** - 2MB payload limit with clear size reporting
✅ **Network Timeout Protection** - 10-second timeout on all requests
✅ **Memory Leak Prevention** - All IDisposable resources properly managed

**SDK Guarantee**: The SDK will **never crash your host API**, regardless of:
- Invalid JSON in requests/responses
- Network failures or timeouts
- Treblle API downtime
- Malicious or malformed payloads
- Circular object references
- Concurrent initialization
- Any other failure scenario

---

## Troubleshooting

### SDK Not Tracking Requests

**Check configuration:**
```xml
<!-- Both are required -->
<add key="Treblle:ApiKey" value="{Your_API_Key}" />
<add key="Treblle:SdkToken" value="{Your_SDK_Token}" />
```

**Enable debug mode to see what's happening:**
```xml
<add key="Treblle:Debug" value="true" />
```

**Common issues:**
1. ✅ Handler not registered in `WebApiConfig.cs`
2. ✅ Request is being filtered out (check debug logs)
3. ✅ Path is in excluded paths list
4. ✅ Content type is not JSON-based

### Requests Being Filtered

Check debug logs for skip reasons:
```
[TREBLLE DEBUG] ⏭️ Request skipped: Filtered out (static asset)
```

**Common filter reasons:**
- Static file extensions (`.css`, `.js`, `.png`, etc.)
- HTML responses (`text/html`)
- Path matches excluded patterns
- No JSON response payload

### Large Payloads

Requests/responses **over 2MB** are replaced with size information:
```json
{
  "message": "Response payload over 2MB limit",
  "size_bytes": 3145728,
  "size_mb": 3.0,
  "treblle_info": "Payload content replaced due to size limit"
}
```

**Solution**: This is by design to prevent performance issues. The event is still tracked with metadata.

### Masking Issues

**Field not being masked:**
1. Check field name spelling
2. Verify it's in the masking map (default or custom)
3. Enable debug mode to see masking application

**Adding custom fields:**
```xml
<add key="Treblle:AdditionalFieldsToMask" value="customField,anotherField" />
```

### Network Errors

**Connection timeout:**
- Default timeout: 10 seconds
- Check firewall rules for outbound HTTPS
- Verify network connectivity to `*.treblle.com`

**DNS issues:**
- Ensure DNS can resolve Treblle endpoints
- Check corporate proxy settings

---

## Supported Content Types

### Requests (Accepted)
- `application/json` ✅
- `application/x-www-form-urlencoded` ✅
- `multipart/form-data` ✅

### Responses (Tracked)
- `application/json` ✅
- `application/vnd.api+json` ✅
- `application/ld+json` ✅
- `application/hal+json` ✅
- `application/problem+json` ✅
- `text/json` ✅
- Empty responses (201, 204, etc.) ✅

### Not Supported
- `application/xml` ❌ (Use Treblle.Net.Wcf for WCF/XML services)
- `text/xml` ❌
- `text/html` ❌
- Binary content types ❌

---

## Support

If you have problems of any kind, feel free to reach out:

- **Email**: support@treblle.com
- **Website**: <https://treblle.com>
- **Documentation**: <https://docs.treblle.com/en/integrations/net>
- **Community**: <https://treblle.com/chat>
- **GitHub Issues**: <https://github.com/Treblle/treblle-net/issues>

---

## License

Copyright 2025, Treblle Inc. Licensed under the MIT license:
http://www.opensource.org/licenses/mit-license.php
