using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using MenuRibbon.WPF;

namespace MenuRibbon.WPF.Controls.Menu
{
	public enum MenuNavigation
	{
		Up, 
		Down,
		Left,
		Rigth,
	}

	[TemplatePart(Name = "PART_Header", Type = typeof(FrameworkElement))]
	public class MenuItem : ActionHeaderedItemsControl, IPopupItem
	{
		static MenuItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(typeof(MenuItem)));
		}
		public MenuItem()
		{
			DataContextChanged += MenuTabItem_DataContextChanged;
		}
		void MenuTabItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateRole();
		}

		#region Role, Root, IsTopLevel

		public IPopupRoot Root
		{
			get { return (IPopupRoot)GetValue(RootProperty); }
			private set { SetValue(RootPropertyKey, value); }
		}

		static readonly DependencyPropertyKey RootPropertyKey = DependencyProperty.RegisterReadOnly(
			"Root", typeof(IPopupRoot), typeof(MenuItem), new PropertyMetadata(default(IPopupRoot)));

		public static readonly DependencyProperty RootProperty = RootPropertyKey.DependencyProperty;

		public IPopupItem Top
		{
			get { return (IPopupItem)GetValue(TopProperty); }
			private set { SetValue(TopPropertyKey, value); }
		}

		static readonly DependencyPropertyKey TopPropertyKey = DependencyProperty.RegisterReadOnly(
			"Top", typeof(IPopupItem), typeof(MenuItem), new PropertyMetadata(default(IPopupItem)));

		public static readonly DependencyProperty TopProperty = TopPropertyKey.DependencyProperty;

		public bool IsTopLevel
		{
			get { return (bool)GetValue(IsTopLevelProperty); }
			private set { SetValue(IsTopLevelPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey IsTopLevelPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsTopLevel", typeof(bool), typeof(MenuItem), new PropertyMetadata(BooleanBoxes.FalseBox));

		public static readonly DependencyProperty IsTopLevelProperty = IsTopLevelPropertyKey.DependencyProperty;

		[Category("Behavior")]
		public MenuItemRole Role
		{
			get { return (MenuItemRole)GetValue(MenuTabItemRoleProperty); }
			private set { SetValue(MenuTabItemRolePropertyKey, value); }
		}

		static readonly DependencyPropertyKey MenuTabItemRolePropertyKey = DependencyProperty.RegisterReadOnly(
			"Role", typeof(MenuItemRole), typeof(MenuItem), new PropertyMetadata(EnumBox<MenuItemRole>.Box((int)MenuItemRole.SubmenuItem)));

		public static readonly DependencyProperty MenuTabItemRoleProperty = MenuTabItemRolePropertyKey.DependencyProperty;

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			UpdateTopNRoot();
			UpdateRole();
		}
		protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			UpdateRole();
		}
		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			UpdateTopNRoot();
			UpdateRole();

			foreach (var item in Items)
				if (item is MenuItem)
					((MenuItem)item).UpdateTopNRoot();
		}

		protected virtual void UpdateTopNRoot()
		{
			DependencyObject p = this;
			IPopupItem top = this;
			while (p != null && !(p is IPopupRoot))
			{
				p = p.VisualParent();
				if (p is IPopupItem)
					top = (IPopupItem)p;
			}
			Root = p as IPopupRoot;
			Top = top;
			IsTopLevel = top == this;
		}

		protected virtual void UpdateRole()
		{
			if (IsTopLevel)
			{
				if (HasItems)
				{
					Role = MenuItemRole.TopLevelHeader;
				}
				else
				{
					Role = MenuItemRole.TopLevelItem;
				}
			}
			else
			{
				if (HasItems)
				{
					Role = MenuItemRole.SubmenuHeader;
				}
				else
				{
					Role = MenuItemRole.SubmenuItem;
				}
			}
		}

		#endregion		

		#region IPopupItem: IsOpen, ParentItem, PopupRoot

		IPopupRoot IPopupItem.PopupRoot { get { return Root; } }
		IPopupItem IPopupItem.ParentItem { get { return ParentItem; } }
		bool IPopupItem.Contains(DependencyObject target)
		{
			return target.VisualHierarchy().Contains(this);
		}
		public virtual MenuItem ParentItem { get { return this is IPopupRoot ? null : this.VisualHierarchy().Skip(1).FirstOrDefault(x => x is MenuItem) as MenuItem; } }

		public bool IsOpen
		{
			get { return (bool)GetValue(IsOpenProperty); }
		}

		bool IPopupItem.IsOpen
		{
			get { return IsOpen; }
			set 
			{
				switch (Role)
				{
					case MenuItemRole.TopLevelHeader:
					case MenuItemRole.SubmenuHeader:
						break;
					case MenuItemRole.TopLevelItem:
					case MenuItemRole.SubmenuItem:
						value = false;
						break;
				}
				SetValue(IsOpenPropertyKey, BooleanBoxes.Box(value)); 
			}
		}

		static readonly DependencyPropertyKey IsOpenPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsOpen", typeof(bool), typeof(MenuItem), new PropertyMetadata(default(bool)));

		public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

		#endregion		

		#region IsPressed, IsHovering, IsHighlighted

		public bool IsPressed
		{
			get { return (bool)GetValue(IsPressedProperty); }
			protected set { SetValue(IsPressedPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey IsPressedPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsPressed", typeof(bool), typeof(MenuItem), new PropertyMetadata(BooleanBoxes.FalseBox));

		public static readonly DependencyProperty IsPressedProperty = IsPressedPropertyKey.DependencyProperty;

		public bool IsHovering
		{
			get { return (bool)GetValue(IsHoveringProperty); }
			private set 
			{
				SetValue(IsHoveringPropertyKey, BooleanBoxes.Box(value));

				if (Root != null)
				{
					if (value)
					{
						Root.PopupManager.Enter(this);
					}
					else
					{
						Root.PopupManager.Exit(this);
					}
				}
			}
		}

		static readonly DependencyPropertyKey IsHoveringPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsHovering", typeof(bool), typeof(MenuItem), new PropertyMetadata(BooleanBoxes.FalseBox));

		public static readonly DependencyProperty IsHoveringProperty = IsHoveringPropertyKey.DependencyProperty;

		public bool IsHighlighted
		{
			get { return (bool)GetValue(IsHighlightedProperty); }
		}
		bool IPopupItem.IsHighlighted
		{
			get { return (bool)GetValue(IsHighlightedProperty); }
			set { SetValue(IsHighlightedPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey IsHighlightedPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsHighlighted", typeof(bool), typeof(MenuItem), new PropertyMetadata(BooleanBoxes.FalseBox, (o,e) => ((MenuItem)o).OnIsHighlightedChanged((bool)e.OldValue, (bool)e.NewValue)));

		public static readonly DependencyProperty IsHighlightedProperty = IsHighlightedPropertyKey.DependencyProperty;

		protected virtual void OnIsHighlightedChanged(bool OldValue, bool NewValue)
		{
			if (NewValue)
				Focus();
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			if (!IsHighlighted)
			{
				var p = (IPopupItem)this;
				var pr = p.PopupRoot;
				if (pr != null)
					pr.PopupManager.HighlightedItem = this;
			}
		}

		#endregion		

		#region ItemsControl override

		class MenuItemContainer : ContentControl, IPopupItem
		{
			public bool IsOpen
			{
				get { return false; }
				set { }
			}
			public bool IsHighlighted { get; set; }
			public IPopupItem ParentItem { get { return this.LogicalParent() as IPopupItem; } }
			public IPopupRoot PopupRoot { get { return ParentItem.PopupRoot; } }
			bool IPopupItem.Contains(DependencyObject target)
			{
				return target.VisualHierarchy().Contains(this);
			}

			void RootAction(Action<IPopupRoot> a)
			{
				var r = (IPopupRoot)this.LogicalHierarchy().FirstOrDefault(x => x is IPopupRoot);
				if (r != null)
					a(r);
			}
			protected override void OnMouseEnter(MouseEventArgs e)
			{
				base.OnMouseEnter(e);
				RootAction(r => r.PopupManager.Enter(this));
			}
			protected override void OnMouseLeave(MouseEventArgs e)
			{
				base.OnMouseLeave(e);
				RootAction(r => r.PopupManager.Exit(this));
			}
		}

		public ItemContainerTemplateSelector ItemContainerTemplateSelector
		{
			get { return (ItemContainerTemplateSelector)GetValue(ItemContainerTemplateSelectorProperty); }
			set { SetValue(ItemContainerTemplateSelectorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ItemContainerTemplateSelector.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemContainerTemplateSelectorProperty =
			DependencyProperty.Register("ItemContainerTemplateSelector", typeof(ItemContainerTemplateSelector), typeof(MenuItem), new PropertyMetadata(null));

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			// It can take anything! ^^
			return item is MenuItem || item is Separator || item is ContentPresenter;// MenuItemContainer;
		}
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ContentPresenter();// MenuItemContainer();
		}
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);
			if (element is ContentPresenter)
			{
				var mic = (ContentPresenter)element;
				mic.Content = null;
			}
		}
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is MenuItem)
			{
				PrepareContainerForItemOverride((MenuItem)element, item);
			}
			else if (element is Separator)
			{
				var sep = (Separator)element;
				if (sep.HasDefaultValue(StyleProperty))
				{
					var st = TryFindResource(SeparatorStyleKey) as Style;
					if (st != null)
						sep.Style = st;
				}
			}
			else if (element is ContentPresenter)
			{
				var mic = (ContentPresenter)element;
				mic.ContentTemplate = this.ItemTemplate;
				mic.Content = item;
			}
		}
		internal void PrepareContainerForItemOverride(MenuItem element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);
			if (item is ICommand)
			{
				element.Command = (ICommand)item;
			}
		}

		#endregion

		#region FrameworkElement override + InputHandling

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			events.Clear();

			var main = GetTemplateChild("PART_Header");
			if (main != null)
			{
				events["H"] = main.MouseHovering().Subscribe(x => IsHovering = x);
				events["L"] = main.MouseDown().Where(x => x.ChangedButton == MouseButton.Left).Subscribe(x => OnMainUI_LeftMouseDown(x));
				events["D"] = main.MouseClicks().Subscribe(x => OnClick());
				events["P"] = main.MousePressed().Subscribe(x => IsPressed = x);
			}
		}
		DisposableBag events = new DisposableBag();

		protected override void OnClick(RoutedEventArgs e)
		{
			if (Root != null)
			{
				switch (Role)
				{
					case MenuItemRole.TopLevelItem:
					case MenuItemRole.SubmenuItem:
						Root.PopupManager.IsResponsive = false;
						break;
				}
			}
			base.OnClick(e);
		}

		protected void OnMainUI_LeftMouseDown(MouseButtonEventArgs e)
		{
			switch (Role)
			{
				case MenuItemRole.TopLevelHeader:
					if (Root.PopupManager.IsResponsive)
					{
						Root.PopupManager.IsResponsive = false;
					}
					else
					{
						Root.PopupManager.Enter(this, true);
					}
					break;
				case MenuItemRole.TopLevelItem:
				case MenuItemRole.SubmenuItem:
					break;
				case MenuItemRole.SubmenuHeader:
				default:
					Root.PopupManager.OpenedItem = this;
					break;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var key = e.Key;
			if (this.FlowDirection == FlowDirection.RightToLeft)
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
			bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
			switch (key)
			{
				case Key.Tab:
					if (Role == MenuItemRole.TopLevelItem || Role == MenuItemRole.TopLevelHeader)
					{
						e.Handled = NavigateTo(shift ? MenuNavigation.Left : MenuNavigation.Rigth);
					}
					else
					{
						e.Handled = NavigateTo(shift ? MenuNavigation.Up : MenuNavigation.Down);
					}
					break;
				case Key.Escape:
					if (Role == MenuItemRole.TopLevelItem || Role == MenuItemRole.TopLevelHeader)
					{
						KeyTipService.Current.RestoreFocusScope();
					}
					else
					{
						e.Handled = NavigateTo(MenuNavigation.Left);
					}
					break;
				case Key.Right:
					e.Handled = NavigateTo(MenuNavigation.Rigth);
					break;
				case Key.Left:
					e.Handled = NavigateTo(MenuNavigation.Left);
					break;
				case Key.Up:
					if (Role == MenuItemRole.TopLevelItem || Role == MenuItemRole.TopLevelHeader)
					{
						e.Handled = NavigateChildren();
					}
					else
					{
						e.Handled = NavigateTo(MenuNavigation.Up);
					}
					break;
				case Key.Down:
					if (Role == MenuItemRole.TopLevelItem || Role == MenuItemRole.TopLevelHeader)
					{
						e.Handled = NavigateChildren();
					}
					else
					{
						e.Handled = NavigateTo(MenuNavigation.Down);
					}
					break;
				case Key.Enter:
					switch (Role)
					{
						case MenuItemRole.SubmenuHeader:
						case MenuItemRole.TopLevelHeader:
							if (IsOpen)
								this.Root.PopupManager.OpenedItem = this;
							break;
						case MenuItemRole.SubmenuItem:
						case MenuItemRole.TopLevelItem:
							OnClick();
							break;
					}
					break;
			}
		}
		protected internal virtual bool NavigateTo(MenuNavigation nav)
		{
			switch (nav)
			{
				case MenuNavigation.Up:
				case MenuNavigation.Down:
					{
						var p = ItemsControl.ItemsControlFromItemContainer(this);
						var next = p.NextEnabledItem(this, nav == MenuNavigation.Down, true, CanNavigateTo(p));
						if (next == null)
							return false;
						NavigateToItem(p, next);
						return true;
					}
				case MenuNavigation.Left:
					{
						var p = this;
						while (p.ParentItem != null)
							p = p.ParentItem;
						return p.NavigateTo(MenuNavigation.Up);
					}
				case MenuNavigation.Rigth:
					{
						if (NavigateChildren())
							return true;
						var p = this;
						while (p.ParentItem != null)
							p = p.ParentItem;
						return p.NavigateTo(MenuNavigation.Down);
					}
			}
			return false;
		}
		protected internal virtual bool NavigateChildren()
		{
			if (!HasItems)
				return false;
			var first = this.NextEnabledItem(null, true, true, CanNavigateTo(this));
			if (first == null)
				return false;
			NavigateToItem(this, first);
			return true;
		}
		Predicate<object> CanNavigateTo(ItemsControl parent)
		{
			if (parent is MenuItem)
			{
				return x =>
				{
					var c = parent.ContainerFromItemOrContainer(x);
					if (c is IPopupItem)
						return true;
					if (c is Separator)
						return false;
					var ui = c as UIElement;
					return ui != null && ui.Focusable;
				};
			}
			else
			{
				return x =>
				{
					var c = parent.ContainerFromItemOrContainer(x) as UIElement;
					return c != null && c.Focusable;
				};
			}
		}
		void NavigateToItem(ItemsControl parent, object item)
		{
			var c = parent.ContainerFromItemOrContainer(item);
			var p = c as IPopupItem;
			if (p != null)
			{
				Root.PopupManager.Enter(p, true);
			}
			else
			{
				var ui = c as UIElement;
				ui.Focus();
			}
		}

		#endregion

		#region SeparatorStyleKey

		/// <summary>
		///     Resource Key for the SeparatorStyle
		/// </summary>
		public static ResourceKey SeparatorStyleKey { get { return sepStyleKey; } }
		static ComponentResourceKey sepStyleKey = new ComponentResourceKey(typeof(MenuItem), "MenuBase.Separator");

		#endregion ItemsStyleKey
	}
}
