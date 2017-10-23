using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using toofz.NecroDancer.Leaderboards;
using toofz.NecroDancer.Leaderboards.Steam.ClientApi;

namespace LeaderboardTools
{
    internal static class Leaderboards
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Leaderboards));

        public static async Task<IEnumerable<Leaderboard>> GetLeaderboardsAsync(ISteamClientApiClient steamClient)
        {
            using (var db = new LeaderboardsContext())
            {
                var products = db.Products.ToList();
                var modes = db.Modes.ToList();
                var runs = db.Runs.ToList();
                var characters = db.Characters.ToList();
                var expectedLeaderboards = new LeaderboardsEnumerable(products, modes, runs, characters);

                // Exclude existing leaderboards
                var leaderboards = db.Leaderboards.ToList();
                var missingLeaderboards = expectedLeaderboards.Except(leaderboards, new LeaderboardEqualityComparer()).ToList();

                if (missingLeaderboards.Any())
                {
                    // Get the leaderboard IDs for the missing leaderboards
                    await UpdateLeaderboardsAsync(steamClient, missingLeaderboards).ConfigureAwait(false);

                    // Store the new leaderboards
                    foreach (var leaderboard in missingLeaderboards)
                    {
                        if (leaderboard.LeaderboardId == 0) { continue; }

                        // null these out so Entity Framework doesn't try to add them as new entities.
                        leaderboard.Product = null;
                        leaderboard.Mode = null;
                        leaderboard.Run = null;
                        leaderboard.Character = null;

                        db.Leaderboards.Add(leaderboard);
                    }

                    db.SaveChanges();
                }

                return (from l in db.Leaderboards
                        orderby l.ProductId, l.IsProduction, l.CharacterId, l.RunId, l.ModeId, l.IsCustomMusic, l.IsCoOp
                        select l)
                        .ToList();
            }
        }

        private static Task UpdateLeaderboardsAsync(ISteamClientApiClient steamClient, IEnumerable<Leaderboard> leaderboards)
        {
            var updateTasks = new List<Task>();

            foreach (var leaderboard in leaderboards)
            {
                var updateTask = UpdateLeaderboardAsync(steamClient, leaderboard);
                updateTasks.Add(updateTask);
            }

            return Task.WhenAll(updateTasks);
        }

        private static async Task UpdateLeaderboardAsync(ISteamClientApiClient steamClient, Leaderboard leaderboard)
        {
            Log.Debug($"Start download {leaderboard.Name}.");
            var response = await steamClient.FindLeaderboardAsync(247080, leaderboard.Name).ConfigureAwait(false);
            Log.Debug($"End download {leaderboard.Name}.");

            if (response.ID == 0)
            {
                var message = $"Leaderboard named '{leaderboard.Name}' could not be found.";
                Log.Info(message);
            }

            leaderboard.LeaderboardId = response.ID;
        }
    }
}
