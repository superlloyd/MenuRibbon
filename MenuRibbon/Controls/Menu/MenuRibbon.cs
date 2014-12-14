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
	public class MenuRibbon : ItemsControl, IPopupRoot
	{
		static MenuRibbon()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MenuRibbon), new FrameworkPropertyMetadata(typeof(MenuRibbon)));
		}

		public MenuRibbon()
		{
			KeyTipService.Current.MenuRibbon = this;
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

			void ICommand.Execute(object parameter)
			{
				ribbon.IsPinning = !ribbon.IsPinning;
			}
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

		void IPopupRoot.UpdatePopupRoot() { UpdateRibbonAppearance(); }

		void UpdateRibbonAppearance()
		{
			var h = ItemContainerGenerator.ContainerFromItem(PopupManager.HighlightedItem) as RibbonItem;
			if (!PopupManager.IsResponsive)
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
			else if (PopupManager.IsResponsive && item != null)
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
			return new MenuItem();
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
			if (element is MenuItem)
			{
				var mi = (MenuItem)element;
				mi.PrepareContainerForItemOverride(mi, item);
			}
			else
			{
				var ri = (RibbonItem)element;
			}
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
			if (PopupManager.IsResponsive || IsPinning)
			{
				cumulativeWheelDelta += e.Delta;
				if (Math.Abs(cumulativeWheelDelta) > MouseWheelSelectionChangeThreshold)
				{
					bool forward = cumulativeWheelDelta < 0;
					cumulativeWheelDelta = 0;

					object selected = null;
					if (PopupManager.IsResponsive)
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
						selected = PinnedItem;
					}

					var next = this.NextEnabledItem(selected, forward, false, x => {
						if (PopupManager.IsResponsive)  { return true; }
						else 
						{
							var it = ItemContainerGenerator.ContainerFromItem(x);
							return it is RibbonItem;
						}
					});

					if (next != null && next != selected)
					{
						e.Handled = true;
						if (PopupManager.IsResponsive)
						{
							PopupManager.Enter(this.ItemContainerGenerator.ContainerFromItem(next) as IPopupItem, true);
						}
						else
						{
							PinnedItem = next;
						}
					}
				}
			}
			base.OnPreviewMouseWheel(e);
		}

		double cumulativeWheelDelta;
		private const double MouseWheelSelectionChangeThreshold = 100;

		#endregion

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
