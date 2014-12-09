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

namespace MenuRibbon.WPF
{
	public interface IPopupRoot
	{
		void UpdatePopupRoot();
		PopupManager PopupManager { get; }
	}
	public interface IPopupItem
	{
		bool IsOpen { get; set; }
		bool IsHighlighted { get; set; }
		IPopupItem ParentItem { get; }
		IPopupRoot PopupRoot { get; }
		bool Contains(DependencyObject target);
	}

	/// <summary>
	/// This class handle nested popup in regard to open / close them and highlight them.
	/// </summary>
	public class PopupManager : IDisposable, INotifyPropertyChanged
	{
		Dispatcher dispatch;
		Action onDispatchTimer;
		PopupRootTracker tracker = new PopupRootTracker();
		IPopupRoot popupRoot;

		public PopupManager(IPopupRoot root)
		{
			popupRoot = root;
			dispatch = ((FrameworkElement)root).Dispatcher;
			onDispatchTimer = OnLater;

			tracker.Element = root;
		}

		public void Dispose()
		{
			Stop();
		}

		#region timer...

		void Start()
		{
			if (popupTimer == null)
				popupTimer = new Timer(OnTimer);
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
			try { if (pt != null) pt.Change(Timeout.Infinite, Timeout.Infinite); }
			catch (ObjectDisposedException) { }
			dispatch.BeginInvoke(onDispatchTimer);
		}
		Timer popupTimer;

		#endregion

		#region IsResponsive

		/// <summary>
		/// While it is not responsive, it will only show highlight on top level, but not open anything
		/// </summary>
		public bool IsResponsive
		{
			get { return responsive; }
			set
			{
				responsive = value;
				if (value)
				{
					FocusManager.SetIsFocusScope(FocusManager.GetFocusScope((DependencyObject)popupRoot), true);
					OpenedItem = HighlightedItem;
				}
				else
				{
					FocusManager.SetIsFocusScope(FocusManager.GetFocusScope((DependencyObject)popupRoot), false);
					Close();
				}

				popupRoot.UpdatePopupRoot();
				OnPropertyChanged();
			}
		}
		bool responsive = false;
 
		#endregion

		#region Delay

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

		#region HighlightedItem, OpenedItem

		public IPopupItem HighlightedItem
		{
			get { return highlightedItem; }
			set 
			{
				if (value == HighlightedItem)
					return;

				if (value != null)
					KeyTipService.Current.CaptureFocusScope();

				PathDiff(HighlightedItem, value, PTarget.Highlight);
				highlightedItem = value;

				popupRoot.UpdatePopupRoot();
				OnPropertyChanged();
			}
		}
		IPopupItem highlightedItem;

		public IPopupItem OpenedItem
		{
			get { return openedItem; }
			set
			{
				if (!IsResponsive) value = null;
				if (value == OpenedItem)
					return;

				PathDiff(OpenedItem, value, PTarget.Open);
				openedItem = value;

				OnPropertyChanged();
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

		#region Enter(), Exit(), Close(), OnLater()

		public void Enter(IPopupItem p, bool forceNow = false)
		{
			if (forceNow)
			{
				IsResponsive = true;
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
