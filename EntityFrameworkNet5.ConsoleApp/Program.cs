using EntityFrameworkNet5.Data;
using EntityFrameworkNet5.Domain;
using System;

namespace EntityFrameworkNet5.ConsoleApp
{
    class Program
    {
        private static FootballLeagueDbContext context = new FootballLeagueDbContext();

        static async Task Main(string[] args)
        {
            var league = new League { Name = "Bundesliga" };
            var team = new Team { Name = "Bayern Munich", League = league };
            await context.AddAsync(team);
            await context.SaveChangesAsync();

            Console.WriteLine("Press any key to end...");
            Console.ReadKey();
        }
    }
}