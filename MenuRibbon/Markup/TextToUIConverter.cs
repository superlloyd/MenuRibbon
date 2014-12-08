using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MenuRibbon.WPF.Markup
{
	// Go there for quick reference on interesting characters
	// http://msdn.microsoft.com/en-us/library/windows/apps/jj841126.aspx
	public class TextToUIConverter : IValueConverter
	{
		public TextToUIConverter()
		{
			FontSize = 13.333;
			FontFamily = SystemFonts.IconFontFamily;
			Foreground = SystemBrushes.ControlTextBrush;
		}

		public double FontSize { get; set; }
		public FontFamily FontFamily { get; set; }
		public Brush Foreground { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return DependencyProperty.UnsetValue;
			var text = value.ToString();
			var ff = FontFamily;
			if (parameter is string)
				ff = new FontFamily((string)parameter);
			return new TextBlock { 
				Text = text,
				Foreground = Foreground,
				FontFamily = ff,
				FontSize = FontSize,
			};
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
