using System;
using Farrellcrafts.DatabaseArchiver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security;

namespace UnitTestProject1
{
    [TestClass]
    public class DatabaseArchiverTest
    {
        [TestMethod]
        public void TestSimpleSave()
        {
            List<string> tables = new List<string>();
            tables.Add("[HumanResources].[Department]");
            DatabaseArchiver archiver = new DatabaseArchiver("C:\\tmp", "Data Source=JEREMY-PC\\LOCALHOST;Initial Catalog=AdventureWorks2014;User ID=sa;Password=password", tables);
        }

        [TestMethod]
        public void TestFullDatabaseSave()
        {
            SecureString s = new SecureString();
            foreach(char c in "password") { s.AppendChar(c); }
            s.MakeReadOnly();
            DatabaseArchiver archiver = new DatabaseArchiver("C:\\tmp", "JEREMY-PC\\LOCALHOST", "AdventureWorks2014", "sa", s);
        }

        [TestMethod]
        public void TestConnect() {
            bool pass = true;
            using(SqlConnection conn = new SqlConnection("Data Source=JEREMY-PC\\LOCALHOST;Initial Catalog=AdventureWorks2014;User ID=sa;Password=password"))
            {
                try
                {
                    conn.Open();
                }catch
                {
                    pass = false;
                }
                
            }
            Assert.IsTrue(pass);
        }
    }
}
