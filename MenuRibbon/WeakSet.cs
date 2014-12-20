using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF
{
	public class WeakSet<T> : ICollection<T>
		where T: class
	{
		System.Collections.Hashtable container = new System.Collections.Hashtable();
		class WeakRefHashed : WeakReference
		{
			public WeakRefHashed(T obj) : base(obj)
			{
				HashCode = obj.GetHashCode();
			}
			public int HashCode { get; private set; }
			public override int GetHashCode() { return HashCode; }
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
					return true;
				var other = obj as WeakRefHashed;
				if (other == null)
					return false;
				if (HashCode != other.HashCode)
					return false;
				if (!IsAlive || !other.IsAlive)
					return false;
				return ReferenceEquals(Target, other.Target);
			}
		}

		public WeakSet()
		{
		}

		public void Add(T item)
		{
			WeakCleanup();

			if (item == null)
				return;
			container[new WeakRefHashed(item)] = null;
		}

		public void Clear() { container.Clear(); }

		public bool Contains(T item)
		{
			if (item == null)
				return false;
			return container.ContainsKey(new WeakRefHashed(item));
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (var item in this)
				array[arrayIndex++] = item;
		}

		public int Count
		{
			get 
			{
				WeakCleanup();
				return container.Count;
			}
		}

		public bool IsReadOnly { get { return false; } }

		public bool Remove(T item)
		{
			if (item == null) return false;
			var k = new WeakRefHashed(item);
			var r = container.ContainsKey(k);
			container.Remove(k);
			WeakCleanup();
			return r;
		}

		public IEnumerator<T> GetEnumerator()
		{
			// don't clean on enumerate, operation should be cheap!
			return container.Keys.Cast<WeakRefHashed>().Where(x => x.IsAlive).Select(x => (T)x.Target).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>
		/// Remove dead items from the collection now. This method is also called automatically on Add(), Remove() and Count.
		/// </summary>
		public void WeakCleanup()
		{
			var keys = container.Keys.Cast<WeakRefHashed>().ToList();
			foreach (var k in keys)
			{
				if (!k.IsAlive)
					container.Remove(k);
			}
		}
	}
}
