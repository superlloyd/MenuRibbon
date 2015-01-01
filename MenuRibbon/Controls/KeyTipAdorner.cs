using MenuRibbon.WPF.Controls.Ribbon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MenuRibbon.WPF.Controls
{
	internal class KeyTipAdorner : Adorner
	{
		#region Constructor

		public KeyTipAdorner(UIElement adornedElement,
			UIElement placementTarget,
			KeyTipHorizontalPlacement horizontalPlacement,
			KeyTipVerticalPlacement verticalPlacement,
			double horizontalOffset,
			double verticalOffset,
			RibbonGroup ownerRibbonGroup)
			: base(adornedElement)
		{
			PlacementTarget = (placementTarget == null ? adornedElement : placementTarget);
			HorizontalPlacement = horizontalPlacement;
			VerticalPlacement = verticalPlacement;
			HorizontalOffset = horizontalOffset;
			VerticalOffset = verticalOffset;
			OwnerRibbonGroup = ownerRibbonGroup;
		}

		#endregion

		#region Basic Adorner

		protected override Visual GetVisualChild(int index)
		{
			if (index != 0 || _keyTipControl == null)
				throw new ArgumentOutOfRangeException("index");
			return _keyTipControl;
		}

		protected override int VisualChildrenCount
		{
			get
			{
				return (_keyTipControl == null ? 0 : 1);
			}
		}

		protected override Size MeasureOverride(Size constraint)
		{
			if (_keyTipControl != null)
			{
				Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
				_keyTipControl.Measure(childConstraint);
			}
			return new Size(0, 0);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_keyTipControl != null)
			{
				_keyTipControl.Arrange(new Rect(_keyTipControl.DesiredSize));
			}
			return finalSize;
		}

		#endregion

		#region KeyTipControl Management

		public KeyTipControl KeyTipControl 
		{
			get { return _keyTipControl; }
			set
			{
				if (value == _keyTipControl)
					return;
				if (_keyTipControl != null)
				{
					_keyTipControl.KeyTipAdorner = null;
					RemoveVisualChild(_keyTipControl);
				}
				_keyTipControl = value;
				if (_keyTipControl != null)
				{
					_keyTipControl.KeyTipAdorner = this;
					AddVisualChild(_keyTipControl);
				}
			}
		}
		KeyTipControl _keyTipControl;

		public DependencyObject Element
		{
			get { return element; }
			set 
			{
				if (value == element)
					return;
				if (value != null)
				{
					if (KeyTipControl == null)
						KeyTipControl = new KeyTipControl();
					LinkKeyTipControl(value);
				}
				else
				{
					KeyTipControl = null;
				}
				element = value;
			}
		}
		DependencyObject element;


		void LinkKeyTipControl(DependencyObject keyTipElement)
		{
			KeyTipControl.Text = KeyTipService.GetKeyTip(keyTipElement).ToUpper(KeyTipService.GetCultureForElement(keyTipElement));
			KeyTipControl.IsEnabled = (bool)keyTipElement.GetValue(UIElement.IsEnabledProperty);

			var keyTipStyle = KeyTipService.GetKeyTipStyle(keyTipElement);
			KeyTipControl.Style = keyTipStyle;
			KeyTipControl.RenderTransform = _keyTipTransform;

			bool clearCustomProperties = true;
			if (keyTipStyle == null)
			{
				var ribbon = (Menu.MenuRibbon)(PlacementTarget ?? keyTipElement).VisualHierarchy().FirstOrDefault(x => x is Menu.MenuRibbon);
				if (ribbon != null)
				{
					// Use Ribbon properties if the owner element belongs to a Ribbon.
					keyTipStyle = KeyTipService.GetKeyTipStyle(ribbon);
					if (keyTipStyle != null)
					{
						_keyTipControl.Style = keyTipStyle;
					}
					else
					{
						clearCustomProperties = false;
						_keyTipControl.Background = ribbon.Background;
						_keyTipControl.BorderBrush = ribbon.BorderBrush;
						_keyTipControl.Foreground = ribbon.Foreground;
					}
				}
			}
			if (clearCustomProperties)
			{
				KeyTipControl.ClearValue(Control.BackgroundProperty);
				KeyTipControl.ClearValue(Control.BorderBrushProperty);
				KeyTipControl.ClearValue(Control.ForegroundProperty);
			}
			EnsureTransform();
		}

		#endregion

		#region KeyTip Placement

		private KeyTipHorizontalPlacement HorizontalPlacement { get; set; }
		private KeyTipVerticalPlacement VerticalPlacement { get; set; }
		private double HorizontalOffset { get; set; }
		private double VerticalOffset { get; set; }
		private UIElement PlacementTarget { get; set; }

		private RibbonGroup OwnerRibbonGroup 
		{
			get { return mOwnerRibbonGroup; }
			set
			{
				mOwnerRibbonGroup = value;
				if (value != null)
				{
					itemsPresenter = (ItemsPresenter)value.VisualChildren().FirstOrDefault(x => x is ItemsPresenter);
				}
				else
				{
					itemsPresenter = null;
				}
			}
		}
		RibbonGroup mOwnerRibbonGroup;
		ItemsPresenter itemsPresenter;

		/// <summary>
		///     Invalidate X/Y properties of keytip transform
		///     when size of KeyTipControl changes accordingly.
		/// </summary>
		/// <param name="e"></param>
		internal void OnKeyTipControlSizeChanged(SizeChangedEventArgs e)
		{
			if (e.WidthChanged)
			{
				EnsureTransformX();
			}
			if (e.HeightChanged)
			{
				EnsureTransformY();
			}
		}

		private void EnsureTransform()
		{
			EnsureTransformX();
			EnsureTransformY();
		}

		/// <summary>
		///     Updates X of keytip transform.
		/// </summary>
		private void EnsureTransformX()
		{
			UIElement placementTarget = PlacementTarget;
			if (placementTarget != null)
			{
				int horizontalPlacementValue = (int)HorizontalPlacement;
				double horizontalPosition = 0;
				if (horizontalPlacementValue >= 0 && horizontalPlacementValue < 9)
				{
					switch (horizontalPlacementValue % 3)
					{
						case 1:
							// compensate horizontal position for center of target
							horizontalPosition += (placementTarget.RenderSize.Width / 2);
							break;
						case 2:
							// compensate horizontal position for right of target
							horizontalPosition += placementTarget.RenderSize.Width;
							break;
					}

					if (_keyTipControl != null)
					{
						if (horizontalPlacementValue >= 6)
						{
							// compensate horizontal position for right of keytip
							horizontalPosition -= _keyTipControl.ActualWidth;
						}
						else if (horizontalPlacementValue >= 3)
						{
							// compensate horizontal position for center of keytip
							horizontalPosition -= (_keyTipControl.ActualWidth / 2);
						}
					}
				}

				horizontalPosition += HorizontalOffset;
				_keyTipTransform.X = horizontalPosition;
			}
			else
			{
				_keyTipTransform.X = 0;
			}
		}

		/// <summary>
		///     Updates Y of keytip transform.
		/// </summary>
		private void EnsureTransformY()
		{
			UIElement placementTarget = PlacementTarget;
			if (placementTarget == null)
			{
				placementTarget = AdornedElement;
			}

			if (placementTarget != null)
			{
				int verticalPlacementValue = (int)VerticalPlacement;
				double verticalPosition = 0;
				if (verticalPlacementValue >= 0 && verticalPlacementValue < 9)
				{
					switch (verticalPlacementValue % 3)
					{
						case 1:
							// compensate vertical position for center of target
							verticalPosition += (placementTarget.RenderSize.Height / 2);
							break;
						case 2:
							// compensate vertical position for bottom of target
							verticalPosition += placementTarget.RenderSize.Height;
							break;
					}

					if (_keyTipControl != null)
					{
						if (verticalPlacementValue >= 6)
						{
							// compensate vertical position for bottom of keytip
							verticalPosition -= _keyTipControl.ActualHeight;
						}
						else if (verticalPlacementValue >= 3)
						{
							// compensate vertical position for center of keytip
							verticalPosition -= (_keyTipControl.ActualHeight / 2);
						}
					}
				}
				verticalPosition += VerticalOffset;
				verticalPosition = NudgeToRibbonGroupAxis(placementTarget, verticalPosition);
				_keyTipTransform.Y = verticalPosition;
			}
			else
			{
				_keyTipTransform.Y = 0;
			}
		}

		/// <summary>
		///     Helper method to nudge the vertical postion of keytip,
		///     to RibbonGroup's top/bottom axis if applicable.
		/// </summary>
		private double NudgeToRibbonGroupAxis(UIElement placementTarget, double verticalPosition)
		{
			if (OwnerRibbonGroup != null)
			{
				if (itemsPresenter != null)
				{
					GeneralTransform transform = placementTarget.TransformToAncestor(itemsPresenter);
					Point targetOrigin = transform.Transform(new Point());
					double keyTipTopY = verticalPosition + targetOrigin.Y;
					double keyTipCenterY = keyTipTopY;
					double keyTipBottomY = keyTipTopY;
					if (_keyTipControl != null)
					{
						keyTipBottomY += _keyTipControl.ActualHeight;
						keyTipCenterY += _keyTipControl.ActualHeight / 2;
					}

					if (Math.Abs(keyTipTopY) < RibbonGroupKeyTipAxisNudgeSpace - 1)
					{
						// Nudge to top axis
						verticalPosition -= (keyTipCenterY - RibbonGroupKeyTipAxisOffset);
					}
					else if (Math.Abs(itemsPresenter.ActualHeight - keyTipBottomY) < RibbonGroupKeyTipAxisNudgeSpace - 1)
					{
						// Nudge to bottom axis
						double centerOffsetFromGroupBottom = keyTipCenterY - itemsPresenter.ActualHeight;
						verticalPosition -= (centerOffsetFromGroupBottom + RibbonGroupKeyTipAxisOffset);
					}
				}
			}
			return verticalPosition;
		}

		/// <summary>
		///     Helper method to nudge the keytip into the
		///     boundary of the adorner layer.
		/// </summary>
		internal void NudgeIntoAdornerLayerBoundary(AdornerLayer adornerLayer)
		{
			if (_keyTipControl != null && _keyTipControl.IsLoaded)
			{
				Point adornerOrigin = this.TranslatePoint(new Point(), adornerLayer);
				Rect adornerLayerRect = new Rect(0, 0, adornerLayer.ActualWidth, adornerLayer.ActualHeight);
				Rect keyTipControlRect = new Rect(adornerOrigin.X + _keyTipTransform.X,
					adornerOrigin.Y + _keyTipTransform.Y,
					_keyTipControl.ActualWidth,
					_keyTipControl.ActualHeight);
				if (adornerLayerRect.IntersectsWith(keyTipControlRect) &&
					!adornerLayerRect.Contains(keyTipControlRect))
				{
					double deltaX = 0;
					double deltaY = 0;

					// Nudge the keytip control horizontally if its left or right
					// edge falls outside the adornerlayer.
					if (keyTipControlRect.Left < adornerLayerRect.Left - 1)
					{
						deltaX = adornerLayerRect.Left - keyTipControlRect.Left;
					}
					else if (keyTipControlRect.Right > adornerLayerRect.Right + 1)
					{
						deltaX = adornerLayerRect.Right - keyTipControlRect.Right;
					}

					// Nudge the keytip control vertically if its top or bottom
					// edge falls outside the adornerlayer.
					if (keyTipControlRect.Top < adornerLayerRect.Top - 1)
					{
						deltaY = adornerLayerRect.Top - keyTipControlRect.Top;
					}
					else if (keyTipControlRect.Bottom > adornerLayerRect.Bottom + 1)
					{
						deltaY = adornerLayerRect.Bottom - keyTipControlRect.Bottom;
					}

					_keyTipTransform.X += deltaX;
					_keyTipTransform.Y += deltaY;
				}
			}
		}

		#endregion

		#region Private Data

		private TranslateTransform _keyTipTransform = new TranslateTransform(0, 0);

		private const double RibbonGroupKeyTipAxisNudgeSpace = 15;
		private const double RibbonGroupKeyTipAxisOffset = 5;

		#endregion
	}
}
