using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MenuRibbon.WPF.Utils;
using System.Windows.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Markup;

namespace MenuRibbon.WPF
{
	public class KeyTipService
	{
		#region ctor(), Current

		private KeyTipService()
		{
			InputManager.Current.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
			InputManager.Current.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
			State = KeyTipState.None;
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
		[ThreadStatic]
		static KeyTipService sCurrent;

		#endregion

		#region Input: PreProcessInput(), PostProcessInput(), IsKeyTipKey(), IsKeyTipClosingKey()

		private void PreProcessInput(object sender, PreProcessInputEventArgs e)
		{
			if (State == KeyTipState.None)
				return;

			var ev = e.StagingItem.Input.RoutedEvent;
			if (ev == Keyboard.PreviewKeyUpEvent)
			{
				KeyEventArgs args = (KeyEventArgs)e.StagingItem.Input;
				if (IsKeyTipKey(args))
					ShowKeyTips();
			}
			else if (ev == Mouse.PreviewMouseDownEvent || ev == Stylus.PreviewStylusDownEvent)
			{
				LeaveKeyTipMode(false);
			}
			else if (ev == TextCompositionManager.PreviewTextInputEvent)
			{
				OnPreviewTextInput((TextCompositionEventArgs)e.StagingItem.Input);
			}
			else if (ev == Keyboard.PreviewKeyDownEvent)
			{
				KeyEventArgs args = (KeyEventArgs)e.StagingItem.Input;
				if (IsKeyTipClosingKey(args))
					LeaveKeyTipMode(false);
			}
			else if (ev == Mouse.PreviewMouseWheelEvent)
			{
				e.StagingItem.Input.Handled = true;
			}
		}

		private void PostProcessInput(object sender, ProcessInputEventArgs e)
		{
			if (e.StagingItem.Input.RoutedEvent == Keyboard.KeyDownEvent)
			{
				var args = (KeyEventArgs)e.StagingItem.Input;
				if (args.Handled)
					return;

				if (State == KeyTipState.None && IsKeyTipKey(args))
				{
					var src = args.OriginalSource as DependencyObject;
					if (EnterKeyTipMode(src.VisualHierarchy().Last(), args.Key != Key.F10))
					{
						args.Handled = true;
					}
				}
				else if (State != KeyTipState.None && args.Key == Key.Escape)
				{
					PopKeyTipScope();
					args.Handled = true;
				}
			}
		}

		private static bool IsKeyTipKey(KeyEventArgs e)
		{
			if (e.Key != Key.System)
				return false;
			Predicate<ModifierKeys> isMod = x => (Keyboard.Modifiers & x) != x;
			switch (e.SystemKey)
			{
				case Key.LeftAlt:
				case Key.RightAlt:
					return true;
				case Key.F10:
					return !isMod(ModifierKeys.Shift) && !isMod(ModifierKeys.Control);
			}
			return false;
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

		#region state management: State, EnterKeyTipMode() [Start]ShowKeyTips[Timer|ForScope|ForElement]() PopKeyTipScope() LeaveKeyTipMode()

		enum KeyTipState
		{
			None,
			Pending,
			Enabled
		}

		KeyTipState State { get; set; }

		private const int ShowKeyTipsWaitTime = 500;

		Stack<DependencyObject> _scopeStack = new Stack<DependencyObject>();
		DependencyObject globalScope;

		#region CurrentWindow

		Window CurrentWindow
		{
			get { return mCurrentWindow; }
			set
			{
				if (Equals(value, mCurrentWindow))
					return;

				EventHandler eh = (o, e) => LeaveKeyTipMode();
				SizeChangedEventHandler sh = (o, e) => LeaveKeyTipMode();
				if (mCurrentWindow != null)
				{
					mCurrentWindow.Deactivated -= eh;
					mCurrentWindow.LocationChanged -= eh;
					mCurrentWindow.SizeChanged -= sh;
				}
				mCurrentWindow = value;
				if (mCurrentWindow != null)
				{
					mCurrentWindow.Deactivated += eh;
					mCurrentWindow.LocationChanged += eh;
					mCurrentWindow.SizeChanged += sh;
				}
			}
		}
		Window mCurrentWindow;

		#endregion

		private bool EnterKeyTipMode(DependencyObject scope, bool showAsync)
		{
			if (scope == null) return false;

			ProcessScoping();
			if (!scopeToElementMap.ContainsKey(scope))
				return false;

			globalScope = scope;
			CurrentWindow = Window.GetWindow(scope);
			State = KeyTipState.Pending;

			if (showAsync)
			{
				StartShowKeyTipsTimer();
			}
			else
			{
				ShowKeyTips();
			}
			return true;
		}

		private void StartShowKeyTipsTimer()
		{
			if (_showKeyTipsTimer == null)
			{
				_showKeyTipsTimer = new DispatcherTimer(DispatcherPriority.Normal);
				_showKeyTipsTimer.Interval = TimeSpan.FromMilliseconds(ShowKeyTipsWaitTime);
				_showKeyTipsTimer.Tick += delegate(object sender, EventArgs e) { ShowKeyTips(); };
			}
			_showKeyTipsTimer.Start();
		}
		DispatcherTimer _showKeyTipsTimer;

		private void ShowKeyTips()
		{
			if (_showKeyTipsTimer != null)
			{
				_showKeyTipsTimer.Stop();
			}
			if (State == KeyTipState.Pending)
			{
				Debug.Assert(globalScope != null);
				if (ShowKeyTipsForScope(globalScope))
				{
					State = KeyTipState.Enabled;
					RaiseKeyTipEnterFocus();
				}
				else
				{
					LeaveKeyTipMode();
				}
			}

		}

		private bool ShowKeyTipsForScope(DependencyObject scopeElement, bool pushOnEmpty = false)
		{
			bool returnValue = false;
			ProcessScoping();
			if (scopeToElementMap.ContainsKey(scopeElement))
			{
				var elementSet = scopeToElementMap[scopeElement];
				// TODO add a method or some code to auto generate KeyTips for ItemsControl
				//AutoGenerateKeyTips(elementSet);
				if (elementSet != null && elementSet.Count > 0)
				{
					foreach (DependencyObject element in elementSet)
					{
						returnValue |= ShowKeyTipForElement(element);
					}
				}

				// KeyTips might have been dismissed during
				// show, hence check again.
				if (State != KeyTipState.None)
				{
					if (_scopeStack.Count == 0 ||
						_scopeStack.Peek() != scopeElement)
					{
						_scopeStack.Push(scopeElement);
					}
				}
			}
			else if (pushOnEmpty)
			{
				// Push the scope even if it is empty.
				// Used for any non-global scope.
				if (_scopeStack.Count == 0 ||
					_scopeStack.Peek() != scopeElement)
				{
					_scopeStack.Push(scopeElement);
				}
			}
			return returnValue;
		}

		bool ShowKeyTipForElement(DependencyObject element)
		{
			if (State == KeyTipState.None)
			{
				return false;
			}

			bool returnValue = false;
			if (!string.IsNullOrEmpty(GetKeyTip(element)))
			{
				returnValue = true;
				element.SetValue(ShowingKeyTipProperty, true);
			}

			return returnValue;
		}

		private void PopKeyTipScope()
		{
			Debug.Assert(_scopeStack.Count > 0);
			var currentScope = _scopeStack.Pop();
			var parentScope = FindScope(currentScope, false);
			var stackParentScope = (_scopeStack.Count > 0 ? _scopeStack.Peek() : null);
			if (stackParentScope != null &&
				parentScope != null &&
				parentScope.VisualHierarchy().Skip(1).Contains(stackParentScope))
			{
				// If there is any intermediate ancestral scope between current scope
				// and the next scope in stack, then push it onto stack.
				_scopeStack.Push(parentScope);
			}
			HideCurrentShowingKeyTips();
			_prefixText = string.Empty;

			if (_scopeStack.Count > 0)
			{
				// Dispatcher operation to show KeyTips for topmost
				// scope on the stack.
				Dispatcher.CurrentDispatcher.BeginInvoke(
					(Action)delegate()
					{
						if (_scopeStack.Count > 0)
						{
							ShowKeyTipsForScope(_scopeStack.Peek(), true);
						}
					},
					DispatcherPriority.Loaded);
			}
			else
			{
				LeaveKeyTipMode();
			}
		}

		private void LeaveKeyTipMode(bool restoreFocus = true)
		{
			HideCurrentShowingKeyTips();
			//ResetAutoGeneratedKeyTips();
			if (restoreFocus)
			{
				RaiseKeyTipExitRestoreFocus();
			}
			Reset();
		}

		private void Reset()
		{
			_cultureCache = null;
			globalScope = null;
			CurrentWindow = null;
			if (_showKeyTipsTimer != null) _showKeyTipsTimer.Stop();
			_prefixText = string.Empty;
			_currentActiveKeyTipElements.Clear();
			_scopeStack.Clear();
			State = KeyTipState.None;
		}

		#endregion

		List<DependencyObject> _currentActiveKeyTipElements = new List<DependencyObject>();

		#region OnPreviewTextInput()

		string _prefixText = "";

		private void OnPreviewTextInput(TextCompositionEventArgs args)
		{
			string text = args.Text;
			if (string.IsNullOrEmpty(text)) text = args.SystemText;
			if (string.IsNullOrWhiteSpace(text))
				return;

			if (State == KeyTipState.Pending)
				ShowKeyTips();

			if (_currentActiveKeyTipElements.Count > 0)
			{
				args.Handled = true;
				text = _prefixText + text;

				DependencyObject exactMatchElement = null;
				List<DependencyObject> activeKeyTipElements = FindKeyTipMatches(text, out exactMatchElement);
				if (exactMatchElement != null)
				{
					OnKeyTipExactMatch(exactMatchElement);
				}
				else
				{
					OnKeyTipPartialMatch(activeKeyTipElements, text);
				}
			}
		}

		private List<DependencyObject> FindKeyTipMatches(string text, out DependencyObject exactMatchElement)
		{
			exactMatchElement = null;
			List<DependencyObject> activeKeyTipElements = null;
			foreach (DependencyObject element in _currentActiveKeyTipElements)
			{
				string keyTip = GetKeyTip(element);
				CultureInfo culture = GetCultureForElement(element);
				if (string.Compare(keyTip, text, true, culture) == 0)
				{
					exactMatchElement = element;
					return null;
				}
				else if (keyTip.StartsWith(text, true, culture))
				{
					if (activeKeyTipElements == null)
					{
						activeKeyTipElements = new List<DependencyObject>();
					}
					activeKeyTipElements.Add(element);
				}
			}
			return activeKeyTipElements;
		}

		Dictionary<XmlLanguage, CultureInfo> _cultureCache = null;
		internal static CultureInfo GetCultureForElement(DependencyObject element)
		{
			CultureInfo culture = CultureInfo.CurrentCulture;
			if (DependencyPropertyHelper.GetValueSource(element, FrameworkElement.LanguageProperty).BaseValueSource != BaseValueSource.Default)
			{
				XmlLanguage language = (XmlLanguage)element.GetValue(FrameworkElement.LanguageProperty);
				if (language != null && language != XmlLanguage.Empty)
				{
					Dictionary<XmlLanguage, CultureInfo> cultureCache = Current._cultureCache;
					if (cultureCache != null && cultureCache.ContainsKey(language))
					{
						culture = cultureCache[language];
					}
					else
					{
						CultureInfo computedCulture = element.GetCultureInfo();
						if (computedCulture != null)
						{
							culture = computedCulture;
							if (cultureCache == null)
							{
								Current._cultureCache = cultureCache = new Dictionary<XmlLanguage, CultureInfo>();
							}
							cultureCache[language] = culture;
						}
					}
				}
			}
			return culture;
		}

		static void Beep()
		{
			NativeMethods.MessageBeep(NativeMethods.BeepType.OK);
		}

		private void OnKeyTipExactMatch(DependencyObject exactMatchElement)
		{
			if (!((bool)(exactMatchElement.GetValue(UIElement.IsEnabledProperty))))
			{
				Beep();
				return;
			}

			HideCurrentShowingKeyTips();
			_prefixText = string.Empty;

			// KeyTips might have been dismissed by one of the event handlers
			// hence check again.
			if (State == KeyTipState.None)
				return;

			var oldFocus = Keyboard.FocusedElement;

			var args = new KeyTipAccessedEventArgs();
			args.RoutedEvent = PreviewKeyTipAccessedEvent;
			IInputElement inputElement = exactMatchElement as IInputElement;
			if (inputElement != null)
			{
				inputElement.RaiseEvent(args);
				args.RoutedEvent = KeyTipAccessedEvent;
				inputElement.RaiseEvent(args);
			}


			object newFocus = Keyboard.FocusedElement;
			DependencyObject newScope = args.TargetKeyTipScope;
			if (newScope != null && !KeyTipService.GetIsKeyTipScope(newScope) && newScope != globalScope)
				throw new InvalidOperationException();

			if (newScope == null && KeyTipService.GetIsKeyTipScope(exactMatchElement))
				newScope = exactMatchElement;

			if (newScope != null)
			{
				// Show KeyTips for new scope in a dispatcher operation.
				Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() => ShowKeyTipsForScope(newScope, true)), DispatcherPriority.Loaded);
			}
			else
			{
				LeaveKeyTipMode(oldFocus == newFocus /*restoreFocus*/);
			}
		}

