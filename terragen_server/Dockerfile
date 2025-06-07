FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
COPY /bin/Release/net8.0/linux-x64/publish /app
ENTRYPOINT ["dotnet", "terragen_server.dll"]
