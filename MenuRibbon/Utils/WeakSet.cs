using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF.Utils
{
	public class WeakSet<T> : ICollection<T>, IWeakCollection
		where T: class
	{
		WeakHashtable container = new WeakHashtable();

		public WeakSet()
		{
		}

		public void Add(T item) { container.Add(item, null); }
		public void Clear() { container.Clear(); }
		public bool Contains(T item) { return container.Contains(item); }
		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (var item in this)
				array[arrayIndex++] = item;
		}
		public int Count { get { return container.Count; } }

		public bool IsReadOnly { get { return false; } }

		public bool Remove(T item)
		{
			var r = container.Contains(item);
			if (r) container.Remove(item);
			return r;
		}

		public IEnumerator<T> GetEnumerator() { return container.Keys.Cast<T>().GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>
		/// Remove dead items from the collection now. This method is also called automatically on Add(), Remove() and Count.
		/// </summary>
		public void WeakCleanup() { container.WeakCleanup(); }
	}
}
