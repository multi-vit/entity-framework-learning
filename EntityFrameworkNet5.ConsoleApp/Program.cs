using EntityFrameworkNet5.Data;
using EntityFrameworkNet5.Domain;
using Microsoft.EntityFrameworkCore;
using System;

namespace EntityFrameworkNet5.ConsoleApp
{
    class Program
    {
        private static FootballLeagueDbContext context = new FootballLeagueDbContext();

        static async Task Main(string[] args)
        {
            /* Simple Insert Operation Methods */
            // await AddNewLeague();
            // await AddNewTeamsWithLeague();

            /* Simple Select Queries */
            // await SimpleSelectAllQuery();

            /*  Queries With Filters*/
            // await QueryFilters();

            /* Aggregate Functions */
            // await AdditionalExecutionMethods();
            // await AlternativeLinqSyntax();

            /* Perform Update */
            // await SimpleUpdateLeagueRecord();
            // await SimpleUpdateTeamRecord();

            /* Perform Delete */
            // await SimpleDelete();
            //await DeleteWithRelationship();

            /* Tracking vs No-Tracking */
            // await TrackingVsNoTracking();

            /* Adding Records with relationships */

            // Adding OneToMany Related Records
            // await AddNewTeamsWithLeague();
            // await AddNewTeamWithLeagueId();
            // await AddNewLeagueWithTeams();

            // Adding ManyToMany Records
            // await AddNewMatches();

            //Adding OneToOne Records
            // await AddNewCoach();

            /* Including Related Data - Eager Loading */
            await QueryRelatedRecords();

            Console.WriteLine("Press any key to end...");
            Console.ReadKey();
        }

        private static async Task QueryRelatedRecords()
        {
            // Get Many Related Records - Leagues -> Teams
            var leagues = await context.Leagues.Include(q => q.Teams).ToListAsync();

            // Get One Related Record - Team -> Coach
            var team = await context.Teams
                .Include(q => q.Coach)
                .FirstOrDefaultAsync(q => q.Id == 17);

            // Get 'Grand Children' Related Record - Team -> matches -> Home/Away Team
            var teamWithMatchesAndOpponents = await context.Teams
                .Include(q => q.AwayMatches).ThenInclude(q => q.HomeTeam)
                .Include(q => q.HomeMatches).ThenInclude(q => q.AwayTeam)
                .FirstOrDefaultAsync(q => q.Id == 12);

            // Get Includes with filters
            var teams = await context.Teams
                .Where(q => q.HomeMatches.Count > 0)
                .Include(q => q.Coach)
                .ToListAsync();
        }

        private static async Task TrackingVsNoTracking()
        {
            // Using FirstOrDefault() because AsNoTracking() doesn't work with Find() method
            var withTracking = await context.Teams.FirstOrDefaultAsync(q => q.Id == 2);
            var withNoTracking = await context.Teams.AsNoTracking().FirstOrDefaultAsync(q => q.Id == 8);
            // When we don't track, it release memory and improves performance
            // But you cannot change that record programmatically
            // Useful for large read-only operations, like retrieving a list for display

            withTracking.Name = "AC Milan";
            withNoTracking.Name = "Rivoli United";

            var entriesBeforeFirstSave = context.ChangeTracker.Entries();
            foreach (var entry in entriesBeforeFirstSave)
            {
                Console.WriteLine(entry);
            }

            await context.SaveChangesAsync();

            var entriesAfterFirstSave = context.ChangeTracker.Entries();
            foreach (var entry in entriesAfterFirstSave)
            {
                Console.WriteLine(entry);
            }

            // You could manually perform operations on the withNoTracking variable by using context.Teams.Update(withNoTracking)
            // Then saving changes, as we've looked at before
            // Then it starts to be tracked

            context.Teams.Update(withNoTracking);

            var entriesBeforeSecondSave = context.ChangeTracker.Entries();
            foreach (var entry in entriesBeforeSecondSave)
            {
                Console.WriteLine(entry);
            }

            await context.SaveChangesAsync();

            var entriesAfterSecondSave = context.ChangeTracker.Entries();
            foreach (var entry in entriesAfterSecondSave)
            {
                Console.WriteLine(entry);
            }

        }

        private static async Task SimpleDelete()
        {
            // Have to find the entity to pass in first
            var leagueToDelete = await context.Leagues.FindAsync(3);
            context.Leagues.Remove(leagueToDelete);
            await context.SaveChangesAsync();
            // Can also use RemoveRange() method to do bulk
        }

        private static async Task DeleteWithRelationship()
        {
            // Same code as the above but deleting a league that has teams attached
            // This only works if Cascade deletion is set in your migration:
            // .OnDelete(DeleteBehavior.Cascade)
            var leagueToDelete = await context.Leagues.FindAsync(2);
            context.Leagues.Remove(leagueToDelete);
            await context.SaveChangesAsync();
            // Can also use RemoveRange() method to do bulk
        }

