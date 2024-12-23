# cd src/StartSch && docker build . -t startsch -f ./StartSch/Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["StartSch/StartSch.csproj", "StartSch/"]
RUN dotnet restore "StartSch/StartSch.csproj"
COPY . .
WORKDIR "/src/StartSch"
RUN dotnet build "StartSch.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StartSch.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StartSch.dll"]
