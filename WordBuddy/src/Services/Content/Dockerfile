# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN --mount=type=secret,id=github_token \
    dotnet nuget remove source local-wordbuddy && \
    dotnet nuget update source github-wordbuddy \
      -u hunglu -p "$(cat /run/secrets/github_token)" --store-password-in-clear-text && \
    dotnet restore ./WordBuddy.Content.Api && \
    dotnet publish ./WordBuddy.Content.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN useradd -m appuser
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/media && chown appuser:appuser /app/media
VOLUME /app/media
USER appuser
EXPOSE 8080 8081
ENV Kestrel__Endpoints__Http__Url=http://+:8080 \
    Kestrel__Endpoints__Grpc__Url=http://+:8081 \
    Kestrel__Endpoints__Grpc__Protocols=Http2
ENTRYPOINT ["dotnet", "WordBuddy.Content.Api.dll"]
