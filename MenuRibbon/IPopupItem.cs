using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MenuRibbon.WPF
{
	public interface IPopupItem
	{
		bool IsOpen { get; set; }
		bool IsHighlighted { get; set; }
		IPopupItem ParentItem { get; }
		IPopupRoot PopupRoot { get; }
		bool Contains(DependencyObject target);
		void Action();
	}

	public enum PopupNavigation
	{
		Previous,
		Next,
	}

	public static class IPopupItemEx
	{
		public static void OnKeyNavigate(this IPopupItem item, KeyEventArgs e)
		{
			if (e.Handled)
				return;
			if (item.PopupRoot == null)
				return;
			if (e.OriginalSource is DependencyObject)
			{
				var dp = (DependencyObject)e.OriginalSource;
				if (!dp.VisualHierarchy()
					.TakeWhile(x => !(x is Window || x is Popup))
					.Contains((DependencyObject)item))
					return;
			}

			var key = e.Key;

			var fe = item as FrameworkElement;
			if (fe != null)
			{
				if (fe.FlowDirection == FlowDirection.RightToLeft)
				{
					switch (key)
					{
						case Key.Right:
							key = Key.Left;
							break;
						case Key.Left:
							key = Key.Right;
							break;
					}
				}

			}

			var pm = item.PopupRoot.PopupManager;
			bool isRoot = item.ParentItem == null;
			var top = item;
			while (top.ParentItem != null)
				top = top.ParentItem;

			if (key == Key.Tab)
			{
				bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
				if (isRoot)
				{
					key = shift ? Key.Left : Key.Right;
				}
				else
				{
					key = shift ? Key.Up : Key.Down;
				}
			}

			switch (key)
			{
				case Key.Escape:
					if (isRoot)
					{
						item.Quit();
						e.Handled = true;
					}
					else
					{
						e.Handled = null != item.ParentItem.NavigateItem();
					}
					break;
				case Key.Right:
					if (isRoot)
					{
						e.Handled = null != item.NavigateSibling(true, true);
					}
					else
					{
						if (item.HasItems())
						{
							item.PopupRoot.PopupManager.OpenedItem = item;
							item.PopupChildren().SelectableItem().FirstOrDefault().NavigateItem();
							e.Handled = true;
						}
						else
						{
							pm.OpenedItem = top.NavigateSibling(true, true);
							e.Handled = null != pm.OpenedItem;
						}
					}
					break;
				case Key.Left:
					if (isRoot)
					{
						e.Handled = null != item.NavigateSibling(false, true);
					}
					else
					{
						if (item.ParentItem != top)
						{
							e.Handled = null != item.ParentItem.NavigateItem();
						}
						else
						{
							pm.OpenedItem = top.NavigateSibling(false, true);
							e.Handled = null != pm.OpenedItem;
						}
					}
					break;
				case Key.Up:
					if (isRoot)
					{
						if (item.HasItems())
						{
							item.PopupRoot.PopupManager.OpenedItem = item;
							item.PopupChildren().SelectableItem().FirstOrDefault().NavigateItem();
							e.Handled = true;
						}
					}
					else
					{
						e.Handled = null != item.NavigateSibling(false, true);
					}
					break;
				case Key.Down:
					if (isRoot)
					{
						if (item.HasItems())
						{
							item.PopupRoot.PopupManager.OpenedItem = item;
							item.PopupChildren().SelectableItem().FirstOrDefault().NavigateItem();
							e.Handled = true;
						}
					}
					else
					{
						e.Handled = null != item.NavigateSibling(true, true);
					}
					break;
				case Key.Enter:
				case Key.Space:
					item.Action();
					e.Handled = true;
					break;
			}
		}

		public static IEnumerable<IPopupItem> SelectableItem(this IEnumerable<IPopupItem> list)
		{
			return list.Where(x =>
			{
				var ui = x as UIElement;
				if (ui == null)
					return false;
				if (!ui.IsEnabled || !ui.IsVisible)
					return false;
				if (ui.FirstFocusableElement() == null)
					return false;
				return true;
			});
		}
		public static IEnumerable<IPopupItem> PopupSiblings(this IPopupItem start, bool forward, bool cycle)
		{
			if (start.PopupRoot == start)
				return new IPopupItem[0];
			var parent = ItemsControl.ItemsControlFromItemContainer((DependencyObject)start);
			if (parent == null || parent.Items.Count < 2)
				return new IPopupItem[0];

			var index = parent.ItemContainerGenerator.IndexFromContainer((DependencyObject)start);
			return Enumerable.Range(1, parent.Items.Count)
				.Select(x => forward ? index + x : index - x)
				.Select(x =>
				{
					if (x < 0)
						return cycle ? x + parent.Items.Count : 0;
					if (x > parent.Items.Count - 1)
						return cycle ? x - parent.Items.Count : parent.Items.Count - 1;
					return x;
				})
				.Distinct()
				.Select(x => parent.Items[x])
				.Select(x => parent.IsItemItsOwnContainer(x) ? x : parent.ItemContainerGenerator.ContainerFromItem(x))
				.Where(x => x is IPopupItem)
				.Select(x => (IPopupItem)x)
				;
		}
		public static IEnumerable<IPopupItem> PopupChildren(this IPopupItem parent)
		{
			var ic = parent as ItemsControl;
			if (ic == null)
				return new IPopupItem[0];

			if (ic.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
			{
				ic.ItemContainerGenerator.GenerateBatches().Dispose();
			}
			return ic.Items.Cast<object>()
				.Select(x => (IPopupItem)(ic.IsItemItsOwnContainer(x) ? x : ic.ItemContainerGenerator.ContainerFromItem(x)))
				.Where(x => x is IPopupItem)
				.Select(x => (IPopupItem)x)
				;
		}

		public static IPopupItem NavigateSibling(this IPopupItem item, bool forward, bool cycle)
		{
			return item.PopupSiblings(forward, cycle).SelectableItem().FirstOrDefault().NavigateItem();
		}
		public static IPopupItem NavigateItem(this IPopupItem item)
		{
			if (item == null)
				return null;
			var c = item.FirstFocusableElement();
			if (c == null)
				return null;

			var pm = item.PopupRoot.PopupManager;
			pm.OpenedItem = (item.ParentItem != null) ? item.ParentItem : item;
			pm.HighlightedItem = item;
			c.Focus();
			return item;
		}
		public static bool HasItems(this IPopupItem item)
		{
			var ic = item as ItemsControl;
			return ic != null && ic.Items.Count > 0;
		}

		public static void Quit(this IPopupItem item)
		{
			KeyTipService.Current.RestoreFocusScope();
		}

	}
}
