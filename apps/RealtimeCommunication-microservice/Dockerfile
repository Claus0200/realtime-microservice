FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/RealtimeCommunication/RealtimeCommunication.csproj src/RealtimeCommunication/
COPY src/Messages/Messages.csproj src/Messages/

RUN dotnet restore src/RealtimeCommunication/RealtimeCommunication.csproj

COPY src/ src/

RUN dotnet publish src/RealtimeCommunication/RealtimeCommunication.csproj -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RealtimeCommunication.dll"]
