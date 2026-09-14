# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN --mount=type=secret,id=github_token \
    dotnet nuget add source https://nuget.pkg.github.com/hunglu/index.json \
      -n github-wordbuddy -u hunglu -p "$(cat /run/secrets/github_token)" --store-password-in-clear-text && \
    dotnet restore ./WordBuddy.Notification.Api && \
    dotnet publish ./WordBuddy.Notification.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN useradd -m appuser
WORKDIR /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WordBuddy.Notification.Api.dll"]
