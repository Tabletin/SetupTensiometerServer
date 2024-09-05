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
    /// Interaction logic for RecoveryCustomDialog.xaml
    /// </summary>
    public partial class RecoveryCustomDialog : Window
    {

        public enum RecoveryCustomDialogResult
        {
            AutomaticRecovery,
            AdvanceRecovery,
            Cancel
        }

        public RecoveryCustomDialogResult Result { get; private set; }

        public RecoveryCustomDialog()
        {
            InitializeComponent();
        }

        private void AutomaticRecovery_Click(object sender, RoutedEventArgs e)
        {
            // Display a confirmation dialog box
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to proceed with the automatic recovery? This will attempt to refresh connection with the Database Server.  ",
                "Proceed with caution!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            // Check the user's choice
            if (result == MessageBoxResult.Yes)

            {
                // Proceed with the automatic recovery
                Result = RecoveryCustomDialogResult.AutomaticRecovery;
                DialogResult = true;
            }
          
        }


        private void AdvancedRecovery_Click(object sender, RoutedEventArgs e)
        {
            Result = RecoveryCustomDialogResult.AdvanceRecovery;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = RecoveryCustomDialogResult.Cancel;
            DialogResult = false;
        }
    }
}