		private void OnKeyTipPartialMatch(List<DependencyObject> activeKeyTipElements, string text)
		{
			if (activeKeyTipElements == null ||
				activeKeyTipElements.Count == 0)
			{
				// Beep when there are no matches.
				Beep();
				return;
			}

			// Hide KeyTips for all the elements which do not
			// match with the new prefix.
			int j = 0;
			DependencyObject newActiveElement = activeKeyTipElements[j++];
			for (int i = 0; i < _currentActiveKeyTipElements.Count; i++)
			{
				DependencyObject currentActiveElement = _currentActiveKeyTipElements[i];
				if (currentActiveElement == newActiveElement)
				{
					newActiveElement = j < activeKeyTipElements.Count ? activeKeyTipElements[j++] : null;
				}
				else
				{
					currentActiveElement.ClearValue(ShowingKeyTipProperty);
				}
			}
			_currentActiveKeyTipElements = activeKeyTipElements;
			_prefixText = text;
		}

		#endregion

		#region ShowingKeyTipProperty

		private static readonly DependencyProperty ShowingKeyTipProperty =
			DependencyProperty.RegisterAttached("ShowingKeyTip", typeof(bool), typeof(KeyTipService),
				new UIPropertyMetadata(false, new PropertyChangedCallback(OnShowingKeyTipChanged)));

