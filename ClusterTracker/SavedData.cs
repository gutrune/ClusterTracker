using System.Collections.Generic;

namespace ClusterTracker
{
    public class SavedData
    {
        public Dictionary<string, MobInfo> ZadnorData { get; set; } = new();
        public Dictionary<string, MobInfo> BSFData { get; set; } = new();
    }
}
