using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF.Utils
{
	/// <summary>
	/// Help recycle and reuse object that are expensive to create.
	/// </summary>
	class ObjectRecycler<T>
		where T: class
	{
		Func<T> creator;
		Stack<T> items;

		public ObjectRecycler(Func<T> creator)
		{
			if (creator == null)
				throw new ArgumentNullException("creator");
			this.creator = creator;
		}

		/// <summary>
		/// Get an expensive items. Either an already existing one (if available) or a new one.
		/// </summary>
		public T Get()
		{
			if (items == null || items.Count == 0)
			{
				return creator();
			}
			else
			{
				return items.Pop();
			}
		}

		/// <summary>
		/// Recycle an expensive item, making it available for reuse.
		/// </summary>
		public void Recycle(T item)
		{
			if (item != null)
			{
				if (items == null)
					items = new Stack<T>();
				items.Push(item);
			}
		}

		/// <summary>
		/// Clear all cached items.
		/// </summary>
		public void Clear()
		{
			items = null;
		}
	}
}
