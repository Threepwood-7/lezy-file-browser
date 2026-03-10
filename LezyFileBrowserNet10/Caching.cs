using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LezyFileBrowser
{
    public class CachedInfo
    {
        public long DirSize { get; set; }
    }
    public class LocalCache
    {
        private IDictionary<string, CachedInfo> cache = new Dictionary<string, CachedInfo>();

        public bool IsCached(string key)
        {
            return cache.ContainsKey(key);
        }

        public void Add(string key, CachedInfo cachedInfo)
        {
            cache[key] = cachedInfo;
        }

        public CachedInfo Get(string key)
        {
            return cache[key];
        }
    }
}