		private void HideCurrentShowingKeyTips()
		{
			foreach (DependencyObject element in _currentActiveKeyTipElements)
			{
				element.ClearValue(ShowingKeyTipProperty);
			}
			_currentActiveKeyTipElements.Clear();
		}

		private static void OnShowingKeyTipChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
		{
#if KEY_TIP_IMPLEMENTED
			if ((bool)e.NewValue)
			{
				UIElement uie = RibbonHelper.GetContainingUIElement(element);
				if (uie != null &&
					uie.Visibility != Visibility.Visible)
				{
					// revert the value if the element is not visible
					element.SetValue(ShowingKeyTipProperty, false);
					return;
				}

				// Raise the ActivatingKeyTip event.
				ActivatingKeyTipEventArgs activatingEventArgs = new ActivatingKeyTipEventArgs();
				IInputElement inputElement = element as IInputElement;
				if (inputElement != null)
				{
					inputElement.RaiseEvent(activatingEventArgs);
				}

				// KeyTips could have been dismissed due to one
				// of the event handler, hence check again.
				KeyTipService current = Current;
				if (current.State != KeyTipState.None)
				{
					if (activatingEventArgs.KeyTipVisibility == Visibility.Visible)
					{
						// Create the keytip and add it as the adorner.
						UIElement adornedElement = RibbonHelper.GetContainingUIElement(activatingEventArgs.PlacementTarget == null ? element : activatingEventArgs.PlacementTarget);
						if (adornedElement != null && adornedElement.IsVisible)
						{
							bool isScrollAdornerLayer = false;
							AdornerLayer adornerLayer = GetAdornerLayer(adornedElement, out isScrollAdornerLayer);
							if (adornerLayer != null)
							{
								KeyTipAdorner adorner = new KeyTipAdorner(adornedElement,
									activatingEventArgs.PlacementTarget,
									activatingEventArgs.KeyTipHorizontalPlacement,
									activatingEventArgs.KeyTipVerticalPlacement,
									activatingEventArgs.KeyTipHorizontalOffset,
									activatingEventArgs.KeyTipVerticalOffset,
									activatingEventArgs.Handled ? null : activatingEventArgs.OwnerRibbonGroup);
								LinkKeyTipControlToAdorner(adorner, element);
								adornerLayer.Add(adorner);
								element.SetValue(KeyTipAdornerProperty, adorner);
								element.SetValue(KeyTipAdornerHolderProperty, adornedElement);

								// Begin invode an operation to nudge all the keytips into the
								// adorner layer boundary unless the layer belongs to a scroll viewer.
								if (!isScrollAdornerLayer)
								{
									current.EnqueueAdornerLayerForPlacementProcessing(adornerLayer);
								}
							}
						}
					}

					if (activatingEventArgs.KeyTipVisibility != Visibility.Collapsed)
					{
						// add the element to currentActiveKeyTipElement list.
						current._currentActiveKeyTipElements.Add(element);
					}
					else
					{
						// Revert the value if it is asked by event handlers
						// (i.e, by setting KeyTipVisibility to collapsed.
						element.SetValue(ShowingKeyTipProperty, false);
					}
				}
				else
				{
					// Revert the value if we already dismissed keytips.
					element.SetValue(ShowingKeyTipProperty, false);
				}
			}
			else
			{
				// Remove keytip from adorner.
				KeyTipAdorner adorner = (KeyTipAdorner)element.GetValue(KeyTipAdornerProperty);
				UIElement adornedElement = (UIElement)element.GetValue(KeyTipAdornerHolderProperty);
				if (adornedElement != null && adorner != null)
				{
					UnlinkKeyTipControlFromAdorner(adorner);
					bool isScrollAdornerLayer = false;
					AdornerLayer adornerLayer = GetAdornerLayer(adornedElement, out isScrollAdornerLayer);
					if (adornerLayer != null)
					{
						adornerLayer.Remove(adorner);
					}
				}
				element.ClearValue(KeyTipAdornerProperty);
				element.ClearValue(KeyTipAdornerHolderProperty);
			}
#endif
		}

