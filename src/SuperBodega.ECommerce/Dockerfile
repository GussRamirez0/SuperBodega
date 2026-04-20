FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/SuperBodega.ECommerce/SuperBodega.ECommerce.csproj", "src/SuperBodega.ECommerce/"]
COPY ["src/SuperBodega.Core/SuperBodega.Core.csproj", "src/SuperBodega.Core/"]
COPY ["src/SuperBodega.Infrastructure/SuperBodega.Infrastructure.csproj", "src/SuperBodega.Infrastructure/"]
RUN dotnet restore "src/SuperBodega.ECommerce/SuperBodega.ECommerce.csproj"
COPY . .
WORKDIR "/src/src/SuperBodega.ECommerce"
RUN dotnet build "SuperBodega.ECommerce.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SuperBodega.ECommerce.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SuperBodega.ECommerce.dll"]
