using System;
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

namespace uchat
{
    public partial class InputTextBox : UserControl
    {
        public InputTextBox()
        {
            InitializeComponent();
        }

        private string? placeholder;
        private string? header;
        private bool isPasswordMode = false;

        public string Text
        {
            get => isPasswordMode ? passwordBox?.Password ?? "" : txtInput.Text;
            set
            {
                if (isPasswordMode)
                {
                    passwordBox.Password = value;
                }  
                else
                {
                    txtInput.Text = value;
                }
            }
        }
        public string Password => Text;

        public string Placeholder
        {
            get { return placeholder ?? ""; }
            set
            {
                placeholder = value;
                tbPlaceholder.Text = placeholder;
            }
        }

        public string Header
        {
            get { return header ?? ""; }
            set
            {
                header = value;
                tbName.Text = header;
            }
        }

        public bool IsPassword
        {
            get => isPasswordMode;
            set
            {
                isPasswordMode = value;
                txtInput.Visibility = isPasswordMode ? Visibility.Collapsed : Visibility.Visible;
                passwordBox.Visibility = isPasswordMode ? Visibility.Visible : Visibility.Collapsed;
                tbPlaceholder.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public void ShowTBError(string message)
        {
            tbError.Text = message;
            tbError.Visibility = Visibility.Visible;
            borderInput.BorderBrush = Brushes.Red;
            borderInput.BorderThickness = new Thickness(3);
        }

        public void HideTBError()
        {
            tbError.Visibility = Visibility.Collapsed;
            borderInput.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7D619B"));
            borderInput.BorderThickness = new Thickness(3);
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                ShowTBError("This field cannot be empty!");
                return false;
            }

            HideTBError();
            return true;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Clear();
            txtInput.Focus();
        }

        private void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbPlaceholder.Visibility = string.IsNullOrEmpty(txtInput.Text) ? Visibility.Visible : Visibility.Hidden;

            if (!string.IsNullOrWhiteSpace(txtInput.Text))
            {
                HideTBError();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            tbPlaceholder.Visibility = string.IsNullOrEmpty(passwordBox.Password) ? Visibility.Visible : Visibility.Hidden;
            if (!string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                HideTBError();
            }
        }

       
    }
}