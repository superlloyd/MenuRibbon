using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MenuRibbon.WPF.Controls.Ribbon
{
	/// <summary>
	/// A wrap panel that spread its child control evenly in the constrained space.
	/// I.e. it will wrap its child control very much like a wrap panel but instead of stacking row / columns
	/// next to each other it will spread them to take available space
	/// </summary>
	public class RibbonGroupPanel : Panel
	{
		List<double> colWidths = new List<double>(); // allocate once, reuse => happy GC

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (this.Children.Count == 0)
				return Size.Empty;

			double w = 0, wTot = 0;
			double y = 0, yMax = 0;
			colWidths.Clear();
			int iCol = 0;
			for (int i = 0; i < this.Children.Count; i++)
			{
				var ui = this.Children[i];
				var s = ui.DesiredSize;

				if (y > 0 && s.Height + y > finalSize.Height)
				{
					colWidths.Add(w);
					iCol += 1;
					wTot += w;
					w = s.Width;
					y = 0;
				}
				else
				{
					if (s.Width > w)
						w = s.Width;
				}
				y += s.Height;
				if (y > yMax)
					yMax = y;
			}
			colWidths.Add(w);

			iCol = 0;
			y = 0;
			wTot = 0;
			for (int i = 0; i < this.Children.Count; i++)
			{
				var ui = this.Children[i];
				var s = ui.DesiredSize;

				if (y > 0 && y + s.Height > finalSize.Height)
				{
					wTot += colWidths[iCol];
					iCol += 1;
					y = 0;
				}

				ui.Arrange(new Rect(wTot, y, colWidths[iCol], s.Height));
				y += s.Height;
			}

			return new Size(wTot + colWidths[iCol], yMax);
		}

		protected override Size MeasureOverride(Size constraint)
		{
			double w = 0, wTot = 0;
			double y = 0, yMax = 0;
			foreach (UIElement ui in this.Children)
			{
				ui.Measure(constraint);
				var s = ui.DesiredSize;

				if (y > 0 && s.Height + y > constraint.Height)
				{
					wTot += w;
					w = s.Width;
					y = 0;
				}
				else
				{
					if (s.Width > w)
						w = s.Width;
				}
				y += s.Height;
				if (y > yMax)
					yMax = y;
			}

			return new Size(wTot + w, yMax);
		}

		public int iCol { get; set; }
	}
}
