using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
	public class Hello : INotifyPropertyChanged
	{
		public Hello()
		{
			Greetings = "Hello, World!";
		}

		#region Greetings

		public string Greetings
		{
			get { return mGreetings; }
			set
			{
				if (Equals(value, mGreetings))
					return;
				mGreetings = value;
				OnPropertyChanged();
			}
		}
		string mGreetings;

		#endregion

		void OnPropertyChanged([CallerMemberName]string name = null)
		{
			var e = PropertyChanged;
			if (e != null)
				e(this, new PropertyChangedEventArgs(name));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
