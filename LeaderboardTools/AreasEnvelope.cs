using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LeaderboardTools
{
    [DataContract]
    internal sealed class AreasEnvelope
    {
        [DataMember(Name = "areas", IsRequired = true)]
        public IEnumerable<Area> Areas { get; set; }
    }
}
