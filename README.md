# Treblle - API Intelligence Platform

[![Treblle API Intelligence](https://github.com/user-attachments/assets/b268ae9e-7c8a-4ade-95da-b4ac6fce6eea)](https://treblle.com)

[Website](http://treblle.com/) • [Documentation](https://docs.treblle.com/) • [Pricing](https://treblle.com/pricing)

Treblle is an API intelligence platfom that helps developers, teams and organizations understand their APIs from a single integration point.

---

# Treblle .NET SDK

[![Latest Version](https://img.shields.io/nuget/v/Treblle.Net)](https://www.nuget.org/packages/Treblle.Net)
[![Total Downloads](https://img.shields.io/nuget/dt/Treblle.Net)](https://www.nuget.org/packages/Treblle.Net)

## Requirements

- **.NET Framework 4.6.2** or higher (supports 4.6.2, 4.7.x, 4.8, and 4.8.1)
- **ASP.NET Web API 5.2.7+**
- **Newtonsoft.Json 13.0.3+**


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

> **Get your credentials**: Visit your [Treblle Dashboard](https://platform.treblle.com) → Find your API → Navigate to Settings → Copy SDK Token and API Key

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
| `Treblle:DisableMasking` | bool | `false` | Disable data masking |
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
    <add key="Treblle:DisableMasking" value="false" />
  </appSettings>
</configuration>
```

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

#### Disabling Masking

```xml
<add key="Treblle:DisableMasking" value="true" />
```

**Warning**: This sends all data (passwords, credit cards, emails, etc.) in **plain text**. We suggest that you use this only in development/testing environments.

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

# Migration Guide (v1 → v2)

Upgrading from Treblle.Net v1.x to v2.x requires a few configuration changes. This guide will help you migrate smoothly.

### Breaking Changes

#### 1. Configuration Key Changes

The configuration keys have been renamed to use the `Treblle:` prefix:

**Old (v1.x):**
```xml
<appSettings>
  <add key="TreblleApiKey" value="{Your_API_Key}" />
  <add key="TreblleProjectId" value="{Your_Project_Id}" />
</appSettings>
```

**New (v2.x):**
```xml
<appSettings>
  <add key="Treblle:ApiKey" value="{Your_API_Key}" />
  <add key="Treblle:SdkToken" value="{Your_SDK_Token}" />
</appSettings>
```

**Note:** `TreblleProjectId` is now called `Treblle:SdkToken`. You can find your SDK Token in the [Treblle Dashboard](https://platform.treblle.com).

#### 2. Additional Configuration Keys

All optional configuration keys now use the `Treblle:` prefix:

| v1.x Key | v2.x Key |
|----------|----------|
| `AdditionalFieldsToMask` | `Treblle:AdditionalFieldsToMask` |
| `DisableMasking` | `Treblle:DisableMasking` |
| `Debug` | `Treblle:Debug` |
| N/A | `Treblle:ExcludedPaths` *(new feature)* |

### Step-by-Step Migration

#### Step 1: Update NuGet Package

```powershell
# Update to v2.x
Update-Package Treblle.Net
```

Or via .NET CLI:
```bash
dotnet add package Treblle.Net --version 2.0.1
```

#### Step 2: Update Web.config

Replace your old configuration:

```xml
<!-- OLD - Remove this -->
<appSettings>
  <add key="TreblleApiKey" value="your-api-key-here" />
  <add key="TreblleProjectId" value="your-project-id-here" />
  <add key="AdditionalFieldsToMask" value="field1,field2" />
</appSettings>
```

With the new configuration:

```xml
<!-- NEW - Use this -->
<appSettings>
  <add key="Treblle:ApiKey" value="your-api-key-here" />
  <add key="Treblle:SdkToken" value="your-sdk-token-here" />
  <add key="Treblle:AdditionalFieldsToMask" value="field1,field2" />
</appSettings>
```

#### Step 3: Get Your SDK Token

1. Log in to [Treblle Dashboard](https://platform.treblle.com)
2. Select your project
3. Go to **Settings** → **Project Info**
4. Copy your **SDK Token** (this replaces the old Project ID)

## Important: Global HTTP Configuration

The Treblle SDK modifies some global `ServicePointManager` settings when first initialized. These changes affect **all HTTP clients** in your application, including third-party libraries.

### Modified Settings

The SDK configures the following global settings for optimal performance and security:

| Setting | Value | Purpose |
|---------|-------|---------|
| `SecurityProtocol` | TLS 1.2 + TLS 1.1 | Ensures secure HTTPS communication |
| `DefaultConnectionLimit` | 10 | Allows up to 10 concurrent connections per endpoint |
| `MaxServicePointIdleTime` | 90 seconds | Keeps connections alive for connection reuse |
| `Expect100Continue` | Disabled | Improves request performance |
| `UseNagleAlgorithm` | Disabled | Reduces latency for small packets |

### Impact on Your Application

These settings will apply to **all** `HttpClient`, `WebClient`, and `HttpWebRequest` instances in your application, not just Treblle's HTTP calls.

**Good news**: These are generally beneficial defaults that improve performance and security for most applications.

### When You Need Different Settings

If your application requires different `ServicePointManager` settings:

1. **Configure after Treblle initializes**: Add your custom settings in `Global.asax.cs` or your startup code after the first Treblle request
2. **Override specific settings**: You can change any setting after Treblle loads - your changes will persist

```csharp
// In Global.asax.cs Application_Start
protected void Application_Start()
{
    // Your Web API config (Treblle gets initialized here)
    GlobalConfiguration.Configure(WebApiConfig.Register);

    // Override Treblle's settings if needed
    ServicePointManager.DefaultConnectionLimit = 100; // Your custom value
    ServicePointManager.UseNagleAlgorithm = true;      // Your custom value
}
```

3. **Need help?**: If this causes issues, please [open an issue on GitHub](https://github.com/Treblle/treblle-net/issues) - we're happy to make this configurable

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

### Network Errors

**Connection timeout:**
- Default timeout: 10 seconds
- Check firewall rules for outbound HTTPS
- Verify network connectivity to `*.treblle.com`

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


## Support

If you have problems of any kind feel free to reach out via <https://treblle.com> or email support@treblle.com and we'll do our best to help you out.

## License

Copyright 2025, Treblle Inc. Licensed under the MIT license:
http://www.opensource.org/licenses/mit-license.php