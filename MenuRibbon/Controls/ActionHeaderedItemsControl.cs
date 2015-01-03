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

namespace MenuRibbon.WPF.Controls
{
	/// <summary>
	/// Base class for a MenuRibbonItem that can act as either a HeaderedContentControl or an HeaderedItemsControl or just ContentControl. 
	/// It contains general purpose properties which have little impact on ribbon behavior or each other.
	/// <see cref="Menu.MenuItem"/> could either plain button, or drop down for more menu items or a single control.
	/// </summary>
	public class ActionHeaderedItemsControl : HeaderedItemsControl, ICommandSource
	{
		static ActionHeaderedItemsControl()
		{
		}

		#region Click event

		public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ActionHeaderedItemsControl));

		public event RoutedEventHandler Click
		{
			add { base.AddHandler(ClickEvent, value); }
			remove { base.RemoveHandler(ClickEvent, value); }
		}

		/// <summary>
		/// Trigger the Click event.
		/// </summary>
		public void OnClick() { OnClick(new RoutedEventArgs(ClickEvent, this)); }

		/// <summary>
		/// Remark: this should be called by subclass. It will update checkable and call command
		/// </summary>
		protected virtual void OnClick(RoutedEventArgs e)
		{
			if (IsCheckable) IsChecked = !IsChecked;

			base.RaiseEvent(e);

			this.ExecuteCommand();

			//if (!this.IsInMainFocusScope() && FocusManager.GetFocusScope((DependencyObject)Keyboard.FocusedElement) == FocusManager.GetFocusScope(this))
			{
				Keyboard.Focus(null);
			}
		}

		#endregion

		#region IsCheckable, IsChecked, Icon

		public bool IsCheckable
		{
			get { return (bool)GetValue(IsCheckableProperty); }
			set { SetValue(IsCheckableProperty, value); }
		}

		public static readonly DependencyProperty IsCheckableProperty = DependencyProperty.Register(
			"IsCheckable", typeof(bool), typeof(ActionHeaderedItemsControl)
			, new PropertyMetadata(default(bool), (o, e) => ((ActionHeaderedItemsControl)o).OnIsCheckableChanged((bool)e.OldValue, (bool)e.NewValue)));

		void OnIsCheckableChanged(bool OldValue, bool NewValue)
		{
		}

		public bool IsChecked
		{
			get { return (bool)GetValue(IsCheckedProperty); }
			set { SetValue(IsCheckedProperty, value); }
		}

		public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
			"IsChecked", typeof(bool), typeof(ActionHeaderedItemsControl)
			, new PropertyMetadata(default(bool), (o, e) => ((ActionHeaderedItemsControl)o).OnIsCheckedChanged((bool)e.OldValue, (bool)e.NewValue)));

		void OnIsCheckedChanged(bool OldValue, bool NewValue)
		{
			if (NewValue)
			{
				OnChecked(new RoutedEventArgs(CheckedEvent));
			}
			else
			{
				OnUnchecked(new RoutedEventArgs(UncheckedEvent));
			}
		}

		public static readonly RoutedEvent CheckedEvent = EventManager.RegisterRoutedEvent("Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ActionHeaderedItemsControl));
		public static readonly RoutedEvent UncheckedEvent = EventManager.RegisterRoutedEvent("Unchecked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ActionHeaderedItemsControl));
		protected virtual void OnChecked(RoutedEventArgs e)
		{
			RaiseEvent(e);
		}
		protected virtual void OnUnchecked(RoutedEventArgs e)
		{
			RaiseEvent(e);
		}

		public object Icon
		{
			get { return (object)GetValue(IconProperty); }
			set { SetValue(IconProperty, value); }
		}

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
			"Icon", typeof(object), typeof(ActionHeaderedItemsControl)
			, new PropertyMetadata(default(object), (o, e) => ((ActionHeaderedItemsControl)o).OnIconChanged((object)e.OldValue, (object)e.NewValue)));

		void OnIconChanged(object OldValue, object NewValue)
		{
		}

		#endregion

		#region ICommandSource

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
			"Command", typeof(ICommand), typeof(ActionHeaderedItemsControl)
			, new PropertyMetadata(default(ICommand), (o, e) => ((ActionHeaderedItemsControl)o).OnCommandChanged((ICommand)e.OldValue, (ICommand)e.NewValue)));

		void OnCommandChanged(ICommand OldValue, ICommand NewValue)
		{
			if (onCommandUpdated == null)
				onCommandUpdated = (o, e) => UpdateFromCommand();

			if (OldValue != null)
				CanExecuteChangedEventManager.RemoveHandler(OldValue, onCommandUpdated);
			if (NewValue != null)
				CanExecuteChangedEventManager.AddHandler(NewValue, onCommandUpdated);
			onCommandUpdated(null, null);
		}
		EventHandler<EventArgs> onCommandUpdated;

		protected virtual void UpdateFromCommand()
		{
			if (Command == null)
			{
				this.IsEnabled = true;
			}
			else
			{
				bool enabled = this.CanExecuteCommand();
				this.IsEnabled = enabled;
			}
		}

		public object CommandParameter
		{
			get { return (object)GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
			"CommandParameter", typeof(object), typeof(ActionHeaderedItemsControl)
			, new PropertyMetadata(default(object), (o, e) => ((ActionHeaderedItemsControl)o).OnCommandParameterChanged((object)e.OldValue, (object)e.NewValue)));

		void OnCommandParameterChanged(object OldValue, object NewValue)
		{
			UpdateFromCommand();
		}

		public IInputElement CommandTarget
		{
			get { return (IInputElement)GetValue(CommandTargetProperty); }
			set { SetValue(CommandTargetProperty, value); }
		}

		public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
			"CommandTarget", typeof(IInputElement), typeof(ActionHeaderedItemsControl)
			, new PropertyMetadata(default(IInputElement), (o, e) => ((ActionHeaderedItemsControl)o).OnCommandTargetChanged((IInputElement)e.OldValue, (IInputElement)e.NewValue)));

		protected virtual void OnCommandTargetChanged(IInputElement OldValue, IInputElement NewValue)
		{
			UpdateFromCommand();
		}

		#endregion
	}
}
