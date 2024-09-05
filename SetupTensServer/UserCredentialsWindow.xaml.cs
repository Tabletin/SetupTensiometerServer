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
using System.Windows.Shapes;

namespace SetupTensServer
{
    /// <summary>
    /// Interaction logic for UserCredentialsWindow.xaml
    /// </summary>
    public partial class UserCredentialsWindow : Window
    {
        public string ServerName { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }

        public UserCredentialsWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ServerName = ServerNameTextBox.Text;
            UserName = UserNameTextBox.Text;
            Password = PasswordBox.Password;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

}
