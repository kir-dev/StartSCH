# StartSCH

## Development
### Configuration
- [Docs: Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)
- [Docs: Options pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)

#### Push notifications (optional)
- [MDN: Web Push API](https://developer.mozilla.org/en-US/docs/Web/API/Push_API)
- [web.dev: Push notifications overview](https://web.dev/articles/push-notifications-overview)

Some push services might require that the server identifies itself with a 
[VAPID](https://datatracker.ietf.org/doc/html/rfc8292) key pair.
[It is required by Apple](https://developer.apple.com/documentation/usernotifications/sending-web-push-notifications-in-web-apps-and-browsers#Prepare-your-server-to-send-push-notifications).
Keys can be generated [here](https://web-push-codelab.glitch.me/).

```sh
dotnet user-secrets set Push__PublicKey "..."
dotnet user-secrets set Push__PrivateKey "..."
dotnet user-secrets set Push__Subject "mailto:..."
```

### Database
#### Migrations
- [Docs: Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations)
- [Docs: Migrations with Multiple Providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)

After modifying the `Db` you must create new migrations:
```sh
dotnet tool install dotnet-ef --global

# Go to the server project directory (StartSch/StartSch)
cd StartSch

# Describe the migration
export MIGRATION_MESSAGE=AddSomethingToSomeOtherThing

# SQLite for development
dotnet ef migrations add --context SqliteDb $MIGRATION_MESSAGE

# PostgreSQL for production
dotnet ef migrations add --context PostgresDb $MIGRATION_MESSAGE
```

Migrations are applied automatically when the server starts.

The last migration can be removed with:

```sh
dotnet ef migrations remove --context SqliteDb
dotnet ef migrations remove --context PostgresDb 
```

#### Injecting a `Db` instance
- [Docs: DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

Depending on where you want to access the database, you have to decide between injecting `Db` or `IDbContextFactory<Db>`.

For example, static forms or API controllers that run in a scope should use `Db`, while methods in an interactive Blazor component should request a new `Db` instance every time they run.

[![](https://i.kym-cdn.com/entries/icons/original/000/044/268/shoescover.jpg)](https://knowyourmeme.com/memes/if-your-boss-lawyers-pants-looks-like-this)
