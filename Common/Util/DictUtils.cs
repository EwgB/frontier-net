using System.Collections.Generic;

namespace FrontierSharp.Common.Util {
    public static class DictUtils {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> k, out TKey t, out TValue u) {
            t = k.Key;
            u = k.Value;
        }
    }
}
