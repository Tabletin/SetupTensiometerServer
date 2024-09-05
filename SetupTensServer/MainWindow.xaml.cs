using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Windows;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace SetupTensServer
{


    public partial class MainWindow : Window
    {
        private const string InstanceName = "TENSIOMETER_PRO";
        private const string SqlServerInstallerPath = @"D:\_J-Technologies\TEST_SQL_Quiet\SQLEXPR_x64_ENU\SETUP.EXE";
        //private const string SqlServerInstallerPath = @"SQLEXPR_x64_ENU\SETUP.EXE";
        public const string BackupDirectory = @"D:\_J-Technologies\_Polonizot\resources\app_tensiometer\db";
        //public const string BackupDirectory = @"DbSource\";
        private const string InstallerDownloadUrl = "https://download.microsoft.com/download/5/1/4/5145fe04-4d30-4b85-b0d1-39533663a2f1/SQL2022-SSEI-Expr.exe";
        private const string DefaultConnectionString = $"Server=localhost\\{InstanceName};Integrated Security=True; TrustServerCertificate=True;";
        private const string TargetDir = BackupDirectory;
        string targetDir = "";
        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            var setupOptionsWindow = new UserOptionsWindow(this);
            setupOptionsWindow.ShowDialog();
        }

        private void OpenUserOptionsWindow()
        {
            UserOptionsWindow userOptionsWindow = new UserOptionsWindow(this);
            userOptionsWindow.ShowDialog();
        }

        private void RestoreDatabase_Click(object sender, RoutedEventArgs e) { 
            
        }

        public async Task PerformAutomaticSetupAsync(bool isCustomSetup = false)
        {
            string logFilePath = GetLogFilePath();
            LogError(logFilePath, "PerformAutomaticSetupAsync triggered.");

            if (!IsUserAdministrator())
            {
                const string message = "Please run this application as an administrator.";
                MessageBox.Show(message, "Permission Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                LogError(logFilePath, message);
                return;
            }

            LogError(logFilePath, "User has administrative privileges.");

            if (!File.Exists(SqlServerInstallerPath))
            {
                const string message = "SQL Server installer not found.";
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogError(logFilePath, message);
                return;
            }

            LogError(logFilePath, $"SQL Server installer found at {SqlServerInstallerPath}.");

            try
            {
                LogError(logFilePath, "Starting SQL Server installation process...");
                await InstallSqlServerAsync().ConfigureAwait(false);
                LogError(logFilePath, "SQL Server installation attempted.");

                var sqlServerDetailsResult = isCustomSetup ? TryGetSqlServerDetailsCustom() : TryGetSqlServerDetailsAutomatic();
                if (!sqlServerDetailsResult.Success)
                {
                    MessageBox.Show(sqlServerDetailsResult.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    LogError(logFilePath, sqlServerDetailsResult.ErrorMessage);
                    return;
                }

                string connectionString = sqlServerDetailsResult.ConnectionString;
                string givenSqlServer = sqlServerDetailsResult.GivenSqlServer;
                string user = AES.getU();
                string password = AES.getP();

                LogError(logFilePath, $"========================================BEGIN=======================================================");
                LogError(logFilePath, $"Connection String: **************************************************************************");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"Given SQL Server: {givenSqlServer}");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"User: {user}");
                LogError(logFilePath, $"Password: ***********************");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"SQL Executable Path: {SqlServerInstallerPath}");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"========================================END=======================================================");

                LicenseFilesGeneration("InitialLicenseString", targetDir);
                ConfigureDatabaseConnection(connectionString, givenSqlServer);
                RestoreDatabase(connectionString, BackupDirectory);
                SetupConnectionString(givenSqlServer, user, password);

                string polonizotConnectionString = $"Server=localhost\\{InstanceName}; Database=master;User Id={user};Password={password};Integrated Security=false; TrustServerCertificate=True;";
                SqlServerPermissionManager permissionManager = new SqlServerPermissionManager(polonizotConnectionString);
                permissionManager.DenyConnectionForAllLocalWindowsUsersExceptPolonizot();

                MessageBox.Show("Permissions for all local Windows users except 'polonizot' have been denied.");
            }
            catch (Exception ex)
            {
                string errorMessage = $"An unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
                MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogError(logFilePath, errorMessage);
            }
        }

        public async Task PerformCustomSetupAsync(string connectionString, string server, string user, string password)
        {
            string logFilePath = GetLogFilePath();
            LogError(logFilePath, "PerformCustomSetupAsync triggered.");

            try
            {
                LogError(logFilePath, $"========================================BEGIN=======================================================");
                LogError(logFilePath, $"Connection String: **************************************************************************");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"Given SQL Server: {connectionString}");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"User: {user}");
                LogError(logFilePath, $"Password: ***********************");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"===============================================================================================");
                LogError(logFilePath, $"========================================END=======================================================");

                LicenseFilesGeneration("CustomLicenseString", targetDir);
                ConfigureDatabaseConnection(connectionString, server);
                SetupConnectionString(server, user, password);

                SqlServerPermissionManager permissionManager = new SqlServerPermissionManager(connectionString);
                permissionManager.DenyConnectionForAllLocalWindowsUsersExceptPolonizot();

                // Ask the user if they want to restore the database
                var restoreResult = MessageBox.Show("Do you want to restore the database?", "Restore Database", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (restoreResult == MessageBoxResult.Yes)
                {
                    RestoreDatabase(connectionString, BackupDirectory);
                    LogError(logFilePath, "Database restored successfully.");
                    MessageBox.Show("Database restored successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LogError(logFilePath, "User chose not to restore the database.");
                    MessageBox.Show("Connection to server restored without changes to the database.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                MessageBox.Show("Custom setup completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = $"An unexpected error occurred during custom setup: {ex.Message}\n{ex.StackTrace}";
                MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogError(logFilePath, errorMessage);
            }
        }


        private string GetLogFilePath()
        {
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TENSIOMETER_PRO_DB_SERVER", "SetupLog.txt");
            string logDirectory = Path.GetDirectoryName(logFilePath);

            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create log directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return logFilePath;
        }

        private bool IsUserAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async Task InstallSqlServerAsync()
        {
            var installerArguments = new[]
            {
                "/QS",
                "/IACCEPTSQLSERVERLICENSETERMS",
                "/ACTION=install",
                $"/INSTANCENAME={InstanceName}",
                "/FEATURES=SQLENGINE,Tools",
                "/SQLSVCACCOUNT=\"NT AUTHORITY\\SYSTEM\"",
                "/SQLSVCSTARTUPTYPE=Automatic",
                "/SQLCOLLATION=SQL_Latin1_General_CP1_CI_AS",
                "/SQLSYSADMINACCOUNTS=\"BUILTIN\\Administrators\""
            };

            var processStartInfo = new ProcessStartInfo
            {
                FileName = SqlServerInstallerPath,
                Arguments = string.Join(" ", installerArguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);
                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                var errors = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

                if (process.ExitCode == 0)
                {
                    MessageBox.Show("SQL Server installed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LogError(GetLogFilePath(), "SQL Server installed successfully.");
                }
                else
                {
                    string errorMessage = $"SQL Server installation failed: {errors}";
                    MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    LogError(GetLogFilePath(), errorMessage);
                }
            }
        }

        private (bool Success, string ConnectionString, string GivenSqlServer, string ErrorMessage) TryGetSqlServerDetailsAutomatic()
        {
            string connectionString = DefaultConnectionString;
            string givenSqlServer = "localhost\\TENSIOMETER_PRO";
            string errorMessage = string.Empty;

            var isInstalled = IsSqlServerInstalled(connectionString, givenSqlServer);
            if (isInstalled)
            {
                if (IsConnectionStringValid(connectionString))
                {
                    return (true, connectionString, givenSqlServer, errorMessage);
                }
                else
                {
                    errorMessage = "Failed to connect using the default SQL Server connection string.";
                    LogError(GetLogFilePath(), errorMessage);
                    return (false, connectionString, givenSqlServer, errorMessage);
                }
            }
            else
            {
                errorMessage = "SQL Server is not installed or the version is lower than required.";
                LogError(GetLogFilePath(), errorMessage);
                return (false, connectionString, givenSqlServer, errorMessage);
            }
        }

        private bool IsSqlServerInstalled(string connectionString, string givenSqlServer)
        {
            const string registryKey = @"SOFTWARE\Microsoft\Microsoft SQL Server\TENSIOMETER_PRO\MSSQLServer\CurrentVersion";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key == null)
                {
                    return false;
                }

                Version existingVersion = new Version(key.GetValue("CurrentVersion").ToString());
                Version minTargetVersion = new Version("16.0");

                return existingVersion.CompareTo(minTargetVersion) >= 0;
            }
        }
        // Tries to get SQL Server details based on user input
        private (bool Success, string ConnectionString, string GivenSqlServer, string ErrorMessage) TryGetSqlServerDetailsCustom()
        {
            string connectionString = string.Empty;
            string givenSqlServer = string.Empty;
            string errorMessage = string.Empty;

            // Prompt the user for the SQL Server name
            givenSqlServer = Interaction.InputBox("Enter the SQL Server name to connect to:", "SQL Server Validation", @"localhost\TENSIOMETER_PRO");

            // Construct the connection string
            connectionString = $"Server={givenSqlServer};Database=master;Integrated Security=true;TrustServerCertificate=True;";

            // Log connection details for debugging
            LogError(GetLogFilePath(), $"Connection string successfully created: {connectionString}");
            LogError(GetLogFilePath(), $"Given SQL Server: {givenSqlServer}");

            if (IsConnectionStringValid(connectionString))
            {
                return (true, connectionString, givenSqlServer, errorMessage);
            }
            else
            {
                errorMessage = "Invalid connection string.";
                LogError(GetLogFilePath(), errorMessage);
                return (false, connectionString, givenSqlServer, errorMessage);
            }
        }

        // Method to generate license files and handle licensing
        static void LicenseFilesGeneration(string licenseString, string targetDir)
        {
            string licensePath = Path.Combine(targetDir, "license\\");
            string licenseJsonText;
            string licenseSignature;

            // Ensure the license directory exists
            if (!Directory.Exists(licensePath))
            {
                Directory.CreateDirectory(licensePath);
            }

            try
            {
                // Decode license information
                byte[] licenseJsonByte = Convert.FromBase64String(licenseString.Split('.')[0]);
                licenseJsonText = Encoding.UTF8.GetString(licenseJsonByte);
                licenseSignature = licenseString.Split('.')[1];
                byte[] licenseSignatureByte = Convert.FromBase64String(licenseString.Split('.')[1]);

                // Verify the license signature
                using (ECDsa ecdsa = ECDsa.Create())
                {
                    byte[] publicKey = Convert.FromBase64String(@"MIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQBvstT86hgOO+H5KHdTcJG+EhvR2bROCiIqDqdC06xahEV2Gt67msATADtM5jhyACubmQ88nraSOdkdXB1bTbfOYYA0w6YyuZihMDTqam5DIGpY002K+NmXSFCvHHjhgrntS/hplHzoY/U8UIJuDRAgwwtbL1dcvfocRzvLsoiuYYgEXs=");
                    ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                    if (!ecdsa.VerifyData(licenseJsonByte, licenseSignatureByte, HashAlgorithmName.SHA256))
                    {
                        throw new Exception("Invalid signature");
                    }
                }

                // Write the license and signature to files
                File.WriteAllText(Path.Combine(licensePath, "license.dat"), licenseJsonText);
                File.WriteAllText(Path.Combine(licensePath, "license.dat.signature"), licenseSignature);
            }
            catch
            {
                // If the license is invalid, prompt the user for a trial installation
                //System.Windows.MessageBox.Show("Your instance will create a trial version of license for Tensiometer Pro");

                // Create a trial license
                var jsonObject = new { IsTrial = "True" };
                licenseJsonText = JsonSerializer.Serialize(jsonObject);
                licenseSignature = string.Empty;

                // Write the trial license and empty signature to files
                File.WriteAllText(Path.Combine(licensePath, "license.dat"), licenseJsonText);
                File.WriteAllText(Path.Combine(licensePath, "license.dat.signature"), licenseSignature);
            }
        }

        private void LogError(string logFilePath, string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to write to log file: {ex.Message}", "Logging Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsConnectionStringValid(string connectionString)
        {
            try
            {
                using (var testConnection = new SqlConnection(connectionString))
                {
                    testConnection.Open();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ConfigureDatabaseConnection(string connectionString, string givenSqlServer)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int loginMode = GetLoginMode(connection);
                    if (loginMode == 1)
                    {
                        ChangeToMixedModeAuthentication(connection);
                        RestartSqlService(givenSqlServer);
                    }

                    string loginName = AES.getU();
                    string password = AES.getP();

                    if (string.IsNullOrEmpty(loginName) || string.IsNullOrEmpty(password))
                    {
                        throw new Exception("Invalid Login and Password;");
                    }

                    CreateLoginIfNotExists(connection, loginName, password);
                    EnsureRoleExists(connection, "AppTensiometerPro");
                    GrantRolesToLogin(connection, loginName);
                    SetupConnectionString(givenSqlServer, loginName, password);
                }

                //MessageBox.Show("Ustalenie połączenia do bazy danych za pomocą loginu i hasła zakończone sukcesem.");
                MessageBox.Show("Establishing a connection to the database using login and password was successful.");
            }
            catch (Exception e)
            {
                MessageBox.Show("Problem Configuring Login and Password: " + e.Message + "\n" + e.StackTrace);
                throw;
            }
        }

        private static int GetLoginMode(SqlConnection connection)
        {
            using (var commandGetValue = new SqlCommand("EXEC xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'LoginMode'", connection))
            {
                using (var reader = commandGetValue.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new Exception("No access to Login Mode");
                    }

                    return (int)reader[1];
                }
            }
        }

        private static void ChangeToMixedModeAuthentication(SqlConnection connection)
        {
            using (var commandChangeAuthentication = new SqlCommand("EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', N'Software\\Microsoft\\MSSQLServer\\MSSQLServer', N'LoginMode', REG_DWORD, 2;", connection))
            {
                commandChangeAuthentication.ExecuteNonQuery();
            }
        }

        private static void RestartSqlService(string instanceName)
        {
            string serviceName = instanceName.Equals("MSSQL", StringComparison.OrdinalIgnoreCase)
                ? instanceName
                : $"MSSQL${InstanceName}";

            try
            {
                var serviceController = new ServiceController(serviceName);

                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                }

                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error restarting SQL Server service: " + ex.Message);
                throw;
            }
        }

        private static bool IsLoginNameExists(SqlConnection connection, string loginName)
        {
            using (var loginCheckCommand = new SqlCommand("SELECT name FROM sys.syslogins WHERE name = @Username", connection))
            {
                loginCheckCommand.Parameters.AddWithValue("@Username", loginName);
                return loginCheckCommand.ExecuteScalar() != null;
            }
        }

        private static void CreateLoginIfNotExists(SqlConnection connection, string loginName, string password)
        {
            if (!IsLoginNameExists(connection, loginName))
            {
                using (var createLoginCommand = new SqlCommand($"CREATE LOGIN {loginName} WITH PASSWORD = '{password}';", connection))
                {
                    createLoginCommand.ExecuteNonQuery();
                }
            }
        }

        private static void EnsureRoleExists(SqlConnection connection, string roleName)
        {
            string checkRoleQuery = $"SELECT name FROM sys.server_principals WHERE type = 'R' AND name = '{roleName}';";
            using (var checkRoleCommand = new SqlCommand(checkRoleQuery, connection))
            {
                if (checkRoleCommand.ExecuteScalar() == null)
                {
                    string createRoleQuery = $"CREATE SERVER ROLE [{roleName}];";
                    using (var createRoleCommand = new SqlCommand(createRoleQuery, connection))
                    {
                        createRoleCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void GrantRolesToLogin(SqlConnection connection, string loginName)
        {
            var roles = new[] { "AppTensiometerPro", "dbcreator", "sysadmin" };
            foreach (var role in roles)
            {
                using (var alterRoleCommand = new SqlCommand($"ALTER SERVER ROLE {role} ADD MEMBER {loginName};", connection))
                {
                    alterRoleCommand.ExecuteNonQuery();
                }
            }
        }

        private static void SetupConnectionString(string givenSqlServer, string loginName, string password)
        {
            string connectionString = $"Server={givenSqlServer};Database=tensiometer_pro;User Id={loginName};Password={password};Integrated Security=false;TrustServerCertificate=true";
            byte[] keyToken = Encoding.UTF8.GetBytes(AES.GenerateKey(@"Y29tcGFueWhpZGRlbnBvcmNoc3RyYW5nZXJzZWVwcmlkZXNsZXB0Y2FyZWNvdXJzZW8="));
            byte[] initializationVector = new byte[16];
            byte[] encrypted = AES.Encrypt(Encoding.UTF8.GetBytes(connectionString), keyToken, initializationVector);
            AES.SetUpSystemVariable(@"app_tensiometer_sv", Convert.ToBase64String(keyToken));
            AES.SetUpConnectionString(@"ProDb", Convert.ToBase64String(encrypted));
        }

        public static void RestoreDatabase(string connectionString, string targetDir)
        {
            string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TENSIOMETER_PRO_DB_SERVER", "Logs");
            Directory.CreateDirectory(logDirectory);
            string logFilePath = Path.Combine(logDirectory, "RestoreLog.txt");

            using (var log = new StreamWriter(logFilePath, true))
            {
                log.WriteLine("Starting database restoration...");

                const string deleteDBCommandText = @"IF EXISTS (SELECT * FROM sys.databases WHERE [name] = 'tensiometer_pro')
                                                    BEGIN
                                                        USE master;
                                                        ALTER DATABASE [tensiometer_pro] SET OFFLINE;
                                                        DROP DATABASE [tensiometer_pro];
                                                    END";

                const string databaseName = "tensiometer_pro";
                string bakOriginalPath = Path.Combine(targetDir, @"database_backup.bak");

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        log.WriteLine("Connected to the SQL Server instance.");

                        using (var deleteDBCommand = new SqlCommand(deleteDBCommandText, connection))
                        {
                            log.WriteLine("Deleting existing database if it exists...");
                            deleteDBCommand.ExecuteNonQuery();
                            log.WriteLine("Existing database deleted (if it existed).");
                        }

                        log.WriteLine("Getting SQL Server paths...");
                        var (dataFilePath, logFileRelocationPath, defaultBackupPath) = GetSqlServerPaths(connection);
                        log.WriteLine($"SQL Server paths obtained: Data file path: {dataFilePath}, Log file path: {logFileRelocationPath}, Backup path: {defaultBackupPath}");

                        log.WriteLine($"Copying backup file from {bakOriginalPath} to {defaultBackupPath}...");
                        File.Copy(bakOriginalPath, defaultBackupPath, true);
                        log.WriteLine("Backup file copied.");

                        string restoreQuery = $"RESTORE DATABASE [{databaseName}] FROM DISK = '{defaultBackupPath}' WITH REPLACE, MOVE '{databaseName}' TO '{dataFilePath}', MOVE '{databaseName}_log' TO '{logFileRelocationPath}'";
                        using (var commandRestoreDB = new SqlCommand(restoreQuery, connection))
                        {
                            log.WriteLine("Restoring database...");
                            commandRestoreDB.ExecuteNonQuery();
                            log.WriteLine("Database restored successfully.");
                        }

                        log.WriteLine("Granting database permissions...");
                        GrantDatabasePermissions(connection, databaseName);
                        log.WriteLine("Database permissions granted successfully.");
                    }
                }
                catch (Exception ex)
                {
                    log.WriteLine($"Exception during restoration: {ex.Message}");
                    log.WriteLine($"Stack Trace: {ex.StackTrace}");
                    MessageBox.Show($"Database restoration failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                log.WriteLine("Restoration process completed.");
            }
        }

        private static (string, string, string) GetSqlServerPaths(SqlConnection connection)
        {
            using (var backupQuery = new SqlCommand("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(MAX))", connection))
            using (var dataPathQuery = new SqlCommand("SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(MAX))", connection))
            {
                string dataFilePath = Path.Combine((string)dataPathQuery.ExecuteScalar(), "tensiometer_pro.mdf");
                string logFileRelocationPath = Path.Combine((string)dataPathQuery.ExecuteScalar(), "tensiometer_pro_log.ldf");
                string defaultBackupPath = Path.Combine((string)backupQuery.ExecuteScalar(), "database_backup.bak");
                return (dataFilePath, logFileRelocationPath, defaultBackupPath);
            }
        }

        private static void GrantDatabasePermissions(SqlConnection connection, string databaseName)
        {
            try
            {
                var userNames = new List<string>();
                using (var commandGetUserName = new SqlCommand("USE tensiometer_pro; SELECT name FROM sys.database_principals WHERE [type] = 'S'", connection))
                using (var readerUserName = commandGetUserName.ExecuteReader())
                {
                    while (readerUserName.Read())
                    {
                        userNames.Add(readerUserName.GetString(0));
                    }
                }

                foreach (var userName in userNames)
                {
                    GrantPermissionsToUser(connection, databaseName, userName);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error granting database permissions: " + e.Message);
                throw;
            }
        }

        private static void GrantPermissionsToUser(SqlConnection connection, string databaseName, string userName)
        {
            string grantPermissionsCommandText = $@"USE {databaseName}; 
                                                GRANT SELECT, INSERT, UPDATE, DELETE ON DATABASE::{databaseName} TO {userName}; 
                                                GRANT BACKUP DATABASE TO [{userName}];";
            using (var grantDBPermissionCommand = new SqlCommand(grantPermissionsCommandText, connection))
            {
                grantDBPermissionCommand.ExecuteNonQuery();
            }
        }

        private bool CheckLastFullBackup()
        {
            string backupFilePath = Path.Combine(BackupDirectory, "database_backup.bak");
            return File.Exists(backupFilePath);
        }

        private void RemoveExistingConfiguration()
        {
            string configFileName = "app2.config";
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);

            if (File.Exists(configPath))
            {
                File.Delete(configPath);
                LogError(GetLogFilePath(), "Configuration file deleted.");
            }
            else
            {
                LogError(GetLogFilePath(), "Configuration file not found.");
            }

            Environment.SetEnvironmentVariable("app_tensiometer_sv", null, EnvironmentVariableTarget.Machine);
            LogError(GetLogFilePath(), "System environment variable 'app_tensiometer_sv' removed.");
            LogError(GetLogFilePath(), "Existing configuration, including connection string and system variable, removed.");
        }

        private async void RecoveryButton_Click(object sender, RoutedEventArgs e)
        {
            string recoveryLogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TENSIOMETER_PRO_DB_SERVER", "recovery_log.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(recoveryLogFilePath));

            LogError(recoveryLogFilePath, "Recovery process initiated.");

            // Show the Recovery Analysis Window
            var recoveryAnalysisWindow = new RecoveryAnalysisWindow();
            recoveryAnalysisWindow.Show();

            try
            {
                // Run the analysis asynchronously while the recovery window is open
                var analysisResult = await recoveryAnalysisWindow.RunRecoveryAnalysis();

                // Show the custom dialog to prompt the user for action
                var dialog = new RecoveryCustomDialog();
                if (dialog.ShowDialog() == true)
                {
                    switch (dialog.Result)
                    {
                        case RecoveryCustomDialog.RecoveryCustomDialogResult.AdvanceRecovery:
                            // User opts to try connecting to the existing SQL Server
                            var credentialsWindow = new UserCredentialsWindow();
                            if (credentialsWindow.ShowDialog() == true)
                            {
                                string serverName = credentialsWindow.ServerNameTextBox.Text;
                                string userName = credentialsWindow.UserNameTextBox.Text;
                                string password = credentialsWindow.PasswordBox.Password;

                                // Attempt to connect using the provided credentials
                                try
                                {
                                    string connectionString = $"Server={serverName};Database=master;User Id={userName};Password={password};TrustServerCertificate=True;";
                                    if (IsConnectionStringValid(connectionString))
                                    {
                                        LogError(recoveryLogFilePath, "User successfully connected to SQL Server with provided credentials.");
                                        MessageBox.Show("Successfully connected to SQL Server.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                                        // Recreate connection string, system variable, and app config
                                        LogError(recoveryLogFilePath, "Recreating connection string, system variable, and app config.");
                                        ConfigureDatabaseConnection(connectionString, serverName);

                                        // Ask the user if they want to restore the database
                                        var restoreResult = MessageBox.Show("Do you want to restore the database?", "Restore Database", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                        if (restoreResult == MessageBoxResult.Yes)
                                        {
                                            RestoreDatabase(connectionString, BackupDirectory);
                                            LogError(recoveryLogFilePath, "Database restored successfully.");
                                            MessageBox.Show("Database restored successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                        }
                                        else
                                        {
                                            LogError(recoveryLogFilePath, "User chose not to restore the database.");
                                            MessageBox.Show("Connection to server restored. You can try now to run the app with no changes in the database.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                                        }
                                    }
                                    else
                                    {
                                        const string message = "Failed to connect to SQL Server with provided credentials. It is recommended to reinstall the server.";
                                        MessageBox.Show(message, "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                        LogError(recoveryLogFilePath, message);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = $"An unexpected error occurred during the manual connection attempt: {ex.Message}\n{ex.StackTrace}";
                                    MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    LogError(recoveryLogFilePath, errorMessage);
                                }
                            }
                            else
                            {
                                LogError(recoveryLogFilePath, "User cancelled the manual connection attempt.");
                            }
                            break;

                        case RecoveryCustomDialog.RecoveryCustomDialogResult.AutomaticRecovery:
                            // User opts to reinstall the server
                            LogError(recoveryLogFilePath, "User opted to make an automatic recovery. App will try to refresh connection to server.");

                            if (CheckInstallerIntegrity())
                            {
                                LogError(recoveryLogFilePath, "SQL Server installer integrity check passed. Proceeding with fresh installation.");
                                await UninstallSqlServerAsync();
                                await InstallSqlServerAsync();
                            }
                            else
                            {
                                LogError(recoveryLogFilePath, "SQL Server installer is corrupted. Attempting to download the latest version.");
                                if (await DownloadInstallerAsync())
                                {
                                    //await InstallSqlServerAsync();
                                    await PerformAutomaticSetupAsync();
                                }
                                else
                                {
                                    const string message = "Download failed. Please connect to the internet and try again.";
                                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    LogError(recoveryLogFilePath, message);
                                    return;
                                }
                            }

                            LogError(recoveryLogFilePath, "Recreating connection string, system variable, and app config after reinstallation.");
                            ConfigureDatabaseConnection("NewConnectionString", "NewServerName");

                            // Ask the user if they want to restore the database
                            var reinstallRestoreResult = MessageBox.Show("Do you want to restore the database?", "Restore Database", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (reinstallRestoreResult == MessageBoxResult.Yes)
                            {
                                RestoreDatabase("NewConnectionString", BackupDirectory);
                                LogError(recoveryLogFilePath, "Database restored successfully.");
                                MessageBox.Show("Database restored successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                LogError(recoveryLogFilePath, "User chose not to restore the database after reinstallation.");
                                MessageBox.Show("SQL Server reinstalled. Connection is restored without changes to the database.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            break;

                        case RecoveryCustomDialog.RecoveryCustomDialogResult.Cancel:
                            // Handle cancellation if needed
                            LogError(recoveryLogFilePath, "User cancelled the recovery process.");
                            break;
                    }
                }
                else
                {
                    LogError(recoveryLogFilePath, "User cancelled the recovery process.");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An unexpected error occurred during recovery: {ex.Message}\n{ex.StackTrace}";
                MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogError(recoveryLogFilePath, errorMessage);
            }
            finally
            {
                // Ensure the recovery analysis window is closed even if an error occurs
                recoveryAnalysisWindow.Close();
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to uninstall the SQL Server instance TENSIOMETER_PRO?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                await UninstallSqlServerAsync().ConfigureAwait(false);
            }
        }

        private async Task UninstallSqlServerAsync()
        {
            var uninstallArguments = new[]
            {
                "/ACTION=Uninstall",
                $"/INSTANCENAME={InstanceName}",
                "/FEATURES=SQLENGINE",
                "/Q"
            };

            var processStartInfo = new ProcessStartInfo
            {
                FileName = SqlServerInstallerPath,
                Arguments = string.Join(" ", uninstallArguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);
                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                var errors = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

                if (process.ExitCode == 0)
                {
                    MessageBox.Show("SQL Server uninstalled successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"SQL Server uninstallation failed: {errors}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CheckInstallerIntegrity()
        {
            return File.Exists(SqlServerInstallerPath);
        }

        private async Task<bool> DownloadInstallerAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(InstallerDownloadUrl).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        await using var fileStream = new FileStream(SqlServerInstallerPath, FileMode.Create);
                        await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
    }

    public class SqlServerPermissionManager
    {
        private readonly string connectionString;

        public SqlServerPermissionManager(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void DenyConnectionForAllLocalWindowsUsersExceptPolonizot()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var loginNames = GetLocalWindowsLoginNames(connection);
                foreach (var loginName in loginNames)
                {
                    if (!loginName.Equals("polonizot", StringComparison.OrdinalIgnoreCase))
                    {
                        DenyUserConnect(connection, loginName);
                        DisableUserLogin(connection, loginName);
                    }
                }
            }
        }

        private List<string> GetLocalWindowsLoginNames(SqlConnection connection)
        {
            var loginNames = new List<string>();
            using (var getLoginsCommand = new SqlCommand("SELECT name FROM sys.server_principals WHERE type_desc = 'WINDOWS_LOGIN'", connection))
            using (var reader = getLoginsCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    loginNames.Add(reader.GetString(0));
                }
            }

            return loginNames;
        }

        private void DenyUserConnect(SqlConnection connection, string loginName)
        {
            string denySql = $"DENY CONNECT SQL TO [{loginName}];";
            using (var denyCommand = new SqlCommand(denySql, connection))
            {
                denyCommand.ExecuteNonQuery();
                Console.WriteLine($"Connection permission denied to {loginName}.");
            }
        }

        private void DisableUserLogin(SqlConnection connection, string loginName)
        {
            string disableLoginSql = $"ALTER LOGIN [{loginName}] DISABLE;";
            using (var disableLoginCommand = new SqlCommand(disableLoginSql, connection))
            {
                disableLoginCommand.ExecuteNonQuery();
                Console.WriteLine($"Login disabled for {loginName}.");
            }
        }
    }
}
