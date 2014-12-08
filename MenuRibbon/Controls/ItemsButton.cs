using MenuRibbon.WPF.Controls.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using MM = MenuRibbon.WPF.Controls.Menu;

namespace MenuRibbon.WPF.Controls
{
	public interface IRibbonGroupControl
	{
		RibbonControlSizeDefinition ControlSizeDefinition { get; set; }
	}

	public partial class ItemsButton : MM.MenuItem, IPopupRoot, IRibbonGroupControl
	{
		static ItemsButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ItemsButton), new FrameworkPropertyMetadata(typeof(ItemsButton)));
		}

		public ItemsButton()
		{
		}

		#region ControlSizeDefinition

		public RibbonControlSizeDefinition ControlSizeDefinition
		{
			get { return (RibbonControlSizeDefinition)GetValue(ControlSizeDefinitionProperty); }
			set { SetValue(ControlSizeDefinitionProperty, value); }
		}

		static readonly DependencyProperty ControlSizeDefinitionProperty = DependencyProperty.Register(
			"ControlSizeDefinition", typeof(RibbonControlSizeDefinition), typeof(ItemsButton), new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.AffectsMeasure 
				| FrameworkPropertyMetadataOptions.AffectsParentMeasure
				,new PropertyChangedCallback((o, e) => ((ItemsButton)o).OnControlSizeDefinitionChanged((RibbonControlSizeDefinition)e.OldValue, (RibbonControlSizeDefinition)e.NewValue)
			)));


		protected virtual void OnControlSizeDefinitionChanged(RibbonControlSizeDefinition OldValue, RibbonControlSizeDefinition NewValue)
		{
		}

		#endregion		

		#region LargeIcon, SmallIcon

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

		#region ShowHeader

		public bool ShowHeader
		{
			get { return (bool)GetValue(ShowHeaderProperty); }
			set { SetValue(ShowHeaderProperty, BooleanBoxes.Box(value)); }
		}

		public static readonly DependencyProperty ShowHeaderProperty = DependencyProperty.Register(
			"ShowHeader", typeof(bool), typeof(ItemsButton)
			, new PropertyMetadata(BooleanBoxes.TrueBox, (o, e) => ((ItemsButton)o).OnShowHeaderChanged((bool)e.OldValue, (bool)e.NewValue)));

		void OnShowHeaderChanged(bool OldValue, bool NewValue)
		{
		}

		#endregion

		#region IPopupRoot

		void IPopupRoot.UpdatePopupRoot() {}

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
	}
}
