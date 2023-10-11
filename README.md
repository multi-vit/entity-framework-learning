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
- Shows things like parameters, time taken to execute command, SQL code that was generated etc.

## Simple Insert Operations

- In the console app, instantiate the context: `private static FootballLeagueDbContext context = new FootballLeagueDbContext();`

### League

- Add any changes that need to be made: `context.Leagues.Add(new League { Name = "Red Stripe Premiere League" });`
    - You can also pass in the object of the right type instead:
    ```cs
    var league = new League { Name = "La Liga" };
    context.Leagues.Add(league);
    ```
- You must **ALWAYS** call SaveChanges() to ensure they are persisted: `context.SaveChanges();`
- You can also do all of these operations asynchronously:
    ```cs
    await context.Leagues.AddAsync(new League { Name = "La Liga" });
    await context.SaveChangesAsync();
    ```

### Teams

- We are using the league ID from our created league because that is the foreign key of our teams: `await AddTeamsWithLeague(league);`
    ```cs
    static async Task AddTeamsWithLeague(League league)
    {
        var teams = new List<Team>
        {
            new Team
            {
            Name = "Juventus",
            LeagueId = league.Id
            },
            new Team
            {
                Name = "AC Milan",
                LeagueId = league.Id
            },
            new Team
            {
                Name = "AS Roma",
                League = league
            }
        };
        await context.AddRangeAsync(teams);
    }
    ```
- Note with Juve and AC Milan we are explicitly setting the Foreign Key (FK), 
but with AS Roma we are actually passing in the navigational property of the League (which includes the FK)
- Both are valid, but using the navigational property can be more powerful
- Also note the use of `context.AddRangeAsync()` to add a list

### Simplifying/Compounding

- EF Core is very clever and doesn't always need explicit instructions! 
- This will still create the league first, and then the team in one go:
    ```cs
    var league = new League { Name = "Bundesliga" };
    var team = new Team { Name = "Bayern Munich", League = league };
    await context.AddAsync(team);
    await context.SaveChangesAsync();
    ```

## Simple Select Operations

- We're going to be using LINQ: Language Integrated Query
- Select ALL:
    ```cs
    // Get all leagues (SQL: SELECT * FROM Leagues)
    // Smartest, efficient way 
    var leagues = context.Leagues.ToList();
    foreach (var league in leagues)
    {
        Console.WriteLine($"{league.Id}: {league.Name}");
    }
    ```
- **ALWAYS** store it  in a variable first before iteration, otherwise you 
will keep the connection open for the duration of iteration and also might
create a lock on the table if you do this
- You can also make this asynchronous:
    ```cs
    // Get all leagues (SQL: SELECT * FROM Leagues)
    // Smartest, efficient way 
    var leagues = await context.Leagues.ToListAsync();
    foreach (var league in leagues)
    {
        Console.WriteLine($"{league.Id}: {league.Name}");
    }
    ```

### Filtering Records

#### Exact Match

- Use a simple `Where()` clause which takes a lambda function for comparison:
    ```cs
    var leagues = await context.Leagues.Where(league => league.Name == "Serie A").ToListAsync();
    foreach (var league in leagues)
    {
        Console.WriteLine($"{league.Id}: {league.Name}");
    }
    ```
- This returns all records that match
- This won't get parameterized like it would normally because we've hardcoded the comparison string so EF Core assumes it's safe
- But it will parameterize it if you pass in a variable, like this:
    ```cs
    Console.Write("Enter League Name: ");
    var leagueName = Console.ReadLine();
    var leagues = await context.Leagues.Where(league => league.Name.Equals(leagueName)).ToListAsync();
    foreach (var league in leagues)
    {
        Console.WriteLine($"{league.Id}: {league.Name}");
    }
    ```

#### Fuzzy logic

- Use `.Contains()` instead of `.Equals()` to give fuzzy logic style results:
    ```cs
    var partialLeagueName = "Premiere";
    var partialMatches = await context.Leagues.Where(league => league.Name.Contains(partialLeagueName)).ToListAsync();
    foreach (var league in partialMatches)
    {
        Console.WriteLine($"{league.Id}: {league.Name}");
    }
    ```
