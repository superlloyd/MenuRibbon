using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MenuRibbon.WPF
{
	public class Screen
	{
		public static IEnumerable<Screen> AllScreens()
		{
			foreach (var screen in System.Windows.Forms.Screen.AllScreens)
				yield return new Screen(screen);
		}

		public static Screen GetScreenFrom(Window window)
		{
			var windowInteropHelper = new WindowInteropHelper(window);
			var screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
			var result = new Screen(screen);
			result.SetDpiFromVisual(window);
			return result;
		}
		public static Screen GetScreenFrom(Visual v)
		{
			var w = Window.GetWindow(v);
			if (w == null)
				return null;
			return GetScreenFrom(w);
		}

		public static Screen GetScreenFrom(Point point)
		{
			int x = (int)Math.Round(point.X);
			int y = (int)Math.Round(point.Y);

			// are x,y device-independent-pixels ??
			var drawingPoint = new System.Drawing.Point(x, y);
			var screen = System.Windows.Forms.Screen.FromPoint(drawingPoint);
			return new Screen(screen);
		}

		public static Screen Primary { get { return new Screen(System.Windows.Forms.Screen.PrimaryScreen); } }

		internal Screen(System.Windows.Forms.Screen screen)
		{
			this.screen = screen;
			this.DpiX = this.DpiY = 96.0f;
		}
		private readonly System.Windows.Forms.Screen screen;

		public Rect DeviceBounds { get { return this.GetRect(this.screen.Bounds); } }
		public Rect WorkingArea { get { return this.GetRect(this.screen.WorkingArea); } }
		public bool IsPrimary { get { return this.screen.Primary; } }
		public string DeviceName { get { return this.screen.DeviceName; } }

		public double DpiX { get; set; }
		public double DpiY { get; set; }

		public void SetDpiFromVisual(Visual v)
		{
			var source = PresentationSource.FromVisual(v);
			if (source != null)
			{
				DpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
				DpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
			}
		}

		private Rect GetRect(System.Drawing.Rectangle value)
		{
			return new Rect
			{
				X = value.X * 96 / DpiX,
				Y = value.Y * 96 / DpiY,
				Width = value.Width * 96 / DpiX,
				Height = value.Height * 96 / DpiY
			};
		}
	}
}
