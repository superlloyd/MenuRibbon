using MenuRibbon.WPF.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MenuRibbon.WPF.Utils
{
	/// <summary>
	/// Help keeping a representation of all KeyTip item in a tree like structure which update in an efficient fashion
	/// </summary>
	class ScopeTree
	{
		WeakDictionary<DependencyObject, WeakSet<DependencyObject>> map = new WeakDictionary<DependencyObject, WeakSet<DependencyObject>>();
		WeakSet<DependencyObject> pendingItems = new WeakSet<DependencyObject>();
		WeakSet<DependencyObject> allItems = new WeakSet<DependencyObject>();

		/// <summary>
		/// Check whether this scope item is known
		/// </summary>
		public bool HasScope(DependencyObject scope)
		{
			UpdateScopeMap();
			return map.ContainsKey(scope);
		}

		/// <summary>
		/// Get the items for that scope
		/// </summary>
		public WeakSet<DependencyObject> this[DependencyObject scope]
		{
			get 
			{
				UpdateScopeMap();
				return map[scope]; 
			}
		}

		/// <summary>
		/// Call that when item is visible
		/// </summary>
		public void AddItem(DependencyObject item)
		{
			allItems.Add(item);
			pendingItems.Add(item);
		}

		/// <summary>
		/// Call that when the item is no longer visible
		/// </summary>
		public void RemoveItem(DependencyObject item)
		{
			var s = FindScope(item);
			var sl = map[s];
			if (sl != null)
				sl.Remove(item);
			if (KeyTipService.GetIsKeyTipScope(item))
			{
				sl = map[item];
				if (sl != null)
					pendingItems.AddRange(sl);
			}
			allItems.Remove(item);
			pendingItems.Remove(item);
		}

		/// <summary>
		/// Call when this object become or stop being a scope
		/// </summary>
		public void UpdateScope(DependencyObject item)
		{
			if (KeyTipService.GetIsKeyTipScope(item))
			{
				// item from parent might now be on this one
				var p = FindScope(item);
				var sl = map[p];
				if (sl != null)
				{
					pendingItems.AddRange(sl);
					sl.Clear();
				}
			}
			else
			{
				var sl = map[item];
				if (sl != null)
				{
					pendingItems.AddRange(sl);
					map.Remove(item);
				}
			}
		}

		public DependencyObject FindScope(DependencyObject obj, bool searchVisualTree = true)
		{
			var getNext = searchVisualTree ? (Func<DependencyObject, DependencyObject>)(x => x.VisualParent()) : (x => x.LogicalParent());
			var o = obj;
			while (true)
			{
				if (o != obj && KeyTipService.GetIsKeyTipScope(o))
					return o;
				var n = getNext(o);
				if (n == null)
					return o;
				o = n;
			}
		}

		void UpdateScopeMap()
		{
			foreach (var t in pendingItems.ToList())
			{
				if (!t.IsLoaded())
					continue;
				var s = FindScope(t);
				var ws = map[s];
				if (ws == null) map[s] = ws = new WeakSet<DependencyObject>();
				ws.Add(t);
				pendingItems.Remove(t);
			}
		}

		public void Clear()
		{
			pendingItems.Clear();
			map.Clear();
			pendingItems.AddRange(allItems);
		}
	}
}
