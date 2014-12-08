using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace MenuRibbon.WPF.Markup
{
	public class ImageExtension : MarkupExtension
	{
		public ImageExtension(string uri)
		{
			Uri = uri;
			Width = 16.0;
			Height = 16;
		}

		public string Uri { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public override object ProvideValue(IServiceProvider sp)
		{
			var uc = (IUriContext)sp.GetService(typeof(IUriContext));
			if (uc != null)
			{
				var uri = new Uri(uc.BaseUri, Uri);
				var bi = new BitmapImage(uri);
				var img = new Image { Source = bi, Width = Width, Height = Height };
				return img;
			}
			return null;
		}
	}
}
