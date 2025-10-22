# Treblle .NET SDK

[![Treblle API Intelligence](https://github.com/user-attachments/assets/b268ae9e-7c8a-4ade-95da-b4ac6fce6eea)](https://treblle.com)

[Website](http://treblle.com/) • [Documentation](https://docs.treblle.com/) • [Pricing](https://treblle.com/pricing)

Treblle is an API intelligence platform that helps developers, teams and organizations understand their APIs from a single integration point.

---

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
  - [Additional Field Masking](#additional-field-masking)
- [Data Masking](#data-masking)
- [Support](#support)
- [License](#license)

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

During installation, you'll be prompted to enter your **API Key** and **Project ID**. You can find these in your [Treblle Dashboard](https://platform.treblle.com).

---

## Quick Start

### Automatic Tracking (Recommended)

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

That's it! All your API endpoints are now being monitored.

### Intelligent Filtering

The SDK **automatically excludes** common non-API requests to keep your data clean:

**Excluded by default:**
- Static assets (`.js`, `.css`, `.png`, `.jpg`, `.svg`, `.woff`, etc.)
- Framework paths (`/swagger/*`, `/assets/*`, `/static/*`, `/node_modules/*`)
- Common files (`/favicon.ico`, `/robots.txt`, `/sitemap.xml`)
- Non-API content types (HTML, CSS, JavaScript, images, videos, fonts)

**Tracked automatically:**
- JSON APIs (`application/json`)
- XML APIs (`application/xml`)
- Form submissions (`application/x-www-form-urlencoded`)
- Other API content types (`application/vnd.api+json`, `application/hal+json`, etc.)

### Excluding Specific Endpoints

You can exclude endpoints using the `Treblle:ExcludedPaths` configuration:

```xml
<configuration>
  <appSettings>
    <add key="Treblle:SdkToken" value="{Your_SDK_Token}" />
    <add key="Treblle:ApiKey" value="{Your_API_Key}" />
    <add key="Treblle:ExcludedPaths" value="/health,/admin/*,/swagger/*" />
  </appSettings>
</configuration>
```

**Pattern matching examples:**
- `/health` - Exact match
- `/admin/*` - Wildcard (excludes all paths starting with `/admin/`)
- `swagger` - Segment match (excludes any path containing `swagger`)

### Manual Tracking (Legacy)

For fine-grained control, you can still use the `[Treblle]` attribute on specific controllers or methods:

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

The SDK requires two configuration values in your `Web.config` or `app.config`:

```xml
<configuration>
  <appSettings>
    <add key="Treblle:SdkToken" value="{Your_SDK_Token}" />
    <add key="Treblle:ApiKey" value="{Your_API_Key}" />
    <add key="Treblle:Debug" value="false" />
  </appSettings>
</configuration>
```

> **Get your credentials**: Visit your [Treblle Dashboard](https://platform.treblle.com) → Select your project → Copy SDK Token and API Key

### Debug Mode

Enable debug mode to see detailed logging of what the SDK is doing:

```xml
<add key="Treblle:Debug" value="true" />
```

**Debug output includes:**
- ✅ Configuration status (SDK Token, API Key - masked for security)
- 🚀 Request tracking start/completion with timing
- ⏭️ Skipped requests with reasons
- 📊 Payload sizes
- 📤 Endpoint selection and transmission
- ❌ Detailed error information

**Important:** Debug mode should be disabled in production (`false` or omitted entirely)

### Additional Field Masking

To mask additional fields beyond the defaults, add them as a comma-separated list:

```xml
<configuration>
  <appSettings>
    <add key="Treblle:SdkToken" value="{Your_SDK_Token}" />
    <add key="Treblle:ApiKey" value="{Your_API_Key}" />
    <add key="Treblle:AdditionalFieldsToMask" value="customSecret,internalId,apiToken" />
  </appSettings>
</configuration>
```

---

## Data Masking

The SDK **automatically masks sensitive fields** before sending data to Treblle. This happens on your server before any data leaves your infrastructure.

### Disabling Masking

⚠️ **For development/testing only**: You can disable masking entirely:

```xml
<add key="Treblle:DisableMasking" value="true" />
```

**Warning**: This sends all data (passwords, credit cards, emails, etc.) in **plain text**. Only use in development environments.

### Default Masked Fields

The following fields are automatically masked with `*****`:

- `password`, `pwd`
- `secret`
- `password_confirmation`, `passwordConfirmation`
- `cc`, `card_number`, `cardNumber`, `ccv`
- `ssn`
- `credit_score`, `creditScore`
- `email`
- `account.*`
- `user.email`, `user.dob`, `user.password`, `user.ss`
- `user.payments.cc`

### Masking Strategies

Different field types use different masking strategies:

- **Passwords/Secrets**: Completely replaced with `*****`
- **Credit Cards**: Partially masked (e.g., `****-****-****-1234`)
- **Emails**: Partially masked (e.g., `j***@example.com`)
- **SSN**: Partially masked (e.g., `***-**-1234`)

---

## Support

If you have problems of any kind feel free to reach out:

- **Email**: support@treblle.com
- **Website**: <https://treblle.com>
- **Documentation**: <https://docs.treblle.com/en/integrations/net>
- **Community**: <https://treblle.com/chat>

---

## License

Copyright 2025, Treblle Inc. Licensed under the MIT license:
http://www.opensource.org/licenses/mit-license.php
