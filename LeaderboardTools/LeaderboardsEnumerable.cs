using System.Collections;
using System.Collections.Generic;
using System.Linq;
using toofz.NecroDancer.Leaderboards;

namespace LeaderboardTools
{
    internal sealed class LeaderboardsEnumerable : IEnumerable<Leaderboard>
    {
        #region Static Members

        private static string GetLeaderboardDisplayName(Leaderboard leaderboard)
        {
            var tokens = new List<string>();

            tokens.Add(leaderboard.Character.DisplayName);
            if (leaderboard.Mode.DisplayName != "Standard") { tokens.Add(leaderboard.Mode.DisplayName); }
            tokens.Add(leaderboard.Run.DisplayName);
            if (leaderboard.Product.DisplayName != "Classic") { tokens.Add($"({leaderboard.Product.DisplayName})"); }
            if (leaderboard.IsCoOp) { tokens.Add("(Co-op)"); }
            if (leaderboard.IsCustomMusic) { tokens.Add("(Custom Music)"); }

            return string.Join(" ", tokens);
        }

        private static string GetLeaderboardName(Leaderboard leaderboard)
        {
            var tokens = new List<string>();

            switch (leaderboard.Product.DisplayName)
            {
                case "Amplified": tokens.Add("DLC"); break;
            }
            switch (leaderboard.Run.DisplayName)
            {
                case "Score":
                case "Deathless": tokens.Add("HARDCORE"); break;
                case "Speed": tokens.Add("SPEEDRUN"); break;
                case "Seeded Score": tokens.Add("HARDCORE SEEDED"); break;
                case "Seeded Speed": tokens.Add("SEEDED SPEEDRUN"); break;
            }
            switch (leaderboard.Character.DisplayName)
            {
                case "All Characters": tokens.Add("All Chars"); break;
                case "All Characters (Amplified)": tokens.Add("All Chars DLC"); break;
                case "Cadence": break;
                case "Dove": tokens.Add("DOVE"); break;
                default: tokens.Add(leaderboard.Character.DisplayName); break;
            }
            if (leaderboard.IsCoOp) { tokens.Add("CO-OP"); }
            switch (leaderboard.Run.DisplayName)
            {
                case "Deathless": tokens.Add("DEATHLESS"); break;
            }
            switch (leaderboard.Mode.DisplayName)
            {
                case "Standard": break;
                default: tokens.Add(leaderboard.Mode.DisplayName.ToUpper()); break;
            }
            if (leaderboard.IsCustomMusic) { tokens.Add("CUSTOM MUSIC"); }

            var name = string.Join(" ", tokens);
            if (leaderboard.IsProduction) { name += "_PROD"; }

            return name;
        }

        #endregion

        public LeaderboardsEnumerable(
             IEnumerable<Product> products,
             IEnumerable<Mode> modes,
             IEnumerable<Run> runs,
             IEnumerable<Character> characters)
        {
            this.products = products.ToList();
            this.modes = modes.ToList();
            this.runs = runs.ToList();
            this.characters = characters.ToList();
        }

        private readonly IEnumerable<Product> products;
        private readonly IEnumerable<Mode> modes;
        private readonly IEnumerable<Run> runs;
        private readonly IEnumerable<Character> characters;

        public IEnumerator<Leaderboard> GetEnumerator()
        {
            foreach (var product in products)
            {
                foreach (var mode in modes)
                {
                    // Only Standard exists in Classic
                    if (product.DisplayName == "Classic" && mode.DisplayName != "Standard") { continue; }

                    foreach (var run in runs)
                    {
                        // Deathless is only possible in Standard
                        if (run.DisplayName == "Deathless" && mode.DisplayName != "Standard") { continue; }

                        foreach (var character in characters)
                        {
                            if (product.DisplayName == "Classic")
                            {
                                switch (character.DisplayName)
                                {
                                    case "All Characters (Amplified)":
                                    case "Diamond":
                                    case "Mary":
                                    case "Nocturna":
                                    case "Tempo": continue;
                                }
                            }

                            switch (character.DisplayName)
                            {
                                case "All Characters":
                                case "All Characters (Amplified)":
                                case "Story Mode":
                                    if (mode.DisplayName != "Standard") { continue; }
                                    switch (run.DisplayName)
                                    {
                                        case "Seeded Score":
                                        case "Seeded Speed":
                                        case "Deathless": continue;
                                    }
                                    break;
                            }

                            foreach (var isProduction in new[] { false, true })
                            {
                                // Amplified Early Access leaderboards were purged and share the same name as Amplified leaderboards.
                                // This means that they cannot be discovered by name through the Steam Client API.
                                if (!isProduction && product.DisplayName == "Amplified") { continue; }

                                foreach (var isCoOp in new[] { false, true })
                                {
                                    if (isCoOp)
                                    {
                                        switch (character.DisplayName)
                                        {
                                            case "All Characters":
                                            case "All Characters (Amplified)": continue;
                                        }
                                    }

                                    foreach (var isCustomMusic in new[] { false, true })
                                    {
                                        var leaderboard = new Leaderboard
                                        {
                                            Product = product,
                                            ProductId = product.ProductId,
                                            Mode = mode,
                                            ModeId = mode.ModeId,
                                            Run = run,
                                            RunId = run.RunId,
                                            Character = character,
                                            CharacterId = character.CharacterId,
                                            IsProduction = isProduction,
                                            IsCustomMusic = isCustomMusic,
                                            IsCoOp = isCoOp,
                                        };

                                        leaderboard.DisplayName = GetLeaderboardDisplayName(leaderboard);
                                        leaderboard.Name = GetLeaderboardName(leaderboard);

                                        yield return leaderboard;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
