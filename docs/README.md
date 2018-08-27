# SDK Documentation

**[API Documentation](https://rawgit.com/Picturepark/Picturepark.SDK.DotNet/master/docs/api/site/index.html)**

## Usage in C#

Install the Picturepark SDK NuGet package in your .NET project (supports .NET 4.5+ and .NET Standard 1.3+): 

    Install-Package Picturepark.SDK.V1
    
Create a new `PictureparkService` instance and access remote Picturepark server as follows: 

```csharp
var authClient = new AccessTokenAuthClient("https://api.mypcpserver.com", "AccessToken", "CustomerAlias");
var settings = new PictureparkServiceSettings(authClient);
using (var client = new PictureparkService(settings))
{
    var content = await client.Content.GetAsync("myContentId");
}
```

### Usage in ASP.NET Core

Register the Picturepark .NET service classes in the ASP.NET Core dependency injection system (`Startup.cs`): 

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry(Configuration);
    services.AddMvc();

    services.AddScoped<IPictureparkService, PictureparkService>();
    services.AddSingleton<IPictureparkServiceSettings>(new PictureparkServiceSettings(
        new AccessTokenAuthClient("https://api.server.com", "MyAccessToken", "MyCustomerAlias")));
}
```

Inject `IPictureparkService` into your controller: 

```csharp
public class MyController : Controller
{
    private readonly IPictureparkService _pictureparkService;

    public MyController(IPictureparkService pictureparkService)
    {
        _pictureparkService = pictureparkService;
    }
    
    ...
```

### Usage with .NET 4.5.x framework

When installing the SDK in .NET 4.5.x targets you need to globally enable TLS 1.2: 

```csharp
ServicePointManager.SecurityProtocol = 
    SecurityProtocolType.Ssl3 | 
    SecurityProtocolType.Tls12 | 
    SecurityProtocolType.Tls11 | 
    SecurityProtocolType.Tls;
```
