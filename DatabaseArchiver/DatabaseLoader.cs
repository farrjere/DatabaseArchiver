using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Farrellcrafts.DatabaseArchiver
{
    public class DatabaseLoader
    {
        private string connectionString;
        private SqlCredential credential;

        public DatabaseLoader(string fileName, string server, string database, string user, SecureString password)
        {   
            this.connectionString = "Data Source=" + server + ";Initial Catalog=" + database;
            this.credential = new SqlCredential(user, password);
            LoadFile(fileName);
        }

        private void LoadFile(string fileName)
        {
           using(ZipFile zip = ZipFile.Read(fileName))
            {
                foreach(ZipEntry entry in zip.Entries)
                {
                    string table = entry.FileName.Replace(".csv", "");
                    DataTable dt = new DataTable();
                    LoadDataTable(entry, dt);
                    if(dt.Rows.Count > 0)
                    {
                        CopyDataTableToServer(dt, table);
                    }
                }
            }
        }

        private void CopyDataTableToServer(DataTable dt, string table)
        {
            using(SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Credential = this.credential;
                conn.Open();
                SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock|SqlBulkCopyOptions.FireTriggers|SqlBulkCopyOptions.UseInternalTransaction, null);
                bulkCopy.DestinationTableName = table;
                SqlCommand cmd = new SqlCommand("TRUNCATE TABLE " + table, conn);
                cmd.BeginExecuteNonQuery();
                bulkCopy.WriteToServer(dt);
            }
            dt.Clear();
        }

        private static void LoadDataTable(ZipEntry entry, DataTable dt)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                entry.Extract(stream);
                stream.Seek(0, 0);
                var reader = new StreamReader(stream);
                string headers = reader.ReadLine();
                foreach (string header in headers.Split(','))
                {
                    if (header.Length > 0)
                    {
                        string[] headerParts = header.Split(':');
                        dt.Columns.Add(new DataColumn(headerParts[0], Type.GetType(headerParts[1])));
                    }
                }
                string row = "";
                while (!reader.EndOfStream)
                {
                    row = reader.ReadLine();
                    string[] values = row.Split(',');
                    DataRow drow = dt.NewRow();
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (i < dt.Columns.Count)
                        {
                            drow[i] = values[i];
                        }
                    }
                }

            }
        }
    }
}
