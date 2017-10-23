using System;
using System.Collections;
using System.Collections.Generic;
using toofz.NecroDancer.Leaderboards;

namespace LeaderboardTools
{
    internal sealed class DailyLeaderboardsEnumerable : IEnumerable<DailyLeaderboard>
    {
        #region Static Members

        private static readonly DateTime ClassicEarlyAccessStart = new DateTime(2014, 7, 30, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ClassicStart = new DateTime(2015, 4, 23, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AmplifiedEarlyAccessStart = new DateTime(2017, 1, 24, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AmplifiedStart = new DateTime(2017, 7, 12, 0, 0, 0, DateTimeKind.Utc);

        private static string GetDailyLeaderboardName(DailyLeaderboard dailyLeaderboard)
        {
            var tokens = new List<string>();

            switch (dailyLeaderboard.ProductId)
            {
                case 1: tokens.Add("DLC"); break;
            }

            tokens.Add(dailyLeaderboard.Date.ToString("d/M/yyyy"));

            var name = string.Join(" ", tokens);

            // Amplified Early Access leaderboards have a _PROD suffix.
            if (!(dailyLeaderboard.ProductId == 0 && !dailyLeaderboard.IsProduction))
            {
                name += "_PROD";
            }

            return name;
        }

        #endregion

        public DailyLeaderboardsEnumerable(DateTime maxDate)
        {
            this.maxDate = maxDate;
        }

        private readonly DateTime maxDate;

        public IEnumerator<DailyLeaderboard> GetEnumerator()
        {
            var date = ClassicEarlyAccessStart;
            do
            {
                var dailyLeaderboard = new DailyLeaderboard
                {
                    DisplayName = $"Daily ({date.ToString("yyyy-MM-dd")}) (Early Access)",
                    IsProduction = false,
                    ProductId = 0,
                    Date = date,
                };
                dailyLeaderboard.Name = GetDailyLeaderboardName(dailyLeaderboard);
                yield return dailyLeaderboard;
                date = date.AddDays(1);
            } while (date < ClassicStart);

            date = ClassicStart;
            do
            {
                var dailyLeaderboard = new DailyLeaderboard
                {
                    DisplayName = $"Daily ({date.ToString("yyyy-MM-dd")})",
                    IsProduction = true,
                    ProductId = 0,
                    Date = date,
                };
                dailyLeaderboard.Name = GetDailyLeaderboardName(dailyLeaderboard);
                yield return dailyLeaderboard;
                date = date.AddDays(1);
            } while (date < maxDate);

            date = AmplifiedEarlyAccessStart;
            do
            {
                var dailyLeaderboard = new DailyLeaderboard
                {
                    DisplayName = $"Daily ({date.ToString("yyyy-MM-dd")}) (Amplified) (Early Access)",
                    IsProduction = false,
                    ProductId = 1,
                    Date = date,
                };
                dailyLeaderboard.Name = GetDailyLeaderboardName(dailyLeaderboard);
                yield return dailyLeaderboard;
                date = date.AddDays(1);
            } while (date < AmplifiedStart);

            date = AmplifiedStart;
            do
            {
                var dailyLeaderboard = new DailyLeaderboard
                {
                    DisplayName = $"Daily ({date.ToString("yyyy-MM-dd")}) (Amplified)",
                    IsProduction = true,
                    ProductId = 1,
                    Date = date,
                };
                dailyLeaderboard.Name = GetDailyLeaderboardName(dailyLeaderboard);
                yield return dailyLeaderboard;
                date = date.AddDays(1);
            } while (date < maxDate);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
