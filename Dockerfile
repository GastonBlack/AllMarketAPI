FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY AllMarketAPI.csproj ./
RUN dotnet restore AllMarketAPI.csproj

COPY . .
RUN dotnet publish AllMarketAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT [ "dotnet", "AllMarketAPI.dll" ]