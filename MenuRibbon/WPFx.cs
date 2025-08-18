using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MenuRibbon.WPF
{
	public static class WPFx
	{
		#region INotifyPropertyChanged

		public static IObservable<PropertyChangedEventArgs> PropertyChanged(this INotifyPropertyChanged that)
		{
			return Observable.FromEvent<PropertyChangedEventArgs>(
				on => that.PropertyChanged += (o, e) => on(e),
				on => that.PropertyChanged -= (o, e) => on(e)
			);
		}

		#endregion

		#region Window Active

		public static IObservable<EventArgs> Activated(this Window that)
		{
			return Observable.FromEvent<EventArgs>(
				on => that.Activated += (o, e) => on(e),
				on => that.Activated -= (o, e) => on(e)
			);
		}
		public static IObservable<EventArgs> Deactivated(this Window that)
		{
			return Observable.FromEvent<EventArgs>(
				on => that.Deactivated += (o, e) => on(e),
				on => that.Deactivated -= (o, e) => on(e)
			);
		}

		#endregion

		#region Keyboard & Focus events

		public static IObservable<KeyboardFocusChangedEventArgs> PreviewGotKeyboardFocus(this DependencyObject that)
		{
			return Observable.FromEvent<KeyboardFocusChangedEventArgs>(
				on => Keyboard.AddPreviewGotKeyboardFocusHandler(that, (o, e) => on(e)),
				on => Keyboard.RemovePreviewGotKeyboardFocusHandler(that, (o, e) => on(e))
			);
		}

		public static IObservable<MouseEventArgs> LostMouseCapture(this DependencyObject that)
		{
			UIElement ui = (UIElement)that;
			return Observable.FromEvent<MouseEventArgs>(
				on => ui.LostMouseCapture += (o, e) => on(e),
				on => ui.LostMouseCapture -= (o, e) => on(e)
			);
		}

		public static IObservable<KeyEventArgs> KeyDown(this DependencyObject that)
		{
			UIElement ui = (UIElement)that;
			return Observable.FromEvent<KeyEventArgs>(
				on => ui.KeyDown += (o, e) => on(e),
				on => ui.KeyDown -= (o, e) => on(e)
			);
		}
		public static IObservable<KeyEventArgs> KeyUp(this DependencyObject that)
		{
			UIElement ui = (UIElement)that;
			return Observable.FromEvent<KeyEventArgs>(
				on => ui.KeyUp += (o, e) => on(e),
				on => ui.KeyUp -= (o, e) => on(e)
			);
		}

        #endregion

        #region Mouse events

        public static IObservable<MouseButtonEventArgs> LeftMouseUpEvents()
        {
            return Observable.FromEventPattern<PreProcessInputEventHandler, PreProcessInputEventArgs>(
                    h => InputManager.Current.PreProcessInput += h,
                    h => InputManager.Current.PreProcessInput -= h)
                .Select(e => e.EventArgs.StagingItem.Input)
                .OfType<MouseButtonEventArgs>()
                .Where(e => e.ChangedButton == MouseButton.Left)
                .Where(e => e.ButtonState == MouseButtonState.Released)
                ;
        }

        public static IObservable<MouseButtonEventArgs> MouseClick(this DependencyObject that)
		{
			UIElement ui = (UIElement)that;
			return that.MouseDown()
				.Where(x => x.ChangedButton == MouseButton.Left)
                .SelectMany(x =>
				{
					return LeftMouseUpEvents()
						.Take(1)
						.Where(x => ui.IsMouseOver)
						.Select(up => (MouseButtonEventArgs)up);
				});
		}
        public static IObservable<MouseButtonEventArgs> MouseDoubleClick(this DependencyObject that)
        {
			var interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime);
            return that.MouseClick()
				.Timestamp()
				.Buffer(2, 1)
				.Where(clicks => (clicks[1].Timestamp - clicks[0].Timestamp) <= interval)
				.Select(clicks => clicks[1].Value); // emit the second click
        }

        public static IObservable<bool> MouseHovering(this DependencyObject that)
		{
			return Observable.Merge(
				that.MouseEnter().Select(x => true),
				that.MouseLeave().Select(x => false)
			);
		}

		public static IObservable<bool> MousePressed(this DependencyObject that)
		{
            UIElement ui = (UIElement)that;
            return that.MouseDown()
                .Where(x => x.ChangedButton == MouseButton.Left)
                .SelectMany(x =>
                {
                    return Observable.Return(true).Concat(
						that.MouseHovering().TakeUntil(LeftMouseUpEvents())
					).DistinctUntilChanged();
                });
		}

		public static IObservable<MouseEventArgs> MouseEnter(this DependencyObject that)
		{
            return Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => Mouse.AddMouseEnterHandler(that, h),
                h => Mouse.RemoveMouseEnterHandler(that, h)
            ).Select(x => x.EventArgs);
		}
		public static IObservable<MouseEventArgs> MouseLeave(this DependencyObject that)
		{
            return Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => Mouse.AddMouseLeaveHandler(that, h),
                h => Mouse.RemoveMouseLeaveHandler(that, h)
            ).Select(x => x.EventArgs);
		}
		public static IObservable<MouseButtonEventArgs> MouseDown(this DependencyObject that)
		{
			return Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
				h => Mouse.AddMouseDownHandler(that, h),
				h => Mouse.RemoveMouseDownHandler(that, h)
			).Select(x => x.EventArgs);
        }

		#endregion
	}
}