		#endregion

		#region Events

		public static readonly RoutedEvent ActivatingKeyTipEvent = EventManager.RegisterRoutedEvent("ActivatingKeyTip", RoutingStrategy.Bubble, typeof(ActivatingKeyTipEventHandler), typeof(KeyTipService));

		public static void AddActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
		{
			element.AddHandler(ActivatingKeyTipEvent, handler);
		}

		public static void RemoveActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
		{
			element.RemoveHandler(ActivatingKeyTipEvent, handler);
		}

		public static readonly RoutedEvent PreviewKeyTipAccessedEvent = EventManager.RegisterRoutedEvent("PreviewKeyTipAccessed", RoutingStrategy.Tunnel, typeof(KeyTipAccessedEventHandler), typeof(KeyTipService));

		public static void AddPreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
		{
			element.AddHandler(PreviewKeyTipAccessedEvent, handler);
		}

		public static void RemovePreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
		{
			element.RemoveHandler(PreviewKeyTipAccessedEvent, handler);
		}

		public static readonly RoutedEvent KeyTipAccessedEvent = EventManager.RegisterRoutedEvent("KeyTipAccessed", RoutingStrategy.Bubble, typeof(KeyTipAccessedEventHandler), typeof(KeyTipService));

		public static void AddKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
		{
			element.AddHandler(KeyTipAccessedEvent, handler);
		}

