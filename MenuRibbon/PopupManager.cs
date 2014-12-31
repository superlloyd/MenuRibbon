using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MenuRibbon.WPF.Utils;

namespace MenuRibbon.WPF
{
	public interface IPopupRoot
	{
		void UpdatePopupRoot();
		void OnLostFocus();
		PopupManager PopupManager { get; }
	}

	/// <summary>
	/// This class handle nested popup in regard to open / close them. 
	/// Also automatically close, un-highlight them when needed.
	/// </summary>
	public class PopupManager : IDisposable, INotifyPropertyChanged
	{
		public PopupManager(IPopupRoot root = null)
		{
			PrepareTrackHandlers();
			PopupRoot = root;
		}

		public void Dispose()
		{
			PopupRoot = null; // stop being tracked by event handlers
			Tracking = false; // stop being tracked by event handlers
			Stop(); // stop being tracked by timer
		}

		#region PopupRoot UIRoot Tracking

		public IPopupRoot PopupRoot
		{
			get { return element; }
			set
			{
				if (element == value) return;
				if (value != null && !(value is FrameworkElement))
					throw new ArgumentException("PopupRoot must be a FrameworkElement.");

				if (feElement != null)
				{
					feElement.Initialized -= element_Initialized;
					feElement.IsVisibleChanged -= element_IsVisibleChanged;
					dispatch = null;
				}
				feElement = (FrameworkElement)value;
				element = value;
				if (feElement != null)
				{
					feElement.Initialized += element_Initialized;
					feElement.IsVisibleChanged += element_IsVisibleChanged;
					dispatch = feElement.Dispatcher;
				}
				UpdateRoot();
				OnPropertyChanged();
			}
		}
		IPopupRoot element;
		FrameworkElement feElement;
		Dispatcher dispatch;

		void element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) { UpdateRoot(); }
		void element_Initialized(object sender, EventArgs e) { UpdateRoot(); }

		void UpdateRoot()
		{
			if (feElement == null || !feElement.IsInitialized || !feElement.IsVisible)
			{
				UIRoot = null;
			}
			else
			{
				UIRoot = feElement.VisualHierarchy().Last();
			}
		}

		public DependencyObject UIRoot
		{
			get { return root; }
			private set
			{
				if (value == root)
					return;

				if (root != null)
				{
					Mouse.RemovePreviewMouseDownHandler(root, onPreviewMouseDown);
					if (root is Window) ((Window)root).Deactivated -= onWindowDeactivated;
				}
				root = value;
				if (root == null)
				{
					Tracking = false;
				}
				else if (Tracking)
				{
					Mouse.AddPreviewMouseDownHandler(root, onPreviewMouseDown);
					if (root is Window) ((Window)root).Deactivated += onWindowDeactivated;
				}
				OnPropertyChanged();
			}
		}
		DependencyObject root;

		public bool Tracking 
		{
			get { return mTracking; }
			internal set
			{
				if (value == mTracking)
					return;
				mTracking = value;
				if (value)
				{
					InputManager.Current.PushMenuMode(pTrackingSource = PresentationSource.FromDependencyObject(root));
					InputManager.Current.PostProcessInput += onPostProcessInput;
					if (root != null) Mouse.AddPreviewMouseDownHandler(root, onPreviewMouseDown);
					if (root is Window) ((Window)root).Deactivated += onWindowDeactivated;
				}
				else
				{
					InputManager.Current.PopMenuMode(pTrackingSource);
					InputManager.Current.PostProcessInput -= onPostProcessInput;
					if (root != null) Mouse.RemovePreviewMouseDownHandler(root, onPreviewMouseDown);
					if (root is Window) ((Window)root).Deactivated -= onWindowDeactivated;

					OpenedItem = null;
					HighlightedItem = null;
				}
				PopupRoot.UpdatePopupRoot();
				OnPropertyChanged();
			}
		}
		bool mTracking;
		PresentationSource pTrackingSource;

		void PrepareTrackHandlers()
		{
			Action leave = () => 
			{ 
				Tracking = false;
				OpenedItem = null;
				HighlightedItem = null;
			};

			onPostProcessInput = (o, e) =>
			{
				if (feElement.IsKeyboardFocusWithin)
					return;
				var target = e.StagingItem.Input.OriginalSource as DependencyObject;
				if (target == null || target.VisualHierarchy().Contains(feElement))
					return;

				var ev = e.StagingItem.Input.RoutedEvent;
				if (ev == Keyboard.GotKeyboardFocusEvent)
				{
					PopupRoot.OnLostFocus();
					return;
				}

				if (e.StagingItem.Input.Handled)
					return;

				if (ev == Keyboard.KeyDownEvent
					//|| ev == Keyboard.KeyUpEvent
					|| ev == Mouse.MouseDownEvent
					|| ev == Mouse.MouseUpEvent
					//|| ev == TextCompositionManager.TextInputEvent
					|| ev == Stylus.StylusDownEvent
					|| ev == Stylus.StylusUpEvent)
				{
					leave();
					return;
				}
				if (ev == Keyboard.KeyDownEvent)
				{
					var args = (KeyEventArgs)e.StagingItem.Input;
					if (args.Key == Key.Escape)
						leave();
				}
			};
			onWindowDeactivated = (o, e) => 
			{
				leave();
			};
			onPreviewMouseDown = (o, e) =>
			{
				var target = e.OriginalSource as DependencyObject;
				if (target == null)
					return;
				if (!feElement.Contains(target))
				{
					//e.Handled = true;
					leave();
				}
				else if (OpenedItem != null)
				{
					//e.Handled = true;
					var op = OpenedItem;
					while (op != null && !op.Contains(target))
					{
						op = op.ParentItem;
					}
					OpenedItem = op;
				}
			};
		}
		EventHandler onWindowDeactivated;
		MouseButtonEventHandler onPreviewMouseDown;
		ProcessInputEventHandler onPostProcessInput;

