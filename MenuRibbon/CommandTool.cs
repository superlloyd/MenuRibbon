using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MenuRibbon.WPF
{
	public static class CommandTool
	{
		static IInputElement GetTarget(this ICommandSource commandSource)
		{
			var target = commandSource.CommandTarget;
			if (target == null)
				target = commandSource as IInputElement;
			return target;
		}

		public static bool CanExecuteCommand(this ICommandSource commandSource)
		{
			ICommand command = commandSource.Command;
			if (command == null)
				return false;
			object parameter = commandSource.CommandParameter;
			IInputElement target = commandSource.GetTarget();
			return CanExecuteCommand(command, parameter, target);
		}
		public static bool CanExecuteCommand(ICommand command, object parameter, IInputElement target)
		{
			if (command == null)
				return false;
			RoutedCommand routed = command as RoutedCommand;
			if (routed != null)
				return routed.CanExecute(parameter, target);
			return command.CanExecute(parameter);
		}

		public static void HandleCommandChanged(this EventHandler<EventArgs> canExecuteChanged, ICommand oldCommand, ICommand newCommand)
		{
			if (oldCommand != null)
				CanExecuteChangedEventManager.RemoveHandler(oldCommand, canExecuteChanged);
			if (newCommand != null)
				CanExecuteChangedEventManager.AddHandler(newCommand, canExecuteChanged);
		}

		public static void ExecuteCommand(this ICommandSource commandSource)
		{
			ICommand command = commandSource.Command;
			if (command == null)
				return;
			object parameter = commandSource.CommandParameter;
			IInputElement target = commandSource.GetTarget();
			ExecuteCommand(command, parameter, target);
		}
		public static void ExecuteCommand(ICommand command, object parameter, IInputElement target)
		{
			if (command == null)
				return;

			RoutedCommand routed = command as RoutedCommand;
			if (routed != null)
			{
				if (routed.CanExecute(parameter, target))
				{
					routed.Execute(parameter, target);
				}
			}
			else if (command.CanExecute(parameter))
			{
				command.Execute(parameter);
			}
		}
	}
}
