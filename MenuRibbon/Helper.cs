using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace MenuRibbon.WPF
{
	/// <summary>
	/// Utility class to reduce boxing operation with boolean
	/// </summary>
	internal static class BooleanBoxes
	{
		internal static object TrueBox = true;
		internal static object FalseBox = false;

		internal static object Box(bool value)
		{
			if (value) { return TrueBox; }
			else { return FalseBox; }
		}
	}

	/// <summary>
	/// Utility class to reduce boxing operation with enums
	/// </summary>
	internal static class EnumBox<T>
		where T : IConvertible
	{
		static EnumBox()
		{
			int N = 255;
			var err = "<T> must be an Enum with underlying values in the [0, " + N + "] range.";

			var t = typeof(T);
			if (!t.IsEnum)
				throw new InvalidOperationException(err);

			var values = Enum.GetValues(t);
			var list = new List<object>(values.Length);
			foreach (var v in values)
			{
				int i = ((T)v).ToInt32(null);
				if (i < 0 || i > N)
					throw new InvalidOperationException(err);

				while (list.Count <= i)
					list.Add(null);
				list[i] = v;
			}
			boxedValues = list.ToArray();
		}

		static object[] boxedValues;
		// must pass an int here, otherwise it's slower AND allocate more memory than default boxing
		public static object Box(int value) { return boxedValues[value]; }
	}

	/// <summary>
	/// Utility class to update and dispose of a bunch of IDisposable
	/// </summary>
	public class DisposableBag : IDisposable
	{
		public IDisposable this[string key]
		{
			get
			{
				IDisposable result;
				storage.TryGetValue(key, out result);
				return result;
			}
			set 
			{
				IDisposable previous;
				storage.TryGetValue(key, out previous);
				if (previous != null)
					previous.Dispose();
				storage[key] = value;
			}
		}
		Dictionary<string, IDisposable> storage = new Dictionary<string, IDisposable>();

		void IDisposable.Dispose() { Clear(); }
		public void Clear()
		{
			foreach (var d in storage.Values)
			{
				d.Dispose();
			}
			storage.Clear();
		}
	}

	public static class UIHelper
	{
		public static CultureInfo GetCultureInfo(this DependencyObject element)
		{
			XmlLanguage language = (XmlLanguage)element.GetValue(FrameworkElement.LanguageProperty);
			try
			{
				if (language == null) return InvariantEnglishUS;
				return language.GetSpecificCulture();
			}
			catch (InvalidOperationException)
			{
				// We default to en-US if no part of the language tag is recognized.
				return InvariantEnglishUS;
			}
		}
		private static CultureInfo invariantEnglishUS;
		public static CultureInfo InvariantEnglishUS
		{
			get
			{
				if (invariantEnglishUS == null)
				{
					invariantEnglishUS = CultureInfo.ReadOnly(new CultureInfo("en-us", false));
				}
				return invariantEnglishUS;
			}
		}

		public static void AddHandler(this DependencyObject element, RoutedEvent routedEvent, Delegate handler)
		{
			Debug.Assert(element != null, "Element must not be null");
			Debug.Assert(routedEvent != null, "RoutedEvent must not be null");

			UIElement uiElement = element as UIElement;
			if (uiElement != null)
			{
				uiElement.AddHandler(routedEvent, handler);
			}
			else
			{
				ContentElement contentElement = element as ContentElement;
				if (contentElement != null)
				{
					contentElement.AddHandler(routedEvent, handler);
				}
				else
				{
					UIElement3D uiElement3D = element as UIElement3D;
					if (uiElement3D != null)
					{
						uiElement3D.AddHandler(routedEvent, handler);
					}
				}
			}
		}
		public static void RemoveHandler(this DependencyObject element, RoutedEvent routedEvent, Delegate handler)
		{
			Debug.Assert(element != null, "Element must not be null");
			Debug.Assert(routedEvent != null, "RoutedEvent must not be null");

			UIElement uiElement = element as UIElement;
			if (uiElement != null)
			{
				uiElement.RemoveHandler(routedEvent, handler);
			}
			else
			{
				ContentElement contentElement = element as ContentElement;
				if (contentElement != null)
				{
					contentElement.RemoveHandler(routedEvent, handler);
				}
				else
				{
					UIElement3D uiElement3D = element as UIElement3D;
					if (uiElement3D != null)
					{
						uiElement3D.RemoveHandler(routedEvent, handler);
					}
				}
			}
		}

		public static void AddLoadedHandler(this DependencyObject element, RoutedEventHandler loaded, RoutedEventHandler unloaded)
		{
			var fe = element as FrameworkElement;
			if (fe != null)
			{
				if (loaded != null) fe.Loaded += loaded;
				if (unloaded != null) fe.Unloaded += unloaded;
				return;
			}
			var fce = element as FrameworkContentElement;
			if (fce != null)
			{
				if (loaded != null) fce.Loaded += loaded;
				if (unloaded != null) fce.Unloaded += unloaded;
				return;
			}
		}
		public static void RemoveLoadedHandler(this DependencyObject element, RoutedEventHandler loaded, RoutedEventHandler unloaded)
		{
			var fe = element as FrameworkElement;
			if (fe != null)
			{
				if (loaded != null) fe.Loaded -= loaded;
				if (unloaded != null) fe.Unloaded -= unloaded;
				return;
			}
			var fce = element as FrameworkContentElement;
			if (fce != null)
			{
				if (loaded != null) fce.Loaded -= loaded;
				if (unloaded != null) fce.Unloaded -= unloaded;
				return;
			}
		}
		public static bool IsLoaded(this DependencyObject element)
		{
			var fe = element as FrameworkElement;
			if (fe != null)
				return fe.IsLoaded;

			var fce = element as FrameworkContentElement;
			if (fce != null)
				return fce.IsLoaded;

			return false;
		}


		public static UIElement FirstFocusableElement(this object o)
		{
			Predicate<DependencyObject> where = x =>
			{
				var ui = x as UIElement;
				if (ui == null || !ui.IsEnabled || !ui.Focusable || !ui.IsVisible || !System.Windows.Input.KeyboardNavigation.GetIsTabStop(ui))
					return false;
				return true;
			};

			var dp = o as DependencyObject;
			if (dp == null)
				return null;
			if (where(dp))
				return (UIElement)dp;
			return dp.VisualChildren()
				.Where(x => where(x))
				.Select(x => (UIElement)x)
				.FirstOrDefault();
		}
		public static bool IsInMainFocusScope(this DependencyObject element)
		{
				var focusScope = FocusManager.GetFocusScope(element) as Visual;
				return focusScope == null || VisualTreeHelper.GetParent(focusScope) == null;
		}

		public static bool IsDefined(this DependencyObject d, DependencyProperty property)
		{
			object val = d.ReadLocalValue(property);
			return val != DependencyProperty.UnsetValue && val != null;
		}
		public static bool HasDefaultValue(this DependencyObject d, DependencyProperty dp) { return !IsDefined(d, dp); }

		public static bool Contains(this DependencyObject parent, DependencyObject child)
		{
			return child != null && (child.LogicalHierarchy().Contains(parent) || child.VisualHierarchy().Contains(parent));
		}

		public static IEnumerable<DependencyObject> LogicalChildren(this DependencyObject obj)
		{
			System.Collections.IEnumerable children = null;
			if (obj is FrameworkContentElement) children = LogicalTreeHelper.GetChildren((FrameworkContentElement)obj);
			else if (obj is FrameworkElement) children = LogicalTreeHelper.GetChildren((FrameworkElement)obj);
			else children = LogicalTreeHelper.GetChildren(obj);

			foreach (var item in children.Cast<object>().Where(x => x is DependencyObject).Cast<DependencyObject>())
			{
				yield return item;
				foreach (var subitem in item.LogicalChildren())
					yield return subitem;
			}
		}
		public static IEnumerable<DependencyObject> LogicalHierarchy(this DependencyObject obj, bool includeVisual = true)
		{
			while (obj != null)
			{
				yield return obj;
				obj = obj.LogicalParent(includeVisual);
			}
		}
		public static DependencyObject LogicalParent(this DependencyObject obj, bool includeVisual = true)
		{
			DependencyObject p;
			if (obj is ContentElement)
			{
				p = ContentOperations.GetParent((ContentElement)obj);
				if (p != null)
					return p;
			}
			p = LogicalTreeHelper.GetParent(obj);
			if (p != null)
				return p;
			p = ItemsControl.ItemsControlFromItemContainer(obj);
			if (p != null)
				return p;
			return VisualParent(obj, false);
		}

		public static IEnumerable<DependencyObject> VisualChildren(this DependencyObject obj)
		{
			int N = VisualTreeHelper.GetChildrenCount(obj);
			for (int i = 0; i < N; i++)
			{
				var child = VisualTreeHelper.GetChild(obj, i);
				yield return child;
				foreach (var subitem in child.VisualChildren())
					yield return subitem;
			}
		}
		public static IEnumerable<DependencyObject> VisualHierarchy(this DependencyObject element, bool includeLogical = true)
		{
			while (element != null)
			{
				yield return element;
				element = element.VisualParent(includeLogical);
			}
		}
		public static DependencyObject VisualParent(this DependencyObject obj, bool includeLogical = true)
		{
			DependencyObject p = null;
			if (obj is FrameworkContentElement)
			{
				p = ((FrameworkContentElement)obj).Parent;
			}
			else if (obj is ContentElement)
			{
				p = ContentOperations.GetParent((ContentElement)obj);
			}
			else if (obj is Visual || obj is System.Windows.Media.Media3D.Visual3D)
			{
				p = VisualTreeHelper.GetParent(obj);
			}
			if (p == null && obj != null && includeLogical)
				p = obj.LogicalParent(false);
			return p;
		}

		public static Visual GetRootVisual(this Visual v)
		{
			var source = PresentationSource.FromVisual(v);
			if (source == null)
				return null;
			return source.CompositionTarget.RootVisual;
		}
		public static Rect ScreenBounds(this UIElement v)
		{
			var p0 = v.PointToScreen(new Point());
			var s = v.RenderSize;
			var p1 = v.PointToScreen(new Point(s.Width, s.Height));
			return new Rect(p0, p1);
		}
	}
}
