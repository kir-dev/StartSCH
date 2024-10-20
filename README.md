# StartSch

[![](https://i.kym-cdn.com/entries/icons/original/000/044/268/shoescover.jpg)](https://knowyourmeme.com/memes/if-your-boss-lawyers-pants-looks-like-this)

## Development
### Database
#### Migrations
- [Docs: Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations)
- [Docs: Migrations with Multiple Providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)

After modifying the `Db` you must create new migrations:
```sh
dotnet tool install dotnet-ef --global

# Describe the migration
export MIGRATION_MESSAGE=AddSomethingToSomeOtherThing

# SQLite for development
ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add $MIGRATION_MESSAGE --context Db --output-dir Data/Migrations/Sqlite

# PostgreSQL for production
ASPNETCORE_ENVIRONMENT=Production dotnet ef migrations add $MIGRATION_MESSAGE --context PostgresDb --output-dir Data/Migrations/PostgreSQL
```
Migrations are applied automatically when the server starts.

#### Injecting a `Db` instance
- [Docs: DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

Depending on where you want to access the database, choose between injecting `Db` or `IDbContextFactory<Db>`.

For example, static forms or API controllers that run in a scope should use `Db`, while methods in an interactive component should request a new `Db` instance every time they run.
