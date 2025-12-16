# StartSCH

The news site of the Schönherz Dormitory and the Budapest University of Technology and Economics.
Built with
[ASP.NET](https://learn.microsoft.com/en-us/aspnet/core/overview),
[Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor),
[Entity Framework](https://learn.microsoft.com/en-us/ef/core), and
[Lit](https://lit.dev).

## Running locally

Instructions on how to quickly set up a development environment.

### Dependencies

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
  - make sure you can run `dotnet --info` and it shows `10.x.x` under *.NET SDKs installed*
- [bun](https://bun.sh/docs/installation)
  - make sure you can run `bun`

### AuthSCH credentials

https://auth.sch.bme.hu > *Bejelentkezés* > *Fejlesztői konzol* > *Új hozzáadása*, set *Átirányítási cím* to
`http://localhost:5264/signin-oidc`,
then use the created credentials in the following commands:

### Running from the terminal

```shell
git clone https://github.com/kir-dev/StartSCH
cd StartSCH/StartSch
dotnet user-secrets set AuthSch:ClientId YOUR_AUTHSCH_CLIENTID
dotnet user-secrets set AuthSch:ClientSecret YOUR_AUTHSCH_CLIENTSECRET
dotnet run
```

### Debugging

I recommend using JetBrains' .NET IDE, Rider for working on StartSCH. Below you can find instructions on how run StartSCH in debug mode using it:

1. [Install Rider](https://www.jetbrains.com/rider/download/)
2. Open `StartSCH.slnx`
3. Ensure AuthSCH credentials are correctly set up: *Explorer* > *StartSch* > right-click > *Tools* > *.NET User Secrets*
4. Click *Debug 'StartSch'* (the green bug icon in the top right) or press `F5`

### Running with hot reloading

Hot reloading allows updating the code of the app while it is running without restarting it.
Rider's built-in hot reloading is not that great, so I highly recommend just running StartSCH from the terminal if you don't need a debugger:

#### Terminal 1
```sh
cd StartSCH/StartSch

# Run StartSCH with hot reloading:
dotnet watch
```

#### Terminal 2
```shell
cd StartSCH/StartSch

# Use Bun to automatically bundle TypeScript and CSS files when they change
# (this is handled by dotnet build when not using hot reloading)
bun watch
```

## Development

### Overview

If you don't like reading, check out these files and directories for a quick overview of the project:

- [`StartSch/`](StartSch)
  - [`Program.cs`](StartSch/Program.cs): the entrypoint of the server
  - [`StartSch.csproj`](StartSch/StartSch.csproj): [NuGet](https://nuget.org) dependencies
  - [`package.json`](StartSch/package.json): [NPM](https://npmjs.org) dependencies and [CSS](https://developer.mozilla.org/en-US/docs/Web/CSS)/[TS](https://typescriptlang.org) build scripts
- [`StartSch.Wasm/`](StartSch.Wasm): C# code and Blazor components that can run in the user's browser. Currently unused.
- [`.config/kubernetes.yaml`](.config/kubernetes.yaml): Kubernetes resource definitions required to run the production
  version of StartSCH in Kir-Dev's vCluster in the KSZK's Kubernetes cluster.

### The server

We use ASP.NET, which is a free, [open-source](https://github/dotnet/aspnetcore), cross-platform, and high-performance HTTP server built on .NET, that supports a boatload of different use cases.

I recommend reading the following sections of its documentation to get a feel for it:
- [Overview of ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/overview)
- [ASP.NET Core fundamentals overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals)
- [ASP.NET Core Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor)
- [APIs overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis)

### Configuration

- [ASP.NET docs: Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration): environment variables, `appsettings.*.json`, `dotnet user-secrets`, etc.
- [ASP.NET docs: Options pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options): accessing the above using type-safe C# classes

### Setting up push notifications

- [MDN: Web Push API](https://developer.mozilla.org/en-US/docs/Web/API/Push_API)
- [web.dev: Push notifications overview](https://web.dev/articles/push-notifications-overview)

To send push notifications, most push services, 
[for example, Apple](https://developer.apple.com/documentation/usernotifications/sending-web-push-notifications-in-web-apps-and-browsers#Prepare-your-server-to-send-push-notifications),
require a [VAPID](https://rfc-editor.org/rfc/rfc8292) key pair.

If you want to try out push notifications, you can use a [VAPID key generator](https://steveseguin.github.io/vapid/)
to generate these, then configure StartSCH to use them:

```sh
cd StartSCH/StartSch

dotnet user-secrets set Push:PublicKey "..."
dotnet user-secrets set Push:PrivateKey "..."
# Push service providers use this if there are issues with a sender, probably not important when developing
dotnet user-secrets set Push:Subject "mailto:example@example.com"
```

### Database

The production version of StartSCH uses [PostgreSQL](https://www.postgresql.org/docs/current/index.html)
to persist data, but to simplify setting up a dev environment, [SQLite](https://sqlite.org/)
is also supported, as it does not require any configuration.

We use Entity Framework to access the database from C# code.
For most changes, you can get by with only a basic knowledge of Entity Framework, but if you have the time,
I highly recommend also reading the documentation for Postgres and SQLite (the Postgres one is especially good).

If you are new to Entity Framework, I recommend skimming through the [Entity Framework introduction](https://learn.microsoft.com/en-us/ef/core/),
then the *Overview* pages for the sections in the table of contents on the left.

#### Migrations

- [EF docs: Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations)
- [EF docs: Migrations with Multiple Providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)

After modifying the database schema (stuff in `StartSch.Data`), you have to create new migrations:

```sh
cd StartSCH/StartSch

# Make sure you can run `dotnet ef`.
# One of these commands, ideally the first one, should install it.
dotnet tool restore
dotnet tool install dotnet-ef
dotnet tool install dotnet-ef --global

# Describe the migration
export MIGRATION_MESSAGE=AddSomethingToSomeOtherThing

# Add migration
dotnet ef migrations add --context SqliteDb $MIGRATION_MESSAGE
dotnet ef migrations add --context PostgresDb $MIGRATION_MESSAGE

# Remove latest migration
dotnet ef migrations remove --context SqliteDb
dotnet ef migrations remove --context PostgresDb

# Reset migrations
rm -r Data/Migrations/Postgres Data/Migrations/Sqlite
dotnet ef migrations add --context SqliteDb --output-dir Data/Migrations/Sqlite $MIGRATION_MESSAGE
dotnet ef migrations add --context PostgresDb --output-dir Data/Migrations/Postgres $MIGRATION_MESSAGE
```

New migrations are applied automatically before StartSCH starts serving requests.

#### Injecting a `Db` instance

- [EF docs: DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [EF docs: ASP.NET Core Blazor with Entity Framework Core](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core)

Depending on where you want to access the database, you have to decide between injecting `Db` or `IDbContextFactory<Db>`.

For example, static forms or API controllers that run in a scope should use `Db`, while methods in an interactive Blazor component should request a new `Db` instance every time they run.

### Tips

#### Reading TLS encrypted HTTP requests using Wireshark

- [Wireshark/TLS](https://wiki.wireshark.org/TLS)
- [dotnet/runtime: Support SSLKEYLOGFILE in SslStream](https://github.com/dotnet/runtime/issues/37915)

Might be useful when trying to reverse-engineer some APIs or debugging issues while developing StartSCH.

1. Run StartSCH using the `SSLKEYLOGFILE` environment variable set to a path to a non-existent file (e.g. `/home/USER/keylog.txt`)
   - This is easiest using `StartSch/Properties/launchSettings.json`:
     add `"SSLKEYLOGFILE": "/home/USER/keylog.txt"` to the `environmentVariables` section under the launch
     profile you are using (`http` by default)
2. Add a `AppContext.SetSwitch("System.Net.EnableSslKeyLogging", true);` line to the top of `Program.cs`
3. Set *Wireshark/Edit/Preferences/Protocols/TLS/(Pre)-Master-Secret log filename* to the path in the above environment
   variable
4. Run StartSCH

`SSLKEYLOGFILE` also works in Firefox and Chrome:
```bash
SSLKEYLOGFILE=~/keylog.txt firefox
```

Make sure the browser is not already running (in the background), otherwise it won't pick up the env var.

---

[![](https://pbs.twimg.com/media/FQNGIMRXsAMXldk?format=webp&name=4096x4096)](https://twitter.com/gf_256/status/1514131084702797827)
