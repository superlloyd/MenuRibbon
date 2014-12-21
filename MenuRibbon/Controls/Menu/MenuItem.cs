using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using MenuRibbon.WPF;

namespace MenuRibbon.WPF.Controls.Menu
{
	[TemplatePart(Name = "PART_Header", Type = typeof(FrameworkElement))]
	public class MenuItem : BasePopupItem
	{
		static MenuItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(typeof(MenuItem)));
		}
		public MenuItem()
		{
		}


		public string InputGestureText
		{
			get { return (string)GetValue(InputGestureTextProperty); }
			set { SetValue(InputGestureTextProperty, value); }
		}

		public static readonly DependencyProperty InputGestureTextProperty = DependencyProperty.Register(
			"InputGestureText", typeof(string), typeof(MenuItem)
			, new PropertyMetadata(
				string.Empty,
				(o, e) => ((MenuItem)o).OnInputGestureTextChanged((string)e.OldValue, (string)e.NewValue),
				new CoerceValueCallback((o, val) => ((MenuItem)o).OnCoerceInputGestureText((string)val))
			));

		void OnInputGestureTextChanged(string OldValue, string NewValue)
		{
		}

		string OnCoerceInputGestureText(string value)
		{
			RoutedCommand c;
			if (string.IsNullOrEmpty(value) && (c = Command as RoutedCommand) != null)
			{
				var col = c.InputGestures;
				if ((col != null) && (col.Count >= 1))
				{
					for (int i = 0; i < col.Count; i++)
					{
						var kg = ((System.Collections.IList)col)[i] as KeyGesture;
						if (kg != null)
						{
							return kg.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
						}
					}
				}
			}

			return value;
		}
	}
}