		public static void RemoveKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
		{
			element.RemoveHandler(KeyTipAccessedEvent, handler);
		}

		#endregion

		#region Focus Events

		internal delegate bool KeyTipFocusEventHandler(object sender, EventArgs e);

		/// <summary>
		///     Event used to notify ribbon to obtain focus
		///     while entering KeyTip mode.
		/// </summary>
		internal event KeyTipFocusEventHandler KeyTipEnterFocus
		{
			add { elKeyTipEnterFocus.Add(value); }
			remove { elKeyTipEnterFocus.Remove(value); }
		}
		WeakList<KeyTipFocusEventHandler> elKeyTipEnterFocus = new WeakList<KeyTipFocusEventHandler>();

		void RaiseKeyTipEnterFocus()
		{
			var src = PresentationSource.FromDependencyObject(globalScope);
			if (src != null)
				foreach (var item in elKeyTipEnterFocus)
					item(src, EventArgs.Empty);
		}

		/// <summary>
		///     Event used to notify ribbon to restore focus
		///     while exiting KeyTip mode.
		/// </summary>
		internal event KeyTipFocusEventHandler KeyTipExitRestoreFocus
		{
			add { elKeyTipExitRestoreFocus.Add(value); }
			remove { elKeyTipExitRestoreFocus.Remove(value); }
		}
		WeakList<KeyTipFocusEventHandler> elKeyTipExitRestoreFocus = new WeakList<KeyTipFocusEventHandler>();

