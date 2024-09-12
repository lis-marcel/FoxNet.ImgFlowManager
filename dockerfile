FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /FoxSky.Img

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /FoxSky.Img
VOLUME src
VOLUME dst
COPY --from=build-env /FoxSky.Img/out .
ENTRYPOINT ["dotnet", "FoxSky.Img.dll", "Lis", "/src", "/dst"]
