using System.Runtime.InteropServices;

namespace StartSch.Wasm;

public static class CollectionDictionaryExtensions
{
    extension<TKey, TCollection, TValue>(Dictionary<TKey, TCollection> dict)
        where TKey : notnull
        where TCollection : ICollection<TValue>, new()
    {
        public void AddToCollection(TKey key, TValue value)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _);
            entry ??= [];
            entry.Add(value);
        }
    }
}
