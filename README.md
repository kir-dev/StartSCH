# StartSch

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
cd StartSch
dotnet tool install dotnet-ef --global

# Describe the migration
export MIGRATION_MESSAGE=AddSomethingToSomeOtherThing

# SQLite for development
ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add --context Db --output-dir Data/Migrations/Sqlite $MIGRATION_MESSAGE

# PostgreSQL for production
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations add --context PostgresDb --output-dir Data/Migrations/PostgreSQL $MIGRATION_MESSAGE
```
Migrations are applied automatically when the server starts.

The last migration can be removed with:
```sh
ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations remove --context Db
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations remove --context PostgresDb 
```

#### Injecting a `Db` instance
- [Docs: DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

Depending on where you want to access the database, choose between injecting `Db` or `IDbContextFactory<Db>`.

For example, static forms or API controllers that run in a scope should use `Db`, while methods in an interactive component should request a new `Db` instance every time they run.

[![](https://i.kym-cdn.com/entries/icons/original/000/044/268/shoescover.jpg)](https://knowyourmeme.com/memes/if-your-boss-lawyers-pants-looks-like-this)
