using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class DiscriminatingDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public Action<TKey, TValue> onAdd;
        public Action<TKey, TValue> onRemove;
        public DiscriminatingDictionary(IDictionary<TKey, TValue>? initialData = null, Action<TKey, TValue> onAdd = null, Action<TKey, TValue> onRemove = null)
        {
            this.onAdd = onAdd;
            this.onRemove = onRemove;
            if (initialData != null)
            {
                foreach (var kvp in initialData)
                {
                    base[kvp.Key] = kvp.Value;
                }
            }
        }

        public new TValue this[TKey key]
        {
            set
            {
                var exists = base.ContainsKey(key);
                var oldValue = exists ? base[key] : default;
                base[key] = value;
                if (exists)
                    OnRemove(key, oldValue);
                OnAdd(key, value);
            }
            get => base[key];
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            OnAdd(key, value);
        }

        public new bool Remove(TKey key)
        {
            if (!base.TryGetValue(key, out var value)) return false;
            var result = base.Remove(key);
            if (result)
                OnRemove(key, value);
            return result;
        }

        public new void Clear()
        {
            var copy = new Dictionary<TKey, TValue>(this);
            base.Clear();
            foreach (var kvp in copy)
            {
                OnRemove(kvp.Key, kvp.Value);
            }
        }

        public new void Update(IDictionary<TKey, TValue> other)
        {
            foreach (var kvp in other)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        public new TValue? this[TKey key, TValue? defaultValue]
        {
            get => base.ContainsKey(key) ? base[key] : defaultValue;
            set
            {
                var exists = base.ContainsKey(key);
                if (!exists)
                {
                    base[key] = value;
                    OnAdd(key, value);
                }
            }
        }

        private void OnAdd(TKey key, TValue value)
        {
            onAdd?.Invoke(key, value);
        }

        private void OnRemove(TKey key, TValue value)
        {
            onRemove?.Invoke(key, value);
        }
    }
}
