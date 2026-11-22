using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace beveikZinojau.lt.Services.ServiceHelpers
{

    public class CacheStore<T> where T : class
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
        private readonly IMemoryCache cache;
        private readonly string prefix;

        public CacheStore(IMemoryCache cache, string prefix)
        {
            this.cache = cache;
            this.prefix = prefix;
        }

        // Stores the item under a new id and returns that id
        public Guid Add(T value)
        {
            Guid id = Guid.NewGuid();
            cache.Set($"{prefix}:{id}", value, Lifetime);
            return id;
        }

        // Returns the item, or null if it is missing or expired
        public T? Get(Guid id)
        {

            return cache.TryGetValue($"{prefix}:{id}", out T? value) ? value : null;
        }
    }
}
