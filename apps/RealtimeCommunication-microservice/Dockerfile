FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY packages/Shared.Contracts/Shared.Contracts.csproj \
     packages/Shared.Contracts/

COPY apps/RealtimeCommunication-microservice/src/Messages/Messages.csproj \
     apps/RealtimeCommunication-microservice/src/Messages/

COPY apps/RealtimeCommunication-microservice/src/RealtimeCommunication.Shared/RealtimeCommunication.Shared.csproj \
     apps/RealtimeCommunication-microservice/src/RealtimeCommunication.Shared/

COPY apps/RealtimeCommunication-microservice/src/RealtimeCommunication/RealtimeCommunication.csproj \
     apps/RealtimeCommunication-microservice/src/RealtimeCommunication/

RUN dotnet restore \
    apps/RealtimeCommunication-microservice/src/RealtimeCommunication/RealtimeCommunication.csproj

COPY packages/Shared.Contracts/ \
     packages/Shared.Contracts/

COPY apps/RealtimeCommunication-microservice/src/ \
     apps/RealtimeCommunication-microservice/src/

RUN dotnet publish \
    apps/RealtimeCommunication-microservice/src/RealtimeCommunication/RealtimeCommunication.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RealtimeCommunication.dll"]