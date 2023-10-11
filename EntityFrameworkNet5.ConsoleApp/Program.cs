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

            await QueryFilters();

            Console.WriteLine("Press any key to end...");
            Console.ReadKey();
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
            var partialMatches = await context.Leagues.Where(league => league.Name.Contains(partialLeagueName)).ToListAsync();
            foreach (var league in partialMatches)
            {
                Console.WriteLine($"{league.Id}: {league.Name}");
            }
        }

        static async Task SimpleSelectAllQuery()
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

        static async Task AddNewLeague()
        {
            // Adding a new League Object
            var league = new League { Name = "Serie A" };
            await context.Leagues.AddAsync(league);
            await context.SaveChangesAsync();

            // Function to add new teams related to the new League Object
            await AddTeamsWithLeague(league);
            await context.SaveChangesAsync();
        }

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

        static async Task AddNewTeamsWithLeague()
        {
            var league = new League { Name = "Bundesliga" };
            var team = new Team { Name = "Bayern Munich", League = league };
            await context.AddAsync(team);
            await context.SaveChangesAsync();
        }
    }
}