using System;
using System.Configuration;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.IO.Compression;
using Duplicati.Library.Backend;
using System.Collections.Generic;

namespace BackupMySql
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

                        var backend = CrearConexionOneDrive();


			var databaseName = ConfigurationManager.AppSettings["DatabaseNameMonitoreo"];
			var connectionString = ConfigurationManager.ConnectionStrings["ConnectionStringMonitoreo"].ConnectionString;
			var destinationDirectory = ConfigurationManager.AppSettings["DestinationDirectoryMonitoreo"];

			DoIt(databaseName, connectionString, destinationDirectory, backend);
			Console.WriteLine("{0} completed ", databaseName);

			databaseName = ConfigurationManager.AppSettings["DatabaseNameAuditoria"];
			connectionString = ConfigurationManager.ConnectionStrings["ConnectionStringAuditoria"].ConnectionString;
			destinationDirectory = ConfigurationManager.AppSettings["DestinationDirectoryAuditoria"];

			DoIt(databaseName, connectionString, destinationDirectory,  backend );
			Console.WriteLine("{0} completed ", databaseName);

		}



        static OneDriveForBusinessBackend CrearConexionOneDrive()
        {
            var servidor = ConfigurationManager.AppSettings["Servidor"]; // "odb4://[]-my.sharepoint.com";
            var rutaEnElServidor = ConfigurationManager.AppSettings["RutaEnElServidor"];// "personal/[]_[]/Documents/duplicati";
            var username = ConfigurationManager.AppSettings["Username"];// "";
            var password = ConfigurationManager.AppSettings["Password"]; // "";
            var url = $"{servidor}/{rutaEnElServidor}";  //?auth-username={username}&auth-password={password}";

            var options = new Dictionary<string, string>();
            options.Add("auth-username", username);
            options.Add("auth-password", password);
            var server = new OneDriveForBusinessBackend(url, options);
            return server;
        }



		static void DoIt(string databaseName,string connectionString, string destinationDirectory, OneDriveForBusinessBackend backend)
		{

            var tempBackupfolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp") + System.IO.Path.DirectorySeparatorChar.ToString();
            if (!System.IO.Directory.Exists(tempBackupfolder))
            {
                System.IO.Directory.CreateDirectory(tempBackupfolder);
            }

            var tempBackupFileName = String.Format("{0}{1}-{2}.sql",
				tempBackupfolder, databaseName,
				DateTime.Now.ToString("yyyyMMdd-HHmmss"));

			var constr = string.Format("{0}; database={1}", connectionString, databaseName);
			using (MySqlConnection conn = new MySqlConnection(constr))
			{
				using (MySqlCommand cmd = new MySqlCommand())
				{
					using (MySqlBackup mb = new MySqlBackup(cmd))
					{
						cmd.Connection = conn;
						conn.Open();
						var dh = mb.ExportInfo.GetDocumentHeaders(cmd);
						dh.Add("/*!40101 SET NAMES utf8 */;");
						mb.ExportInfo.SetDocumentHeaders(dh);
						dh = mb.ExportInfo.GetDocumentHeaders(cmd);
						mb.ExportToFile(tempBackupFileName);
						conn.Close();
					}
				}
			}


            Console.WriteLine("bak file creado {0}", tempBackupFileName);

            Console.WriteLine("comprimiendo...");
            var tmpZipfile = string.Format("{0}.zip", tempBackupFileName);

            using (ZipArchive zip = ZipFile.Open(tmpZipfile, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(tempBackupFileName, System.IO.Path.GetFileName(tempBackupFileName));
            }

            Console.WriteLine("zip file creado {0}", tmpZipfile);

            Console.WriteLine("borrando bak file");
            System.IO.File.Delete(tempBackupFileName);
            Console.WriteLine("bak file borrado {0}", tempBackupFileName);


            try
            {
                Console.WriteLine("subiendo {0}", tmpZipfile);
                backend.Put(System.IO.Path.GetFileName(tmpZipfile), tmpZipfile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //

            var backupFileName = String.Format("{0}{1}-{2}.sql",
                                              destinationDirectory, databaseName,
                                              DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            var zipfile = string.Format("{0}.zip", backupFileName);

            // movi
            Console.WriteLine("moviendo el zip file {0} :  {1} ", tmpZipfile, zipfile);

            System.IO.File.Move(tmpZipfile, zipfile);

            Console.WriteLine("Fin");



		}
	}
}
