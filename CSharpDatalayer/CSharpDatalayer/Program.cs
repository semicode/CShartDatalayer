using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Collections;
using System.Linq.Expressions;
using System.IO;
namespace CSharpDatalayer {
    public class TestTable : Entity<TestTable> {
        #region Fields
        public long m_testId;
        public string m_name;
        public decimal m_data;
        public double m_value;
        public int m_number;
        #endregion Fields
        public long TestId {
            get { return m_testId; }
            set { if (m_testId != value) { m_testId = value; SetModificationFlags(Fields.TestId); } }
        }
        public string Name {
            get { return m_name; }

            set { if (m_name != value) { m_name = value; SetModificationFlags(Fields.Name); } }
        }
        public decimal Data {
            get { return m_data; }

            set { if (m_data != value) { m_data = value; SetModificationFlags(Fields.Data); } }
        }
        public int Number {
            get { return m_number; }

            set { if (m_number != value) { m_number = value; SetModificationFlags(Fields.Number); } }
        }
        #region Constructor
        public TestTable()
            : base("tblTest") {


        }
        #endregion Constructor
        #region Fields;
        public enum Fields {
            [DBFieldName("TestId")]
            [PrimaryKey]
            TestId,
            [DBFieldName("Name")]
            Name,
            [DBFieldName("Data")]
            Data,
            [DBFieldName("Number")]
            Number
        }
        #endregion Fields
        protected override Type GetEnumType() {
            return typeof(Fields);
        }
    }

    public class TestTableCollection : EntityCollection<TestTable> {
        public TestTableCollection() {
        }
    }


    class Program {
        static void Main(string[] args) {
            System.Diagnostics.Stopwatch s = System.Diagnostics.Stopwatch.StartNew();
            System.Data.SqlClient.SqlConnectionStringBuilder connectionStringBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder();

            connectionStringBuilder.DataSource = "localhost";
            connectionStringBuilder.PersistSecurityInfo = false;
            connectionStringBuilder.UserID = "sa";
            connectionStringBuilder.InitialCatalog = "Test";
            System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(connectionStringBuilder.ConnectionString);
            con.Open();
            TestTableCollection collection = new TestTableCollection();
            string sql = "SELECT * FROM tblTest";
            try {
                s.Restart();
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, con)) {///WHERE CAST(lng*10 AS INT) = 49;", con);
                    using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader()) {
                        Console.WriteLine("ExecuteReader " + s.Elapsed);
                        s.Restart();
                        Console.WriteLine("Begin Load " + s.Elapsed);
                        s.Restart();
                        
                        collection.Load(reader);
                        Console.WriteLine("End Load " + s.Elapsed);
                        s.Restart();
                    }
                }
            } finally {
                if (con.State == ConnectionState.Open) {
                    con.Close();
                }
            }
            Console.WriteLine("SQL Server end " + s.Elapsed);
            s.Restart();

            //Console.ReadLine();
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!Directory.Exists(Path.Combine(appDataFolder, "CSharpDatalayer"))) {
                Directory.CreateDirectory(Path.Combine(appDataFolder, "CSharpDatalayer"));

            }
            if (File.Exists(Path.Combine(appDataFolder, "CSharpDatalayer", "test.xml"))) {
                File.Delete(Path.Combine(appDataFolder, "CSharpDatalayer", "test.xml"));
            }
            DataSet ds = new DataSet();
            
            DataTable dt = new DataTable();
            
            ds.Tables.Add(dt);
            dt.TableName = "tblTest";
            //TestTable testTable = new TestTable() {
            //}
            Console.WriteLine("SQL Server end " + s.Elapsed);
            s.Restart();
            
            foreach (FieldsBase<TestTable> fieldBase in TestTable.FieldsInfo) {
                DataColumn dataColumn = new DataColumn();
                dataColumn.ColumnName = fieldBase.PropertyInfo.Name;
                dataColumn.DataType = fieldBase.PropertyInfo.PropertyType;
                dt.Columns.Add(dataColumn);
            }
            foreach(TestTable testTable in collection) {
                DataRow dataRow = dt.NewRow();
                for (int i = 0; i <Enum.GetValues(typeof(TestTable.Fields)).Length; i++) {
                    Enum field = (Enum)Enum.GetValues(typeof(TestTable.Fields)).GetValue(i);
                    object val =testTable.GetValue(field);
                    dataRow[i] = val;
                }
                dt.Rows.Add(dataRow);
            }
            ds.WriteXml(Path.Combine(appDataFolder, "CSharpDatalayer", "test.xml"));

            
         

            
        }
    }
}
