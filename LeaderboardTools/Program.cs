using System;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LeaderboardTools.Properties;
using log4net;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Polly;
using toofz.NecroDancer.Leaderboards;
using toofz.NecroDancer.Leaderboards.Steam.ClientApi;

namespace LeaderboardTools
{
    internal static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        private static readonly TelemetryClient TelemetryClient = new TelemetryClient();

        // Await synchronously. async Main is a debugging nightmare.
        private static void Main(string[] args)
        {
            Log.Debug("Initialized logging...");
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            if (args.Length != 2)
                throw new ArgumentException("Your Steam user name and password must be passed in as arguments.");

            var userName = args[0];
            var password = args[1];

            var areas = Areas.GetAreas();
            WriteJson(areas, "areas", DefaultValueHandling.Ignore);

            using (var db = new LeaderboardsContext())
            {
                var products = JsonConvert.DeserializeObject<Product[]>(Resources.Products);
                db.Products.AddOrUpdate(p => p.ProductId, products);

                var modes = JsonConvert.DeserializeObject<Mode[]>(Resources.Modes);
                db.Modes.AddOrUpdate(m => m.ModeId, modes);

                var runs = JsonConvert.DeserializeObject<Run[]>(Resources.Runs);
                db.Runs.AddOrUpdate(r => r.RunId, runs);

                var characters = JsonConvert.DeserializeObject<Character[]>(Resources.Characters);
                db.Characters.AddOrUpdate(c => c.CharacterId, characters);

                db.SaveChanges();
            }

            using (var steamClient = new SteamClientApiClient(userName, password, Policy.NoOpAsync(), TelemetryClient))
            {
                steamClient.Timeout = TimeSpan.FromSeconds(30);

                await steamClient.ConnectAndLogOnAsync().ConfigureAwait(false);

                var leaderboards = await Leaderboards.GetLeaderboardsAsync(steamClient).ConfigureAwait(false);
                WriteJson(leaderboards.Select(l => new
                {
                    l.LeaderboardId,
                    l.DisplayName,
                    l.Name,
                    l.IsProduction,
                    l.ProductId,
                    l.ModeId,
                    l.RunId,
                    l.CharacterId,
                    l.IsCustomMusic,
                    l.IsCoOp,
                }), "Leaderboards");

                var dailyLeaderboards = await DailyLeaderboards.GetDailyLeaderboardsAsync(steamClient).ConfigureAwait(false);
                WriteJson(dailyLeaderboards.Select(l => new
                {
                    l.LeaderboardId,
                    l.DisplayName,
                    l.Name,
                    l.IsProduction,
                    l.ProductId,
                    l.Date,
                }), "DailyLeaderboards");
            }
        }

        private static void WriteJson(
            object obj,
            string filename,
            DefaultValueHandling defaultValueHandling = DefaultValueHandling.Include)
        {
            var settings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = defaultValueHandling,
                Formatting = Formatting.Indented,
            };

            var json = JsonConvert.SerializeObject(obj, settings);
            File.WriteAllText($"{filename}.json", json);
        }
    }
}
