using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MenuRibbon.WPF
{
	public enum KeyTipState
	{
		None,
		Completing,
	}

	public class KeyTipService
	{
		#region ctor(), Current

		private KeyTipService()
		{
			InputManager.Current.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
			InputManager.Current.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
		}
		public static KeyTipService Current
		{
			get
			{
				if (sCurrent == null)
					sCurrent = new KeyTipService();
				return sCurrent;
			}
		}

		// PROBLEM leaks, register with InputManager, never unregister
		[ThreadStatic]
		static KeyTipService sCurrent;

		#endregion

		internal MenuRibbon.WPF.Controls.Menu.MenuRibbon MenuRibbon { get; set; }

		#region PreProcessInput(), PostProcessInput(), IsKeyTipKey(), IsKeyTipClosingKey()

		private void PreProcessInput(object sender, PreProcessInputEventArgs e)
		{
			if (MenuRibbon == null)
				return;

			var ev = e.StagingItem.Input.RoutedEvent;
			if (ev == Keyboard.PreviewKeyUpEvent)
			{
				if (IsKeyTipKey((KeyEventArgs)e.StagingItem.Input))
				{
					var ns = State == KeyTipState.None ? KeyTipState.Completing : KeyTipState.None;
					State = ns;
					switch (ns)
					{
						case KeyTipState.None:
							RestoreFocusScope();
							break;
						default:
							CaptureFocusScope();
							break;
					}
					e.StagingItem.Input.Handled = true;
				}
				else if (IsKeyTipClosingKey((KeyEventArgs)e.StagingItem.Input))
				{
					State = KeyTipState.None;
				}
			}
			else if (ev == TextCompositionManager.PreviewTextInputEvent)
			{
				// handle key tips
			}
			else if (ev == Mouse.PreviewMouseWheelEvent)
			{
			}
			else if (ev == Mouse.PreviewMouseDownEvent || ev == Stylus.PreviewStylusDownEvent)
			{
				State = KeyTipState.None;
			}
			else if (ev == Keyboard.PreviewGotKeyboardFocusEvent)
			{
				//Console.WriteLine("Got Focus to " + e.StagingItem.Input.Source);
			}
			else if (ev == Keyboard.PreviewLostKeyboardFocusEvent)
			{
				//Console.WriteLine("Lost Focus to " + e.StagingItem.Input.Source);
			}
		}

		private void PostProcessInput(object sender, ProcessInputEventArgs e)
		{
		}

		private static bool IsKeyTipKey(KeyEventArgs e)
		{
			return ((e.Key == Key.System) &&
				(e.SystemKey == Key.RightAlt ||
				e.SystemKey == Key.LeftAlt ||
				(e.SystemKey == Key.F10 &&
				(Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift &&
				(Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)));
		}
		private static bool IsKeyTipClosingKey(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Space:
				case Key.Enter:
				case Key.Left:
				case Key.Right:
				case Key.Up:
				case Key.Down:
				case Key.PageDown:
				case Key.PageUp:
				case Key.Home:
				case Key.End:
				case Key.Tab:
					return true;
			}
			return false;
		}

		#endregion	

		#region KeyTip

		WeakSet<DependencyObject> targets = new WeakSet<DependencyObject>();

		public static readonly DependencyProperty KeyTipProperty = DependencyProperty.RegisterAttached(
			"KeyTip", typeof(string), typeof(KeyTipService)
			, new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnKeyTipChanged))
			);

		public static string GetKeyTip(DependencyObject element)
		{
			if (element == null)
				throw new ArgumentNullException("element");
			return (string)element.GetValue(KeyTipProperty);
		}

		public static void SetKeyTip(DependencyObject element, string value)
		{
			if (element == null)
				throw new ArgumentNullException("element");
			element.SetValue(KeyTipProperty, value);
		}

		private static void OnKeyTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			string newKeyTip = (string)e.NewValue;
			string oldKeyTip = (string)e.OldValue;

			bool isNewEmpty = string.IsNullOrEmpty(newKeyTip);
			if (isNewEmpty != string.IsNullOrEmpty(oldKeyTip))
			{
				if (isNewEmpty)
				{
					Current.targets.Remove(d);
				}
				else
				{
					Current.targets.Add(d);
				}
			}
		}

		#endregion

		#region State, CaptureFocusScope(), RestoreFocusScope()

		public KeyTipState State
		{
			get { return mState; }
			private set
			{
				if (value == mState)
					return;
				mState = value;
			}
		}
		KeyTipState mState;

		public void RestoreFocusScope()
		{
			var w = Window.GetWindow(MenuRibbon);
			var scope = FocusManager.GetFocusScope(w);
			var t = FocusManager.GetFocusedElement(scope);
			if (t != null)
				Keyboard.Focus(t);
			else
				Keyboard.ClearFocus();
		}

		public void CaptureFocusScope()
		{
			if (MenuRibbon.Items.Count > 0)
			{
				var t = (IInputElement)MenuRibbon.ItemContainerGenerator.ContainerFromItem(MenuRibbon.Items[0]);
				FocusManager.SetIsFocusScope(MenuRibbon, true);
				Keyboard.Focus(t);
				MenuRibbon.PopupManager.IsResponsive = true;
			}
		}

		#endregion
	}
}
