using System.Linq;
using System.Collections.Generic;

namespace HtmlClipboard
{
    static class Functional
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary<TKey, TValue> d, bool ignoreDuplicateKeys)
        {
            var clone = new Dictionary<TKey, TValue>();

            foreach (var kvp in d)
            {
                if (!ignoreDuplicateKeys || !d.ContainsKey(kvp.Key))
                    clone.Add(kvp.Key, kvp.Value);
            }

            return clone;
        }
    }
}