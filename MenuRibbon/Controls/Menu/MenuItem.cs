using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using MenuRibbon.WPF;

namespace MenuRibbon.WPF.Controls.Menu
{
	[TemplatePart(Name = "PART_Header", Type = typeof(FrameworkElement))]
	public class MenuItem : BasePopupItem
	{
		static MenuItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MenuItem), new FrameworkPropertyMetadata(typeof(MenuItem)));
		}
		public MenuItem()
		{
		}

		#region InputGestureText

		public string InputGestureText
		{
			get { return (string)GetValue(InputGestureTextProperty); }
			set { SetValue(InputGestureTextProperty, value); }
		}

		public static readonly DependencyProperty InputGestureTextProperty = DependencyProperty.Register(
			"InputGestureText", typeof(string), typeof(MenuItem)
			, new PropertyMetadata(
				string.Empty,
				(o, e) => ((MenuItem)o).OnInputGestureTextChanged((string)e.OldValue, (string)e.NewValue),
				new CoerceValueCallback((o, val) => ((MenuItem)o).OnCoerceInputGestureText((string)val))
			));

		void OnInputGestureTextChanged(string OldValue, string NewValue)
		{
		}

		string OnCoerceInputGestureText(string value)
		{
			RoutedCommand c;
			if (string.IsNullOrEmpty(value) && (c = Command as RoutedCommand) != null)
			{
				var col = c.InputGestures;
				if ((col != null) && (col.Count >= 1))
				{
					for (int i = 0; i < col.Count; i++)
					{
						var kg = ((System.Collections.IList)col)[i] as KeyGesture;
						if (kg != null)
						{
							return kg.GetDisplayStringForCulture(CultureInfo.CurrentCulture);
						}
					}
				}
			}

			return value;
		} 

		#endregion

		#region IsContainer

		/// <summary>
		/// If the MenuItem hold a custom Item the default template will be replaced by another template filling the whole MenuItem.
		/// </summary>
		public bool HasCustomItem
		{
			get { return (bool)GetValue(HasCustomItemProperty); }
			internal set { SetValue(HasCustomItemPropertyKey, BooleanBoxes.Box(value)); }
		}

		static readonly DependencyPropertyKey HasCustomItemPropertyKey =
			DependencyProperty.RegisterReadOnly("HasCustomItem", typeof(bool), typeof(MenuItem), new PropertyMetadata(BooleanBoxes.FalseBox));

		public static readonly DependencyProperty HasCustomItemProperty = HasCustomItemPropertyKey.DependencyProperty;

		public static bool GetIsCustomItem(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsCustomItemProperty);
		}

		public static void SetIsCustomItem(DependencyObject obj, bool value)
		{
			obj.SetValue(IsCustomItemProperty, BooleanBoxes.Box(value));
		}

		/// <summary>
		/// If this DependencyObject is set to true (the default value) on a UIElement in a MenuItem, it will replace the default template
		/// And expand to the full content (minus padding) of the MenuItem.
		/// </summary>
		public static readonly DependencyProperty IsCustomItemProperty =
			DependencyProperty.RegisterAttached("IsCustomItem", typeof(bool), typeof(MenuItem), new PropertyMetadata(BooleanBoxes.TrueBox));

		#endregion

		#region FrameworkElement override + InputHandling

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			events.Clear();

			var main = GetTemplateChild("PART_Header");
			if (main != null)
			{
				events["H"] = main.MouseHovering().Subscribe(x => IsHovering = x);
				events["L"] = main.MouseDown().Where(x => x.ChangedButton == MouseButton.Left).Subscribe(x => OnMainUI_LeftMouseDown(x));
				events["D"] = main.MouseClicks().Subscribe(x => OnClick());
				events["P"] = main.MousePressed().Subscribe(x => IsPressed = this.IsPressed());
			}
			else
			{
				events.Clear();
			}
		}
		DisposableBag events = new DisposableBag();

		protected void OnMainUI_LeftMouseDown(MouseButtonEventArgs e)
		{
			switch (Role)
			{
				case MenuItemRole.TopLevelHeader:
					Focus();
					if (PopupRoot != null)
						PopupRoot.PopupManager.Enter(this, true);
					break;
				case MenuItemRole.TopLevelItem:
				case MenuItemRole.SubmenuItem:
					break;
				case MenuItemRole.SubmenuHeader:
				default:
					PopupRoot.PopupManager.OpenedItem = this;
					break;
			}
		}

		protected override void OnClick(RoutedEventArgs e)
		{
			switch (Role)
			{
				case MenuItemRole.TopLevelItem:
				case MenuItemRole.SubmenuItem:
					base.OnClick(e);
					if (PopupRoot != null)
						PopupRoot.PopupManager.Tracking = false;
					break;
			}
		}
		#endregion

		#region OnActivatingKeyTip(), OnKeyTipAccessed()

		protected override void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
		{
			if (e.OriginalSource != this)
				return;

			if (HasItems)
			{
				this.OnNavigateChildren();
				e.TargetKeyTipScope = this;
			}
			else
			{
				OnClick();
			}

			e.Handled = true;
			if (!IsOpen)
			{
				this.CloseAllPopups();
			}
		}

		#endregion
	}
}