		void RaiseKeyTipExitRestoreFocus()
		{
			var src = PresentationSource.FromDependencyObject(globalScope);
			if (src != null)
				foreach (var item in elKeyTipExitRestoreFocus)
					item(src, EventArgs.Empty);
		}

		#endregion

		#region KeyTip, IsKeyTipScope, KeyTipStyle

		WeakSet<DependencyObject> mTargets = new WeakSet<DependencyObject>();

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
				// TODO scope update with less calculation
				RoutedEventHandler onLoadedChanged = (o, ev) => Current.scopeToElementMap.Clear();

				if (isNewEmpty)
				{
					Current.mTargets.Remove(d);
					d.RemoveLoadedHandler(onLoadedChanged, onLoadedChanged);
				}
				else
				{
					Current.mTargets.Add(d);
					d.AddLoadedHandler(onLoadedChanged, onLoadedChanged);
				}
				onLoadedChanged(null, null);
			}
		}

		public static bool GetIsKeyTipScope(DependencyObject element)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			return (bool)element.GetValue(IsKeyTipScopeProperty);
		}

		public static void SetIsKeyTipScope(DependencyObject element, bool value)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			element.SetValue(IsKeyTipScopeProperty, value);
		}

		public static readonly DependencyProperty IsKeyTipScopeProperty = DependencyProperty.RegisterAttached(
			"IsKeyTipScope", typeof(bool), typeof(KeyTipService), 
			new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, new PropertyChangedCallback(OnIsKeyTipScopeChanged)));

		private static void OnIsKeyTipScopeChanged(DependencyObject scopeElement, DependencyPropertyChangedEventArgs e)
		{
			bool newIsScope = (bool)e.NewValue;
			KeyTipService current = Current;
			// TODO scope update with less calculation
			Current.scopeToElementMap.Clear();
		}

		public static Style GetKeyTipStyle(DependencyObject element)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			return (Style)element.GetValue(KeyTipStyleProperty);
		}

		public static void SetKeyTipStyle(DependencyObject element, Style value)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			element.SetValue(KeyTipStyleProperty, value);
		}

		// Using a DependencyProperty as the backing store for KeyTipStyle.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty KeyTipStyleProperty =
			DependencyProperty.RegisterAttached("KeyTipStyle", typeof(Style), typeof(KeyTipService), new FrameworkPropertyMetadata(null));

		WeakDictionary<DependencyObject, WeakSet<DependencyObject>> scopeToElementMap = new WeakDictionary<DependencyObject, WeakSet<DependencyObject>>();

		static DependencyObject FindScope(DependencyObject obj, bool searchVisualTree = true)
		{
			var getNext = searchVisualTree ? (Func<DependencyObject, DependencyObject>)(x => x.VisualParent()) : (x => x.LogicalParent());
			var o = obj;
			while (true)
			{
				if (GetIsKeyTipScope(o))
					return o;
				var n = getNext(o);
				if (n == null)
					return o;
				o = n;
			}
		}

		void ProcessScoping()
		{
			// TODO scope update with less calculation
			if (scopeToElementMap.Count != 0)
				return;
			foreach (var t in mTargets)
			{
				var s = FindScope(t);
				var ws = scopeToElementMap[s];
				if (ws == null) scopeToElementMap[s] = ws = new WeakSet<DependencyObject>();
				ws.Add(t);
			}
		}

		#endregion
	}
}
