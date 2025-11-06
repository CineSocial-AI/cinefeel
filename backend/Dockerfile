FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CineSocial.sln .
COPY src/CineSocial.Api/CineSocial.Api.csproj src/CineSocial.Api/
COPY src/CineSocial.Application/CineSocial.Application.csproj src/CineSocial.Application/
COPY src/CineSocial.Domain/CineSocial.Domain.csproj src/CineSocial.Domain/
COPY src/CineSocial.Infrastructure/CineSocial.Infrastructure.csproj src/CineSocial.Infrastructure/

RUN dotnet restore "CineSocial.sln"

COPY . .

WORKDIR /src/src/CineSocial.Api
RUN dotnet publish "CineSocial.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5047
EXPOSE 5047

ENTRYPOINT ["dotnet", "CineSocial.Api.dll"]
