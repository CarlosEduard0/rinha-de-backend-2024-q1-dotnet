FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-alpine-aot AS build
WORKDIR /src

COPY ./app/src/*.csproj .

RUN dotnet restore ./RinhaBackend.csproj -r linux-musl-x64

COPY ./app/src .

RUN dotnet publish ./RinhaBackend.csproj -c Release -o /app -r linux-musl-x64 --no-restore

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./RinhaBackend"]
