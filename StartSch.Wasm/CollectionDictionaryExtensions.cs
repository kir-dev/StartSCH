using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace StartSch.Wasm;

public static class CollectionDictionaryExtensions
{
    extension<TKey, TCollection, TValue>(Dictionary<TKey, TCollection> dict)
        where TKey : notnull
        where TCollection : ICollection<TValue>, new()
    {
        [CollectionAccess(CollectionAccessType.UpdatedContent)]
        public void AddToCollection(TKey key, TValue value)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _);
            entry ??= [];
            entry.Add(value);
        }
        
        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        public void RemoveFromCollection(TKey key, TValue value)
        {
            var collection = dict[key];
            collection.Remove(value);
            if (collection.Count == 0)
                dict.Remove(key);
        }
    }
}
