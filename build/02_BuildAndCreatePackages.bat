rmdir %~dp0/Packages /Q /S nonemptydir
mkdir %~dp0/Packages

del "%~dp0/../src/Picturepark.SDK.V1.Contract/project.lock.json"
dotnet restore "%~dp0/../src/Picturepark.SDK.V1.Contract/" --no-cache
dotnet pack "%~dp0/../src/Picturepark.SDK.V1.Contract/" --output "../../build/Packages" --configuration Release

del "%~dp0/../src/Picturepark.SDK.V1/project.lock.json"
dotnet restore "%~dp0/../src/Picturepark.SDK.V1/" --no-cache
dotnet pack "%~dp0/../src/Picturepark.SDK.V1/" --output "../../build/Packages" --configuration Release

del "%~dp0/../src/Picturepark.SDK.V1.Localization/project.lock.json"
dotnet restore "%~dp0/../src/Picturepark.SDK.V1.Localization/" --no-cache
dotnet pack "%~dp0/../src/Picturepark.SDK.V1.Localization/" --output "../../build/Packages" --configuration Release

del "%~dp0/../src/Picturepark.SDK.V1.ServiceProvider/project.lock.json"
dotnet restore "%~dp0/../src/Picturepark.SDK.V1.ServiceProvider/" --no-cache
dotnet pack "%~dp0/../src/Picturepark.SDK.V1.ServiceProvider/" --output "../../build/Packages" --configuration Release

del "%~dp0/../src/Picturepark.SDK.V1.CloudManager/project.lock.json"
dotnet restore "%~dp0/../src/Picturepark.SDK.V1.CloudManager/" --no-cache
dotnet pack "%~dp0/../src/Picturepark.SDK.V1.CloudManager/" --output "../../build/Packages" --configuration Release