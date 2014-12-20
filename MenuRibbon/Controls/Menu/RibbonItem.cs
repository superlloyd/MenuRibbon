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

namespace MenuRibbon.WPF.Controls.Menu
{
	[TemplatePart(Name = "PART_Header", Type = typeof(FrameworkElement))]
	[TemplateVisualState(Name = "HighlightOn", GroupName = "Highlight")]
	[TemplateVisualState(Name = "HighlightOff", GroupName = "Highlight")]
	public class RibbonItem : HeaderedContentControl, IPopupItem
	{
		static RibbonItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonItem), new FrameworkPropertyMetadata(typeof(RibbonItem)));
		}

		#region MenuRibbon, IsPinning

		[Bindable(true), Browsable(false)]
		public MenuRibbon MenuRibbon
		{
			get { return (MenuRibbon)GetValue(MenuRibbonProperty); }
			private set { SetValue(MenuRibbonPropertyKey, value); }
		}

		static readonly DependencyPropertyKey MenuRibbonPropertyKey = DependencyProperty.RegisterReadOnly(
			"MenuRibbon", typeof(MenuRibbon), typeof(RibbonItem), new PropertyMetadata(default(MenuRibbon)));

		public static readonly DependencyProperty MenuRibbonProperty = MenuRibbonPropertyKey.DependencyProperty;

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			UpdateMenuRibbon();
		}
		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			UpdateMenuRibbon();
		}

		private void UpdateMenuRibbon()
		{
			var p = this.LogicalParent();
			while (p is RibbonItem)
				p = p.LogicalParent();
			MenuRibbon = p as MenuRibbon;
		}

		public bool IsPinned
		{
			get { return (bool)GetValue(IsPinnedProperty); }
			set { SetValue(IsPinnedProperty, BooleanBoxes.Box(value)); }
		}

		public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
			"IsPinned", typeof(bool), typeof(RibbonItem)
			, new PropertyMetadata(
				BooleanBoxes.FalseBox,
				(o, e) => ((RibbonItem)o).OnIsPinnedChanged((bool)e.OldValue, (bool)e.NewValue),
				new CoerceValueCallback((o, val) => ((RibbonItem)o).OnCoerceIsPinned((bool)val))
			));

		void OnIsPinnedChanged(bool OldValue, bool NewValue)
		{
			if (NewValue)
			{
				MenuRibbon.PinnedItem = this;
			}
			else if (MenuRibbon != null && MenuRibbon.PinnedItem == this)
			{
				MenuRibbon.PinnedItem = null;
			}
		}

		bool OnCoerceIsPinned(bool value)
		{
			return value;
		}

		#endregion		

		#region IPopupItem: IsOpen, ParentItem, PopupRoot

		void IPopupItem.Action() { MenuRibbon.TogglePin(); }
		IPopupRoot IPopupItem.PopupRoot { get { return MenuRibbon; } }
		IPopupItem IPopupItem.ParentItem { get { return null; } }
		bool IPopupItem.Contains(DependencyObject target)
		{
			var res = target.VisualHierarchy().Contains(this);
			if (!res && MenuRibbon != null)
			{
				res = target.VisualHierarchy().Contains(MenuRibbon);
				if (MenuRibbon.PinnedRibbonItem == this || MenuRibbon.DroppedRibbonItem == this)
				{
					res = target.VisualHierarchy().Contains(MenuRibbon);
				}
			}
			return res;
		}

		public bool IsOpen
		{
			get { return (bool)GetValue(IsOpenProperty); }
		}

		bool IPopupItem.IsOpen
		{
			get { return IsOpen; }
			set { SetValue(IsOpenPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey IsOpenPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsOpen", typeof(bool), typeof(RibbonItem), new PropertyMetadata(default(bool)));

		public static readonly DependencyProperty IsOpenProperty = IsOpenPropertyKey.DependencyProperty;

		#endregion		

		#region IsHovering, IsHighlighted

		public bool IsHovering
		{
			get { return (bool)GetValue(IsHoveringProperty); }
			private set 
			{
				SetValue(IsHoveringPropertyKey, BooleanBoxes.Box(value));

				if (MenuRibbon != null)
				{
					if (value)
					{
						MenuRibbon.PopupManager.Enter(this);
					}
					else
					{
						// leave it there!
					}
				}
			}
		}

		static readonly DependencyPropertyKey IsHoveringPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsHovering", typeof(bool), typeof(RibbonItem), new PropertyMetadata(BooleanBoxes.FalseBox));

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
			"IsHighlighted", typeof(bool), typeof(RibbonItem), new PropertyMetadata(BooleanBoxes.FalseBox, (o, e) => ((RibbonItem)o).OnIsHighlightedChanged((bool)e.OldValue, (bool)e.NewValue)));

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

		#region FrameworkElement override + MouseHandling

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			events.Clear();

			var main = GetTemplateChild("PART_Header");
			if (main != null)
			{
				events["H"] = main.MouseHovering().Subscribe(x => IsHovering = x);
				events["L"] = main.MouseDown().Where(x => x.ChangedButton == MouseButton.Left).Subscribe(x => OnMainUI_LeftMouseDown(x));
				events["D"] = main.MouseClicks().Subscribe(x => OnClicks(x));
			}
		}
		DisposableBag events = new DisposableBag();

		void OnClicks(Tuple<MouseButtonEventArgs, int> x)
		{
			if (MenuRibbon != null && x.Item2 % 2 == 0)
			{
				MenuRibbon.TogglePin();
			}
		}
		protected void OnMainUI_LeftMouseDown(MouseButtonEventArgs e)
		{
			var item = MenuRibbon.ItemContainerGenerator.ItemFromContainer(this);
			MenuRibbon.PinnedItem = item;
			MenuRibbon.PopupManager.Enter(this, true);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			this.OnKeyNavigate(e);
			base.OnKeyDown(e);
			if (!e.Handled)
			{
				switch (e.Key)
				{
					case Key.Up:
					case Key.Down:
						if (MenuRibbon != null && MenuRibbon.RibbonDisplay == RibbonDisplay.Drop)
						{
							var c = MenuRibbon.DroppedRibbonItem.Content.FirstFocusableElement();
							if (c == null)
							{
								var p = MenuRibbon.VisualChildren().Where(x => (string)x.GetValue(FrameworkElement.NameProperty) == "PART_Popup").FirstOrDefault() as Popup;
								if (p != null) c = p.Child.FirstFocusableElement();
							}
							if (c != null)
							{
								c.Focus();
								e.Handled = true;
							}
						}
						break;
				}
			}
		}

		protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnPreviewGotKeyboardFocus(e);
			var r = MenuRibbon;
			if (r != null)
			{
				r.PopupManager.Tracking = true;
			}
		}

		#endregion
	}
}
