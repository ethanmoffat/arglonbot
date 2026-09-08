FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY arglonbot/arglonbot.csproj arglonbot/
RUN dotnet restore arglonbot/arglonbot.csproj
COPY . .
RUN dotnet publish arglonbot/arglonbot.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "arglonbot.dll"]
