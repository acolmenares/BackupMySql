using System;
using System.Configuration;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace BackupMySql
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");


			var databaseName = ConfigurationManager.AppSettings["DatabaseNameMonitoreo"];
			var connectionString = ConfigurationManager.ConnectionStrings["ConnectionStringMonitoreo"].ConnectionString;
			var destinationDirectory = ConfigurationManager.AppSettings["DestinationDirectoryMonitoreo"];

			DoIt(databaseName, connectionString, destinationDirectory);
			Console.WriteLine("{0} completed ", databaseName);

			databaseName = ConfigurationManager.AppSettings["DatabaseNameAuditoria"];
			connectionString = ConfigurationManager.ConnectionStrings["ConnectionStringAuditoria"].ConnectionString;
			destinationDirectory = ConfigurationManager.AppSettings["DestinationDirectoryAuditoria"];

			DoIt(databaseName, connectionString, destinationDirectory);
			Console.WriteLine("{0} completed ", databaseName);

		}

		static void DoIt(string databaseName,string connectionString, string destinationDirectory)
		{
			
			var tempBackupfolder = System.IO.Directory.GetCurrentDirectory();
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


			var backupFileName = String.Format("{0}{1}-{2}.sql",
											   destinationDirectory, databaseName,
											   DateTime.Now.ToString("yyyyMMdd-HHmmss"));

			System.IO.File.Move(tempBackupFileName, backupFileName);
		}
	}
}
