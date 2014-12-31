using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF.Utils
{
	public class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IWeakCollection
		where TKey: class
	{
		WeakHashtable container = new WeakHashtable();

		public void WeakCleanup() { container.WeakCleanup(); }

		public void Add(TKey key, TValue value) { container.Add(key, value); }

		public bool ContainsKey(TKey key) { return container.Contains(key); }

		public ICollection<TKey> Keys { get { return container.Keys.Cast<TKey>().ToList(); } }

		public bool Remove(TKey key)
		{
			var c = container.Contains(key);
			if (c) container.Remove(key);
			return c;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			var c = container.Contains(key);
			if (c) value = (TValue)container[key];
			else value = default(TValue);
			return c;
		}

		public ICollection<TValue> Values
		{
			get 
			{
				var res = new List<TValue>();
				var e = container.GetEnumerator();
				try
				{
					while (e.MoveNext())
						res.Add((TValue)e.Value);
				}
				finally 
				{
 					var d = e as IDisposable;
					if (d != null)
						d.Dispose();
				}
				return res;
			}
		}

		public TValue this[TKey key]
		{
			get 
			{
				TValue res;
				TryGetValue(key, out res);
				return res;
			}
			set { container[key] = value; }
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) { Add(item.Key, item.Value); }

		public void Clear() { container.Clear(); }

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) 
		{
			TValue val;
			if (!TryGetValue(item.Key, out val))
				return false;
			return Equals(val, item.Value);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<TKey, TValue> item in this)
			{
				array[arrayIndex++] = item;
			}
		}

		public int Count { get { return container.Count; } }

		public bool IsReadOnly { get { return false; } }

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			var c = container.Contains(item.Key);
			if (c) container.Remove(item.Key);
			return c;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			var res = new List<TValue>();
			var e = container.GetEnumerator();
			try
			{
				while (e.MoveNext())
					yield return new KeyValuePair<TKey, TValue>((TKey)e.Key, (TValue)e.Value);
			}
			finally
			{
				var d = e as IDisposable;
				if (d != null)
					d.Dispose();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
