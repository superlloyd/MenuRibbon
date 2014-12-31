using System;
using System.Collections;
using System.Linq;

namespace MenuRibbon.WPF.Utils
{
	public class WeakHashtable : IDictionary, IWeakCollection
	{
		System.Collections.Hashtable container = new System.Collections.Hashtable();
		class WeakRefHashed : WeakReference
		{
			public WeakRefHashed(object obj)
				: base(obj)
			{
				HashCode = obj.GetHashCode();
			}
			public int HashCode { get; private set; }
			public override int GetHashCode() { return HashCode; }
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
					return true;
				if (!IsAlive)
					return false;
				var other = obj as WeakRefHashed;
				if (other == null)
				{
					return ReferenceEquals(Target, obj);
				}
				else
				{
					if (HashCode != other.HashCode)
						return false;
					if (!other.IsAlive)
						return false;
					return ReferenceEquals(Target, other.Target);
				}
			}
		}

		/// <summary>
		/// Remove dead items from the collection now. This method is also called automatically on Add(), Remove() and Count.
		/// </summary>
		public void WeakCleanup()
		{
			var keys = container.Keys.Cast<WeakRefHashed>().ToArray();
			foreach (var k in keys)
			{
				if (!k.IsAlive)
					container.Remove(k);
			}
		}

		public void Add(object key, object value) 
		{
			if (key == null)
				throw new ArgumentNullException("key");
			WeakCleanup();
			container[new WeakRefHashed(key)] = value; 
		}

		public object this[object key]
		{
			get { return container[key]; }
			set 
			{
				if (key == null)
					throw new ArgumentNullException("key");
				WeakCleanup();
				container[new WeakRefHashed(key)] = value; 
			}
		}

		public void Remove(object key)
		{
			WeakCleanup();
			container.Remove(key);
		}

		public int Count { get { return container.Count; } }

		public void Clear() { container.Clear(); }

		public bool Contains(object key) { return container.Contains(key); }

		public bool IsFixedSize { get { return false; } }
		public bool IsReadOnly { get { return false; } }

		public IDictionaryEnumerator GetEnumerator() { return new WHE(container.GetEnumerator()); }
		class WHE : IDictionaryEnumerator, IDisposable
		{
			IDictionaryEnumerator src;

			public WHE(IDictionaryEnumerator src) { this.src = src; }
			public DictionaryEntry Entry { get { return new DictionaryEntry(Key, Value); } }
			public object Key 
			{
				get 
				{
					var k = src.Key as WeakRefHashed;
					return k.Target;
				}
			}
			public object Value { get { return src.Value; } }
			public object Current { get { return Entry; } }
			public bool MoveNext()
			{
				while (src.MoveNext())
				{
					var k = src.Key as WeakRefHashed;
					if (k.IsAlive)
						return true;
				}
				return false;
			}
			public void Reset() { src.Reset(); }

			public void Dispose()
			{
				var d = src as IDisposable;
				if (d != null)
					d.Dispose();
			}
		}

		public ICollection Keys { get { return container.Keys.Cast<WeakRefHashed>().Where(x => x.IsAlive).Select(x => x.Target).ToList(); } }
		public ICollection Values 
		{
			get 
			{
				var res = new ArrayList();
				using (var whe = new WHE(container.GetEnumerator()))
					while (whe.MoveNext())
						res.Add(whe.Value);
				return res;
			} 
		}

		public void CopyTo(Array array, int index)
		{
			foreach (var item in this)
				array.SetValue(item, index++);
		}

		public bool IsSynchronized { get { return false; } }
		public object SyncRoot { get { return container.SyncRoot; } }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
