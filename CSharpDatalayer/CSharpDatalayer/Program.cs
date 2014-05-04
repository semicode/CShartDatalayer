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
namespace CSharpDatalayer
{
    public delegate object ObjectActivator(params object[] args);
    /*
    public class Task : Entity<Task, Task.Fields> {
        #region Members
        public string m_first = string.Empty;
        public string m_second = string.Empty;
        public int m_third = 0;
        public string m_four = string.Empty;
        public double m_five;
        #endregion Members
        #region Proporties
        public string First {
            get {
                return m_first;
            }
            set {
                if (m_first != value) {
                    m_first = value;
                    SetModificationFlags(Fields.First);
                }
            }
        }
        public string Second {
            get {
                return m_second;
            }
            set {
                if (m_second != value) {
                    m_second = value;
                    SetModificationFlags(Fields.Second);
                }
            }
        }
        public int Third {
            get {
                return m_third;
            }
            set {
                if (m_third != value) {
                    m_third = value;
                    SetModificationFlags(Fields.Third);
                }
            }
        }
        public string Four {
            get {
                return m_four;
            }
            set {
                if (m_four != value) {
                    m_four = value;
                    SetModificationFlags(Fields.Four);
                }
            }
        }
        public double Five {
            get {
                return m_five;
            }
            set {
                if (m_five != value) {
                    m_five = value;
                    SetModificationFlags(Fields.Five);
                }
            }
        }
        #endregion Proporties
        #region Constructor
        public Task() : base("tableName", Task.Fields.First) {
        }
        #endregion Constructor
        #region Fields
        public class Fields : FieldsBase<Fields, Task> {

            public Fields(): base(string.Empty) { }
            private Fields(string dbFieldName) : base(dbFieldName) {}
            public static readonly Fields First = new Fields("first");
            public static readonly Fields Second = new Fields("second");
            public static readonly Fields Third = new Fields("third");
            public static readonly Fields Four = new Fields("four");
            public static readonly Fields Five = new Fields("five");

            public override IEnumerable<Fields> Values {
                get {
                    yield return First;
                    yield return Second;
                    yield return Third;
                    yield return Four;
                    yield return Five;
                }
            }
        }
        #endregion Fields
    }

    public class P : Entity<P.Fields> {
        #region Members
        public string m_first = string.Empty;
        public string m_second = string.Empty;
        public int m_third = 0;
        public string m_four = string.Empty;
        #endregion Members
        #region Proporties
        public string First {
            get {
                return m_first;
            }
            set {
                if (m_first != value) {
                    m_first = value;
                    SetModificationFlags(Fields.First);
                }
            }
        }
        public string Second {
            get {
                return m_second;
            }
            set {
                if (m_second != value) {
                    m_second = value;
                    SetModificationFlags(Fields.Second);
                }
            }
        }
        public int Third {
            get {
                return m_third;
            }
            set {
                if (m_third != value) {
                    m_third = value;
                    SetModificationFlags(Fields.Third);
                }
            }
        }
        public string Four {
            get {
                return m_four;
            }
            set {
                if (m_four != value) {
                    m_four = value;
                    SetModificationFlags(Fields.Four);
                }
            }
        }
        #endregion Proporties
        #region Constructor
        public P()
            : base("tableName", P.Fields.First) {
        }
        #endregion Constructor
        #region Fields
        public class Fields : FieldsBase<Fields> {

            public Fields() : base(string.Empty) { }
            private Fields(string dbFieldName) : base(dbFieldName) { }
            public static readonly Fields First = new Fields("first");
            public static readonly Fields Second = new Fields("second");
            public static readonly Fields Third = new Fields("third");
            public static readonly Fields Four = new Fields("four");

            public override IEnumerable<Fields> Values {
                get {
                    yield return First;
                    yield return Second;
                    yield return Third;
                    yield return Four;
                }
            }
        }
        #endregion Fields
    }
    
    public class TaskCollection : EntityCollection<Task, Task.Fields> {
    }*/
    [AttributeUsage(AttributeTargets.Field)]
    public class DBFieldNameAttribute : Attribute
    {


