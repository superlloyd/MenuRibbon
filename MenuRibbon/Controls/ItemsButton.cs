using MenuRibbon.WPF.Controls.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MM = MenuRibbon.WPF.Controls.Menu;

namespace MenuRibbon.WPF.Controls
{
	public interface IRibbonGroupControl
	{
		RibbonControlSizeDefinition ControlSizeDefinition { get; set; }
	}

	[TemplatePart(Name = "PART_Header", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PART_Splitter", Type = typeof(FrameworkElement))]
	[TemplatePart(Name = "PART_BUTTON", Type = typeof(FrameworkElement))]
	public partial class ItemsButton : BasePopupItem, IPopupRoot, IRibbonGroupControl
	{
		static ItemsButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ItemsButton), new FrameworkPropertyMetadata(typeof(ItemsButton)));
		}

		public ItemsButton()
		{
		}

		#region OnActivatingKeyTip(), OnKeyTipAccessed()

		protected override void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
		{
			base.OnActivatingKeyTip(e);

			if (e.OriginalSource != this)
				return;
			var csd = ControlSizeDefinition;
			if (csd == null)
				return;

			if(csd.IsHeaderVisible)
			{
				if (csd.IconSize == RibbonIconSize.Large)
				{
					e.KeyTipVerticalPlacement = KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom;
				}
				else if (csd.IconSize == RibbonIconSize.Small)
				{
					e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
					e.PlacementTarget = Icon as UIElement;
				}
			}
			else
			{
				if(csd.IconSize == RibbonIconSize.Small)
				{
					e.KeyTipHorizontalPlacement = KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter;
				}
			}
		}

		protected override void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
		{
			if (e.OriginalSource != this)
				return;

			if (IsSplitButton || HasItems)
			{
				this.OnNavigateChildren();
				e.TargetKeyTipScope = this;
			}
			else
			{
				OnClick();
			}

			e.Handled = true;
			if (!IsOpen)
			{
				this.CloseAllPopups();
			}
		}

		#endregion

		#region ControlSizeDefinition, LargeIcon, SmallIcon

		public RibbonControlSizeDefinition ControlSizeDefinition
		{
			get { return (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); }
			set { SetValue(ControlSizeDefinitionProperty, value); }
		}

		static readonly DependencyProperty ControlSizeDefinitionProperty = DependencyProperty.Register(
			"ControlSizeDefinition", typeof(RibbonControlSizeDefinition), typeof(ItemsButton), new FrameworkPropertyMetadata(
				RibbonControlSizeDefinition.Medium,
				FrameworkPropertyMetadataOptions.AffectsMeasure 
				| FrameworkPropertyMetadataOptions.AffectsParentMeasure
				,new PropertyChangedCallback((o, e) => ((ItemsButton)o).OnControlSizeDefinitionChanged((RibbonControlSizeDefinition)e.OldValue, (RibbonControlSizeDefinition)e.NewValue)
			)));


		protected virtual void OnControlSizeDefinitionChanged(RibbonControlSizeDefinition OldValue, RibbonControlSizeDefinition NewValue)
		{
		}

		public object LargeIcon
		{
			get { return (object)GetValue(LargeIconProperty); }
			set { SetValue(LargeIconProperty, value); }
		}

		public static readonly DependencyProperty LargeIconProperty = DependencyProperty.Register(
			"LargeIcon", typeof(object), typeof(ItemsButton)
			, new PropertyMetadata(default(object), (o, e) => ((ItemsButton)o).OnLargeIconChanged((object)e.OldValue, (object)e.NewValue)));

		void OnLargeIconChanged(object OldValue, object NewValue)
		{
		}

		public object SmallIcon
		{
			get { return (object)GetValue(SmallIconProperty); }
			set { SetValue(SmallIconProperty, value); }
		}

		public static readonly DependencyProperty SmallIconProperty = DependencyProperty.Register(
			"SmallIcon", typeof(object), typeof(ItemsButton)
			, new PropertyMetadata(default(object), (o, e) => ((ItemsButton)o).OnSmallIconChanged((object)e.OldValue, (object)e.NewValue)));

		void OnSmallIconChanged(object OldValue, object NewValue)
		{
		}

		#endregion

		#region IsSplitButton

		public bool IsSplitButton
		{
			get { return (bool)GetValue(IsSplitButtonProperty); }
			set { SetValue(IsSplitButtonProperty, BooleanBoxes.Box(value)); }
		}

		public static readonly DependencyProperty IsSplitButtonProperty = DependencyProperty.Register(
			"IsSplitButton", typeof(bool), typeof(ItemsButton)
			, new PropertyMetadata(BooleanBoxes.FalseBox, (o, e) => ((ItemsButton)o).OnIsSplitButtonChanged((bool)e.OldValue, (bool)e.NewValue)));

		void OnIsSplitButtonChanged(bool OldValue, bool NewValue)
		{
		}

		#endregion		

		#region IPopupRoot

		void IPopupRoot.UpdatePopupRoot() {}
		void IPopupRoot.OnLostFocus() { PopupManager.OpenedItem = null; }

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

		#endregion		

		#region IsHoveringSplitter

		public bool IsHoveringSplitter
		{
			get { return (bool)GetValue(IsHoveringSplitterProperty); }
			private set { SetValue(IsHoveringSplitterPropertyKey, BooleanBoxes.Box(value)); }
		}

		private static readonly DependencyPropertyKey IsHoveringSplitterPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsHoveringSplitter", typeof(bool), typeof(ItemsButton)
			, new PropertyMetadata(BooleanBoxes.FalseBox, (o, e) => ((ItemsButton)o).OnIsHoveringSplitterChanged((bool)e.OldValue, (bool)e.NewValue)));

		public static readonly DependencyProperty IsHoveringSplitterProperty = IsHoveringSplitterPropertyKey.DependencyProperty;

		void OnIsHoveringSplitterChanged(bool OldValue, bool NewValue)
		{
		}

		#endregion

		#region FrameworkElement override + InputHandling

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			events.Clear();

			var header = GetTemplateChild("PART_Header");
			var splitter = GetTemplateChild("PART_Splitter");
			var all = GetTemplateChild("PART_BUTTON");

			events["1"] = all != null ? all.MouseHovering().Subscribe(x => IsHovering = x) : null;
			events["HLBD"] = header != null ? header.MouseDown().Where(x => x.ChangedButton == MouseButton.Left).Subscribe(x => 
			{
				if (HasItems && !IsSplitButton) PopupManager.Enter(this, true);
			}) : null;
			events["2"] = header != null ? header.MouseClicks().Subscribe(x => OnClick()) : null;
			events["3"] = header != null ? header.MousePressed().Subscribe(x => IsPressed = this.IsPressed()) : null;
			events["5"] = splitter != null ? splitter.MouseHovering().Subscribe(x => IsHoveringSplitter = x) : null;
			events["4"] = splitter != null ? splitter.MouseDown().Where(x => x.ChangedButton == MouseButton.Left).Subscribe(x => PopupManager.OpenedItem = this) : null;
		}
		DisposableBag events = new DisposableBag();

		protected override void OnClick(RoutedEventArgs e)
		{
			if (!HasItems || IsSplitButton)
			{
				base.OnClick(e);
				PopupManager.Tracking = false;
			}
		}

		#endregion
	}
}
