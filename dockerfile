FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG TARGETARCH
ARG BUILDPLATFORM
WORKDIR /FoxSky.Img

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore -a $TARGETARCH
# Build and publish a release
RUN dotnet publish -a $TARGETARCH -c Release -o out

# Build runtime image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /FoxSky.Img
VOLUME src
VOLUME dst
COPY --from=build-env /FoxSky.Img/out .
ENTRYPOINT ["dotnet", "FoxSky.Img.dll", "Lis", "/src", "/dst", "lis.marc@gmail.com", "50"]
