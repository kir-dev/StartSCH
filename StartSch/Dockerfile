# cd src/StartSCH && docker build . -t startsch -f ./StartSch/Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore

# Install bun
RUN apt update
RUN apt install -y curl unzip
RUN curl -fsSL https://bun.sh/install | bash
ENV BUN_INSTALL="/root/.bun"
ENV PATH="$BUN_INSTALL/bin:$PATH"

WORKDIR /src
COPY StartSch.Wasm/StartSch.Wasm.csproj StartSch.Wasm/
COPY StartSch/StartSch.csproj StartSch/
RUN dotnet restore StartSch/StartSch.csproj

COPY . .
WORKDIR /src/StartSch

FROM restore AS build
RUN dotnet build -c Release -o /app/build

FROM restore AS publish
RUN dotnet publish -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StartSch.dll"]
