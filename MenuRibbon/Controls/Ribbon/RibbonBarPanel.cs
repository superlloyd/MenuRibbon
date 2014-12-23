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

		#region CachedGroupSizes...

		/// <summary>
		/// Whether the group size in this panel is calculated only once. And use a cache value for next resize.
		/// </summary>
		public bool UseCachedGroupSize
		{
			get { return (bool)GetValue(UseCachedGroupSizeProperty); }
			set { SetValue(UseCachedGroupSizeProperty, BooleanBoxes.Box(value)); }
		}

		public static readonly DependencyProperty UseCachedGroupSizeProperty = DependencyProperty.Register(
			"UseCachedGroupSize", typeof(bool), typeof(RibbonBarPanel), new PropertyMetadata(BooleanBoxes.TrueBox));

		public void ClearCachedGroupSize()
		{
			foreach (UIElement uic in this.Children)
			{
				var ls = GetCachedSizes(uic);
				if (ls != null)
					ls.Clear();
			}
		}

		public static List<double> GetCachedSizes(DependencyObject obj)
		{
			return (List<double>)obj.GetValue(CachedSizesProperty);
		}

		public static void SetCachedSizes(DependencyObject obj, List<double> value)
		{
			obj.SetValue(CachedSizesProperty, value);
		}

		/// <summary>
		/// Cached group size for each size level. Use the value in this array is using cache.
		/// </summary>
		public static readonly DependencyProperty CachedSizesProperty =
			DependencyProperty.RegisterAttached("CachedSizes", typeof(List<double>), typeof(RibbonBarPanel), new PropertyMetadata(null));

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

		// reuse the same arrays to minimize memory allocation
		List<double> previousSizes = new List<double>();
		List<int> selectedGroupSizes = new List<int>();

		protected override Size MeasureOverride(Size availableSize)
		{
			var W = availableSize.Width;
			var H = availableSize.Height;
			if (double.IsInfinity(H)) H = (double)Menu.MenuRibbon.RibbonHeightProperty.DefaultMetadata.DefaultValue;

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

			var hInf = new Size(double.MaxValue, H);
			Func<UIElement, int, double> getWidth = (ui, i) =>
			{
				var rg = ui as RibbonGroup;
				if (UseCachedGroupSize && rg != null)
				{
					var l = GetCachedSizes(ui);
					if (l == null) SetCachedSizes(ui, l = new List<double>());
					while (l.Count <= i) l.Add(-1);
					var w = l[i];
					if (w < 0)
					{
						SetGroupSizeIndex(rg, i);
						ui.Measure(hInf);
						l[i] = w = ui.DesiredSize.Width;
					}
					return w;
				}
				else
				{
					ui.Measure(hInf);
					return ui.DesiredSize.Width;
				}
			};

			// calculate original size
			double total = 0;
			int MaxSR = 0;
			int N = 0;
			previousSizes.Clear();
			selectedGroupSizes.Clear();
			foreach (var uic in children)
			{
				var rg = uic as RibbonGroup;
				if (rg != null && rg.GroupSizeDefinitions != null && rg.GroupSizeDefinitions.Count > 0)
				{
					N++;
					if (rg.GroupSizeDefinitions.Count > MaxSR)
						MaxSR = rg.GroupSizeDefinitions.Count;
				}

				var w = getWidth(uic, 0);
				total += w;
				selectedGroupSizes.Add(0);
				previousSizes.Add(w);
			}

			for (int SR = 1; SR < MaxSR; SR++)
			{
				if (total <= availableSize.Width)
					break;
				int iC = 0;
				foreach (var uic in children)
				{
					if (total <= availableSize.Width)
						break;
					var rg = uic as RibbonGroup;
					if (rg != null && rg.GroupSizeDefinitions != null && rg.GroupSizeDefinitions.Count > SR)
					{
						selectedGroupSizes[iC] = SR;
						var prev = previousSizes[iC];
						var w = getWidth(uic, SR);
						previousSizes[iC] = w;
						total += w - prev;
					}
					iC++;
				}
			}

			// Measure() need be called on final setting, otherwise "sometimes" there are artifacts ...
			if (UseCachedGroupSize)
			{
				var iC = 0;
				foreach (var uic in children)
				{
					if (uic is RibbonGroup)
					{
						SetGroupSizeIndex((RibbonGroup)uic, selectedGroupSizes[iC]);
						uic.Measure(hInf);
					}
					iC++;
				}
			}

			return new Size(total, H);
		}
	}
}
