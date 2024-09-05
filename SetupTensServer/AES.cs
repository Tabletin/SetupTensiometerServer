using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace SetupTensServer
{
    internal class AES
    {
        public static byte[] bU = { 112, 111, 108, 111, 110, 105, 122, 111, 116 };
        //STATION 1
        //public static byte[] bP = { 83, 101, 114, 118, 105, 99, 101, 73, 122, 111, 116, 111, 112, 121, 46 };
        public static byte[] bP = { 73, 122, 111, 116, 111, 112, 121, 46 };
        public static byte[] kTo = { 100, 107, 120, 112, 98, 86, 82, 86, 101, 71, 112, 89, 99, 67, 57, 110, 83, 86, 74, 89, 85, 107, 120, 105, 81, 84, 81, 50, 100, 122, 48, 57 };

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        // Encrypt the value using a base64 token and an initialization vector. All of them as byte arrays
        // Initialization vector is optional.
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }

        public static string GenerateKey(string input)
        // Creates Key mix with an Base64 input string.
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static void SetUpSystemVariable(string systemVariableName, string keyValue)
        {
            // Creates System Variable with the Cypher value.
            Environment.SetEnvironmentVariable(systemVariableName, keyValue, EnvironmentVariableTarget.Machine);
        }

        public static void SetUpConnectionString(string connectionStringName, string connectionStringValue)
        {
            string documentsPath = @"C:\Users\Public\polonizot\app_tensiometer_pro";
            if (!Directory.Exists(documentsPath))
            {
                Directory.CreateDirectory(documentsPath);
            }
            string configFilePath = Path.Combine(documentsPath, "App2.config");

            // Load the existing app.config file or create new if it doesn't exist
            XDocument configXml = File.Exists(configFilePath)
                ? XDocument.Load(configFilePath)
                : new XDocument(new XElement("configuration"));

            // Add or update the ConnectionStrings element in the app.config file
            XElement connectionStringsElement = configXml.Root.Element("connectionStrings");
            if (connectionStringsElement == null)
            {
                connectionStringsElement = new XElement("connectionStrings");
                configXml.Root.Add(connectionStringsElement);
            }
            XElement connectionStringElement = connectionStringsElement
                .Elements("add")
                .FirstOrDefault(x => x.Attribute("name")?.Value == connectionStringName);
            if (connectionStringElement == null)
            {
                connectionStringElement = new XElement("add");
                connectionStringsElement.Add(connectionStringElement);
            }
            connectionStringElement.SetAttributeValue("name", connectionStringName);
            connectionStringElement.SetAttributeValue("connectionString", connectionStringValue);

            // Save the app.config file
            configXml.Save(configFilePath);
        }

        public static byte[] Decrypt(byte[] encryptedData, byte[] key, byte[] iv)
        // Decrypt the value using a KEY and an initialization vector. All of them as byte arrays
        // Depending on how was encrypted the value the initialization vector is optional or not.
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(encryptedData, 0, encryptedData.Length);
                    }
                    return msDecrypt.ToArray();
                }
            }
        }

        static public string getU()
        {
            byte[] cypher = Encrypt(bU, kTo, new byte[16]);

            return Encoding.UTF8.GetString(Decrypt(cypher, kTo, new byte[16]));
        }


        static public string getP()
        {
            byte[] cypher = Encrypt(bP, kTo, new byte[16]);

            return Encoding.UTF8.GetString(Decrypt(cypher, kTo, new byte[16]));
        }

    }
}
