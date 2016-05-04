using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hurricane.Utilities
{
    public class MultiDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<TValue>> _dict = new Dictionary<TKey, List<TValue>>();

        /// <summary>
        /// Gets or sets list of values for the key.
        /// Can throw KeyNotFoundException.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<TValue> this[TKey key]
        {
            get { return _dict[key]; }
            set { _dict[key] = value; }
        }

        public Dictionary<TKey, List<TValue>>.KeyCollection Keys
        {
            get { return _dict.Keys; }
        }

        public Dictionary<TKey, List<TValue>>.ValueCollection Values
        {
            get { return _dict.Values; }
        }
        
        public void Add(TKey key, TValue value)
        {
            List<TValue> values;
            if (_dict.TryGetValue(key, out values))
            {
                values.Add(value);
            }
            else
            {
                _dict.Add(key, new List<TValue> { value });
            }
        }

        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            foreach (var v in values)
            {
                Add(key, v);
            }
        }

        public bool Remove(TKey key, TValue value)
        {
            return _dict[key].Remove(value);
        }

        public bool RemoveAll(TKey key)
        {
            return _dict.Remove(key);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(TKey key, TValue value)
        {
            return _dict[key].Contains(value);
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        /// <summary>
        /// Returns list of values corresponding to the key or an empty list.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public List<TValue> GetValueList(TKey key)
        {
            List<TValue> values;
            if (_dict.TryGetValue(key, out values))
            {
                return values;
            }
            else
            {
                return new List<TValue>();
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var kv in _dict)
            {
                foreach (var value in kv.Value)
                {
                    yield return new KeyValuePair<TKey, TValue>(kv.Key, value);
                }
            }
        }
    }
}
