using Ionic.Zip;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace Farrellcrafts.DatabaseArchiver
{
    internal class TableArchiver
    {
        private ZipFile zip;
        private string table;
        private string connection;
        private SqlCredential cred;

        public TableArchiver(string table, ZipFile zip, string connection, SqlCredential cred) {
            this.zip = zip;
            this.cred = cred;
            this.table = table;
            this.connection = connection;
        }

        public void AddTableToArchive()
        {
            DataTable dt = readDataTable();
            zip.AddEntry(table+".csv", (name, stream) => WriteDataTableToStream(dt, stream));
        }

        private DataTable readDataTable()
        {
            DataTable dt = new DataTable();
            using(SqlConnection conn = new SqlConnection(connection))
            {
                if(cred != null) { conn.Credential = cred; }
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM "+table, conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        private void WriteDataTableToStream(DataTable dt, Stream output) {
            using(StreamWriter writer = new StreamWriter(output))
            {
                WriteHeader(dt, writer);
                foreach(DataRow row in dt.Rows)
                {
                    foreach (DataColumn column in dt.Columns)
                    {
                        writer.Write( row[column.ColumnName].ToString() + ",");
                    }
                    writer.Write("\n");
                }
            }

        }

        private static void WriteHeader(DataTable dt, StreamWriter writer)
        {
            for (int col = 0; col < dt.Columns.Count; col++)
            {
                writer.Write(dt.Columns[col].ColumnName);
                if (col < dt.Columns.Count - 1)
                {
                    writer.Write(",");
                }
                else
                {
                    writer.Write("\n");
                }
            }
        }
    }
}
