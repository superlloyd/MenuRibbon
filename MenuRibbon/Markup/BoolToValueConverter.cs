using System;
using System.Windows.Data;

namespace MenuRibbon.WPF.Markup
{
	public class BoolToValueConverter : IValueConverter
	{
		public object TrueValue { get; set; }
		public object FalseValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (Equals(value, true))
				return TrueValue;
			return FalseValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Equals(value, TrueValue);
		}
	}
}
