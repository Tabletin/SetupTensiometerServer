using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace SetupTensServer
{
    /// <summary>
    /// Interaction logic for UserOptionsWindow.xaml
    /// </summary>
    public partial class UserOptionsWindow : Window
    {

        private MainWindow _mainWindow;

        public UserOptionsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            SetButtonState();
            _mainWindow = mainWindow;
        }

        private async void AutomaticSetupButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformAutomaticSetupAsync();
        }

        private async Task PerformAutomaticSetupAsync()
        {
            MainWindow mainWindow = new MainWindow();
            Close();
            try
            {
                await mainWindow.PerformAutomaticSetupAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("An error occurred during the automatic setup", ex);
            }
        }

        private void CustomServerButton_Click(object sender, RoutedEventArgs e)
        {
            OpenCustomServerWindow();
        }

        private async void OpenCustomServerWindow()
        {
            // Open the CustomServerWindow as a dialog
            CustomServerWindow customServerWindow = new CustomServerWindow();

            // Show the dialog and check if the user completed the operation (DialogResult is true)
            if (customServerWindow.ShowDialog() == true)
            {
                // Retrieve the connection details from the dialog's Tag property
                var data = customServerWindow.Tag as dynamic;
                if (data != null)
                {
                    // Extract the necessary connection details
                    string connectionString = data.ConnectionString;
                    string serverName = data.ServerName;
                    string userName = data.UserName;
                    string password = data.Password;

                    // Call PerformCustomSetupAsync with these parameters
                    await _mainWindow.PerformCustomSetupAsync(connectionString, serverName, userName, password);
                }
                else
                {
                    // Handle the case where data could not be retrieved
                    MessageBox.Show("Failed to retrieve the connection details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // The user canceled the operation
                MessageBox.Show("Custom server setup was canceled.", "Operation Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenHyperlink(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void OpenHyperlink(string uri)
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }

        private void ShowErrorMessage(string message, Exception ex)
        {
            MessageBox.Show($"{message}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetButtonState();
        }

        private void SetButtonState()
        {
            bool isTermsAccepted = AgreeTermsCheckBox.IsChecked == true && AgreeTensiometerCheckBox.IsChecked == true;
            AutomaticSetupButton.IsEnabled = isTermsAccepted;
            CustomServerButton.IsEnabled = isTermsAccepted;
        }
    }
}
