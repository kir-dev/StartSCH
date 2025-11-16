# StartSCH

The news site of the Schönherz Dormitory and the Budapest University of Technology and Economics.

Built with
[ASP.NET](https://learn.microsoft.com/en-us/aspnet/core/overview),
[Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor),
[Entity Framework](https://learn.microsoft.com/en-us/ef/core), and
[Lit](https://lit.dev).

Below you can find documentation on
- [how to run StartSCH on your own computer](#running-locally),

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

1. [Install Rider](https://www.jetbrains.com/rider/download/), the .NET IDE by JetBrains. Free for non-commercial use, like education or open-source.
2. Open `StartSCH.slnx`
3. Ensure AuthSCH credentials are correctly set up: *Explorer* > *StartSch* > right-click > *Tools* > *.NET User Secrets*
4. Click *Debug 'StartSch'* (the green bug icon in the top right) or press `F5`

### Hot Reloading

Open two different terminals:

#### Terminal 1
```sh
# Open the directory containing StartSch.csproj:
cd ~/src/StartSCH/StartSch # Linux example
cd C:\src\StartSCH\StartSch # Windows example

# Run StartSCH with .NET Hot Reload enabled:
dotnet watch
```

#### Terminal 2
```shell
# Open the directory containing StartSch.csproj

# Enable automatic building of TS and CSS files:
bun watch
```

## Development



### Configuration

- [ASP.NET docs: Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)
- [ASP.NET docs: Options pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)

#### Push notifications (optional)

- [MDN: Web Push API](https://developer.mozilla.org/en-US/docs/Web/API/Push_API)
- [web.dev: Push notifications overview](https://web.dev/articles/push-notifications-overview)

When sending push notifications, most push services, 
[for example Apple](https://developer.apple.com/documentation/usernotifications/sending-web-push-notifications-in-web-apps-and-browsers#Prepare-your-server-to-send-push-notifications),
require a [VAPID](https://rfc-editor.org/rfc/rfc8292) key pair.
You can use [Push Companion](https://web-push-codelab.glitch.me/) to generate these.

```sh
dotnet user-secrets set Push:PublicKey "..."
dotnet user-secrets set Push:PrivateKey "..."
dotnet user-secrets set Push:Subject "mailto:..."
```

### Database

#### Migrations

- [Entity Framework: Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations)
- [Entity Framework: Migrations with Multiple Providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)

After modifying the `Db`, you have to create new migrations:
```sh
# Go to the server project directory (e.g. ~/src/StartSCH/StartSch)
cd StartSch

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

Migrations are applied automatically on server startup.

#### Injecting a `Db` instance

- [Docs: DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [Docs: ASP.NET Core Blazor with Entity Framework Core](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core)

Depending on where you want to access the database, you have to decide between injecting `Db` or `IDbContextFactory<Db>`.

For example, static forms or API controllers that run in a scope should use `Db`, while methods in an interactive Blazor component should request a new `Db` instance every time they run.

### Reading TLS encrypted HTTP requests using Wireshark

- [Wireshark/TLS](https://wiki.wireshark.org/TLS)
- [dotnet/runtime: Support SSLKEYLOGFILE in SslStream](https://github.com/dotnet/runtime/issues/37915)

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

[![](https://pbs.twimg.com/media/FQNGIMRXsAMXldk?format=webp&name=4096x4096)](https://twitter.com/gf_256/status/1514131084702797827)
