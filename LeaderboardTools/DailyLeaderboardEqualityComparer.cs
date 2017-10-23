using System.Collections.Generic;
using toofz.NecroDancer.Leaderboards;

namespace LeaderboardTools
{
    internal sealed class DailyLeaderboardEqualityComparer : EqualityComparer<DailyLeaderboard>
    {
        public override bool Equals(DailyLeaderboard x, DailyLeaderboard y)
        {
            return
                x.Date == y.Date &&
                x.IsProduction == y.IsProduction &&
                x.ProductId == y.ProductId;
        }

        public override int GetHashCode(DailyLeaderboard obj)
        {
            return (((int)obj.Date.Ticks & 0xff) << 2) +
                ((obj.IsProduction ? 1 : 0) << 1) +
                (obj.ProductId << 0);
        }
    }
}
