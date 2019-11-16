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

using MenuRibbon.WPF;
using MenuRibbon.WPF.Controls;
using MenuRibbon.WPF.Utils;

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
				new AAA { Header = "One", Shortcut = "F2", KeyTip = "A" },
				new AAA { Header = "Two", KeyTip = "B", Children = new List<AAA> {
						new AAA { Header = "Ahaha", KeyTip = "C" },
						new AAA { Header = "Ahaha", KeyTip = "D" },
					}
				},
				new AAA { Header = "Two", Shortcut = "Ctrl+F1", KeyTip = "E", Children = new List<AAA> {
						new AAA { Header = "Ahaha", KeyTip = "F" },
						new AAA { Header = "Ahaha", KeyTip = "G" },
					}
				},
			};

			InitializeComponent();
			AutoGenerateKeyTips();
		}

		public List<AAA> RandomList { get; set; }

		private void Button_Click(object sender, RoutedEventArgs e)
		{

		}

		public void AutoGenerateKeyTips()
		{
			int total = AutoGenerateKeyTips(this);
			Console.WriteLine("{0} IPopupItem(s)", total);
		}
		int AutoGenerateKeyTips(DependencyObject dp)
		{
			int res = 0;
			int i = 0;
			Action<DependencyObject> setKP = dp2 =>
			{
				if (dp2.HasDefaultValue(MenuRibbon.WPF.Controls.KeyTipService.KeyTipProperty))
				{
                    MenuRibbon.WPF.Controls.KeyTipService.SetKeyTip(dp2, string.Format("{0:00}", i++));
					res++;
				}
			};

			dp.LogicalChildren(x => !(x is IPopupItem))
				.Where(x => x is IPopupItem)
				.ForEach(x => {
					setKP(x);
					res += AutoGenerateKeyTips(x);
				});

			return res;
		}
	}
}
