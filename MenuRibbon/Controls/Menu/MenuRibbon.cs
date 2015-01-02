using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MenuRibbon.WPF.Controls.Menu
{
	public enum RibbonDisplay
	{
		None,
		Drop,
		Pin,
	}

	// About Ribbon UI
	// http://msdn.microsoft.com/en-us/library/dn742393.aspx

	[TemplatePart(Name = "PART_PopupContent", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PART_PinnedContent", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PART_KeyTipScope", Type = typeof(DependencyObject))]
	public class MenuRibbon : ItemsControl, IPopupRoot
	{
		static MenuRibbon()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MenuRibbon), new FrameworkPropertyMetadata(typeof(MenuRibbon)));
		}

		internal static void Beep()
		{
			NativeMethods.MessageBeep(NativeMethods.BeepType.OK);
		}

		public MenuRibbon()
		{
		}

		#region RibbonHeight

		public double RibbonHeight
		{
			get { return (double)GetValue(RibbonHeightProperty); }
			private set { SetValue(RibbonHeightPropertyKey, value); }
		}

		static readonly DependencyPropertyKey RibbonHeightPropertyKey = DependencyProperty.RegisterReadOnly(
			"RibbonHeight", typeof(double), typeof(MenuRibbon), new PropertyMetadata(90.0));

		public static readonly DependencyProperty RibbonHeightProperty = RibbonHeightPropertyKey.DependencyProperty;

		#endregion		

		#region IsPinning, PinnedItem, TogglePinCommand

		/// <summary>
		/// Whether the Ribbon is expanded or not
		/// </summary>
		public bool IsPinning
		{
			get { return (bool)GetValue(IsPinningProperty); }
			internal set 
			{
				if (value == IsPinning) return;

				if (value)
				{
					if (PinnedItem == null)
					{
						PinnedItem = Items.Cast<object>().FirstOrDefault(x =>
						{
							var ri = ItemContainerGenerator.ContainerFromItem(x) as RibbonItem;
							return ri != null;
						});
						if (PinnedItem == null) value = false;
					}
				}

				SetValue(IsPinningPropertyKey, BooleanBoxes.Box(value));
				UpdateRibbonAppearance();
			}
		}

		static readonly DependencyPropertyKey IsPinningPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsPinning", typeof(bool), typeof(MenuRibbon), new PropertyMetadata(BooleanBoxes.FalseBox));

		public static readonly DependencyProperty IsPinningProperty = IsPinningPropertyKey.DependencyProperty;

		public object PinnedItem
		{
			get { return (object)GetValue(PinnedItemProperty); }
			set { SetValue(PinnedItemProperty, value); }
		}

		public static readonly DependencyProperty PinnedItemProperty = DependencyProperty.Register(
			"PinnedItem", typeof(object), typeof(MenuRibbon)
			, new PropertyMetadata(
				default(object),
				(o, e) => ((MenuRibbon)o).OnPinnedItemChanged((object)e.OldValue, (object)e.NewValue),
				new CoerceValueCallback((o, val) => ((MenuRibbon)o).OnCoercePinnedItem((object)val))
			));

		void OnPinnedItemChanged(object OldValue, object NewValue)
		{
			UpdateRibbonAppearance();

			var oldMRI = ItemContainerGenerator.ContainerFromItem(OldValue) as RibbonItem;
			var newMRI = ItemContainerGenerator.ContainerFromItem(NewValue) as RibbonItem;
			if (oldMRI != null) oldMRI.IsPinned = false;
			if (newMRI != null) newMRI.IsPinned = true;
		}

		object OnCoercePinnedItem(object value)
		{
			var ri = ItemContainerGenerator.ContainerFromItem(value) as RibbonItem;
			if (ri == null) return null;
			return value;
		}

		#endregion

		#region TogglePinCommand

		public void TogglePin()
		{
			this.IsPinning = !this.IsPinning;
		}

		public ICommand TogglePinCommand
		{
			get
			{
				if (mTogglePinCmd == null)
					mTogglePinCmd = new TogglePinCmd(this);
				return mTogglePinCmd;
			}
		}
		TogglePinCmd mTogglePinCmd;
		class TogglePinCmd : ICommand
		{
			public TogglePinCmd(MenuRibbon ribbon)
			{
				this.ribbon = ribbon;
			}
			MenuRibbon ribbon;

			bool ICommand.CanExecute(object parameter) { return true; }

			event EventHandler ICommand.CanExecuteChanged
			{
				add { }
				remove { }
			}

			void ICommand.Execute(object parameter) { ribbon.TogglePin(); }
		}

		#endregion

		#region (UI State:) RibbonDisplay, DroppedRibbonItem, PinnedRibbonItem, UpdateRibbonAppearance()

		public RibbonDisplay RibbonDisplay
		{
			get { return (RibbonDisplay)GetValue(RibbonDisplayProperty); }
			private set { SetValue(RibbonDisplayPropertyKey, EnumBox<RibbonDisplay>.Box((int)value)); }
		}

		static readonly DependencyPropertyKey RibbonDisplayPropertyKey = DependencyProperty.RegisterReadOnly(
			"RibbonDisplay", typeof(RibbonDisplay), typeof(MenuRibbon), new PropertyMetadata(EnumBox<RibbonDisplay>.Box((int)RibbonDisplay.None)));

		public static readonly DependencyProperty RibbonDisplayProperty = RibbonDisplayPropertyKey.DependencyProperty;

		public RibbonItem DroppedRibbonItem
		{
			get { return (RibbonItem)GetValue(DroppedRibbonItemProperty); }
			private set { SetValue(DroppedRibbonItemPropertyKey, value); }
		}

		static readonly DependencyPropertyKey DroppedRibbonItemPropertyKey = DependencyProperty.RegisterReadOnly(
			"DroppedRibbonItem", typeof(RibbonItem), typeof(MenuRibbon), new PropertyMetadata(null));

		public static readonly DependencyProperty DroppedRibbonItemProperty = DroppedRibbonItemPropertyKey.DependencyProperty;

		public RibbonItem PinnedRibbonItem
		{
			get { return (RibbonItem)GetValue(PinnedRibbonItemProperty); }
			private set { SetValue(PinnedRibbonItemPropertyKey, value); }
		}

		static readonly DependencyPropertyKey PinnedRibbonItemPropertyKey = DependencyProperty.RegisterReadOnly(
			"PinnedRibbonItem", typeof(RibbonItem), typeof(MenuRibbon), new PropertyMetadata(null));

		public static readonly DependencyProperty PinnedRibbonItemProperty = PinnedRibbonItemPropertyKey.DependencyProperty;

		void UpdateRibbonAppearance()
		{
			RibbonItem h = DroppedRibbonItem;
			if (PopupManager.HighlightedItem != null)
				h = ItemContainerGenerator.ContainerFromItem(PopupManager.HighlightedItem) as RibbonItem;
			if (!PopupManager.Tracking)
				h = null;
			var org = ItemContainerGenerator.ContainerFromItem(PinnedItem) as RibbonItem;
			if (!IsPinning)
				org = null;
			var item = h ?? org;
			if (IsPinning && item != null)
			{
				RibbonDisplay = RibbonDisplay.Pin;
				DroppedRibbonItem = null;
				PinnedRibbonItem = item;
			}
			else if (PopupManager.Tracking && item != null)
			{
				RibbonDisplay = RibbonDisplay.Drop;
				DroppedRibbonItem = item;
				PinnedRibbonItem = null;
			}
			else
			{
				RibbonDisplay = RibbonDisplay.None;
				DroppedRibbonItem = null;
				PinnedRibbonItem = null;
			}
		}

		#endregion

		#region ItemsControl override

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new Menu.MenuItem();
		}
		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is MenuItem || item is RibbonItem;
		}
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);
		}
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is Menu.MenuItem)
			{
				var mi = (Menu.MenuItem)element;
				mi.PrepareContainerForItemOverride(mi, item);
			}
			else
			{
				var ri = (RibbonItem)element;
			}

			var c = (Control)element;
			Action<DependencyProperty> copyIfEmpty = dp =>
			{
				if (c.HasDefaultValue(dp))
					BindingOperations.SetBinding(c, dp, new Binding(dp.Name) { Source = this });
			};
			copyIfEmpty(Control.BackgroundProperty);
			copyIfEmpty(Control.ForegroundProperty);
			copyIfEmpty(Control.BorderBrushProperty);
		}
		protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			if (PinnedItem == null || !Items.Contains(PinnedItem))
			{
				PinnedItem = Items.Cast<object>().FirstOrDefault(x =>
				{
					var ri = ItemContainerGenerator.ContainerFromItem(x) as RibbonItem;
					return ri != null;
				});
				UpdateRibbonAppearance();
			}
		}

		#endregion

		#region mouse, keyboard, focus handling

		protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
		{
			if (PopupManager.Tracking || IsPinning)
			{
				cumulativeWheelDelta += e.Delta;
				if (Math.Abs(cumulativeWheelDelta) > MouseWheelSelectionChangeThreshold)
				{
					bool forward = cumulativeWheelDelta < 0;
					cumulativeWheelDelta = 0;

					IPopupItem selected = null;
					if (PopupManager.Tracking)
					{
						if (PopupManager.HighlightedItem != null)
						{
							var p = PopupManager.HighlightedItem;
							while (p.ParentItem != null) p = p.ParentItem;
							selected = p;
						}
					}
					else
					{
						var item = PinnedItem;
						selected = IsItemItsOwnContainer(item) ? (IPopupItem)item : (IPopupItem)ItemContainerGenerator.ContainerFromItem(item);
					}

					IPopupItem next = null;
					if (selected != null)
					{
						next = selected
							.PopupSiblings(forward, false)
							.Where(x => PopupManager.Tracking || x is RibbonItem)
							.FirstOrDefault();
					}
					else
					{
						next = this.PopupChildren()
							.Where(x => PopupManager.Tracking || x is RibbonItem)
							.FirstOrDefault();
					}

					if (next != null && next != selected)
					{
						e.Handled = true;
						if (PopupManager.Tracking)
						{
							PopupManager.Enter(next, true);
						}
						else
						{
							PinnedItem = ItemContainerGenerator.ItemFromContainer((DependencyObject)next);
						}
					}
				}
			}
			base.OnPreviewMouseWheel(e);
		}

		double cumulativeWheelDelta;
		private const double MouseWheelSelectionChangeThreshold = 100;

		protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnPreviewGotKeyboardFocus(e);
			PopupManager.Tracking = true;
		}

		#endregion

		#region OnApplyTemplate(), KeyTipScope

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			KeyTipScope = GetTemplateChild("PART_KeyTipScope");
		}

		public DependencyObject KeyTipScope { get; private set; }

		#endregion

		void IPopupRoot.UpdatePopupRoot() { UpdateRibbonAppearance(); }
		void IPopupRoot.OnLostFocus() { }

		public PopupManager PopupManager
		{
			get 
			{
				if (mPopupManager == null)
					mPopupManager = new PopupManager(this);
				return mPopupManager;
			}
		}
		PopupManager mPopupManager;
	}
}
