
CLS
# Переходим в папку проекта
Set-Location -Path "C:\Develop\WpfDesktopIpDisplayApp"
#dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true /p:EnableCompressionInSingleFile=true /p:TrimUnusedDependencies=true /p:AssemblyName="IpDisplay"
dotnet publish `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:EnableCompressionInSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:TrimUnusedDependencies=false `
    /p:AssemblyName="IpDisplay"
pause

