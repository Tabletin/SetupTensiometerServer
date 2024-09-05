using Microsoft.Win32;
using System.Windows;

namespace SetupTensServer
{
    public partial class RestoreDatabaseWindow : Window
    {
        public RestoreDatabaseWindow()
        {
            InitializeComponent();
        }

        // Browse button to open file dialog
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName; // Show selected file path in TextBox
            }
        }

        // Restore button click handler
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = FilePathTextBox.Text;

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Please select a file to restore the database.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Call the database restore logic here using the filePath
            MessageBox.Show($"Restoring database from {filePath}", "Database Restore", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
