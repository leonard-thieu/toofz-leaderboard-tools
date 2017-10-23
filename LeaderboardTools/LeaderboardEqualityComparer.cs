using System.Collections.Generic;
using toofz.NecroDancer.Leaderboards;

namespace LeaderboardTools
{
    internal sealed class LeaderboardEqualityComparer : EqualityComparer<Leaderboard>
    {
        public override bool Equals(Leaderboard x, Leaderboard y)
        {
            return
                x.ProductId == y.ProductId &&
                x.ModeId == y.ModeId &&
                x.RunId == y.RunId &&
                x.CharacterId == y.CharacterId &&
                x.IsProduction == y.IsProduction &&
                x.IsCoOp == y.IsCoOp &&
                x.IsCustomMusic == y.IsCustomMusic;
        }

        public override int GetHashCode(Leaderboard obj)
        {
            return
                (obj.CharacterId * 1000000) +
                (obj.RunId * 100000) +
                (obj.ModeId * 10000) +
                (obj.ProductId * 1000) +
                ((obj.IsProduction ? 1 : 0) * 100) +
                ((obj.IsCoOp ? 1 : 0) * 10) +
                ((obj.IsCustomMusic ? 1 : 0) * 1);
        }
    }
}
