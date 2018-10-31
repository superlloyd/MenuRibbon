using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MenuRibbon.WPF
{
    internal static class CommandTool
    {
        public static object GetCommandContext(this ICommandSource commandSource)
        {
            ICommand command = commandSource.Command;
            if (command == null)
                return null;

            if (command is RoutedCommand routed)
            {
                if (commandSource.GetTarget() is FrameworkElement fe)
                    return fe.DataContext;
            }
            else
            {
                if (commandSource is FrameworkElement fe)
                    return fe.DataContext;
            }
            return null;
        }

        static IInputElement GetTarget(this ICommandSource commandSource)
        {
            return commandSource.CommandTarget
                ?? Keyboard.FocusedElement
                ?? commandSource as IInputElement;
        }

        public static bool CanExecuteCommand(this ICommandSource commandSource)
        {
            ICommand command = commandSource.Command;
            if (command == null)
                return false;

            object parameter = commandSource.CommandParameter;

            if (command is RoutedCommand routed)
            {
                return routed.CanExecute(parameter, commandSource.GetTarget());
            }
            else
            {
                return command.CanExecute(parameter);
            }
        }

        public static bool ExecuteCommand(this ICommandSource commandSource)
        {
            ICommand command = commandSource.Command;
            if (command == null)
                return false;

            object parameter = commandSource.CommandParameter;

            if (command is RoutedCommand routed)
            {
                var target = commandSource.GetTarget();
                if (routed.CanExecute(parameter, target))
                {
                    routed.Execute(parameter, target);
                    return true;
                }
            }
            else
            {
                if (command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                    return true;
                }
            }
            return false;
        }
    }
}
