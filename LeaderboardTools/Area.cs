using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LeaderboardTools
{
    [DataContract]
    internal sealed class Area
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "path")]
        public string Path { get; set; }
        [DataMember(Name = "icon")]
        public string Icon { get; set; }
        [DataMember(Name = "categories")]
        public ICollection<Area> Categories { get; set; }
    }
}
