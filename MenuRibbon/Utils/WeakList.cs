using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF.Utils
{
	public class WeakList<T> : IList<T>, IWeakCollection
		where T: class
	{
		List<WeakReference<T>> container = new List<WeakReference<T>>();

		public int IndexOf(T item)
		{
			for (int i = 0; i < container.Count; i++)
			{
				var wr = container[i];
				T target;
				if (wr.TryGetTarget(out target) && Equals(target, item))
					return i;
			}
			return -1;
		}

		public void Insert(int index, T item)
		{
			if (item == null)
				throw new ArgumentNullException("item");
			var wr = new WeakReference<T>(item);
			container.Insert(index, wr);
			WeakCleanup();
		}

		public void RemoveAt(int index)
		{
			container.RemoveAt(index);
			WeakCleanup();
		}

		public T this[int index]
		{
			get
			{
				T target;
				container[index].TryGetTarget(out target);
				return target;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");
				var wr = new WeakReference<T>(value);
				container[index] = wr;
				WeakCleanup();
			}
		}

		public void Add(T item)
		{
			if (item == null)
				throw new ArgumentNullException("item");
			container.Add(new WeakReference<T>(item));
			WeakCleanup();
		}

		public void Clear() { container.Clear(); }

		public bool Contains(T item) { return IndexOf(item) > -1; }

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (var item in this)
				array[arrayIndex++] = item;
		}

		public int Count { get { return container.Count; } }

		public bool IsReadOnly { get { return false; } }

		public bool Remove(T item)
		{
			var i = IndexOf(item);
			if (i > 0)
			{
				container.RemoveAt(i);
				return true;
			}
			return false;
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (var wr in container)
			{
				T item;
				if (wr.TryGetTarget(out item))
					yield return item;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public void WeakCleanup()
		{
			for (int i = container.Count - 1; i >= 0; i--)
			{
				T item;
				if (!container[i].TryGetTarget(out item))
					container.RemoveAt(i);
			}
		}
	}
}
