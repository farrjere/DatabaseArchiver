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
        private List<string> _keysToRecreate = new List<string>();
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
                    try
                    {
                        string table = entry.FileName.Replace(".csv", "");
                        DataTable dt = new DataTable();
                        LoadDataTable(entry, dt);
                        if (dt.Rows.Count > 0)
                        {
                            CopyDataTableToServer(dt, table);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error processing " + entry.FileName);
                        Console.WriteLine(e.StackTrace);
                    }
                }
                ReaddForeignKeys();
            }
        }

        private void ReaddForeignKeys()
        {
            using(SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Credential = credential;
                conn.Open();
                foreach(string sql in _keysToRecreate)
                {
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CopyDataTableToServer(DataTable dt, string table)
        {
            using(SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Credential = this.credential;
                conn.Open();
                SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.UseInternalTransaction, null);
                bulkCopy.DestinationTableName = table;
                HandleForeignKeys(table, conn);
                SqlCommand truncateCmd = new SqlCommand("TRUNCATE TABLE " + table, conn);
                truncateCmd.ExecuteNonQuery();
                bulkCopy.WriteToServer(dt);
            }
            dt.Clear();
        }

        private void HandleForeignKeys(string table, SqlConnection conn)
        {
            SqlCommand keyQuery = new SqlCommand(KeyQuery(table), conn);
            SqlDataAdapter da = new SqlDataAdapter(keyQuery);
            DataTable keyTable = new DataTable();
            da.Fill(keyTable);
            foreach (DataRow row in keyTable.Rows)
            {
                SqlCommand dropKey = new SqlCommand("ALTER TABLE " + row["schema_name"] + "." + row["table"] +" DROP CONSTRAINT " + row["FK_NAME"], conn);
                dropKey.ExecuteNonQuery();
                _keysToRecreate.Add("ALTER TABLE " + row["schema_name"] + "." + row["table"] + " ADD FOREIGN KEY (" + row["column"]
                    + ") REFERENCES "
                    + row["referenced_schema"] + "." + row["referenced_table"]
                    + "(" + row["referenced_column"] + ")");
            }
        }

        private string KeyQuery(string schemaTable)
        {
            string[] schemaTableParts = schemaTable.Split('.');
            string table = "";
            string schema = "";
            if(schemaTableParts.Length > 1)
            {
                schema = schemaTableParts[0];
                table = schemaTableParts[1];
            }
            else
            {
                table = schemaTableParts[0];
            }
            
            string query =  @"SELECT  obj.name AS FK_NAME,
                            sch.name AS [schema_name],
                            tab1.name AS [table],
                            col1.name AS [column],
                            ref_table.name AS [referenced_table],
                            ref_col.name AS [referenced_column],
	                        ref_schema.name AS [referenced_schema]
                        FROM sys.foreign_key_columns fkc
                        INNER JOIN sys.objects obj
                            ON obj.object_id = fkc.constraint_object_id
                        INNER JOIN sys.tables tab1
                            ON tab1.object_id = fkc.parent_object_id
                        INNER JOIN sys.schemas sch
                            ON tab1.schema_id = sch.schema_id
                        INNER JOIN sys.columns col1
                            ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
                        INNER JOIN sys.tables ref_table
                            ON ref_table.object_id = fkc.referenced_object_id
                        INNER JOIN sys.schemas ref_schema 
	                        on ref_schema.schema_id = ref_table.schema_id
                        INNER JOIN sys.columns ref_col
                            ON ref_col.column_id = referenced_column_id AND ref_col.object_id = ref_table.object_id
                        where ref_table.name = '"+table+"'";

            if(schema != "")
            {
                query += " AND ref_schema.name = '" + schema + "'";
            }
            return query;
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
                            try
                            {
                                drow[i] = values[i];
                            }
                            catch(ArgumentException)
                            {
                                Console.WriteLine("Error writing column: " + dt.Columns[i].ColumnName + " " + dt.Columns[i].DataType + " with value " + values[i]);
                                Console.WriteLine("Attempting to set value to null");
                                drow[i] = DBNull.Value;
                            }
                            
                        }
                    }
                    dt.Rows.Add(drow);
                }

            }
        }
    }
}
