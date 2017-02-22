# Picturepark Content Platform .NET SDK
## Picturepark.Sdk.DotNet

[![Build status](https://img.shields.io/appveyor/ci/Picturepark/picturepark-sdk-dotnet.svg?label=build)](https://ci.appveyor.com/project/Picturepark/picturepark-sdk-dotnet)
[![NuGet Version](https://img.shields.io/nuget/v/Picturepark.SDK.V1.svg)](https://www.nuget.org/packages?q=Picturepark)

[![Build status](https://img.shields.io/appveyor/ci/Picturepark/picturepark-sdk-dotnet-y10cr.svg?label=CI+build)](https://ci.appveyor.com/project/Picturepark/picturepark-sdk-dotnet-y10cr)
[![MyGet CI](https://img.shields.io/myget/picturepark-sdk-dotnet-ci/vpre/Picturepark.SDK.V1.svg)](https://www.myget.org/gallery/picturepark-sdk-dotnet-ci)

Links: 

- [Documentation](docs/README.md)
- [Build Scripts](build/README.md)
- [Sources](src/)

## Usage

Install required NuGet package: 

    Install-Package Picturepark.SDK.V1
    
Create new `PictureparkClient` and access remote PCP server: 

```csharp
using (var authClient = new UsernamePasswordAuthClient("http://mypcpserver.com", username, password))
using (var client = new PictureparkClient(authClient))
{
    var asset = await client.Assets.GetAsync("myAssetId");
}
```

### .NET 4.5.x

For .NET 4.5.x targets you need to enable TLS 1.2: 

```csharp
ServicePointManager.SecurityProtocol = 
    SecurityProtocolType.Ssl3 | 
    SecurityProtocolType.Tls12 | 
    SecurityProtocolType.Tls11 | 
    SecurityProtocolType.Tls;
```

## Packages

Public APIs: 

- Picturepark.SDK.V1
- Picturepark.SDK.V1.Contract
- Picturepark.SDK.V1.Localization

Management APIs: 

- Picturepark.SDK.V1.CloudManager
- Picturepark.SDK.V1.ServiceProvider

## Development

NuGet Feed: https://www.nuget.org/packages?q=Picturepark

MyGet CI Feed: https://www.myget.org/gallery/picturepark-sdk-dotnet-ci

AppVeyor Build: https://ci.appveyor.com/project/Picturepark/picturepark-sdk-dotnet

AppVeyor CI Build: https://ci.appveyor.com/project/Picturepark/picturepark-sdk-dotnet-y10cr

