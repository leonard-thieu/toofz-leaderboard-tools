using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using LeaderboardTools.Properties;
using log4net;
using Newtonsoft.Json;
using toofz.NecroDancer.Leaderboards;
using toofz.NecroDancer.Leaderboards.Steam.ClientApi;

namespace LeaderboardTools
{
    internal static class DailyLeaderboards
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DailyLeaderboards));

        public static async Task<IEnumerable<DailyLeaderboard>> GetDailyLeaderboardsAsync(ISteamClientApiClient steamClient)
        {
            var expectedDailyLeaderboards = new DailyLeaderboardsEnumerable(DateTime.UtcNow.Date);

            var missingDaily = expectedDailyLeaderboards.First(d => d.Date == new DateTime(2017, 10, 3, 0, 0, 0, DateTimeKind.Utc));

            // Add leaderboards found in local cache.
            // This does not perform updates since the local cache may contain bad data due to bugs from previous builds.
            var cachedDailyLeaderboards = JsonConvert.DeserializeObject<IEnumerable<DailyLeaderboard>>(Resources.DailyLeaderboards);
            var connectionString = ConfigurationManager.ConnectionStrings[nameof(LeaderboardsContext)].ConnectionString;
            using (var storeClient = new LeaderboardsStoreClient(connectionString))
            {
                var options = new BulkUpsertOptions { UpdateWhenMatched = false };
                await storeClient.BulkUpsertAsync(cachedDailyLeaderboards, options, default).ConfigureAwait(false);
            }

            using (var db = new LeaderboardsContext())
            {
                // Exclude existing daily leaderboards
                var dailyLeaderboards = db.DailyLeaderboards.ToList();
                var missingDailyLeaderboards = expectedDailyLeaderboards.Except(dailyLeaderboards, new DailyLeaderboardEqualityComparer()).ToList();

                if (missingDailyLeaderboards.Any())
                {
                    // Get the leaderboard IDs for the missing leaderboards
                    await UpdateDailyLeaderboardsAsync(steamClient, missingDailyLeaderboards).ConfigureAwait(false);

                    // Store the new leaderboards
                    foreach (var dailyLeaderboard in missingDailyLeaderboards)
                    {
                        if (dailyLeaderboard.LeaderboardId == 0) { continue; }

                        db.DailyLeaderboards.Add(dailyLeaderboard);
                    }

                    db.SaveChanges();
                }

                return (from l in db.DailyLeaderboards
                        orderby l.Date, l.ProductId, l.IsProduction
                        select l)
                        .ToList();
            }
        }

        private static Task UpdateDailyLeaderboardsAsync(ISteamClientApiClient steamClient, IEnumerable<DailyLeaderboard> leaderboards)
        {
            var updateTasks = new List<Task>();

            foreach (var leaderboard in leaderboards)
            {
                var updateTask = UpdateDailyLeaderboardAsync(steamClient, leaderboard);
                updateTasks.Add(updateTask);
            }

            return Task.WhenAll(updateTasks);
        }

        private static async Task UpdateDailyLeaderboardAsync(ISteamClientApiClient steamClient, DailyLeaderboard leaderboard)
        {
            try
            {
                var response = await steamClient.FindLeaderboardAsync(247080, leaderboard.Name).ConfigureAwait(false);
                leaderboard.LeaderboardId = response.ID;
            }
            catch (SteamClientApiException)
            {
                Log.Warn($"Leaderboard named '{leaderboard.Name}' could not be found.");
            }
        }
    }
}
