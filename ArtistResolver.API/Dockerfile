FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN mkdir -p /db && chmod 777 /db
USER app
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DB_PATH=/db/artists.db

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ArtistResolver.API/ArtistResolver.API.csproj", "ArtistResolver.API/"]
RUN dotnet restore "./ArtistResolver.API/ArtistResolver.API.csproj"
COPY . .
WORKDIR "/src/ArtistResolver.API"
RUN dotnet build "./ArtistResolver.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ArtistResolver.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArtistResolver.API.dll"]