using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MenuRibbon.WPF.Utils;

namespace MenuRibbon.WPF
{
	/// <summary>
	/// This class track the currently focused element and fire a weak events when it changes.
	/// </summary>
	public class FocusTracker
	{
		/// <summary>
		/// Get a focus tracker for this thread
		/// </summary>
		public static FocusTracker Current 
		{
			get 
			{
				if (sCurrent == null)
					sCurrent = new FocusTracker();
				return sCurrent; 
			} 
		}
		[ThreadStatic]
		static FocusTracker sCurrent;

		private FocusTracker()
		{
			InputManager.Current.PreProcessInput += Current_PreProcessInput;
		}
		void Current_PreProcessInput(object sender, PreProcessInputEventArgs e)
		{
			var ev = e.StagingItem.Input.RoutedEvent;
			if (ev == Keyboard.PreviewLostKeyboardFocusEvent)
			{
			}
			else if (ev == Keyboard.PreviewGotKeyboardFocusEvent)
			{
				mFutureFocus = (IInputElement)e.StagingItem.Input.Source;
			}
			else if (ev == Keyboard.LostKeyboardFocusEvent)
			{
				FocusedElement = mFutureFocus;
			}
			else if (ev == Keyboard.GotKeyboardFocusEvent)
			{
				// just to be sure, update to last one
				//FocusedElement = Keyboard.FocusedElement; // could also do that
				FocusedElement = (IInputElement)e.StagingItem.Input.Source;
			}
		}

		/// <summary>
		/// The <see cref="IInputElement"/> which currently hold the keyboard focus
		/// </summary>
		public IInputElement FocusedElement
		{
			get { return mFocusedElement; }
			set
			{
				if (Equals(value, mFocusedElement))
					return;
				mFocusedElement = value;
				mFutureFocus = null;
				RaiseFocusedElementChanged();
			}
		}
		IInputElement mFocusedElement;
		IInputElement mFutureFocus;

		/// <summary>
		/// Weak event fired when the <see cref="FocusedElement"/> changes.
		/// </summary>
		public event EventHandler FocusedElementChanged
		{
			add { mFocusChangedHandlers.Add(value); }
			remove { mFocusChangedHandlers.Remove(value); }
		}
		WeakList<EventHandler> mFocusChangedHandlers = new WeakList<EventHandler>();

		void RaiseFocusedElementChanged()
		{
			foreach (var h in mFocusChangedHandlers)
				h(this, EventArgs.Empty);
		}
	}
}
