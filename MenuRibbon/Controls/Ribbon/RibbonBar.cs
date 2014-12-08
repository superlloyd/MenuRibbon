using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MenuRibbon.WPF.Controls.Ribbon
{
	#region StringCollecionConverter

	public class StringCollectionConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				return true;
			}
			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			string valueAsStr = value as string;
			if (valueAsStr != null)
			{
				string str = valueAsStr.Trim();
				if (str.Length == 0)
				{
					return null;
				}

				char ch = ',';
				if (culture != null)
				{
					ch = culture.TextInfo.ListSeparator[0];
				}
				string[] strings = str.Split(ch);
				StringCollection stringCollection = new StringCollection();
				foreach (string s in strings)
				{
					stringCollection.Add(s);
				}

				return stringCollection;
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
		{
			if (null == value)
			{
				throw new ArgumentNullException("value");
			}

			if (null == destinationType)
			{
				throw new ArgumentNullException("destinationType");
			}

			StringCollection stringCollectionValue = value as StringCollection;
			if (stringCollectionValue != null)
			{
				if (destinationType == typeof(string))
				{
					char ch = ',';
					if (culture != null)
					{
						ch = culture.TextInfo.ListSeparator[0];
					}
					StringBuilder sb = new StringBuilder();
					int count = stringCollectionValue.Count;
					for (int i = 0; i < count; i++)
					{
						if (i != 0)
						{
							sb.Append(ch);
						}
						sb.Append(stringCollectionValue[i]);
					}
					return sb.ToString();
				}
			}

			// Pass unhandled cases to base class
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	#endregion

	public class RibbonBar : ItemsControl
	{
		static RibbonBar()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonBar), new FrameworkPropertyMetadata(typeof(RibbonBar)));
		}

		public RibbonBar()
		{
		}

		[TypeConverterAttribute(typeof(StringCollectionConverter))]
		public StringCollection GroupSizeReductionOrder { get; set; }

		#region ItemsControl override

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new RibbonGroup();
		}
		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is RibbonGroup;
		}
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);
		}
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);
		}

		#endregion
	}
}
