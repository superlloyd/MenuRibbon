using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace MenuRibbon.WPF.Markup
{
    public class ThicknessConverter : IValueConverter
    {
        public double? Top { get; set; }
        public double? Bottom { get; set; }
        public double? Left { get; set; }
        public double? Right { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness thick)
            {
                if (Top.HasValue)
                    thick.Top = Top.Value;
                if (Bottom.HasValue)
                    thick.Bottom = Bottom.Value;
                if (Left.HasValue)
                    thick.Left = Left.Value;
                if (Right.HasValue)
                    thick.Right = Right.Value;
                return thick;
            }
            return value;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
