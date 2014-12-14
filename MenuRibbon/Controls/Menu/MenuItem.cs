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

		void IPopupItem.Action() { OnClick(); }
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
			public MenuItemContainer()
			{
				Focusable = false;
			}

			public bool IsOpen
			{
				get { return false; }
				set { }
			}
			public bool IsHighlighted { get; set; }
			void IPopupItem.Action() {}
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

			public void SetContent(object content, DataTemplate tpl, DataTemplateSelector sel)
			{
				if (sel != null)
				{
					tpl = sel.SelectTemplate(content, this);
				}
				if (tpl != null)
				{
					var dp = tpl.LoadContent();
					var fe = dp as FrameworkElement;
					if (fe != null)
						fe.DataContext = content;
					content = dp;
				}
				Content = content as UIElement;
			}

			protected override void OnKeyDown(KeyEventArgs e)
			{
				this.OnKeyNavigate(e);
				base.OnKeyDown(e);
			}

			//public UIElement Content
			//{
			//	get { return mContent; }
			//	set
			//	{
			//		if (value == mContent)
			//			return;
			//		if (mContent != null)
			//			base.RemoveVisualChild(mContent);
			//		mContent = value;
			//		if (mContent != null)
			//			base.AddVisualChild(mContent);
			//		InvalidateVisual();
			//	}
			//}
			//UIElement mContent;
			//
			//protected override int VisualChildrenCount { get { return Content == null ? 0 : 1; } }
			//protected override Visual GetVisualChild(int index) { return Content; }
			//protected override void ArrangeCore(Rect finalRect)
			//{
			//	var ui = Content;
			//	if (ui != null)
			//	{
			//		ui.Arrange(finalRect);
			//	}
			//}
			//protected override Size MeasureCore(Size availableSize)
			//{
			//	var ui = Content;
			//	if (ui != null)
			//	{
			//		ui.Measure(availableSize);
			//		return ui.DesiredSize;
			//	}
			//	else
			//	{
			//		return Size.Empty;
			//	}
			//}
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
			return item is MenuItem || item is Separator || item is MenuItemContainer;
		}
		protected override DependencyObject GetContainerForItemOverride()
		{
			var c = new MenuItemContainer();
			//KeyboardNavigation.SetDirectionalNavigation(c, KeyboardNavigationMode.Once);
			//KeyboardNavigation.SetControlTabNavigation(c, KeyboardNavigationMode.Once);
			//KeyboardNavigation.SetTabNavigation(c, KeyboardNavigationMode.Once);
			return c;
		}
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);
			if (element is MenuItemContainer)
			{
				var mic = (MenuItemContainer)element;
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
			else if (element is MenuItemContainer)
			{
				var mic = (MenuItemContainer)element;
				mic.SetContent(item, this.ItemTemplate, this.ItemTemplateSelector);
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
			this.OnKeyNavigate(e);
			base.OnKeyDown(e);
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