		#endregion

		#region HighlightedItem, OpenedItem

		public IPopupItem HighlightedItem
		{
			get { return highlightedItem; }
			set 
			{
				if (value == HighlightedItem)
					return;

				PathDiff(HighlightedItem, value, PTarget.Highlight);
				highlightedItem = value;

				if (PopupRoot != null) PopupRoot.UpdatePopupRoot();
				OnPropertyChanged();
			}
		}
		IPopupItem highlightedItem;

		public IPopupItem OpenedItem
		{
			get { return openedItem; }
			set
			{
				if (value == OpenedItem)
					return;

				PathDiff(OpenedItem, value, PTarget.Open);
				openedItem = value;

				OnPropertyChanged();

				if (value != null && !Tracking)
				{
					Keyboard.Focus(value as IInputElement);
					Tracking = true;
				}
			}
		}
		IPopupItem openedItem;

		enum PTarget
		{
			Highlight,
			Open,
		}

		void PathDiff(IPopupItem oldValue, IPopupItem newValue, PTarget target)
		{
			GetPath(oldPath, oldValue);
			GetPath(newPath, newValue);

			oldPath.Where(x => !newPath.Contains(x)).ForEach(p =>
			{
				switch (target)
				{
					case PTarget.Highlight:
						p.IsHighlighted = false;
						break;
					case PTarget.Open:
						p.IsOpen = false;
						break;
				}
			});
			newPath.ForEach(p =>
			{
				switch (target)
				{
					case PTarget.Highlight:
						p.IsHighlighted = true;
						break;
					case PTarget.Open:
						p.IsOpen = true;
						break;
				}
			});
		}

		// convoluted to avoid memory allocation
		List<IPopupItem> oldPath = new List<IPopupItem>();
		List<IPopupItem> newPath = new List<IPopupItem>();
		void GetPath(List<IPopupItem> list, IPopupItem p)
		{
			list.Clear();
			while (p != null)
			{
				list.Insert(0, p);
				p = p.ParentItem;
			}
		}

		#endregion

		#region Enter(), Exit(), Close(), OnLater() + timer

		public void Enter(IPopupItem p, bool forceNow = false)
		{
			if (forceNow)
			{
				HighlightedItem = p;
				OpenedItem = p;
			}
			else
			{
				HighlightedItem = p;
				OpenLater(p);
			}
		}

		public void Exit(IPopupItem p)
		{
			if (p == HighlightedItem)
				HighlightedItem = p.ParentItem;
		}

		public void Close()
		{
			OpenedItem = null;
			HighlightedItem = null;
		}

		public void OpenLater(IPopupItem p)
		{
			laterItem = p;
			Start();
		}
		void OnLater()
		{
			if (laterItem == HighlightedItem)
				OpenedItem = HighlightedItem;
			laterItem = null;
		}
		IPopupItem laterItem;

		void Start()
		{
			if (popupTimer == null) popupTimer = new Timer(OnTimer);
			if (onDispatchTimer == null) onDispatchTimer = OnLater;
			popupTimer.Change(Delay, Delay);
		}
		void Stop()
		{
			// try to reduce incidence of possible ObjectDisposeException (in OnTimer)
			// by nullifying first and then disposing
			var pt = popupTimer;
			popupTimer = null;
			if (pt != null)
			{
				pt.Dispose();
				pt = null;
			}
		}
		void OnTimer(object state)
		{
			var pt = popupTimer;
			try 
			{
				if (pt != null) pt.Change(Timeout.Infinite, Timeout.Infinite);
				if (dispatch != null) dispatch.BeginInvoke(onDispatchTimer);
			}
			catch (ObjectDisposedException) { }
		}
		Timer popupTimer;
		Action onDispatchTimer;


		/// <summary>
		/// Delay before opening child popup
		/// </summary>
		public TimeSpan Delay
		{
			get { return delay; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();
				delay = value;

				OnPropertyChanged();
			}
		}
		TimeSpan delay = TimeSpan.FromMilliseconds(SystemParameters.MenuShowDelay);

		#endregion

		#region INotifyPropertyChanged

		void OnPropertyChanged([CallerMemberName]string name = null)
		{
			var e = PropertyChanged;
			if (e != null)
				e(this, new PropertyChangedEventArgs(name));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion	
	}
}
