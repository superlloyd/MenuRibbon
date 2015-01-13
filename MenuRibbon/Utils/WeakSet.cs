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
		HashSet<WeakRefHashed> container = new HashSet<WeakRefHashed>();

		public WeakSet()
		{
		}

		public void AddRange(params T[] items) { AddRange((IEnumerable<T>)items); }
		public void AddRange(IEnumerable<T> items) { items.ForEach(x => Add(x)); }

		public void Add(T item) 
		{
			if (item == null)
				return;
			WeakCleanup();
			container.Add(new WeakRefHashed(item)); 
		}
		public void Clear() { container.Clear(); }
		public bool Contains(T item) { return container.Contains(new WeakRefHashed(item)); }
		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (var item in this)
				array[arrayIndex++] = item;
		}
		public int Count { get { return container.Count; } }

		public bool IsReadOnly { get { return false; } }

		public bool Remove(T item)
		{
			if (item == null) return false;
			WeakCleanup();
			return container.Remove(new WeakRefHashed(item));
		}

		public IEnumerator<T> GetEnumerator() { return container.Where(x => x.IsAlive).Select(x => (T)x.Target).GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>
		/// Remove dead items from the collection now. This method is also called automatically on Add(), Remove() and Count.
		/// </summary>
		public void WeakCleanup() { container.RemoveWhere(x => !x.IsAlive); }
	}
}
