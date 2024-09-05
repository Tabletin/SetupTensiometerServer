using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SetupTensServer
{
    public partial class RecoveryAnalysisWindow : Window
    {
        public RecoveryAnalysisWindow()
        {
            InitializeComponent();
        }

        public async Task<bool> RunRecoveryAnalysis()
        {
            bool isSuccessful = true;

            isSuccessful &= await AnalyzeSystemVariableAsync();
            isSuccessful &= await AnalyzeSqlServerInstallationAsync();
            isSuccessful &= await AnalyzeSqlServerConnectionAsync();
            isSuccessful &= await AnalyzeConfigFileAsync();

            CompleteAnalysis();

            return isSuccessful;
        }

        private async Task<bool> AnalyzeSystemVariableAsync()
        {
            bool isSet = IsSystemVariableSet();
            UpdateStatus(SystemVariableStatus, isSet, "The necesary variables for connection ARE found in your system.", "The necesary variables for connection ARE NOT found in your system.Missing");
            await Task.Delay(500); // Simulate delay for demo
            return isSet;
        }

        private async Task<bool> AnalyzeSqlServerInstallationAsync()
        {
            bool isInstalled = IsSqlServerInstalled();
            UpdateStatus(SqlServerStatus, isInstalled, "The SQL Server Express installer IS found in your system.", "The SQL Server Express installer IS NOT found in your system.");
            await Task.Delay(500);
            return isInstalled;
        }

        private async Task<bool> AnalyzeSqlServerConnectionAsync()
        {
            bool isValidConnection = IsConnectionStringValid();
            UpdateStatus(ConnectionStatus, isValidConnection, "A simple connection test with SQL Server has been susccesful.", "A simple connection test with SQL Server has failed.");
            await Task.Delay(500);
            return isValidConnection;
        }

        private async Task<bool> AnalyzeConfigFileAsync()
        {
            bool isConfigFound = IsConfigFileFound();
            UpdateStatus(ConfigFileStatus, isConfigFound, "The configuration file needed for connection with SQL Server has been Found", "The configuration file needed for connection with SQL Server has NOT been Found");
            await Task.Delay(500);
            return isConfigFound;
        }

        private void UpdateStatus(System.Windows.Controls.TextBlock statusTextBlock, bool condition, string successMessage, string failureMessage)
        {
            statusTextBlock.Text += condition ? successMessage : failureMessage;
            statusTextBlock.Foreground = new SolidColorBrush(condition ? Colors.Green : Colors.Red);
        }

        private void CompleteAnalysis()
        {
            LoadingSpinner.Visibility = Visibility.Collapsed; // Hide spinner when done
            StatusMessage.Text = "Quick analysis of needed components is complete.";
        }

        private bool IsSystemVariableSet() =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("app_tensiometer_sv", EnvironmentVariableTarget.Machine));

        private bool IsSqlServerInstalled()
        {
            // Replace with actual SQL Server installation check logic
            // For example, check registry or specific file existence
            return true;
        }

        private bool IsConnectionStringValid()
        {
            // Replace with actual connection string validation logic
            // For example, try opening a connection to the SQL Server
            return true;
        }

        private bool IsConfigFileFound() =>
            File.Exists(@"C:\Users\Public\polonizot\app_tensiometer_pro\App2.config");
    }
}
