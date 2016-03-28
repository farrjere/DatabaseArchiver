using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Farrellcrafts.DatabaseArchiver
{
    public class DatabaseArchiver
    {
        private string connection;
        private string database;
        private SqlCredential credential;
        private string outputDir;
        private List<string> tables = new List<string>();
        public DatabaseArchiver(string outputLocation, string server, string database, string user, SecureString password) {
            outputDir = outputLocation;
            SetConnection(server, database);
            SetCredential(user, password);
            ObtainTables();
            ArchiveTables();
        }

        //Test constructor
        public DatabaseArchiver(string outputLocation, string connection, List<string> tables)
        {
            outputDir = outputLocation;
            this.tables.AddRange(tables);
            this.connection = connection;
            ArchiveTables();
        }

        private void SetConnection(string server, string database)
        {
            this.database = database;
            connection = "Data Source=" + server + ";Initial Catalog=" + database;
        }

        private void SetCredential(string user, SecureString password)
        {
            credential = new SqlCredential(user, password);
        }

        private void ObtainTables()
        {
            using (SqlConnection conn = new SqlConnection(connection)) {
                if(credential != null) { conn.Credential = credential;  }
                DataTable dt = new DataTable();
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT concat(TABLE_SCHEMA, '.', TABLE_NAME) FROM " + database + ".INFORMATION_SCHEMA.Tables WHERE TABLE_TYPE != 'VIEW'", conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                foreach (DataRow row in dt.Rows) {
                    tables.Add(row[0].ToString());
                }
            }
        }

        private void ArchiveTables()
        {
            using (ZipFile zipFile = new ZipFile())
            {
                foreach (string table in tables)
                {
                    try
                    {
                        TableArchiver archiver = new TableArchiver(table, zipFile, connection, credential);
                        archiver.AddTableToArchive();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Crap failed to write " + table);
                        Console.WriteLine(e.StackTrace);
                    }

                }
                zipFile.Save(outputDir + "\\" + database + ".zip");
            }
        }

        private void ThreadedArchiveTables()
        {
            using(ZipFile zipFile = new ZipFile())
            {
                Task[] taskArray = new Task[tables.Count-1];
                for (int i = 0; i < taskArray.Length; i++)
                {
                    taskArray[i] = Task.Run(() => AddTable(tables[i], zipFile));
                }
                Task.WaitAll(taskArray);
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.Save(outputDir + "\\" + database + ".zip");
            }
            
        }

        private void AddTable(string table, ZipFile zip)
        {
            try
            {
                TableArchiver archiver = new TableArchiver(table, zip, connection, credential);
                archiver.AddTableToArchive();

            }
            catch (Exception e)
            {
                Console.WriteLine("Crap failed to write " + table);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
