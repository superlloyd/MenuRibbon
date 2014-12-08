using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MenuRibbon.WPF.Controls.Ribbon
{
	public class RibbonGroup : ActionHeaderedItemsControl
	{
		static RibbonGroup()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonGroup), new FrameworkPropertyMetadata(typeof(RibbonGroup)));
			RibbonBarPanel.GroupSizeIndexProperty.OverrideMetadata(typeof(RibbonGroup), new FrameworkPropertyMetadata(
				new PropertyChangedCallback((o, e) => ((RibbonGroup)o).OnGroupSizeIndexChanged((int)e.OldValue, (int)e.NewValue))
			));
		}

		public RibbonGroup()
		{
			GroupSizeDefinitions = new RibbonGroupSizeDefinitionCollection();
		}

		#region GroupSizeDefinitions

		public RibbonGroupSizeDefinitionCollection GroupSizeDefinitions
		{
			get { return (RibbonGroupSizeDefinitionCollection)GetValue(GroupSizeDefinitionsProperty); }
			set { SetValue(GroupSizeDefinitionsProperty, value); }
		}

		public static readonly DependencyProperty GroupSizeDefinitionsProperty = DependencyProperty.Register(
			"GroupSizeDefinitions", typeof(RibbonGroupSizeDefinitionCollection), typeof(RibbonGroup)
			, new FrameworkPropertyMetadata(
				default(RibbonGroupSizeDefinitionCollection)
				, FrameworkPropertyMetadataOptions.AffectsMeasure
				, (o, e) => ((RibbonGroup)o).OnGroupSizeDefinitionsChanged((RibbonGroupSizeDefinitionCollection)e.OldValue, (RibbonGroupSizeDefinitionCollection)e.NewValue)
			)
		);

		void OnGroupSizeDefinitionsChanged(RibbonGroupSizeDefinitionCollection OldValue, RibbonGroupSizeDefinitionCollection NewValue)
		{
			mCurrentGroupSizeDefinition = null;
		}

		void OnGroupSizeIndexChanged(int OldValue, int NewValue)
		{
			mCurrentGroupSizeDefinition = null;
			var v = CurrentGroupSizeDefinition;
			IsCollapsed = v != null ? v.IsCollapsed : false;
		}

		public int GroupSizeIndex { get { return RibbonBarPanel.GetGroupSizeIndex(this); } }

		#endregion

		#region CurrentGroupSizeDefinition
		
		public RibbonGroupSizeDefinition CurrentGroupSizeDefinition 
		{
			get 
			{
				if (mCurrentGroupSizeDefinition == null)
					UpdateCurrentGroupSize();
				return mCurrentGroupSizeDefinition; 
			}
		}
		RibbonGroupSizeDefinition mCurrentGroupSizeDefinition;

		void UpdateCurrentGroupSize()
		{
			var gsd = GroupSizeDefinitions;
			if (gsd == null || gsd.Count == 0)
			{
				mCurrentGroupSizeDefinition = null;
			}
			else
			{
				var iGS = IsCollapsed ? 0 : GroupSizeIndex;
				iGS = iGS < 0 ? 0 : iGS >= gsd.Count ? gsd.Count - 1 : iGS;
				mCurrentGroupSizeDefinition = gsd[iGS];

				int iItem = 0;
				for (int i = 0; i + iItem < Items.Count && i < mCurrentGroupSizeDefinition.ControlSizeDefinitions.Count; i++)
				{
					while (i + iItem < Items.Count)
					{
						var ic = Items[i + iItem] as IRibbonGroupControl;
						if (ic != null)
						{
							ic.ControlSizeDefinition = mCurrentGroupSizeDefinition.ControlSizeDefinitions[i];
							break;
						}
						else
						{
							iItem++;
						}
					}
				}
			}
		}

		#endregion

		#region IsCollapsed

		public bool IsCollapsed
		{
			get { return (bool)GetValue(IsCollapsedProperty); }
			private set { SetValue(IsCollapsedPropertyKey, BooleanBoxes.Box(value)); }
		}

		private static readonly DependencyPropertyKey IsCollapsedPropertyKey = DependencyProperty.RegisterReadOnly(
			"IsCollapsed", typeof(bool), typeof(RibbonGroup)
			, new PropertyMetadata(BooleanBoxes.FalseBox, (o, e) => ((RibbonGroup)o).OnIsCollapsedChanged((bool)e.OldValue, (bool)e.NewValue)));
		public static readonly DependencyProperty IsCollapsedProperty = IsCollapsedPropertyKey.DependencyProperty;

		void OnIsCollapsedChanged(bool OldValue, bool NewValue)
		{
			mCurrentGroupSizeDefinition = null;
		}

		#endregion

		#region IsDropDownOpen

		public bool IsDropDownOpen
		{
			get { return (bool)GetValue(IsDropDownOpenProperty); }
			set { SetValue(IsDropDownOpenProperty, BooleanBoxes.Box(value)); }
		}

		public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
			"IsDropDownOpen", typeof(bool), typeof(RibbonGroup)
			, new PropertyMetadata(BooleanBoxes.FalseBox, (o, e) => ((RibbonGroup)o).OnIsDropDownOpenChanged((bool)e.OldValue, (bool)e.NewValue)));

		void OnIsDropDownOpenChanged(bool OldValue, bool NewValue)
		{
		}

		#endregion

		public object LargeIcon
		{
			get
			{
				var ib = (ItemsButton)this.LogicalChildren().FirstOrDefault(x => x is ItemsButton);
				if (ib == null)
					return null;
				var o = ib.LargeIcon ?? ib.Icon ?? ib.SmallIcon;
				ImageSource imgs = null;
				if (o is ImageSource)
				{
					imgs = (ImageSource)o;
				}
				else if (o is Image)
				{
					imgs = ((Image)o).Source;
				}
				if (imgs == null) return null;
				return new Image { Source = imgs, Width = 32 };
			}
		}

		protected override Size MeasureOverride(Size constraint)
		{
			var sd = CurrentGroupSizeDefinition; // trigger calculation, if needed ...
			return base.MeasureOverride(constraint);
		}

		#region ItemsControl override

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new ItemsButton();
		}
		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return true; // cant take anything! :P
		}
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);
		}
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			if (element is ItemsButton)
			{
				var rb = (ItemsButton)element;
				if (item is ICommand)
				{
					rb.Command = (ICommand)item;
				}
			}
		}

		#endregion
	}
}