        // Summary:
        //     Initializes a new instance of the DBFieldAttributee
        //     class with no parameters.
        public DBFieldNameAttribute()
        {
        }

        //
        // Summary:
        //     Initializes a new instance of the DBFieldAttribute
        //     class with a description.
        //
        // Parameters:
        //   description:
        //     The description text.
        [System.Runtime.TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public DBFieldNameAttribute(string dbFieldName)
        {
            this.DBFieldNameValue = dbFieldName;
        }

        // Summary:
        //     Gets the description stored in this attribute.
        //
        // Returns:
        //     The description stored in this attribute.
        public virtual string DBFieldName
        {
            get
            {
                return DBFieldNameValue;
            }
        }
        //
        // Summary:
        //     Gets or sets the string stored as the description.
        //
        // Returns:
        //     The string stored as the description. The default value is an empty string
        //     ("").
        protected string DBFieldNameValue { get; set; }

        // Summary:
        //     Returns whether the value of the given object is equal to the current System.ComponentModel.DescriptionAttribute.
        //
        // Parameters:
        //   obj:
        //     The object to test the value equality of.
        //
        // Returns:
        //     true if the value of the given object is equal to that of the current; otherwise,
        //     false.
        public override bool Equals(object obj)
        {
            return (obj is DBFieldNameAttribute) && DBFieldNameValue == (obj as DBFieldNameAttribute).DBFieldNameValue;
        }
        public override int GetHashCode()
        {
            return DBFieldNameValue.GetHashCode();
        }
        //
        // Summary:
        //     Returns a value indicating whether this is the default System.ComponentModel.DescriptionAttribute
        //     instance.
        //
        // Returns:
        //     true, if this is the default System.ComponentModel.DescriptionAttribute instance;
        //     otherwise, false.
        public override bool IsDefaultAttribute()
        {
            return false;
        }
    }
    public class TestTable : Entity<TestTable>
    {
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

            set{ if (m_name != value) { m_name = value; SetModificationFlags(Fields.Name);} }
        }
        public decimal Data
        {
            get { return m_data; }

            set { if (m_data != value) { m_data = value; SetModificationFlags(Fields.Data); } }
        }
        public int Number
        {
            get { return m_number; }

            set { if (m_number != value) { m_number = value; SetModificationFlags(Fields.Number); } }
        }
        #region Constructor
        public TestTable()
            : base("tblTest", Fields.TestId)
        {
            

        }
        #endregion Constructor
        #region Fields;
        public enum Fields
        {
            [DBFieldName("TestId")]
            TestId,
            [DBFieldName("Name")]
            Name,
            [DBFieldName("Data")]
            Data,
            [DBFieldName("Number")]
            Number
        }
        #endregion Fields
        protected override Type GetEnumType()
        {
            return typeof(Fields);
        }
    }
    
    public class TestTableCollection : EntityCollection<TestTable>
    {
        public TestTableCollection()
        {
        }
    }
 
    
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch s = System.Diagnostics.Stopwatch.StartNew();
            System.Data.SqlClient.SqlConnectionStringBuilder connectionStringBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder();

            connectionStringBuilder.DataSource = "localhost";
            connectionStringBuilder.PersistSecurityInfo = false;
            connectionStringBuilder.UserID = "sa";
            
            connectionStringBuilder.InitialCatalog = "Test";
            System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(connectionStringBuilder.ConnectionString);
            con.Open();
            s.Restart();
            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("SELECT * FROM tblTest", con); ///WHERE CAST(lng*10 AS INT) = 49;", con);
            System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader();
            Console.WriteLine("Execute rader " + s.Elapsed);
            s.Restart();
            Console.WriteLine("Initalze fields " + s.Elapsed);
            s.Restart();
            TestTableCollection collection = new TestTableCollection();
            collection.Load(reader);

            Console.WriteLine("SQLExpress " + s.Elapsed);
            s.Restart();
            
            Console.ReadLine();

           
        }
    }
}
