using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
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

		public static IObservable<IObservable<MouseEventArgs>> MouseDrag(this DependencyObject that)
		{
			return that.MouseDown()
				.Where(x => x.ChangedButton == MouseButton.Left)
				.Do(x => { if (that is UIElement) { ((UIElement)that).CaptureMouse(); } })
				.Select(x => 
				{
					var start = new List<MouseEventArgs>() { x }.ToObservable();
					var end = that.MouseUp()
						.Where(y => y.ChangedButton == MouseButton.Left)
						.Take(1)
						.Do(y => { if (that is UIElement) { ((UIElement)that).ReleaseMouseCapture(); } });
					var move = that.MouseMove().TakeUntil(end);
					return Observable.Merge<MouseEventArgs>(start, move, end);
				});
		}

		public static IObservable<MouseButtonEventArgs> MouseClick(this DependencyObject that)
		{
			UIElement ui = (UIElement)that;
			bool isIn = false;
			return Observable.Merge(
				that.MouseDown()
					.Where(x => x.ChangedButton == MouseButton.Left)
					.Do(x =>
					{
						isIn = true;
						ui.CaptureMouse();
					})
					.Where(x => false) // don't select mouse down
				, that.LostMouseCapture()
					.Do(x => { isIn = false; })
					.Where(x => false) // don't select that!
				// event to return
				, that.MouseUp()
					.Where(x => x.ChangedButton == MouseButton.Left && isIn)
					.Do(x => ui.ReleaseMouseCapture())
					.Where(x => that.Contains(x.Source as DependencyObject))
			)
			.Select(x => (MouseButtonEventArgs)x);
		}

		public static IObservable<Tuple<MouseButtonEventArgs, int>> MouseClicks(this DependencyObject that) { return MouseClicks(that, TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime)); }
		public static IObservable<Tuple<MouseButtonEventArgs, int>> MouseClicks(this DependencyObject that, TimeSpan interval)
		{
			// trying to avoid "new (ReferenceType)" as much as possible
			UIElement ui = (UIElement)that;
			int count = 0;
			long tInterval = interval.Ticks / 2; // time between clicks is half a double click!
			long t0 = 0, tLast = 0;
			bool isIn = false;
			return Observable.Merge(
				that.MouseDown()
					.Where(x => x.ChangedButton == MouseButton.Left)
					.Do(x =>
					{
						isIn = true;
						tLast = DateTime.Now.Ticks;
						ui.CaptureMouse();
					})
					.Where(x => false) // don't select mouse down
				, LostMouseCapture(ui)
					.Do(x => { isIn = false; })
					.Where(x => false) // don't select that!
				// event to return
				, that.MouseUp()
					.Where(x => x.ChangedButton == MouseButton.Left && isIn)
					.Do(x => ui.ReleaseMouseCapture())
					.Where(x => that.Contains(x.Source as DependencyObject))
			)
				// no transform mouse up events
			.Select(x =>
			{
				var e = (MouseButtonEventArgs)x;
				count++;
				var tNow = DateTime.Now.Ticks;
				// multi click test
				if (count > 1 && tNow > t0 + count * tInterval)
				{
					t0 = tLast;
					count = 1;
				}
				return Tuple.Create(e, count);
			});
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
			bool isDown = false, isHover = false;
			bool isPressed = false;
			return Observable.Merge(
				that.MouseMove().Do(x =>
				{
					var s = ui.RenderSize;
					var p = x.GetPosition(ui);
					isHover = p.X >= 0 && p.Y >= 0 && p.X < s.Width && p.Y < s.Height;
				}),
				that.MouseDown()
					.Where(x => x.ChangedButton == MouseButton.Left)
					.Do(x =>
					{
						isDown = true;
						ui.CaptureMouse();
					})
				, that.LostMouseCapture().Do(x => { isDown = false; })
				, that.MouseUp()
					.Where(x => x.ChangedButton == MouseButton.Left && isDown)
					.Do(x =>
					{
						ui.ReleaseMouseCapture();
						isDown = false;
					})
			)
			.Where(x => 
			{
				var prev = isPressed;
				isPressed = isDown && isHover;
				return isPressed != prev;
			})
			.Select(x => isPressed);
		}

		public static IObservable<MouseEventArgs> MouseEnter(this DependencyObject that)
		{
			return Observable.FromEvent<MouseEventArgs>(
				on => Mouse.AddMouseEnterHandler(that, (o, e) => on(e)),
				on => Mouse.RemoveMouseEnterHandler(that, (o, e) => on(e))
			);
		}
		public static IObservable<MouseEventArgs> MouseLeave(this DependencyObject that)
		{
			return Observable.FromEvent<MouseEventArgs>(
				on => Mouse.AddMouseLeaveHandler(that, (o, e) => on(e)),
				on => Mouse.RemoveMouseLeaveHandler(that, (o, e) => on(e))
			);
		}
		public static IObservable<MouseEventArgs> MouseMove(this DependencyObject that)
		{
			return Observable.FromEvent<MouseEventArgs>(
				on => Mouse.AddMouseMoveHandler(that, (o, e) => on(e)),
				on => Mouse.RemoveMouseMoveHandler(that, (o, e) => on(e))
			);
		}
		public static IObservable<MouseButtonEventArgs> MouseDown(this DependencyObject that)
		{
			return Observable.FromEvent<MouseButtonEventArgs>(
				on => Mouse.AddMouseDownHandler(that, (o, e) => on(e)),
				on => Mouse.RemoveMouseDownHandler(that, (o, e) => on(e))
			);
		}
		public static IObservable<MouseButtonEventArgs> MouseUp(this DependencyObject that)
		{
			return Observable.FromEvent<MouseButtonEventArgs>(
				on => Mouse.AddMouseUpHandler(that, (o, e) => on(e)),
				on => Mouse.RemoveMouseUpHandler(that, (o, e) => on(e))
			);
		}

		#endregion
	}
}
