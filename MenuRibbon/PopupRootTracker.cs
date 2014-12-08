using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MenuRibbon.WPF
{
	/// <summary>
	/// This class help <see cref="IPopupRoot"/> track whether or not the user activity has left the popup root 
	/// (and its associated <see cref="IPopupItem"/>) or not.
	/// </summary>
	public class PopupRootTracker
	{
		public IPopupRoot Element
		{
			get { return element; }
			set
			{
				if (value != null && !(value is FrameworkElement))
					throw new ArgumentException("PopupRoot must be a FrameworkElement.");

				if (element == value) return;
				if (feElement != null)
				{
					feElement.Initialized -= element_Initialized;
					feElement.IsVisibleChanged -= element_IsVisibleChanged;
				}
				feElement = (FrameworkElement)value; // REMARK: do not use "as", wants both IPopupRoot and FrameworkElement
				element = value;
				if (feElement != null)
				{
					feElement.Initialized += element_Initialized;
					feElement.IsVisibleChanged += element_IsVisibleChanged;
				}
				UpdateRoot();
			}
		}
		IPopupRoot element;
		FrameworkElement feElement;

		void element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) { UpdateRoot(); }
		void element_Initialized(object sender, EventArgs e) { UpdateRoot(); }
		void UpdateRoot()
		{
			if (feElement == null || !feElement.IsInitialized || !feElement.IsVisible)
			{
				Root = null;
			}
			else
			{
				Root = feElement.VisualHierarchy().Last();
			}
		}

		public DependencyObject Root
		{
			get { return root; }
			private set
			{
				if (value == root)
					return;

				if (root != null)
				{
					FocusManager.RemoveGotFocusHandler(root, OnFocusManagerFocusChangedEventHandler);
					Keyboard.RemovePreviewGotKeyboardFocusHandler(root, OnKeyboardFocusChangedEventHandler);
					Mouse.RemovePreviewMouseDownHandler(root, OnPreviewMouseButtonEventHandler);
					if (root is Window)
					{
						((Window)root).Deactivated -= RootTracker_Deactivated;
					}
				}
				root = value;
				if (root != null)
				{
					FocusManager.AddGotFocusHandler(root, OnFocusManagerFocusChangedEventHandler);
					Keyboard.AddPreviewGotKeyboardFocusHandler(root, OnKeyboardFocusChangedEventHandler);
					Mouse.AddPreviewMouseDownHandler(root, OnPreviewMouseButtonEventHandler);
					if (root is Window)
					{
						((Window)root).Deactivated += RootTracker_Deactivated;
					}
				}
			}
		}
		DependencyObject root;

		void RootTracker_Deactivated(object sender, EventArgs e)
		{
			Element.PopupManager.IsResponsive = false;
		}
		void OnAction(InputEventArgs e)
		{
			var target = e.OriginalSource as DependencyObject;
			if (target == null)
				return;
			if (!feElement.Contains(target))
			{
				Element.PopupManager.IsResponsive = false;
			}
			else if (Element.PopupManager.OpenedItem != null)
			{
				var op = Element.PopupManager.OpenedItem;
				while (op != null && !op.Contains(target))
				{
					op = op.ParentItem;
				}
				Element.PopupManager.OpenedItem = op;
			}
		}
		void OnPreviewMouseButtonEventHandler(object sender, MouseButtonEventArgs e)
		{
			OnAction(e);
		}
		void OnKeyboardFocusChangedEventHandler(object sender, KeyboardFocusChangedEventArgs e)
		{
			//OnAction(e.Source as DependencyObject);
		}
		void OnFocusManagerFocusChangedEventHandler(object sender, RoutedEventArgs e)
		{
			//OnAction(e.Source as DependencyObject);
		}
	}
}
