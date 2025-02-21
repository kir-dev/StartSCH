# StartSCH
## Running locally
### Dependencies
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download)
  - make sure you can run `dotnet --info` and it shows `9.x.x` under *.NET SDKs installed*
- [bun](https://bun.sh/docs/installation)
  - make sure you can run `bun`

### AuthSCH credentials
Go to https://auth.sch.bme.hu/console/create, set *Átirányítási cím* to
`http://localhost:5264/signin-oidc`,
then use the created credentials in the following commands:

### Running from terminal
```shell
git clone https://github.com/albi005/StartSCH
cd StartSCH/StartSch
dotnet user-secrets set AuthSch:ClientId $YOUR_AUTHSCH_CLIENTID
dotnet user-secrets set AuthSch:ClientSecret $YOUR_AUTHSCH_CLIENTSECRET
dotnet run
```

## Development
### Configuration
- [Docs: Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)
- [Docs: Options pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)

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
- [Docs: Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations)
- [Docs: Migrations with Multiple Providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)

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
rm -r Data/Migrations
dotnet ef migrations add --context SqliteDb --output-dir Data/Migrations/Sqlite $MIGRATION_MESSAGE
dotnet ef migrations add --context PostgresDb --output-dir Data/Migrations/Postgres $MIGRATION_MESSAGE
```

Migrations are applied automatically on server startup.

#### Injecting a `Db` instance
- [Docs: DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [Docs: ASP.NET Core Blazor with Entity Framework Core](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core)

Depending on where you want to access the database, you have to decide between injecting `Db` or `IDbContextFactory<Db>`.

For example, static forms or API controllers that run in a scope should use `Db`, while methods in an interactive Blazor component should request a new `Db` instance every time they run.

[![](https://i.kym-cdn.com/entries/icons/original/000/044/268/shoescover.jpg)](https://knowyourmeme.com/memes/if-your-boss-lawyers-pants-looks-like-this)
