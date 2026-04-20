FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/SuperBodega.API/SuperBodega.API.csproj", "src/SuperBodega.API/"]
COPY ["src/SuperBodega.Core/SuperBodega.Core.csproj", "src/SuperBodega.Core/"]
COPY ["src/SuperBodega.Infrastructure/SuperBodega.Infrastructure.csproj", "src/SuperBodega.Infrastructure/"]
RUN dotnet restore "src/SuperBodega.API/SuperBodega.API.csproj"
COPY . .
WORKDIR "/src/src/SuperBodega.API"
RUN dotnet build "SuperBodega.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SuperBodega.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SuperBodega.API.dll"]
