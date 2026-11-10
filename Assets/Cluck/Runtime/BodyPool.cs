using System.Collections.Generic;
using UnityEngine;

namespace Cluck
{
    internal sealed class BodyPool
    {
        readonly Dictionary<PoolBody, Stack<PoolBody>> free = new();
        readonly Dictionary<PoolBody, PoolBody> source = new();
        readonly Dictionary<PoolBody, int> index = new();
        readonly Transform root;
        internal readonly List<PoolBody> Live = new(128);
        internal BodyPool(Transform root) => this.root = root;

        PoolBody Create(PoolBody prefab)
        {
            var body = Object.Instantiate(prefab, root);
            body.name = prefab.name;
            body.Owner = this;
            body.gameObject.SetActive(false);
            source.Add(body, prefab);
            return body;
        }

        Stack<PoolBody> Bucket(PoolBody prefab)
        {
            if (!free.TryGetValue(prefab, out var stack)) free.Add(prefab, stack = new());
            return stack;
        }

        internal void Warm(PoolBody prefab, int count)
        {
            var stack = Bucket(prefab);
            while (stack.Count < count) stack.Push(Create(prefab));
        }

        internal PoolBody Spawn(PoolBody prefab, Arena arena, Vector2 at, float life = 99)
        {
            var stack = Bucket(prefab);
            var body = stack.Count > 0 ? stack.Pop() : Create(prefab);
            index.Add(body, Live.Count);
            Live.Add(body);
            body.Activate(arena, at, life);
            return body;
        }

        internal void Release(PoolBody body)
        {
            if (!body.Active || body.Owner != this) return;
            int slot = index[body], last = Live.Count - 1;
            if (slot != last)
            {
                // Swap with the last active object to avoid shifting the list.
                var moved = Live[last];
                Live[slot] = moved;
                index[moved] = slot;
            }
            Live.RemoveAt(last);
            index.Remove(body);
            body.Active = false;
            body.gameObject.SetActive(false);
            Bucket(source[body]).Push(body);
        }
    }
}
