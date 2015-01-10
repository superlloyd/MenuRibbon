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
			if (target == null && Keyboard.FocusedElement == null)
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

		public static bool ExecuteCommand(this ICommandSource commandSource)
		{
			ICommand command = commandSource.Command;
			if (command == null)
				return false;
			object parameter = commandSource.CommandParameter;
			IInputElement target = commandSource.GetTarget();
			return ExecuteCommand(command, parameter, target);
		}
		public static bool ExecuteCommand(ICommand command, object parameter, IInputElement target)
		{
			if (command == null)
				return false;

			RoutedCommand routed = command as RoutedCommand;
			if (routed != null)
			{
				if (routed.CanExecute(parameter, target))
				{
					routed.Execute(parameter, target);
					return true;
				}
			}
			else if (command.CanExecute(parameter))
			{
				command.Execute(parameter);
				return true;
			}
			return false;
		}
	}
}
