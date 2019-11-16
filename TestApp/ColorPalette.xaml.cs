using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace TestApp
{
    public partial class ColorPalette : UserControl
    {
        public SolidColorBrush CurrentColor
        {
            get { return (SolidColorBrush)GetValue(CurrentColorProperty); }
            set { SetValue(CurrentColorProperty, value); }
        }

        public static DependencyProperty CurrentColorProperty =
            DependencyProperty.Register("CurrentColor", typeof(SolidColorBrush), typeof(ColorPalette), new PropertyMetadata(Brushes.Black));
        
        public static RoutedUICommand SelectColorCommand = new RoutedUICommand("SelectColorCommand","SelectColorCommand", typeof(ColorPalette));
        private Window _advancedPickerWindow = new Window();

        public ColorPalette()
        {
            DataContext = this;
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(SelectColorCommand, SelectColorCommandExecute));
        }

        private void SelectColorCommandExecute(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(e.Parameter.ToString()));
        }

        private static void ShowModal(Window advancedColorWindow)
        {
            advancedColorWindow.Owner = Application.Current.MainWindow;
            advancedColorWindow.ShowDialog();
        }

        void AdvancedPickerPopUpKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                _advancedPickerWindow.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}