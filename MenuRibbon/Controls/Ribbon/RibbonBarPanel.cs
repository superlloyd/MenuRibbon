using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MenuRibbon.WPF.Controls.Ribbon
{
	public class RibbonBarPanel : Panel
	{
		public RibbonBarPanel()
		{
		}

		RibbonBar RibbonBar
		{
			get
			{
				DependencyObject p = this;
				while (p != null && !(p is RibbonBar))
					p = VisualTreeHelper.GetParent(p);
				return p as RibbonBar;
			}
		}

		#region AP: GroupSize, GroupSizeDefinition, IsCollapsed

		private static object[] Int100AsObject = Enumerable.Range(0, 100).Cast<object>().ToArray();

		public static int GetGroupSizeIndex(RibbonGroup obj)
		{
			return (int)obj.GetValue(GroupSizeIndexProperty);
		}
		public static void SetGroupSizeIndex(RibbonGroup obj, int value)
		{
			obj.SetValue(GroupSizeIndexProperty, Int100AsObject[value]);
		}

		public static readonly DependencyProperty GroupSizeIndexProperty = DependencyProperty.RegisterAttached(
			"GroupSizeIndex", typeof(int), typeof(RibbonBarPanel), 
			new FrameworkPropertyMetadata(
				Int100AsObject[0],
				FrameworkPropertyMetadataOptions.Inherits
				| FrameworkPropertyMetadataOptions.AffectsMeasure
				)
		);

		#endregion

		protected override Size ArrangeOverride(Size finalSize)
		{
			double pos = 0;
			foreach (UIElement uic in this.Children)
			{
				var s = uic.DesiredSize;
				uic.Arrange(new Rect(pos, 0, s.Width, finalSize.Height));
				pos += s.Width;
			}
			return new Size(pos, finalSize.Height);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var W = availableSize.Width;
			var H = availableSize.Height;

			var children = this.Children.Cast<UIElement>().ToList();
			var rb = RibbonBar;
			if (rb != null)
			{
				var order = rb.GroupSizeReductionOrder;
				Func<UIElement, int> getSO = (uic) =>
				{
					if (order == null)
						return children.IndexOf(uic);
					var fe = uic as FrameworkElement;
					if (fe == null || string.IsNullOrEmpty(fe.Name))
						return children.IndexOf(uic);
					var index = order.IndexOf(fe.Name);
					if (index < 0)
						return children.IndexOf(uic);
					return index;
				};
				children.Sort((x1, x2) => getSO(x1) - getSO(x2));
			}

			// calculate original size
			double total = 0;
			var hInf = new Size(double.MaxValue, availableSize.Height);
			int MaxSR = 0;
			int N = 0;
			foreach (var uic in children)
			{
				var rg = uic as RibbonGroup;
				if (rg != null && rg.GroupSizeDefinitions != null && rg.GroupSizeDefinitions.Count > 0)
				{
					N++;
					SetGroupSizeIndex(rg, 0);
					if (rg.GroupSizeDefinitions.Count > MaxSR)
						MaxSR = rg.GroupSizeDefinitions.Count;
				}

				uic.Measure(hInf);
				var s = uic.DesiredSize;
				total += s.Width;
			}

			for (int SR = 1; SR < MaxSR; SR++)
			{
				//var diff = (availableSize.Width - total) / N;
				if (total <= availableSize.Width)
					break;
				foreach (var uic in children)
				{
					if (total <= availableSize.Width)
						break;
					var rg = uic as RibbonGroup;
					if (rg != null && rg.GroupSizeDefinitions != null && rg.GroupSizeDefinitions.Count > SR)
					{
						var prev = uic.DesiredSize;
						SetGroupSizeIndex(rg, SR);
						uic.Measure(hInf);
						var next = uic.DesiredSize;
						total += next.Width - prev.Width;
					}
				}
			}

			return new Size(total, H);
		}
	}
}
