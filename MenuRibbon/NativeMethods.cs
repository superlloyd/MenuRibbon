using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF
{
	internal static class NativeMethods
	{
		public enum BeepType : uint
		{
			/// <summary>
			/// A simple windows beep
			/// </summary>            
			SimpleBeep = 0xFFFFFFFF,
			/// <summary>
			/// A standard windows OK beep
			/// </summary>
			OK = 0x00,
			/// <summary>
			/// A standard windows Question beep
			/// </summary>
			Question = 0x20,
			/// <summary>
			/// A standard windows Exclamation beep
			/// </summary>
			Exclamation = 0x30,
			/// <summary>
			/// A standard windows Asterisk beep
			/// </summary>
			Asterisk = 0x40,
		}


		[DllImport("User32.dll", ExactSpelling = true)]
		private static extern bool MessageBeep(uint type);

		/// <summary>
		/// Method to call interop for system beep
		/// </summary>
		/// <remarks>Calls Windows to make computer beep</remarks>
		/// <param name="type">The kind of beep you would like to hear</param>
		public static void MessageBeep(BeepType type)
		{
			MessageBeep((uint)type);
		}
	}
}
