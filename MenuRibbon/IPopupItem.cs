using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
			if (!item.PopupRoot.PopupManager.IsResponsive)
				return;

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
					}
					else
					{
						item.ParentItem.NavigateItem();
					}
					break;
				case Key.Right:
					if (isRoot)
					{
						item.NavigateSibling(true, true);
					}
					else
					{
						if (item.HasItems())
						{
							item.PopupRoot.PopupManager.OpenedItem = item;
							item.EnabledChildren().FirstOrDefault().NavigateItem();
						}
						else
						{
							pm.OpenedItem = top.NavigateSibling(true, true);
						}
					}
					e.Handled = true;
					break;
				case Key.Left:
					if (isRoot)
					{
						item.NavigateSibling(false, true);
					}
					else
					{
						if (item.ParentItem != top)
						{
							item.ParentItem.NavigateItem();
						}
						else
						{
							pm.OpenedItem = top.NavigateSibling(false, true);
						}
					}
					e.Handled = true;
					break;
				case Key.Up:
					if (isRoot)
					{
						item.PopupRoot.PopupManager.OpenedItem = item;
						item.EnabledChildren().FirstOrDefault().NavigateItem();
					}
					else
					{
						item.NavigateSibling(false, true);
					}
					e.Handled = true;
					break;
				case Key.Down:
					if (isRoot)
					{
						item.PopupRoot.PopupManager.OpenedItem = item;
						item.EnabledChildren().FirstOrDefault().NavigateItem();
					}
					else
					{
						item.NavigateSibling(true, true);
					}
					e.Handled = true;
					break;
				case Key.Enter:
					item.Action();
					break;
			}
		}

		public static IEnumerable<IPopupItem> NextEnabledSiblings(this IPopupItem start, bool forward, bool cycle)
		{
			var parent = ItemsControl.ItemsControlFromItemContainer((DependencyObject)start);
			if (parent.Items.Count == 1)
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
				.Where(x =>
				{
					if (!(x is IPopupItem)) return false;
					var ui = x as UIElement;
					return ui != null && ui.IsEnabled && ui.IsVisible;
				})
				.Select(x => (IPopupItem)x)
				;
		}
		public static IEnumerable<IPopupItem> EnabledChildren(this IPopupItem parent)
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
				.Where(x =>
				{
					if (!(x is IPopupItem)) return false;
					var ui = x as UIElement;
					return ui != null && ui.IsEnabled && ui.IsVisible; // ui.Visible might be problematic, but better make it work
				})
				;
		}

		public static IPopupItem NavigateSibling(this IPopupItem item, bool forward, bool cycle)
		{
			var it = item.NextEnabledSiblings(forward, cycle).FirstOrDefault();
			if (it != null)
				it.NavigateItem();
			return it;
		}
		public static IPopupItem NavigateItem(this IPopupItem item)
		{
			if (item == null)
				return null;
			var pm = item.PopupRoot.PopupManager;
			if (!pm.IsResponsive)
				return null;

			pm.OpenedItem = item.ParentItem;
			pm.HighlightedItem = item;

			var c = item.FirstFocusableElement();
			if (c != null)
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