        private static async Task SimpleUpdateTeamRecord()
        {
            // Update a record where we already have the details of it from a previous GET
            var team = new Team
            {
                Id = 3,
                Name = "Andy United",
                LeagueId = 1
            };
            context.Teams.Update(team);
            await context.SaveChangesAsync();
        }

        private static async Task SimpleUpdateLeagueRecord()
        {
            // Retrieve Record (Find league with an ID of 3)
            var league = await context.Leagues.FindAsync(3);
            // Make Record Changes
            league.Name = "Scottish Premiership";
            // Save Changes
            await context.SaveChangesAsync();
            // Retrieve Record again to check it has been updated
            var updatedLeague = await context.Leagues.FindAsync(3);
            Console.WriteLine($"{updatedLeague.Id}: {updatedLeague.Name}");
        }

        private static async Task AlternativeLinqSyntax()
        {
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
            // Use with EF LIKE function
            var fuzzyTeamName = "Mil";
            var fuzzyTeam = from i in context.Teams where EF.Functions.Like(i.Name, $"%{fuzzyTeamName}%") select i;
            foreach (var team in fuzzyTeam)
            {
                Console.WriteLine($"{team.Id}: {team.Name}");
            }
        }

        private static async Task AdditionalExecutionMethods()
        {
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
        }

        private static async Task QueryFilters()
        {
            var fullLeagueName = "Serie A";
            var exactMatches = await context.Leagues.Where(league => league.Name.Equals(fullLeagueName)).ToListAsync();
            foreach (var league in exactMatches)
            {
                Console.WriteLine($"{league.Id}: {league.Name}");
            }
            var partialLeagueName = "Premiere";
            // Using LINQ syntax
            // var partialMatches = await context.Leagues.Where(league => league.Name.Contains(partialLeagueName)).ToListAsync();
            // Using EF Core functions
            var partialMatches = await context.Leagues.Where(league => EF.Functions.Like(league.Name, $"%{partialLeagueName}%")).ToListAsync();
            foreach (var league in partialMatches)
            {
                Console.WriteLine($"{league.Id}: {league.Name}");
            }
        }

        private static async Task SimpleSelectAllQuery()
        {
            // Get all leagues (SQL: SELECT * FROM Leagues)
            // Smartest, efficient way 
            var leagues = await context.Leagues.ToListAsync();
            foreach (var league in leagues)
            {
                Console.WriteLine($"{league.Id}: {league.Name}");
            }

            // Only works with ToList() on the end otherwise won't execute until foreach
            // Then will keep the connection to the DB open for the time of the foreach loop running (which won't scale)
            // Might also create a lock on the table
            //foreach (var league in context.Leagues)
            //{
            //    Console.WriteLine($"{league.Id}: {league.Name}");
            //}
        }

        private static async Task AddNewLeague()
        {
            // Adding a new League Object
            var league = new League { Name = "Serie A" };
            await context.Leagues.AddAsync(league);
            await context.SaveChangesAsync();

            // Function to add new teams related to the new League Object
            await AddTeamsWithLeague(league);
            await context.SaveChangesAsync();
        }

        private static async Task AddTeamsWithLeague(League league)
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

        private static async Task AddNewTeamsWithLeague()
        {
            var league = new League { Name = "Bundesliga" };
            var team = new Team { Name = "Bayern Munich", League = league };
            await context.AddAsync(team);
            await context.SaveChangesAsync();
        }

        private static async Task AddNewTeamWithLeagueId()
        {
            var team = new Team { Name = "Bayern Munich", LeagueId = 1 };
            await context.AddAsync(team);
            await context.SaveChangesAsync();
        }

        private static async Task AddNewLeagueWithTeams()
        {
            var teams = new List<Team>()
            {
                new Team { Name = "Arsenal"},
                new Team { Name = "Chelsea"},
                new Team { Name = "Manchester City"},
                new Team { Name = "Manchester United"},
                new Team { Name = "Crystal Palace"},

            };
            var league = new League { Name = "Premier League", Teams = teams };
            await context.AddAsync(league);
            await context.SaveChangesAsync();

        }

        private static async Task AddNewMatches()
        {
            var matches = new List<Match>
            {
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

        private static async Task AddNewCoach()
        {
            var coachOne = new Coach { Name = "Ted Lasso", TeamId = 17 };
            var coachTwo = new Coach { Name = "Antonio Conte" };
            await context.AddAsync(coachOne);
            await context.AddAsync(coachTwo);
            await context.SaveChangesAsync();
        }
    }
}