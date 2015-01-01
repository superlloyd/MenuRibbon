using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MenuRibbon.WPF.Controls
{
    /// <summary>
    ///     The Control used inside the KeyTip
    /// </summary>
	public class KeyTipControl : Control
	{
		static KeyTipControl()
		{
			Type ownerType = typeof(KeyTipControl);
			DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
			IsHitTestVisibleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
			FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(false));
			EventManager.RegisterClassHandler(ownerType, SizeChangedEvent, new SizeChangedEventHandler(OnSizeChanged), true);
		}

		internal KeyTipAdorner KeyTipAdorner { get; set; }

		/// <summary>
		///     Notify corresponding KeyTipAdorner regarding size change.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			KeyTipControl keyTipControl = sender as KeyTipControl;
			if (keyTipControl != null &&
				keyTipControl.KeyTipAdorner != null)
			{
				keyTipControl.KeyTipAdorner.OnKeyTipControlSizeChanged(e);
			}
		}

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextProperty =
				DependencyProperty.Register(
						"Text",
						typeof(string),
						typeof(KeyTipControl),
						new FrameworkPropertyMetadata(
								string.Empty,
								FrameworkPropertyMetadataOptions.AffectsMeasure |
								FrameworkPropertyMetadataOptions.AffectsRender));
	}
}
