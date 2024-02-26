﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bonfire.Views
{
    /// <summary>
    /// Логика взаимодействия для SeedlingsWindow.xaml
    /// </summary>
    public partial class SeedlingsWindow : UserControl
    {
        public SeedlingsWindow()
        {
            InitializeComponent();
        }

        private void ComboBox_OnKeyUp(object sender, KeyEventArgs e)
        {

            if (sender is UserControl control && e.Key == Key.Escape &&
                (DataGrid)(control.Content as Grid)?.Children[0]! != null)
            {
                var dataGrid = (DataGrid)(control.Content as Grid).Children[0];
                dataGrid.SelectedIndex = -1;
                dataGrid.UnselectAllCells();

            }
            if (sender is not ComboBox box ||  e.Key != Key.Enter) return;
            box.Focus();
            if (Keyboard.PrimaryDevice == null) return;
            if (Keyboard.PrimaryDevice.ActiveSource == null) return;
            var e1 = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0,
                    Key.Tab)
                { RoutedEvent = Keyboard.KeyDownEvent };
            InputManager.Current.ProcessInput(e1);
        }

        private void TextBlock_OnKeyUp(object sender, KeyEventArgs e)
            {

           
            if (sender is not TextBox box || e.Key != Key.Enter) return;
            box.Focus();
            if (Keyboard.PrimaryDevice == null) return;
            if (Keyboard.PrimaryDevice.ActiveSource == null) return;
            var e1 = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0,
                    Key.Tab)
                { RoutedEvent = Keyboard.KeyDownEvent };
            InputManager.Current.ProcessInput(e1);
        }

    }
}
