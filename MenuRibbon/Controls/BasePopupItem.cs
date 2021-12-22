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

namespace MenuRibbon.WPF.Controls
{
	public class BasePopupItem : ActionHeaderedItemsControl, IPopupItem
	{
		static BasePopupItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BasePopupItem), new FrameworkPropertyMetadata(typeof(BasePopupItem)));

			Type ownerType = typeof(BasePopupItem);
			EventManager.RegisterClassHandler(ownerType, KeyTipService.ActivatingKeyTipEvent, new ActivatingKeyTipEventHandler((o, e) => ((BasePopupItem)o).OnActivatingKeyTip(e)));
			EventManager.RegisterClassHandler(ownerType, KeyTipService.KeyTipAccessedEvent, new KeyTipAccessedEventHandler((o, e) => ((BasePopupItem)o).OnKeyTipAccessed(e)));
		}

		public BasePopupItem()
		{
			DataContextChanged += (o, e) => UpdateRole();
		}

		#region OnActivatingKeyTip(), OnKeyTipAccessed()

		protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
		{
			if (e.OriginalSource != this)
				return;
			e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
		}

		protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
		{
			if (e.OriginalSource != this)
				return;

			OnClick();
			e.Handled = true;

			if (IsOpen && KeyTipService.GetIsKeyTipScope(this))
			{
				e.TargetKeyTipScope = this;
			}
		}
		#endregion

		#region Role, Root, IsTopLevel

		public IPopupRoot PopupRoot
		{
			get { return (IPopupRoot)GetValue(PopupRootProperty); }
			private set { SetValue(RootPropertyKey, value); }
		}

		static readonly DependencyPropertyKey RootPropertyKey = DependencyProperty.RegisterReadOnly(
			"PopupRoot", typeof(IPopupRoot), typeof(BasePopupItem), new PropertyMetadata(default(IPopupRoot)));

		public static readonly DependencyProperty PopupRootProperty = RootPropertyKey.DependencyProperty;

		public IPopupItem Top
		{
			get { return (IPopupItem)GetValue(TopProperty); }
			private set { SetValue(TopPropertyKey, value); }
		}

		static readonly DependencyPropertyKey TopPropertyKey = DependencyProperty.RegisterReadOnly(
			"Top", typeof(IPopupItem), typeof(BasePopupItem), new PropertyMetadata(default(IPopupItem)));

		public static readonly DependencyProperty TopProperty = TopPropertyKey.DependencyProperty;

		public bool IsTopLevel
		{
			get { return (bool)GetValue(IsTopLevelProperty); }
			private set { SetValue(IsTopLevelPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey IsTopLevelPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsTopLevel", typeof(bool), typeof(BasePopupItem), new PropertyMetadata(BooleanBoxes.FalseBox));

		public static readonly DependencyProperty IsTopLevelProperty = IsTopLevelPropertyKey.DependencyProperty;

		[Category("Behavior")]
		public MenuItemRole Role
		{
			get { return (MenuItemRole)GetValue(RoleProperty); }
			private set { SetValue(RolePropertyKey, value); }
		}

		static readonly DependencyPropertyKey RolePropertyKey = DependencyProperty.RegisterReadOnly(
			"Role", typeof(MenuItemRole), typeof(BasePopupItem), new PropertyMetadata(EnumBox<MenuItemRole>.Box((int)MenuItemRole.SubmenuItem)));

		public static readonly DependencyProperty RoleProperty = RolePropertyKey.DependencyProperty;

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
				if (item is BasePopupItem)
					((BasePopupItem)item).UpdateTopNRoot();
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
			PopupRoot = p as IPopupRoot;
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

		#region IPopupItem: IsOpen, ParentItem, PopupRoot, IsOpenChanged

		void IPopupItem.Action() { OnClick(); }
		bool IPopupItem.IsPressed { get { return IsPressed; } set { IsPressed = value; } }

		bool IPopupItem.Contains(DependencyObject target)
		{
			return target.VisualHierarchy().Contains(this);
		}
		public IPopupItem ParentItem { get { return this is IPopupRoot ? null : this.VisualHierarchy().Skip(1).FirstOrDefault(x => x is IPopupItem) as IPopupItem; } }

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
                var previous = IsOpen;
                if (previous == value)
                    return;

                RaiseEvent(new RoutedEventArgs(IsOpenChangingEvent, this));
                SetValue(IsOpenPropertyKey, BooleanBoxes.Box(value));
                RaiseEvent(new RoutedEventArgs(IsOpenChangedEvent, this));
			}
		}

		static readonly DependencyPropertyKey IsOpenPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsOpen", typeof(bool), typeof(BasePopupItem), new PropertyMetadata(default(bool)));

		public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

        public static readonly RoutedEvent IsOpenChangedEvent = EventManager.RegisterRoutedEvent(
            "IsOpenChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BasePopupItem));

        public event RoutedEventHandler IsOpenChanged
        {
            add { AddHandler(IsOpenChangedEvent, value); }
            remove { RemoveHandler(IsOpenChangedEvent, value); }
        }

        public static readonly RoutedEvent IsOpenChangingEvent = EventManager.RegisterRoutedEvent(
            "IsOpenChanging", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(BasePopupItem));

        public event RoutedEventHandler IsOpenChanging
        {
            add { AddHandler(IsOpenChangingEvent, value); }
            remove { RemoveHandler(IsOpenChangingEvent, value); }
        }

        #endregion

        #region IsPressed, IsHovering, IsHighlighted

        public bool IsPressed
		{
			get { return (bool)GetValue(IsPressedProperty); }
			protected set { SetValue(IsPressedPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey IsPressedPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsPressed", typeof(bool), typeof(BasePopupItem), 
			new PropertyMetadata(BooleanBoxes.FalseBox, (o,e) => ((BasePopupItem)o).OnIsPressedChanged((bool)e.OldValue, (bool)e.NewValue)));

		protected virtual void OnIsPressedChanged(bool OldValue, bool NewValue)
		{
		}

		public static readonly DependencyProperty IsPressedProperty = IsPressedPropertyKey.DependencyProperty;

		public bool IsHovering
		{
			get { return (bool)GetValue(IsHoveringProperty); }
			protected set { SetValue(IsHoveringPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey IsHoveringPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsHovering", typeof(bool), typeof(BasePopupItem), 
			new PropertyMetadata(BooleanBoxes.FalseBox, (o,e) => ((BasePopupItem)o).OnIsHoveringChanged((bool)e.OldValue, (bool)e.NewValue)));

		public static readonly DependencyProperty IsHoveringProperty = IsHoveringPropertyKey.DependencyProperty;

		protected virtual void OnIsHoveringChanged(bool OldValue, bool NewValue)
		{
			if (PopupRoot != null && PopupRoot.PopupManager.Tracking)
			{
				if (NewValue)
				{
					Focus();
					PopupRoot.PopupManager.OpenLater(this);
				}
				else
				{
					PopupRoot.PopupManager.Exit(this);
				}
			}
			else
			{
				IsHighlighted = NewValue;
			}
		}

		public bool IsHighlighted
		{
			get { return (bool)GetValue(IsHighlightedProperty); }
			protected set { SetValue(IsHighlightedPropertyKey, BooleanBoxes.Box(value)); }
		}
		bool IPopupItem.IsHighlighted
		{
			get { return IsHighlighted; }
			set { IsHighlighted = value; }
		}

		static readonly DependencyPropertyKey IsHighlightedPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsHighlighted", typeof(bool), typeof(BasePopupItem), new PropertyMetadata(BooleanBoxes.FalseBox, (o,e) => ((BasePopupItem)o).OnIsHighlightedChanged((bool)e.OldValue, (bool)e.NewValue)));

		public static readonly DependencyProperty IsHighlightedProperty = IsHighlightedPropertyKey.DependencyProperty;

		protected virtual void OnIsHighlightedChanged(bool OldValue, bool NewValue)
		{
		}

		#endregion	

		#region input override

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			var pr = PopupRoot;
			if (pr != null)
			{
				pr.PopupManager.HighlightedItem = this;
			}
			else
			{
				IsHighlighted = true;
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			var pr = PopupRoot;
			if (pr != null)
			{
				pr.PopupManager.Exit(this);
			}
			else
			{
				IsHighlighted = false;
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			this.OnKeyDownNavigate(e);
			base.OnKeyDown(e);
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			this.OnKeyUpNavigate(e);
			base.OnKeyUp(e);
		}

		protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnPreviewGotKeyboardFocus(e);
			var r = PopupRoot;
			if (r != null)
			{
				r.PopupManager.Tracking = true;
			}
		}
	
		#endregion

		#region ItemsControl override

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is BasePopupItem || item is Separator;
		}
		protected override DependencyObject GetContainerForItemOverride()
		{
			var c = new Menu.MenuItem();
			return c;
		}
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);
		}
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is Menu.MenuItem)
			{
				PrepareContainerForItemOverride((Menu.MenuItem)element, item);
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
		}
		internal void PrepareContainerForItemOverride(Menu.MenuItem element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);
			if (item is ICommand)
			{
				element.Command = (ICommand)item;
			}
		}

		#endregion

		#region SeparatorStyleKey

		/// <summary>
		///     Resource Key for the SeparatorStyle
		/// </summary>
		public static ResourceKey SeparatorStyleKey { get { return sepStyleKey; } }
		static ComponentResourceKey sepStyleKey = new ComponentResourceKey(typeof(BasePopupItem), "BasePopupItem.Separator");

		#endregion ItemsStyleKey
	}
}
