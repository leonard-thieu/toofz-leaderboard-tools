using System;
using System.Collections.Generic;
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
            using (var db = new LeaderboardsContext())
            {
                var expectedDailyLeaderboards = new DailyLeaderboardsEnumerable(DateTime.UtcNow.Date);

                // Exclude cached daily leaderboards
                var cachedDailyLeaderboards = JsonConvert.DeserializeObject<IEnumerable<DailyLeaderboard>>(Resources.DailyLeaderboards);
                var missingDailyLeaderboards = expectedDailyLeaderboards.Except(cachedDailyLeaderboards, new DailyLeaderboardEqualityComparer()).ToList();
                // Exclude existing daily leaderboards
                var dailyLeaderboards = db.DailyLeaderboards.ToList();
                missingDailyLeaderboards = missingDailyLeaderboards.Except(dailyLeaderboards, new DailyLeaderboardEqualityComparer()).ToList();

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
