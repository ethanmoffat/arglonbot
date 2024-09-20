FROM mcr.microsoft.com/dotnet/runtime:8.0
COPY arglonbot/bin/Release/net8.0/publish/ /app
WORKDIR /app
ENTRYPOINT ["dotnet", "arglonbot.dll"]
