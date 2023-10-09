# Entity Framework Learning

Learning and steps from following: Trevoir Williams' `Entity Framework Core - A Full Tour` video on O'Reilly.

EF = Entity Framework

## DB Setup

### Primary Key

- If you set one of your domain field names to `Id` or `<CurrentTableName>Id`, then EF will automatically pick up on that and set it as the Primary Key

### Foreign Key

- To set a foreign key, use the naming convention `<ForeignTableName>Id` along with a separate `public virtual` field with the `Type` and `Name` of the foreign table like so:
  ```cs
  public class Team
  {
    public int Id { get; set; }
    public string Name { get; set; }
    // Foreign key
    public int LeagueId { get; set; }
    // Foreign table
    public virtual League League { get; set; }
  }

### Database Context

Think of Context as another word for Connection

- Added a dependency of Microsoft.EntityFrameworkCore.SqlServer via NuGet Package Manager
- Created a `DbContext` file in the Data project with a `project reference` to our Domain project
- Added a reference to each table that will represent the rows in that table `public DbSet<TableName> <TableNamePlural> { get; set; }`:
    ```cs
    public DbSet<Team> Teams { get; set; }
    ```
- Overrode the options builder configuration to: 
    - Create a connection string to connect to the DB (`Data Source`)
        - We connected to a localdb that's inbuilt in to Visual Studio (access via the View menu)
    - Used the `Initial Catalog` attribute to give our DB a name
    ```cs
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=FootballLeague_EfCore");
    }
    ```

### Migrations

Our instructions to the Database (such as creating tables)

- Added dependency of EntityFrameworkCore.Tools via NuGet Package Manager
- Added project reference in .ConsoleApp project to both Data and Domain projects
- Used Nuget Package Manager Console (accessed via the Tools menu) to run `add-migration InitialMigration`
    - Ensure default project is set to wherever the DBContext is stored (`.Data` in our case)
    - If it's the first time running, it creates a folder called Migrations in the project, containing the migration file and a context snapshot
    - The migration contains both an `Up` and `Down` method
- Finally ran `update-database` which actually runs the migrations

#### Migration Scripts

- If you don't want EF Core to have complete control, you can use the Package Manager Console to generate SQL scripts instead
- Just use the `script-migration` in the Console
- This might be useful if you need to hand off the DB changes to someone else in your organisation (e.g. a DB administrator)

#### Reverse engineer an existing database

- You still need to have domain models for your tables (but don't need DbContext)
- Use the `Scaffold-DbContext` command:
    `Scaffold-DbContext -provider Microsoft.EntityFrameworkCore.SqlServer - connection "Data Source=(localdb)\MSSQLLocalDB; Initial Catalog=FootballLeague_EfCore"`
- We're using the same stuff for our DB above, but:
    - `provider` = the DB provider - doesn't need to be SqlServer, could be PostgreSQL etc.
    - `connection` = the equivalent of the connection string we set up in DbContext earlier

## Interacting with your Database

### EF Core Power Tools

- Visual Studio extension 
- Right-click a project and you will see `EF Core Power Tools` in the context menu
- Amongst other things, it can create a Diagram of your DB Context as a dgml file - useful for visualising and documentation

### Adding verbose logging to EF Core's Workload

- In the DbContext file, add a `.LogTo()` call: tell it where to log to, pass in an array of what to log along with the level of logging:
    ```cs
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB; Initial Catalog=FootballLeague_EfCore")
        .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
        .EnableSensitiveDataLogging();
    }
    ```
- Adding the `EnableSensitiveDataLogging() call will show more detail (that we wouldn't want shown elsewhere)
- Shows things like parameters, time taken to executre command, SQL code that was generated etc.
