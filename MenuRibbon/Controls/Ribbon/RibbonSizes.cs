using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace MenuRibbon.WPF.Controls.Ribbon
{
	public enum RibbonIconSize
	{
		Collapsed,
		Small,
		Large,
	}

	public class RibbonControlSizeDefinition
	{
		public RibbonControlSizeDefinition()
		{
			MinWidth = 0;
			Width = double.NaN;
			MaxWidth = double.MaxValue;
			IsHeaderVisible = true;
			IconSize = RibbonIconSize.Small;
		}
		public bool IsHeaderVisible { get; set; }
		public RibbonIconSize IconSize { get; set; }
		public double MinWidth { get; set; }
		public double MaxWidth { get; set; }
		public double Width { get; set; }
	}

	[ContentProperty("ControlSizeDefinitions")]
	public class RibbonGroupSizeDefinition
	{
		public RibbonGroupSizeDefinition()
		{
			ControlSizeDefinitions = new List<RibbonControlSizeDefinition>();
		}

		public List<RibbonControlSizeDefinition> ControlSizeDefinitions { get; set; }
		public bool IsCollapsed { get; set; }
	}

	public class RibbonGroupSizeDefinitionCollection : List<RibbonGroupSizeDefinition>
	{
	}
}
