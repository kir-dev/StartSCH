# refer to deploy.sh for an example on how to build this image

# https://mcr.microsoft.com/en-us/artifact/mar/dotnet/aspnet
FROM mcr.microsoft.com/dotnet/aspnet:9.0.7 AS base
WORKDIR /app

# https://mcr.microsoft.com/en-us/artifact/mar/dotnet/sdk
FROM mcr.microsoft.com/dotnet/sdk:9.0.303 AS restore

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

COPY StartSch/package.json StartSch/bun.lock StartSch/
WORKDIR /src/StartSch
RUN bun install

WORKDIR /src
COPY . .

FROM restore AS build
WORKDIR /src/StartSch
RUN dotnet build -c Release -o /app/build

FROM restore AS publish
WORKDIR /src/StartSch
RUN dotnet publish -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StartSch.dll"]
