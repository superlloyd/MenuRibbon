using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MenuRibbon.WPF.Utils;

namespace MenuRibbon.WPF.Controls.Menu
{
	public class MenuHeaderPanel : Panel
	{
		MenuItemsPanel ItemsPanel 
		{
			get { return (MenuItemsPanel)this.VisualHierarchy().FirstOrDefault(x => x is MenuItemsPanel); }
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var lp = ItemsPanel;

			double[] widths = new double[this.Children.Count];
			Enumerable.Range(0, widths.Length).ForEach(i => { 
				var w = this.Children[i].DesiredSize.Width;
				if (lp != null && lp[i] > w) w = lp[i];
				widths[i] = w;
			});
			var dw = finalSize.Width - widths.Sum();
			if (dw > 0 && widths.Length > 2)
				widths[1] += dw;

			double pos = 0;
			for (int i = 0; i < this.Children.Count; i++)
			{
				this.Children[i].Arrange(new Rect(pos, 0, widths[i], finalSize.Height));
				pos += widths[i];
			}

			return new Size(pos, finalSize.Height);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var lp = ItemsPanel;
			int i = -1;
			var result = new Size();
			foreach (UIElement child in this.Children)
			{
				i++;

				child.Measure(availableSize);
				var s = child.DesiredSize;

				if (lp != null)
				{
					var cur = lp[i];
					if (s.Width > cur)
					{
						lp[i] = s.Width;
					}
					else
					{
						s.Width = cur;
					}
				}

				if (result.Height < s.Height) result.Height = s.Height;
				result.Width += s.Width;
			}
			return result;
		}
	}

	public class MenuItemsPanel : Panel
	{
		List<double> headerColumnSize = new List<double>(5); // should contains 4 column, allocate once, reuse, happy GC!

		public double this[int column]
		{
			get
			{
				var h = GetHeaderSizes(column);
				return h[column];
			}
			set
			{
				var h = GetHeaderSizes(column);
				h[column] = value;
			}
		}
		List<double> GetHeaderSizes(int column)
		{
			while (headerColumnSize.Count <= column)
				headerColumnSize.Add(0);
			return headerColumnSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			double pos = 0.0;
			foreach (UIElement child in this.Children)
			{
				var h = child.DesiredSize.Height;
				child.Arrange(new Rect(0, pos, finalSize.Width, h));
				pos += h;
			}
			return new Size(finalSize.Width, pos);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			// reset column sizes & measure each child
			foreach (UIElement child in this.Children)
			{
				child.Measure(availableSize);
				var w = child.DesiredSize.Width;
			}

			// get desired size at the end, so that they could use the final column size
			var result = new Size();
			foreach (UIElement child in this.Children)
			{
				// need to compute again so all container around MenuHeaderPanel get their desired size updated
				child.Measure(availableSize);
				var s = child.DesiredSize;
				result.Width = Math.Max(result.Width, s.Width);
				result.Height += s.Height;
			}

			return result;
		}
	}
}
