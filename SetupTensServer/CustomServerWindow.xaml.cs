using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SetupTensServer
{
    public partial class CustomServerWindow : Window
    {
        public CustomServerWindow()
        {
            InitializeComponent();
            InitializePlaceholderText();
        }

        private void InitializePlaceholderText()
        {
            SetPlaceholderText(DefaultServerIpTextBox);
            SetPlaceholderText(ServerNameTextBox);
            SetPlaceholderText(DefaultPortTextBox);
            SetPlaceholderText(UserTextBox);
            SetPasswordPlaceholderText(PasswordBox);
        }

        private void SetPlaceholderText(TextBox textBox)
        {
            textBox.Text = textBox.Tag.ToString();
            textBox.Foreground = Brushes.Gray;
            textBox.GotFocus += RemovePlaceholderText;
            textBox.LostFocus += AddPlaceholderText;
        }

        private void SetPasswordPlaceholderText(PasswordBox passwordBox)
        {
            passwordBox.Password = "Enter Password";
            passwordBox.Foreground = Brushes.Gray;
            passwordBox.GotFocus += RemovePasswordPlaceholderText;
            passwordBox.LostFocus += AddPasswordPlaceholderText;
        }

        private void RemovePlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = string.Empty;
                textBox.Foreground = Brushes.Black;
            }
        }

        private void AddPlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Tag.ToString();
                textBox.Foreground = Brushes.Gray;
            }
        }

        private void RemovePasswordPlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.Password == "Enter Password")
            {
                passwordBox.Password = string.Empty;
                passwordBox.Foreground = Brushes.Black;
            }
        }

        private void AddPasswordPlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                passwordBox.Password = "Enter Password";
                passwordBox.Foreground = Brushes.Gray;
            }
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordTextBox.Visibility = Visibility.Collapsed;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string serverIp = string.IsNullOrEmpty(GetTextFromTextBox(DefaultServerIpTextBox)) ? "localhost" : GetTextFromTextBox(DefaultServerIpTextBox);
            string serverName = GetTextFromTextBox(ServerNameTextBox);
            string portNumber = GetTextFromTextBox(DefaultPortTextBox); // Get the port number if provided by the user
            string userName = GetTextFromTextBox(UserTextBox);
            string password = GetPassword();

            if (!string.IsNullOrEmpty(serverName))
            {
                try
                {
                    string connectionString = BuildConnectionString(serverIp, serverName, portNumber, userName, password);

                    // Validate the connection string by attempting a simple connection
                    bool isValidConnection = ValidateConnectionString(connectionString);

                    if (isValidConnection)
                    {
                        // Pass the necessary parameters back to the main window
                        this.Tag = new { ConnectionString = connectionString, ServerName = serverName, UserName = userName, Password = password };
                        this.DialogResult = true; // Close the dialog and signal success
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to connect to SQL Server with the provided credentials. Please check the details and try again.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect to SQL Server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter the server name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // This method is added for connection string validation
        private bool ValidateConnectionString(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }



        private string GetTextFromTextBox(TextBox textBox)
        {
            return textBox.Text == textBox.Tag.ToString() ? string.Empty : textBox.Text;
        }

        private string GetPassword()
        {
            return PasswordBox.Visibility == Visibility.Visible
                ? (PasswordBox.Password == "Enter Password" ? string.Empty : PasswordBox.Password)
                : PasswordTextBox.Text;
        }

        private string BuildConnectionString(string serverIp, string serverName, string portNumber, string userName, string password)
        {
            string serverPart;

            if (serverIp == "localhost")
            {
                // If it's localhost, don't append the port unless explicitly provided
                serverPart = string.IsNullOrEmpty(portNumber) ? $"{serverIp}\\{serverName}" : $"{serverIp},{portNumber}\\{serverName}";
            }
            else
            {
                // For IP addresses, always append the port, defaulting to 1433 if not provided
                portNumber = string.IsNullOrEmpty(portNumber) ? "1433" : portNumber;
                serverPart = $"{serverIp},{portNumber}\\{serverName}";
            }

            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(password))
            {
                // Use Windows Authentication
                return $"Server={serverPart};Integrated Security=True;TrustServerCertificate=True;";
            }
            else
            {
                // Use SQL Server Authentication
                return $"Server={serverPart};User ID={userName};Password={password};TrustServerCertificate=True;";
            }
        }
    }
}
