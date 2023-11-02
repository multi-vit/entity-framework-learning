# Entity Framework Learning

Learning and steps from following: Trevoir Williams' `Entity Framework Core - A Full Tour` video on O'Reilly.

EF = Entity Framework

## Developing on Mac M1

This project began life being developed on a windows machine, but due to hardware failure had to be swapped over to a Mac with an M1 chip. Follow [these steps](#transition-to-mac-m1) to transition your own project.

## Chapter 2: Getting Started with Entity Framework Core

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

## Chapter 3: Interact with your Database

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

### Simple Select Operations

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
- An alternative is to use `EF.Functions()` such as `Like`, enclosing the variable with `%` either side:
    ```cs
    var partialLeagueName = "Premiere";
    var partialMatches = await context.Leagues.Where(league => EF.Functions.Like(league.Name, $"%{partialLeagueName}%")).ToListAsync();
    foreach (var league in partialMatches)
    {
        Console.WriteLine($"{league.Id}: {league.Name}");
    }
    ```

### Additional Execution Methods

- Extends from [filtering records](#filtering-records)
- Sometimes you won't want the entire filtered list, you may just want the first or last
- These methods can replace the `Where()` method call, but still take in the lambda query as normal
- You should generally prefer methods that have a *OrDefault* option, as these don't throw exceptions. E.g.:
- `First()` always expects a list and it will get the first, so if nothing is returned, it will throw an exception
- `FirstOrDefault()` tends to be safer as it will attempt to get the first but return null if not successful
    ```cs
    // Syntax for use is context.<TableName>.<Method>(<Optional Lambda query>) e.g.:
    // var league = await context.Leagues.FirstOrDefaultAsync(q => q.Name.Contains("A"));
    // This is just setting up leagues for reuse and keeping the examples DRY
    var leagues = context.Leagues;
    // All items
    var list = await leagues.ToListAsync();
    // Specific items
    var first = await leagues.FirstAsync();
    var firstOrDefault = await leagues.FirstOrDefaultAsync();
    var single = await leagues.SingleAsync();
    var singleOrDefault = await leagues.SingleOrDefaultAsync();
    // Mathematical methods
    var count = await leagues.CountAsync();
    var longCount = await leagues.LongCountAsync();
    var min = await leagues.MinAsync();
    var max = await leagues.MaxAsync();
    // Find by ID - returns record or null
    var idToFind = 1;
    var leagueById = await leagues.FindAsync(idToFind);
    ```

#### Alternative LINQ Syntax

- Can write our queries in a more SQL-like way
- Not as clean as lambda query expression, so not recommended
- But worth knowing as legacy code may use it:
    ```cs
    // Select All
    // From <queryTokenRepresentingARecord> in <tableName>, select <Record> (no filer)
    // Returns IQueryable but can call .ToList() or .ToListAsync() methods on it if needed
    // As IQueryable doesn't have all the same methods as a List
    var teams = from i in context.Teams select i;
    foreach (var team in teams)
    {
        Console.WriteLine($"{team.Id}: {team.Name}");
    }
    // With equality WHERE clause and ToListAsync
    var specificTeam = await (from i in context.Teams where i.Name == "Juventus" select i).ToListAsync();
    foreach (var team in specificTeam)
    {
        Console.WriteLine($"{team.Id}: {team.Name}");
    }
    ```

### Simple Update Query

- Follows the usual pattern of retrieving the record, modifying it then saving changes:
    ```cs
    // Retrieve Record (Find league with an ID of 3)
    var league = await context.Leagues.FindAsync(3);
    // Make Record Changes
    league.Name = "Scottish Premiership";
    // Save Changes
    await context.SaveChangesAsync();
    // Retrieve Record again to check it has been updated
    var updatedLeague = await context.Leagues.FindAsync(3);
    Console.WriteLine($"{updatedLeague.Id}: {updatedLeague.Name}");
    ```
- If you ran this statement twice, nothing will change
- If EF Core notices nothing is changing about the record, it won't even generate the SQL to run the query
- This is called *Tracking* and we will go into more detail about this later
- You can also use the `Update()` method if you already know the details of the record to update:
    ```cs
    var team = new Team
    {
        Id = 5,
        Name = "Andy United",
        LeagueId = 1
    };
    context.Teams.Update(team);
    await context.SaveChangesAsync();
    ```
- **N.B.** If you don't give it a Primary Key, it will INSERT the record instead
- If you give it a Primary Key that doesn't exist, it will threw an exception:
    > DbUpdateConcurrencyException: "Database operation expected to affect 1 row(s) but actually affected 0 row(s)"

### Simple Delete Query

- Follows the usual pattern of retrieving the record to remove first:
    ```cs
    // Have to find the entity to pass in first
    var leagueToDelete = await context.Leagues.FindAsync(3);
    context.Leagues.Remove(leagueToDelete);
    await context.SaveChangesAsync();
    // Can also use RemoveRange() method to do bulk
    ```

### Tracking vs No Tracking

- EF Core automatically tracks objects by default
- This tracking does not stop, even after we call `.SaveChanges()`
- We can manually tell EF Core not to track objects we retrieve from the database using the `AsNoTracking()` method
    - When we don't track, it releases memory and improves performances
    - But you cannot then change that record programmatically
    - This is useful for large read-only operations, like retrieving a list for display
- You can manually perform operations on the an untracked record by using `context.<Table>.Update(<recordToUpdate>), then calling `saveChanges()` as we've seen before in [Simple Update Query] (#simple-update-query)
- EF Core then starts tracking this record

    ```cs
    // Using FirstOrDefault() because AsNoTracking() doesn't work with Find() method
    var withTracking = await context.Teams.FirstOrDefaultAsync(q => q.Id == 2);
    var withNoTracking = await context.Teams.AsNoTracking().FirstOrDefaultAsync(q => q.Id == 8);

    withTracking.Name = "AC Milan";
    withNoTracking.Name = "Rivoli United";

    // Will show EF Core only tracking the withTracking record
    var entriesBeforeFirstSave = context.ChangeTracker.Entries();
    foreach (var entry in entriesBeforeFirstSave)
    {
        Console.WriteLine(entry);
    }

    await context.SaveChangesAsync();

    // Shows EF Core is still tracking the withTracking record but now shows unchanged
    var entriesAfterFirstSave = context.ChangeTracker.Entries();
    foreach (var entry in entriesAfterFirstSave)
    {
        Console.WriteLine(entry);
    }

    // Manually adding to tracking
    context.Teams.Update(withNoTracking);

    // Shows EF Core now tracking the withNoTracking record 
    var entriesBeforeSecondSave = context.ChangeTracker.Entries();
    foreach (var entry in entriesBeforeSecondSave)
    {
        Console.WriteLine(entry);
    }

    await context.SaveChangesAsync();

    // Shows EF Core still tracking both records, showing as unchanged
    var entriesAfterSecondSave = context.ChangeTracker.Entries();
    foreach (var entry in entriesAfterSecondSave)
    {
        Console.WriteLine(entry);
    }
    ```

## Chapter 4: Interacting with Related Records

### Review One-To-Many Relationships
- We have one league, with one or more teams
- EF Core did it automatically for us because we followed naming convention of using `LeagueId` in the Team Domain model
    - If we called it `LeagueFK`, but still had the `public virtual League League` property, EF Core would still generate a `LeagueId` column but **ALSO** a `LeagueFK`
- We *could* make our Foreign Key nullable by adding a question mark after the type declaration: `public int? LeagueId { get; set; }`
    - In this example, we could have a team that's not in a league
- We could add a collection type to the League table `public List<Team> Teams { get; set; }`. `List<Team>` could also be `ICollectable` or other enumerable type
    - This allows the us to access all the teams linked to a league, just by querying that league from the League table
    - e.g. without having to do a separate query of the Teams table with the `LeagueId`

### Inheriting common entity properties

- Rather than duplicating the Id field in each Domain object, we abstracted it into a BaseDomainObject class
- Each Domain object can then inherit from the BaseDomainObject and automatically get an Id by default

### Adding Many-To-Many Relationships

- As the name suggests, where we have many entities related to each other
- In our context, many teams will play each other through a season
- So we will have a new entity called `Match.cs` to Data project, that will contain a foreign key for both a home and away team, as well as a Date:
    ```cs
    public class Match : BaseDomainObject
    {
	    public int HomeTeamId { get; set; }
	    public virtual Team HomeTeam { get; set; }
	    public int AwayTeamId { get; set; }
	    public virtual Team AwayTeam { get; set; }
	    public DateTime Date { get; set; }
    }
    ```
- Added Match table reference to the `FootballLeagueDbContext.cs`:
    ```cs
    public DbSet<Match> Matches { get; set; }
    ```
- We are breaking away from naming conventions in this case, as EF Core won't recognise the IDs as representing a Team
- As a result, we had some manual configuration to do:
    - Added navigation record in `Team.cs`:
        ```cs
        public virtual List<Match> HomeMatches { get; set; }
        public virtual List<Match> AwayMatches { get; set; }
        ```
    - In `FootballLeagueDbContext.cs`, override the creation of the model:
        ```cs
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>()
                .HasMany(m => m.HomeMatches)
                .WithOne(m => m.HomeTeam)
                .HasForeignKey(m => m.HomeTeamId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Team>()
                .HasMany(m => m.AwayMatches)
                .WithOne(m => m.AwayTeam)
                .HasForeignKey(m => m.AwayTeamId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
        ```
- We went with `.OnDelete(DeleteBehaviour.Restrict)` because `.Cascade` was not allowed by EF Core as it may cause a cycle or multiple cascade paths
    - `.Restrict` means we cannot delete this entity until all links to other entities have been removed
- Added migration using `add-migration AddedMatchesTable` in the Package Manager Console
    - On [Mac](#transition-to-mac-m1), use `dotnet ef migrations add AddedMatchesTable --context FootballLeagueDbContext`
- Updated the DB using `Update-Databse` in Package Manager Console
    - On [Mac](#transition-to-mac-m1), use `dotnet ef database update -c FootballLeagueDbContext`

### Adding One-To-One Relationships

- This is depicted by a `Coach`:
    - A coach can only belong to one team
    - A team only has one coach
    ```cs
    public class Coach : BaseDomainObject
    {
	    public string Name { get; set; }
        // The TeamId is nullable because a coach may not belong to a team if they get fired
	    public int? TeamId { get; set; }
	    public virtual Team Team { get; set; }
    }
    ```
- Added a navigational link to the `Team.cs`: `public virtual Coach Coach { get; set; }`

### Inserting Related Data

#### Adding OneToMany Related Records

- We previously had a `AddNewTeamsWithLeague()` method, where we created a new league and a new team, then linked them together before inserting them
- A more common use case is a league will already exist, so we want to insert a new team attached to a pre-existing league
- This is why we add a `AddNewTeamWithLeagueId()` method where the `LeagueId` is already known
- We also inverted the logic to add a `AddNweLeagueWithTeams()` method, using a new League record and leveraging the `List<Team>` navigational link

#### Adding ManyToMany Related Records

- For `AddNewMatches()` method, we have assumed that all teams already exist, but if not we could leverage the navigational property to create a new one at the same time:
    ```cs
        static async Task AddNewMatches()
        {
            var newTeam = new Team { Name = "Andy United", LeagueId = 1 };
            var matches = new List<Match>
            {
                new Match
                {
                    HomeTeam = newTeam, AwayTeamId = 12, Date = DateTime.Now
                },
                new Match
                {
                    HomeTeamId = 12, AwayTeamId = 13, Date = DateTime.Now
                },
                new Match
                {
                    HomeTeamId = 14, AwayTeamId = 12, Date = new DateTime(2023, 11, 30)
                },
                new Match
                {
                    HomeTeamId = 13, AwayTeamId = 15, Date = DateTime.Now
                }
            };
            await context.AddRangeAsync(matches);
            await context.SaveChangesAsync();
        }
    ```

#### Adding OneToOne Related Records

- For `AddNewCoach()` method, we add both a coach linked to a team and without a team, as TeamId is nullable
    - As with `AddNewMatch()`, we could create a `new Team{}` at the same time and EF Core will create both records

### (Eager Loading) Including Related Data

- Use `.include()` to do table JOINs and bring back related child data
    - This uses the Foreign Key to find the related record
    - You can chain multiple `.Include()` to get multiple children
- Where we have children of children records, we can chain `.ThenInclude()` to use further foreign keys to get grand children
    - You can chain multiple `.ThenInclude()` to go as far down the children as possible
- As well as filtering using the lambda function e.g. `(q => q.Id == 1)` you can also use the `.Where()` method

### Projections and Anonymous Data Types

- Used for when we want specific properties only to be returned
- Use `.Select()` to select a specific property from a record
- To leverage anonymous types:
    - These should only be used in one scope (so never returned from a method etc.), else use a defined domain model
    - Combine `.Include()` to add multiple related records and `.Select()` with the `new {}` to define an anonymous type:
        ```cs
            var teams = await context.Teams
            .Include(q => q.Coach)
            // Creating a new anonymous data type
            .Select(q => new { TeamName = q.Name, CoachName = q.Coach.Name })
            .ToListAsync();
        ```

## Transition to Mac M1

Very difficult! :sweat_smile:

### Docker

- Install Docker Desktop
- In Settings -> Features in development, enable 'Use Rosetta' option (may move elsewhere in future)
- In a terminal -> Pull and run the latest SQL Server image:
    ```
    docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<yourPasswordHere>" -p 1433:1433 --name sql --hostname sql --platform linux/amd64 -v <pathToHardDriveFolderForPersitentStorage>:/var/opt/mssql -d mcr.microsoft.com/mssql/server:2022-latest
    ```

### Terminal

- Visual Studio for Mac does not support NuGet Package Manager Console
- You need to add dotnet ef support to your terminal: `dotnet tool install --global dotnet-ef`
- Verify the EF core tools are correctly installed: `dotnet ef`

### Migrations

- On a new machine, you will need to rerun your migrations to create the database and tables: `dotnet ef database update`

### Code changes

- Update your FootballLeagueDbContext.cs file to:
    ```
    optionsBuilder.UseSqlServer("Data Source=localhost; Initial Catalog=FootballLeague_EfCore; User Id=sa; Password=<yourPasswordHere>; TrustServerCertificate=True; Encrypt=True")
    ```
- Use the password you set in the [Docker section](#docker)

### Populate your new database

- Rerun the `ConsoleApp` `Program.cs` file with the following methods uncommented:
    ```
    /* Simple Insert Operation Methods */
    await AddNewLeague();
    await AddNewTeamsWithLeague();
    ```

### Add Secrets

- Using Visual Studio's `Manage User Secrets' menu, add a `secrets.json` file with the following attributes:
    ```json
    {
        "UserId": "YourDBUserName",
        "Password": "YourDBPassword"
    }
    ```