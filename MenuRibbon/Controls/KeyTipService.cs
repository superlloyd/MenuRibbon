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
using System.Windows.Documents;
using System.Windows.Controls;

namespace MenuRibbon.WPF.Controls
{
	/// <summary>
	/// Handler for event fired when KeyTip mode starts or end.
	/// </summary>
	public delegate void KeyTipFocusEventHandler(object sender, EventArgs e);

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

		#region state management: State, EnterKeyTipMode() (Start)ShowKeyTips(Timer)() PopKeyTipScope() LeaveKeyTipMode()

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
				mCurrentPresentationSource = null;
				mCurrentWindow = value;
				if (mCurrentWindow != null)
				{
					mCurrentPresentationSource = PresentationSource.FromVisual(mCurrentWindow);
					mCurrentWindow.Deactivated += eh;
					mCurrentWindow.LocationChanged += eh;
					mCurrentWindow.SizeChanged += sh;
				}
			}
		}
		Window mCurrentWindow;
		PresentationSource mCurrentPresentationSource;

		#endregion

		private bool EnterKeyTipMode(DependencyObject scope, bool showAsync)
		{
			if (scope == null) return false;

			var activated = ActivateMenuRibbon(scope);

			if (!scopeToElementMap.HasScope(scope))
				return activated;

			CurrentWindow = Window.GetWindow(scope);
			if (CurrentWindow == null)
				return activated;

			globalScope = scope;
			State = KeyTipState.Pending;
			InputManager.Current.PushMenuMode(mCurrentPresentationSource);

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
			if (_showKeyTipsTimer != null) _showKeyTipsTimer.Stop();

			if (State == KeyTipState.Pending)
			{
				Debug.Assert(globalScope != null);

				PopupManager.CloseAll();

				if (PushKeyTipsScope(globalScope))
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

		private bool PushKeyTipsScope(DependencyObject scopeElement, bool pushOnEmpty = false)
		{
			if (State == KeyTipState.None)
				return false;

			bool result = ShowScope(scopeElement);
			if (result || pushOnEmpty)
			{
				_scopeStack.Push(scopeElement);
			}
			return result;
		}
		private void PopKeyTipScope()
		{
			if (_scopeStack.Count == 0)
			{
				LeaveKeyTipMode();
				return;
			}

			var currentScope = _scopeStack.Pop();
			var parentScope = scopeToElementMap.FindScope(currentScope, false);
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
							ShowScope(_scopeStack.Peek());
						}
					},
					DispatcherPriority.Loaded);
			}
			else
			{
				LeaveKeyTipMode();
			}
		}
		private bool ShowScope(DependencyObject scope)
		{
			HideCurrentShowingKeyTips();
			_prefixText = string.Empty;

			bool returnValue = false;
			if (scopeToElementMap.HasScope(scope))
			{
				var elementSet = scopeToElementMap[scope];
				// TODO add a method or some code to auto generate KeyTips for ItemsControl
				//AutoGenerateKeyTips(elementSet);
				if (elementSet != null && elementSet.Count > 0)
				{
					foreach (DependencyObject element in elementSet)
					{
						returnValue |= ShowKeyTipForElement(element);
					}
				}
			}
			return returnValue;
		}

		private void LeaveKeyTipMode(bool restoreFocus = true)
		{
			if (State == KeyTipState.None)
				return;

			HideCurrentShowingKeyTips();
			//ResetAutoGeneratedKeyTips();
			if (restoreFocus)
			{
				Keyboard.Focus(null);
				RaiseKeyTipExitRestoreFocus();
			}


			InputManager.Current.PopMenuMode(mCurrentPresentationSource);
			CurrentWindow = null;
			State = KeyTipState.None;

			//_cultureCache = null;
			globalScope = null;
			if (_showKeyTipsTimer != null) _showKeyTipsTimer.Stop();
			_prefixText = string.Empty;
			_currentActiveKeyTipElements.Clear();
			_scopeStack.Clear();
			mKeyTipControlRecycler.Clear();
		}

		#endregion

		List<DependencyObject> _currentActiveKeyTipElements = new List<DependencyObject>();

		#region OnPreviewTextInput()

		string _prefixText = string.Empty;

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
			List<DependencyObject> result = null;
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
					if (result == null) 
						result = new List<DependencyObject>();
					result.Add(element);
				}
			}
			return result;
		}

		Dictionary<XmlLanguage, CultureInfo> _cultureCache = null;
		internal static CultureInfo GetCultureForElement(DependencyObject element)
		{
			if (DependencyPropertyHelper.GetValueSource(element, FrameworkElement.LanguageProperty).BaseValueSource == BaseValueSource.Default)
				return CultureInfo.CurrentCulture;

			XmlLanguage language = (XmlLanguage)element.GetValue(FrameworkElement.LanguageProperty);
			if (language == null || language == XmlLanguage.Empty)
				return CultureInfo.CurrentCulture;

			if (Current._cultureCache == null)
				Current._cultureCache = new Dictionary<XmlLanguage, CultureInfo>();

			CultureInfo result;
			if (!Current._cultureCache.TryGetValue(language, out result))
				Current._cultureCache[language] = result = language.GetCultureInfo() ?? CultureInfo.CurrentCulture;

			return result;
		}

		private void OnKeyTipExactMatch(DependencyObject exactMatchElement)
		{
			if (!((bool)(exactMatchElement.GetValue(UIElement.IsEnabledProperty))))
			{
				Menu.MenuRibbon.Beep();
				_prefixText = string.Empty;
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
				Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() => PushKeyTipsScope(newScope, true)), DispatcherPriority.Loaded);
			}
			else
			{
				LeaveKeyTipMode(oldFocus == newFocus /*restoreFocus*/);
			}
		}

		private void OnKeyTipPartialMatch(List<DependencyObject> activeKeyTipElements, string text)
		{
			if (activeKeyTipElements == null || activeKeyTipElements.Count == 0)
			{
				Menu.MenuRibbon.Beep();
				return;
			}

			// Hide KeyTips for all the elements which do not
			// match with the new prefix.
			int j = 0;
			DependencyObject newActiveElement = activeKeyTipElements[j++];
			for (int i = 0; i < _currentActiveKeyTipElements.Count; i++)
			{
				var elm = _currentActiveKeyTipElements[i];
				if (elm == newActiveElement)
				{
					newActiveElement = j < activeKeyTipElements.Count ? activeKeyTipElements[j++] : null;
				}
				else
				{
					elm.ClearValue(ShowingKeyTipProperty);
				}
			}
			_currentActiveKeyTipElements = activeKeyTipElements;
			_prefixText = text;
		}

		#endregion

		#region ShowingKeyTipProperty, ShowKeyTipsForScope(), ShowKeyTipForElement(), HideCurrentShowingKeyTips(), OnShowingKeyTipChanged()

		bool ShowKeyTipForElement(DependencyObject element)
		{
			if (State == KeyTipState.None)
			{
				return false;
			}

			bool returnValue = false;
			if (!string.IsNullOrEmpty(GetKeyTip(element)))
			{
				element.SetValue(ShowingKeyTipProperty, true);
				returnValue |= (bool)element.GetValue(ShowingKeyTipProperty);
			}

			return returnValue;
		}

		private void HideCurrentShowingKeyTips()
		{
			foreach (DependencyObject element in _currentActiveKeyTipElements)
			{
				element.ClearValue(ShowingKeyTipProperty);
			}
			_currentActiveKeyTipElements.Clear();
		}

		private static readonly DependencyProperty KeyTipAdornerProperty =
			DependencyProperty.RegisterAttached("KeyTipAdorner", typeof(KeyTipAdorner), typeof(KeyTipService), new UIPropertyMetadata(null));

		private static readonly DependencyProperty KeyTipAdornerHolderProperty =
			DependencyProperty.RegisterAttached("KeyTipAdornerHolder", typeof(UIElement), typeof(KeyTipService), new UIPropertyMetadata(null));

		private static readonly DependencyProperty ShowingKeyTipProperty = DependencyProperty.RegisterAttached(
			"ShowingKeyTip", typeof(bool), typeof(KeyTipService),
			new UIPropertyMetadata(BooleanBoxes.FalseBox, new PropertyChangedCallback(OnShowingKeyTipChanged)));

		private static void OnShowingKeyTipChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				var uie = (UIElement)element.LogicalHierarchy().FirstOrDefault(x => x is UIElement);
				if (uie != null && uie.Visibility != Visibility.Visible)
				{
					element.ClearValue(ShowingKeyTipProperty);
					return;
				}

				// Raise the ActivatingKeyTip event.
				var activatingEventArgs = new ActivatingKeyTipEventArgs();
				var inputElement = element as IInputElement;
				if (inputElement != null)
					inputElement.RaiseEvent(activatingEventArgs);

				// KeyTips could have been dismissed due to one
				// of the event handler, hence check again.
				KeyTipService current = Current;
				if (current.State == KeyTipState.None)
				{
					element.ClearValue(ShowingKeyTipProperty);
					return;
				}

				if (activatingEventArgs.KeyTipVisibility != Visibility.Visible)
				{
					element.ClearValue(ShowingKeyTipProperty);
					return;
				}

				// Create the KeyTip and add it as the adorner.
				AdornerLayer adornerLayer = null;
				var adornedElement = (UIElement)(activatingEventArgs.PlacementTarget ?? element).VisualHierarchy().FirstOrDefault(x => x is UIElement);
				if (adornedElement != null)
				{
					adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
					if (adornerLayer == null || !adornedElement.IsVisible)
					{
						element.ClearValue(ShowingKeyTipProperty);
						return;
					}
				}

				KeyTipAdorner adorner = new KeyTipAdorner(adornedElement,
					activatingEventArgs.PlacementTarget,
					activatingEventArgs.KeyTipHorizontalPlacement,
					activatingEventArgs.KeyTipVerticalPlacement,
					activatingEventArgs.KeyTipHorizontalOffset,
					activatingEventArgs.KeyTipVerticalOffset,
					activatingEventArgs.Handled ? null : activatingEventArgs.OwnerRibbonGroup);
				adorner.KeyTipControl = current.mKeyTipControlRecycler.Get();
				adorner.Element = element;
				adornerLayer.Add(adorner);
				element.SetValue(KeyTipAdornerProperty, adorner);
				element.SetValue(KeyTipAdornerHolderProperty, adornedElement);

				if (adorner.VisualHierarchy().FirstOrDefault(x => x is ScrollViewer) == null)
					current.EnqueueAdornerLayerForPlacementProcessing(adornerLayer);

				// add the element to currentActiveKeyTipElement list.
				current._currentActiveKeyTipElements.Add(element);
			}
			else
			{
				// Remove keytip from adorner.
				KeyTipAdorner adorner = (KeyTipAdorner)element.GetValue(KeyTipAdornerProperty);
				if (adorner != null)
				{
					Current.mKeyTipControlRecycler.Recycle(adorner.KeyTipControl);
					adorner.KeyTipControl = null;
				}

				UIElement adornedElement = (UIElement)element.GetValue(KeyTipAdornerHolderProperty);
				if (adornedElement != null && adorner != null)
				{
					AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
					if (adornerLayer != null)
						adornerLayer.Remove(adorner);
				}
				element.ClearValue(KeyTipAdornerProperty);
				element.ClearValue(KeyTipAdornerHolderProperty);
			}
		}
		ObjectRecycler<KeyTipControl> mKeyTipControlRecycler = new ObjectRecycler<KeyTipControl>(() => new KeyTipControl());

		Dictionary<AdornerLayer, bool> toBePlacementProcessed = new Dictionary<AdornerLayer, bool>();
		private void EnqueueAdornerLayerForPlacementProcessing(AdornerLayer adornerLayer)
		{
			toBePlacementProcessed[adornerLayer] = true;

			adornerLayer.Dispatcher.BeginInvoke(
				(Action)delegate()
				{
					foreach (var layer in toBePlacementProcessed.Keys)
					{
						foreach (var child in LogicalTreeHelper.GetChildren(layer))
						{
							var keyTipAdorner = child as KeyTipAdorner;
							if (keyTipAdorner != null)
							{
								keyTipAdorner.NudgeIntoAdornerLayerBoundary(layer);
							}
						}
					}
					toBePlacementProcessed.Clear();
				},
				DispatcherPriority.Input,
				null);
		}

		#endregion

		#region Events


		/// <summary>
		/// Event triggered when the KeyTip is shown. Targets should handle it by filling its argument with placement information required for the KeyTip.
		/// </summary>
		public static readonly RoutedEvent ActivatingKeyTipEvent = EventManager.RegisterRoutedEvent("ActivatingKeyTip", RoutingStrategy.Bubble, typeof(ActivatingKeyTipEventHandler), typeof(KeyTipService));

		public static void AddActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
		{
			element.AddHandler(ActivatingKeyTipEvent, handler);
		}

		public static void RemoveActivatingKeyTipHandler(DependencyObject element, ActivatingKeyTipEventHandler handler)
		{
			element.RemoveHandler(ActivatingKeyTipEvent, handler);
		}

		/// <summary>
		/// Preview event for <see cref="KeyTipAccessedEvent"/>.
		/// </summary>
		public static readonly RoutedEvent PreviewKeyTipAccessedEvent = EventManager.RegisterRoutedEvent("PreviewKeyTipAccessed", RoutingStrategy.Tunnel, typeof(KeyTipAccessedEventHandler), typeof(KeyTipService));

		public static void AddPreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
		{
			element.AddHandler(PreviewKeyTipAccessedEvent, handler);
		}

		public static void RemovePreviewKeyTipAccessedHandler(DependencyObject element, KeyTipAccessedEventHandler handler)
		{
			element.RemoveHandler(PreviewKeyTipAccessedEvent, handler);
		}

		/// <summary>
		/// Event triggered the KeyTip is fired / selected / matched by the user. Target should handle it, if a new (sub) scope is desired 
		/// the target should show it and set the <see cref="KeyTipAccessedEventArgs.TargetKeyTipScope"/> property.
		/// </summary>
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

		#region MenuRibbons & Focus Events

		WeakSet<Controls.Menu.MenuRibbon> menuRibbons = new WeakSet<Menu.MenuRibbon>();

		public void Register(Controls.Menu.MenuRibbon mr)
		{
			menuRibbons.Add(mr);
		}
		public void Unregister(Controls.Menu.MenuRibbon mr)
		{
			menuRibbons.Remove(mr);
		}

		bool ActivateMenuRibbon(DependencyObject from)
		{
			var rs = PresentationSource.FromDependencyObject(from);
			var t = menuRibbons
				.Where(x => PresentationSource.FromDependencyObject(x) == rs && x.FocusOnEnterKeyTip)
				.FirstOrDefault();
			if (t == null)
				return false;
			var ui = t.FirstFocusableElement();
			if (ui == null)
				return false;
			ui.Focus();
			return true;
		}


		/// <summary>
		///     Event used to notify ribbon to obtain focus
		///     while entering KeyTip mode.
		/// </summary>
		public event KeyTipFocusEventHandler KeyTipEnterFocus
		{
			add { elKeyTipEnterFocus.Add(value); }
			remove { elKeyTipEnterFocus.Remove(value); }
		}
		WeakList<KeyTipFocusEventHandler> elKeyTipEnterFocus = new WeakList<KeyTipFocusEventHandler>();

		void RaiseKeyTipEnterFocus()
		{
			var fe = Keyboard.FocusedElement as DependencyObject;
			if (fe == null)
				return;
			var src = PresentationSource.FromDependencyObject(fe);
			if (src == null)
				return;
			foreach (var item in elKeyTipEnterFocus)
				item(src, EventArgs.Empty);
		}

		/// <summary>
		///     Event used to notify ribbon to restore focus
		///     while exiting KeyTip mode.
		/// </summary>
		public event KeyTipFocusEventHandler KeyTipExitRestoreFocus
		{
			add { elKeyTipExitRestoreFocus.Add(value); }
			remove { elKeyTipExitRestoreFocus.Remove(value); }
		}
		WeakList<KeyTipFocusEventHandler> elKeyTipExitRestoreFocus = new WeakList<KeyTipFocusEventHandler>();

		void RaiseKeyTipExitRestoreFocus()
		{
			var fe = Keyboard.FocusedElement as DependencyObject;
			if (fe == null)
				return;
			var src = PresentationSource.FromDependencyObject(fe);
			if (src == null)
				return;
			foreach (var item in elKeyTipExitRestoreFocus)
				item(src, EventArgs.Empty);
		}

		#endregion

		#region KeyTip, IsKeyTipScope, KeyTipStyle, FindScope(), ProcessScoping()

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
			bool newHasValue = !string.IsNullOrEmpty((string)e.NewValue);
			bool oldHasValue = !string.IsNullOrEmpty((string)e.OldValue);
			if (newHasValue != oldHasValue)
			{
				RoutedEventHandler onLoadedChanged = (o, ev) =>
				{
					if (newHasValue && d.IsLoaded())
						Current.scopeToElementMap.AddItem(d);
					else
						Current.scopeToElementMap.RemoveItem(d);
				};

				if (newHasValue)
				{
					Current.mTargets.Add(d);
					d.AddLoadedHandler(onLoadedChanged, onLoadedChanged);
				}
				else
				{
					Current.mTargets.Remove(d);
					d.RemoveLoadedHandler(onLoadedChanged, onLoadedChanged);
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
			KeyTipService current = Current;
			Current.scopeToElementMap.UpdateScope(scopeElement);
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

		ScopeTree scopeToElementMap = new ScopeTree();

		#endregion
	}
}
