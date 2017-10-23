using System.Collections.Generic;
using System.Linq;
using toofz.NecroDancer.Leaderboards;

namespace LeaderboardTools
{
    internal static class Areas
    {
        private static readonly List<string> Weapons = new List<string>
        {
            "Bows",
            "Broadswords",
            "Cats",
            "Crossbows",
            "Daggers",
            "Flails",
            "Longswords",
            "Rapiers",
            "Spears",
            "Whips",
        };

        private static readonly List<string> Chests = new List<string>
        {
            "Red",
            "Purple",
            "Black",
            "Mimic",
        };

        private static readonly List<string> Items = new List<string>
        {
            "Armor",
            "Consumable",
            "Feet",
            "Food",
            "Head",
            "Rings",
            "Scrolls",
            "Spells",
            "Torches",
        };

        private static readonly List<string> Enemies = new List<string>
        {
            "Floating",
            "Massive",
            "Miniboss",
            "Ignore Walls",
        };

        public static AreasEnvelope GetAreas()
        {
            var areas = new List<Area>
            {
                GetItemsArea(),
                GetEnemiesArea(),
                GetLeaderboardsArea()
            };

            return new AreasEnvelope { Areas = areas };
        }

        private static Area GetItemsArea()
        {
            var area = new Area { Name = "Items", Path = "/items", Categories = new List<Area>() };

            var weapons = new Area { Name = "Weapons", Path = "/items/weapons", Categories = new List<Area>() };
            foreach (var weapon in Weapons)
            {
                var category = new Area { Name = weapon, Path = $"/items/weapons/{EncodeUriComponent(weapon)}" };
                weapons.Categories.Add(category);
            }
            area.Categories.Add(weapons);

            var chests = new Area { Name = "Chests", Categories = new List<Area>() };
            foreach (var chest in Chests)
            {
                var category = new Area { Name = chest, Path = $"/items/chest/{EncodeUriComponent(chest)}" };
                chests.Categories.Add(category);
            }
            area.Categories.Add(chests);

            foreach (var item in Items)
            {
                var category = new Area { Name = item, Path = $"/items/{EncodeUriComponent(item)}" };
                area.Categories.Add(category);
            }

            return area;
        }

        private static Area GetEnemiesArea()
        {
            var area = new Area { Name = "Enemies", Path = "/enemies", Categories = new List<Area>() };

            foreach (var enemy in Enemies)
            {
                var category = new Area { Name = enemy, Path = $"/enemies/{EncodeUriComponent(enemy)}" };
                area.Categories.Add(category);
            }

            return area;
        }

        private static Area GetLeaderboardsArea()
        {
            var area = new Area
            {
                Name = "Leaderboards",
                Path = "/leaderboards",
            };

            using (var db = new LeaderboardsContext())
            {
                var areas = (from l in db.Leaderboards
                             group l by l.Product into byProduct
                             select new
                             {
                                 byProduct.Key,
                                 Categories = from l in byProduct
                                              group l by l.Mode into byMode
                                              select new
                                              {
                                                  byMode.Key,
                                                  Categories = from l in byMode
                                                               group l by l.Run into byRun
                                                               select new
                                                               {
                                                                   byRun.Key,
                                                                   Categories = from l in byRun
                                                                                group l by l.Character into byCharacter
                                                                                select new { byCharacter.Key },
                                                               },
                                              },
                             }).ToList();

                area.Categories = (from a in areas
                                   select new Area
                                   {
                                       Name = a.Key.DisplayName,
                                       Categories = (from b in a.Categories
                                                     select new Area
                                                     {
                                                         Name = b.Key.DisplayName,
                                                         Categories = (from c in b.Categories
                                                                       select new Area
                                                                       {
                                                                           Name = c.Key.DisplayName,
                                                                           Categories = (from d in c.Categories
                                                                                         select new Area
                                                                                         {
                                                                                             Name = d.Key.DisplayName,
                                                                                             Path = ToPath(a.Key.Name, b.Key.Name, c.Key.Name, d.Key.Name),
                                                                                             Icon = $"/images/characters/{d.Key.Name}.png",
                                                                                         }).ToList(),
                                                                       }).ToList(),
                                                     }).ToList(),
                                   }).ToList();
            }

            var classic = area.Categories.Single(c => c.Name == "Classic");
            classic.Categories.Add(new Area
            {
                Name = "Daily",
                Path = "/leaderboards/daily",
            });

            var amplified = area.Categories.Single(c => c.Name == "Amplified");
            amplified.Categories.Add(new Area
            {
                Name = "Daily",
                Path = "/leaderboards/amplified/daily",
            });

            return area;
        }

        private static string EncodeUriComponent(string str)
        {
            return str.ToLower()
                .Replace(" ", "-")
                .Replace("(", "")
                .Replace(")", "");
        }

        private static string ToPath(string product, string mode, string run, string character)
        {
            var tokens = new List<string> { "", "leaderboards" };

            switch (product)
            {
                case "classic": break;
                default: tokens.Add(product); break;
            }
            tokens.Add(character);
            tokens.Add(run);
            switch (mode)
            {
                case "standard": break;
                default: tokens.Add(mode); break;
            }

            return string.Join("/", tokens);
        }
    }
}
