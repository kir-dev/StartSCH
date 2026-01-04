# refer to deploy.sh for an example on how to build this image

# https://mcr.microsoft.com/en-us/artifact/mar/dotnet/aspnet
FROM mcr.microsoft.com/dotnet/aspnet:10.0.1 AS base
WORKDIR /app

# https://mcr.microsoft.com/en-us/artifact/mar/dotnet/sdk
FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS restore

# Install bun
RUN apt update
RUN apt install -y curl unzip
RUN curl -fsSL https://bun.sh/install | bash
ENV BUN_INSTALL="/root/.bun"
ENV PATH="$BUN_INSTALL/bin:$PATH"

WORKDIR /src
COPY global.json .
COPY StartSch.Wasm/StartSch.Wasm.csproj StartSch.Wasm/
COPY StartSch/StartSch.csproj StartSch/

# RequiresAspNetWebAssets: blazor.web.js is only installed if there are .blazor files.
# In this stage there aren't any, so we need to tell dotnet that we do need blazor.web.js.
# https://learn.microsoft.com/en-us/aspnet/core/blazor/project-structure#location-of-the-blazor-script
# https://github.com/dotnet/aspnetcore/issues/60366
RUN dotnet restore StartSch/StartSch.csproj -p:RequiresAspNetWebAssets=true

COPY StartSch/package.json StartSch/bun.lock StartSch/
WORKDIR /src/StartSch
RUN bun install

WORKDIR /src
COPY . .

FROM restore AS publish
WORKDIR /src/StartSch
RUN dotnet publish --no-restore -o /app/publish -p UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StartSch.dll"]
