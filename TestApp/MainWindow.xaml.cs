using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestApp
{
	public class AAA
	{
		public string Image { get; set; }
		public string Header { get; set; }
		public string KeyTip { get; set; }
		public string Shortcut { get; set; }
		public List<AAA> Children { get; set; }
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		public MainWindow()
		{
			RandomList = new List<AAA>
			{
				new AAA { Header = "One", Shortcut = "F2" },
				new AAA { Header = "Two", Children = new List<AAA> {
						new AAA { Header = "Ahaha" },
						new AAA { Header = "Ahaha" },
					}
				},
				new AAA { Header = "Two", Shortcut = "Ctrl+F1", Children = new List<AAA> {
						new AAA { Header = "Ahaha" },
						new AAA { Header = "Ahaha" },
					}
				},
			};

			InitializeComponent();
		}
		public List<AAA> RandomList { get; set; }

		private void Button_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
