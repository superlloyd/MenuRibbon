using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF.Utils
{
	/// <summary>
	/// Interface implemented by collection holding weak reference to their items.
	/// <see cref="WeakCleanup()"/> should be called automatically on any collection update as well.
	/// </summary>
	public interface IWeakCollection
	{
		/// <summary>
		/// Call this to explicitly remove from the <see cref="IWeakCollection"/> all disposed items.
		/// </summary>
		void WeakCleanup();
	}
}
