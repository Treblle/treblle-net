# Treblle .NET SDK

[![Treblle API Intelligence](https://github.com/user-attachments/assets/b268ae9e-7c8a-4ade-95da-b4ac6fce6eea)](https://treblle.com)

[Website](http://treblle.com/) • [Documentation](https://docs.treblle.com/) • [Pricing](https://treblle.com/pricing)

Treblle is an API intelligence platform that helps developers, teams and organizations understand their APIs from a single integration point.

---

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
  - [ASP.NET Web API](#aspnet-web-api)
  - [WCF Services](#wcf-services)
- [Configuration](#configuration)
  - [Additional Field Masking](#additional-field-masking)
- [What Gets Tracked](#what-gets-tracked)
- [Data Masking](#data-masking)
- [Support](#support)
- [License](#license)

---

## Requirements

- **.NET Framework 4.6.2** or higher
- **ASP.NET Web API 5.2.7+** (for Web API support)
- **WCF / System.ServiceModel** (for WCF support)
- **Newtonsoft.Json 13.0.3+**

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

### ASP.NET Web API

Add the `[Treblle]` attribute to track API controllers or methods:

**Track entire controller:**

```csharp
using Treblle.Net;

[Treblle]
public class ProductsController : ApiController
{
    public IHttpActionResult Get()
    {
        var products = _productService.GetAll();
        return Ok(products);
    }

    public IHttpActionResult Get(int id)
    {
        var product = _productService.GetById(id);
        return Ok(product);
    }
}
```

**Track specific methods:**

```csharp
public class OrdersController : ApiController
{
    [Treblle]
    public IHttpActionResult Get(int id)
    {
        // This endpoint is tracked
        return Ok(_orderService.GetById(id));
    }

    public IHttpActionResult GetInternal(int id)
    {
        // This endpoint is NOT tracked
        return Ok(_orderService.GetInternalOrder(id));
    }
}
```

### WCF Services

Add the `[TreblleBehavior]` attribute to track WCF service operations:

**Track entire service:**

```csharp
using Treblle.Net;

[TreblleBehavior]
public class UserService : IUserService
{
    // All operations are tracked
    public UserResponse GetUser(int id)
    {
        return _repository.GetUser(id);
    }

    public async Task<UserResponse> CreateUserAsync(UserRequest request)
    {
        return await _repository.CreateAsync(request);
    }
}
```

**Track specific operations:**

```csharp
public class UserService : IUserService
{
    [TreblleBehavior]
    public UserResponse GetUser(int id)
    {
        // This operation is tracked
        return _repository.GetUser(id);
    }

    public UserResponse GetInternalUser(int id)
    {
        // This operation is NOT tracked
        return _repository.GetInternalUser(id);
    }
}
```

**WCF Web.config example:**

```xml
<system.serviceModel>
  <services>
    <service name="YourNamespace.UserService">
      <endpoint address=""
                binding="basicHttpBinding"
                contract="YourNamespace.IUserService" />
    </service>
  </services>
  <serviceHostingEnvironment aspNetCompatibilityEnabled="true" />
</system.serviceModel>
```

> **Note**: The SDK supports both synchronous and asynchronous WCF operations, including Task-based async methods.

---

## Configuration

The SDK requires two configuration values in your `Web.config` or `app.config`:

```xml
<configuration>
  <appSettings>
    <add key="TreblleApiKey" value="{Your_API_Key}" />
    <add key="TreblleProjectId" value="{Your_Project_Id}" />
  </appSettings>
</configuration>
```

> **Get your credentials**: Visit your [Treblle Dashboard](https://platform.treblle.com) → Select your project → Copy API Key and Project ID

### Additional Field Masking

To mask additional fields beyond the defaults, add them as a comma-separated list:

```xml
<configuration>
  <appSettings>
    <add key="TreblleApiKey" value="{Your_API_Key}" />
    <add key="TreblleProjectId" value="{Your_Project_Id}" />
    <add key="AdditionalFieldsToMask" value="customSecret,internalId,apiToken" />
  </appSettings>
</configuration>
```

---

## Data Masking

The SDK automatically masks sensitive fields before sending data to Treblle. This happens on your server before any data leaves your infrastructure.

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
