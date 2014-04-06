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
namespace CSharpDatalayer {

    public delegate object ObjectActivator(params object[] args);
    public class TypeCache {
        internal static IDictionary Cache;

        static TypeCache() {
            Cache = new Hashtable();
        }
    }
    public static class TypeExtensions
    {
        
        public static object New(this Type input, params object[] args)
        {
            object newObject = TypeCache.Cache[input];
            if (newObject != null)
                return ((ObjectActivator)newObject)(args);

            var types = args.Select(p => p.GetType());
            var constructor = input.GetConstructor(types.ToArray());

            var paraminfo = constructor.GetParameters();

            var paramex = Expression.Parameter(typeof(object[]), "args");

            var argex = new Expression[paraminfo.Length];
            for (int i = 0; i < paraminfo.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = paraminfo[i].ParameterType;
                var accessor = Expression.ArrayIndex(paramex, index);
                var cast = Expression.Convert(accessor, paramType);
                argex[i] = cast;
            }

            var newex = Expression.New(constructor, argex);
            var lambda = Expression.Lambda(typeof(ObjectActivator), newex, paramex);
            var result = (ObjectActivator)lambda.Compile();
            TypeCache.Cache.Add(input, result);
            return result(args);
        }
    }

    public class FieldsBase<T>
        where T : Entity<T> {
        //public delegate void ByRefAction(T instance, object value); 
        public delegate void ActionGetValue(T instance, System.Data.Common.DbDataReader dr);
        #region Members
        static Hashtable s_table = new Hashtable();
        private Enum m_enumItem;
        private string m_dbFieldName;
        private ulong m_modificationFlag = 0;
        private int m_ordinal = -1;
        public System.Reflection.PropertyInfo PropertyInfo { get; set; } 
        //public object DefaultValue { get; set; }
        //public bool ReturnTypeString { get; set; }
        public System.Reflection.FieldInfo FieldInfo { get; set; }
        //public ByRefAction Setter { get; set; }
        public ActionGetValue ActionGetValueSetter { get; set; }
        #endregion Members

        #region Proporties
        public int Ordinal {
            get{
                if (m_ordinal == -1){
                    m_ordinal = Array.IndexOf(Enum.GetValues(m_enumItem.GetType()), m_enumItem);
                    
                    
                }
                return m_ordinal;
            }
        }   
        public string DbFieldName {
            get {
                return m_dbFieldName;
            }
            set {
                m_dbFieldName = value;
            }
        }
        public ulong ModificationFlags  {
            get {
                if (m_modificationFlag == 0) {
                    m_modificationFlag = 0x01UL << Ordinal;
                }
                return m_modificationFlag;
            }
        }
        public Enum EnumItem {
            get {
                return m_enumItem;
            }
        }
        #endregion Proporties

        #region Constructors
        public FieldsBase() {
        }
        public FieldsBase(string dbFieldName, Enum el) {
            this.m_dbFieldName = dbFieldName;
            this.m_enumItem = el;
        }
        #endregion Constructors

        #region Methods
        public override string ToString() {
            return m_enumItem.ToString();
        }
        #endregion Methods

    }
    public abstract class Entity<T> 
        where T : Entity<T> {
        private ulong m_modificationFlags = 0L;
        private string m_tableName;
        private FieldsBase<T> m_primaryField;
        public static List<FieldsBase<T>> m_fields = null;
        public static SortedDictionary<string, FieldsBase<T>> m_dbFieldName2BaseField = null;



        protected abstract Type GetEnumType();
      

        
        private static T GetDefault<W>() {
            return default(T);
        }

        public static Q GetAttributeOfType<Q>(Enum enumVal) where Q : System.Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(Q), false);
            return (Q)attributes[0];
        }
        public static void InializeFields(Type enumType) {
            //m_fields = new FieldsBase<T, F>();
            m_dbFieldName2BaseField = new SortedDictionary<string, FieldsBase<T>>();
            m_fields = new List<FieldsBase<T>>();
            foreach (Enum f in Enum.GetValues(enumType)) {
                DBFieldNameAttribute dbfna = GetAttributeOfType<DBFieldNameAttribute>(f);
                FieldsBase<T> el = new FieldsBase<T>(dbfna.DBFieldName, f);
                string PropertyName = f.ToString();
                el.PropertyInfo = typeof(T).GetProperty(PropertyName);
                m_fields.Add(el);
                m_dbFieldName2BaseField.Add(el.DbFieldName, el);
                ulong modificationFlags = el.ModificationFlags;
            }
        }
        public Entity (string tableName, FieldsBase<T> primaryField) {
            this.m_tableName = tableName;
            this.m_primaryField = primaryField;
            if (m_dbFieldName2BaseField == null) {
                InializeFields(GetEnumType());
            }
        }
        public void Load(System.Data.Common.DbDataReader dr, T obj, FieldsBase<T>[] m_loadingFieldBase,int m_loadingFieldCount) {
           
            for (int i = 0; i < m_loadingFieldCount; i++) {
                FieldsBase<T> el = m_loadingFieldBase[i];
                el.ActionGetValueSetter((T)this, dr); 
            }
            m_modificationFlags = 0L;
        }
        public static DbType GetDBType(Type t) {
            f (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                t = Nullable.GetUnderlyingType(t);
            }
            TypeCode code = Type.GetTypeCode(t);
            switch (code) {
                case TypeCode.Boolean:
                    return DbType.Boolean;
                case TypeCode.Byte:
                    return DbType.Byte;
                case TypeCode.Char:
                    return DbType.StringFixedLength;
                case TypeCode.String:
                    return DbType.String;
                case TypeCode.Single:
                    return DbType.Single;
                case TypeCode.Double:
                    return DbType.Double;
                case TypeCode.Int16:
                    return DbType.Int16;
                case TypeCode.Int32:
                    return DbType.Int32;
                case TypeCode.Int64:
                    return DbType.Int64;
                case TypeCode.UInt16:
                    return DbType.UInt16;
                case TypeCode.UInt32:
                    return DbType.UInt32;
                case TypeCode.UInt64:
                    return DbType.UInt64;
                default:
                    return DbType.Object;
            }
        }
        public void GetUpdateStatement(IDbCommand cmd) {
            
            cmd.CommandType = CommandType.Text;
            
            IEnumerable<FieldsBase<T>> elements = m_fields;
            List<string> updateParts = new List<string>();
            int paramIndex = 0;
            StringBuilder updateStatement = new StringBuilder();
            updateStatement.Append("UPDATE ").Append(m_tableName).Append(" SET ");
            foreach (FieldsBase<T> el in elements) {
                if ((m_modificationFlags & el.ModificationFlags) > 0)
                {
                    object val = el.PropertyInfo.GetValue(this, null);
                    
                    if (paramIndex > 0)
                    {
                        updateStatement.Append(", ");
                    }
                    if (val == null) {
                        updateStatement.Append(el.DbFieldName).Append(" = NULL");
                    } else {

                        IDbDataParameter param = cmd.CreateParameter();
                    
                        param.Value = val;
                        if (param.Value == null){
                            param.DbType = DbType.
                        } else{
                            param.DbType = GetDBType(el.PropertyInfo.PropertyType);
                        }
                        param.ParameterName = el.DbFieldName;
                        cmd.Parameters.Add(param);
                        StringBuilder str = new StringBuilder();
                        updateStatement.Append(el.DbFieldName).Append("=");
                        updateStatement.Append("@").Append(el.DbFieldName);
                    }
                    paramIndex++;
                }
            }
  
            updateStatement.Append(" WHERE ");
            if (m_primaryField.PropertyInfo == null) {
                m_primaryField.PropertyInfo = this.GetType().GetProperty(m_primaryField.ToString());
            }
            object primaryKey = m_primaryField.FieldInfo.GetValue(this);
            updateStatement.Append(m_primaryField.DbFieldName).Append("=");
            IDbDataParameter primaryKeyParam = cmd.CreateParameter();
            primaryKeyParam.DbType = GetDBType(m_primaryField.PropertyInfo.PropertyType);
            primaryKeyParam.Value = m_primaryField.FieldInfo.GetValue(this);
            primaryKeyParam.ParameterName = m_primaryField.DbFieldName;
            cmd.Parameters.Add(primaryKeyParam);
            updateStatement.Append("@").Append(m_primaryField.DbFieldName);
            cmd.CommandText = updateStatement.ToString();
            return;
           
        }
        public void GetInsertStatment(IDbCommand cmd) {
            StringBuilder dbFields = new StringBuilder();
            StringBuilder dbValues = new StringBuilder();
            IEnumerable<FieldsBase<T>> elements = m_fields;
            StringBuilder insertStatement = new StringBuilder();
            int paramIndex = 0;
            foreach (FieldsBase<T> el in elements.Where(it=> it != m_primaryField)) {
                object val = el.PropertyInfo.GetValue(this, null);
                if (val == null) {
                    continue;
                }
                if (paramIndex > 0) {
                    dbFields.Append(", ");
                    dbValues.Append(", ");
                }
              
               
                IDataParameter param = cmd.CreateParameter();
                param.ParameterName = el.DbFieldName;
                param.Value = val;
                param.DbType = GetDBType(el.PropertyInfo.PropertyType);
                dbFields.Append(el.DbFieldName);
                dbValues.Append("@").Append(el.DbFieldName);
                
                paramIndex++;
            }
            
            insertStatement.Append("INSERT INTO ").Append(m_tableName).Append(" ").Append(dbFields).Append(") VALUES (").Append(dbValues).Append(")");
            
            cmd.CommandText = insertStatement.ToString();
            
        }
        protected void SetModificationFlags(Enum field) {


            m_modificationFlags |= m_fields.FirstOrDefault(it => it.EnumItem == field).ModificationFlags;
        }
        
    }

    public class EntityCollection<T> : List<T> where T : Entity<T>, new ()  {
        private static FieldsBase<T>.ActionGetValue Build(System.Reflection.PropertyInfo field, int ordinal) {
            ParameterExpression instance = Expression.Parameter(typeof(T), "instance");
            ParameterExpression dbDataReader = Expression.Parameter(typeof(System.Data.Common.DbDataReader), "dr");
            ConstantExpression ordinalExpression = Expression.Constant(ordinal, typeof(int));


            Expression<FieldsBase<T>.ActionGetValue> expr =
                Expression.Lambda<FieldsBase<T>.ActionGetValue>(
                    Expression.Assign(
                        Expression.Property(instance, field),
                        Expression.Condition(Expression.Call(dbDataReader, DBDataReader_IsDbNull, ordinalExpression),
                                             Expression.Default(field.PropertyType), 
                                             Expression.Call(dbDataReader, GetMethodFromType(field.PropertyType), ordinalExpression)
                        )
                    ),
                    instance,
                    dbDataReader);

            return expr.Compile();
        }
        private static SortedDictionary<TypeCode, System.Reflection.MethodInfo> typeCode2Method = null;
        private static System.Reflection.MethodInfo GetMethodFromType(Type t) {
            if (typeCode2Method == null) {
                typeCode2Method = new SortedDictionary<TypeCode, System.Reflection.MethodInfo>();
                typeCode2Method.Add(TypeCode.Boolean, DBDataReader_GetBoolean);
                typeCode2Method.Add(TypeCode.Byte, DBDataReader_GetByte);
                typeCode2Method.Add(TypeCode.Int16, DBDataReader_GetInt16);
                typeCode2Method.Add(TypeCode.Int32, DBDataReader_GetInt32);
                typeCode2Method.Add(TypeCode.Int64, DBDataReader_GetInt64);
                typeCode2Method.Add(TypeCode.Single, DBDataReader_GetFloat);
                typeCode2Method.Add(TypeCode.Double, DBDataReader_GetDouble);
                typeCode2Method.Add(TypeCode.Decimal, DBDataReader_GetDecimal);
                typeCode2Method.Add(TypeCode.String, DBDataReader_GetString);
                
            }
            
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                t = Nullable.GetUnderlyingType(t);
            }
            return (System.Reflection.MethodInfo)typeCode2Method[Type.GetTypeCode(t)];
        }
        private static readonly System.Reflection.MethodInfo DBDataReader_IsDbNull = typeof(System.Data.Common.DbDataReader).GetMethod("IsDBNull");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetValue = typeof(System.Data.Common.DbDataReader).GetMethod("GetValue");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetBoolean = typeof(System.Data.Common.DbDataReader).GetMethod("GetBoolean");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetByte = typeof(System.Data.Common.DbDataReader).GetMethod("GetByte");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetInt16 = typeof(System.Data.Common.DbDataReader).GetMethod("GetInt16");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetInt32 = typeof(System.Data.Common.DbDataReader).GetMethod("GetInt32");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetInt64= typeof(System.Data.Common.DbDataReader).GetMethod("GetInt64");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetFloat = typeof(System.Data.Common.DbDataReader).GetMethod("GetFloat");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetDouble = typeof(System.Data.Common.DbDataReader).GetMethod("GetDouble");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetDecimal = typeof(System.Data.Common.DbDataReader).GetMethod("GetDecimal");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetString = typeof(System.Data.Common.DbDataReader).GetMethod("GetString");
        
        public void Load(System.Data.Common.DbDataReader dr) {
            
            
            //int loadingFieldCount = dr.FieldCount;
            List<FieldsBase<T>> loadingFieldBasePrep = new List<FieldsBase<T>>();

           
            foreach (KeyValuePair<string, FieldsBase<T>> kv in Entity<T>.m_dbFieldName2BaseField) {

                try
                {
                    int ordinal = dr.GetOrdinal(kv.Key);


                    kv.Value.ActionGetValueSetter = Build(kv.Value.PropertyInfo, ordinal);
                    loadingFieldBasePrep.Add(kv.Value);
                }
                catch (IndexOutOfRangeException missfield)
                {
                    throw missfield;
                }
            }

            FieldsBase<T>[] loadingFieldBase = loadingFieldBasePrep.ToArray();
            int loadingFieldCount = loadingFieldBase.Length;
            
            while(dr.Read()) {
                T t = (T)typeof(T).New();
                t.Load(dr, t,loadingFieldBase, loadingFieldCount);
                this.Add(t);
            }
            
        }
    }
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
        public virtual string DBFieldName { get {
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
        public override bool IsDefaultAttribute() {
            return false;
        }
    }

    public class TmpTable : Entity<TmpTable> {
        #region Fields
        
        public double m_lng;
        
        public double m_lat;
        public int m_dum;
        public string m_pc4Code;
        public string m_code;
        public string m_pongo;
        #endregion Fields 
        #region Proporties
        public double Lng {
            get {
                return m_lng;
            }

            set {
                if (m_lng != value) {
                    m_lng = value;
                    SetModificationFlags(Fields.Lng);
                }
            }
        }
        public double Lat {
            get {
                return m_lat;
            }
            set {
                if (m_lat != value) {
                    m_lat = value;
                    SetModificationFlags(Fields.Lat);
                }
            }
        }
        public int Dum {
            get {
                return m_dum;
            }
            set {
                if (m_dum != value) {
                    m_dum = value;
                    SetModificationFlags(Fields.Dum);
                }
            }
        }
        public string Pc4Code {
            get {
                return m_pc4Code;
            }
            set {
                if (m_pc4Code != value) {
                    m_pc4Code = value;
                    SetModificationFlags(Fields.Pc4Code);
                }
            }
        }
        public string Code {
            get {
                return m_code;
            }
            set {
                if (m_code != value) {
                    m_code = value;
                    SetModificationFlags(Fields.Code);
                }
            }
        }
        public string Pongo {
            get {
                return m_pongo;
            }
            set {
                if (m_pongo != value) {
                    m_pongo = value;
                    SetModificationFlags(Fields.Pongo);
                }
            }
        }
        #endregion Proporties
        #region Constructor
        public TmpTable()
            : base("tmpTable", null) {
        }
        #endregion Constructor
        #region Fields;
        public enum Fields {
            [DBFieldName("lng")]
            Lng,
            [DBFieldName("lat")]
            Lat,
            [DBFieldName("dum")]
            Dum,
            [DBFieldName("pc4code")]
            Pc4Code,
            [DBFieldName("code")]
            Code,
            [DBFieldName("pongo")]
            Pongo
        }
        protected override Type GetEnumType()
        {
            return typeof(Fields);
        }
        /*public class Fields : FieldsBase<TmpTable,Fields> {
            public Fields() : base(string.Empty) { }
            private Fields(string dbFieldName) : base(dbFieldName) { }
            public static readonly Fields Lng = new Fields("lng");
            public static readonly Fields Lat = new Fields("lat");
            public static readonly Fields Dum = new Fields("dum");
            public static readonly Fields Pc4Code= new Fields("pc4code");
            public static readonly Fields Code = new Fields("code");
            public static readonly Fields Pongo = new Fields("pongo");

            public override IEnumerable<Fields> Values {
                get {
                    yield return Lng;
                    yield return Lat;
                    yield return Dum;
                    yield return Pc4Code;
                    yield return Code;
                    yield return Pongo;

                }
            }
        }*/
        #endregion Fields
    }
    public class TmpTableCollection : EntityCollection<TmpTable> {
        public TmpTableCollection() {
        }
    }
    /*
    public class TmpTable2 : Entity<TmpTable2.Fields> {
        #region Fields
        public double m_lng;
        public double m_lat;
        public int m_dum;
        public string m_pc4Code;
        public string m_code;
        public string m_pongo;
        #endregion Fields
        #region Proporties
        public double Lng {
            get {
                return m_lng;
            }

            set {
                if (m_lng != value) {
                    m_lng = value;
                    SetModificationFlags(Fields.Lng);
                }
            }
        }
        public double Lat {
            get {
                return m_lat;
            }
            set {
                if (m_lat != value) {
                    m_lat = value;
                    SetModificationFlags(Fields.Lat);
                }
            }
        }
        public int Dum {
            get {
                return m_dum;
            }
            set {
                if (m_dum != value) {
                    m_dum = value;
                    SetModificationFlags(Fields.Dum);
                }
            }
        }
        public string Pc4Code {
            get {
                return m_pc4Code;
            }
            set {
                if (m_pc4Code != value) {
                    m_pc4Code = value;
                    SetModificationFlags(Fields.Pc4Code);
                }
            }
        }
        public string Code {
            get {
                return m_code;
            }
            set {
                if (m_code != value) {
                    m_code = value;
                    SetModificationFlags(Fields.Code);
                }
            }
        }
        public string Pongo {
            get {
                return m_pongo;
            }
            set {
                if (m_pongo != value) {
                    m_pongo = value;
                    SetModificationFlags(Fields.Pongo);
                }
            }
        }
        #endregion Proporties
        #region Constructor
        public TmpTable2()
            : base("tableName", Fields.Pc4Code) {
        }
        #endregion Constructor
        #region Fields;
        public class Fields : FieldsBase<Fields> {
            public Fields() : base(string.Empty) { }
            private Fields(string dbFieldName) : base(dbFieldName) { }
            public static readonly Fields Lng = new Fields("lng");
            public static readonly Fields Lat = new Fields("lat");
            public static readonly Fields Dum = new Fields("dum");
            public static readonly Fields Pc4Code = new Fields("pc4code");
            public static readonly Fields Code = new Fields("code");
            public static readonly Fields Pongo = new Fields("pongo");

            public override IEnumerable<Fields> Values {
                get {
                    yield return Lng;
                    yield return Lat;
                    yield return Dum;
                    yield return Pc4Code;
                    yield return Code;
                    yield return Pongo;

                }
            }
        }
        #endregion Fields
    }
    public class TmpTableCollection2 : EntityCollection<TmpTable2, TmpTable2.Fields> {
        public TmpTableCollection2() {
        }
    }*/
    class Program {
        static void Main(string[] args) {
            System.Diagnostics.Stopwatch s = System.Diagnostics.Stopwatch.StartNew();
            System.Data.SqlClient.SqlConnectionStringBuilder connectionStringBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder();
            
            connectionStringBuilder.DataSource = "ZION\\SQLExpress";
            connectionStringBuilder.PersistSecurityInfo = false;
            connectionStringBuilder.UserID = "sa";
            connectionStringBuilder.Password = "Canjirav9";
            connectionStringBuilder.InitialCatalog = "pc4boundaries";
            System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection(connectionStringBuilder.ConnectionString);
            con.Open();
            s.Restart();
            System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("SELECT * FROM tmpTable", con); ///WHERE CAST(lng*10 AS INT) = 49;", con);
            System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader();
            Console.WriteLine("Execute rader " + s.Elapsed);
            s.Restart();
            Console.WriteLine("Initalze fields " + s.Elapsed);
            s.Restart();
            TmpTableCollection collection = new TmpTableCollection();
            collection.Load(reader);
            
            Console.WriteLine("SQLExpress " + s.Elapsed);
            s.Restart();
            TmpTable el = collection.FirstOrDefault(it => it.Pc4Code == "1011");
            el.Lat = 13;
            reader.Close();
            Console.ReadLine();
            
            return;
            IDbCommand updateStatement2 = con.CreateCommand();


            el.GetUpdateStatement(updateStatement2);
            int count = updateStatement2.ExecuteNonQuery();
            Console.WriteLine("Update statement " + s.Elapsed);
            s.Restart();
            IDbCommand insertCommand = con.CreateCommand();
            el.GetInsertStatment(insertCommand);
            count = insertCommand.ExecuteNonQuery();
            Console.WriteLine("Insert statement " + s.Elapsed);

           
            
            
            
            s.Restart();
            //IEnumerable<TmpTable> subSet = collection.Where(it => ((int)(it.Lng * 10)) == 49);
            
            reader.Close();
            con.Close();
            Console.ReadLine();
            return;
           
            Oracle.DataAccess.Client.OracleConnectionStringBuilder strBuilder = new Oracle.DataAccess.Client.OracleConnectionStringBuilder();
            strBuilder.DataSource = "localhost:1521/XE";
            strBuilder.UserID = "semicode";
            strBuilder.Password = "Canjirav9";
            Oracle.DataAccess.Client.OracleConnection con2 = new Oracle.DataAccess.Client.OracleConnection(strBuilder.ConnectionString);
            con2.Open();
            Oracle.DataAccess.Client.OracleCommand c = new Oracle.DataAccess.Client.OracleCommand("SELECT * FROM \"tmpTable\" ", con2);
            Oracle.DataAccess.Client.OracleDataReader d = c.ExecuteReader();
 
            s.Restart();
            TmpTableCollection colllection2 = new TmpTableCollection();
            colllection2.Load(d);
            Console.WriteLine("Oracle XE " + s.Elapsed);
                      
            con2.Close();
            /*System.Data.SQLite.SQLiteConnectionStringBuilder builder2 = new System.Data.SQLite.SQLiteConnectionStringBuilder();
            builder2.DataSource = "C:\\SQLite\\data\\tmpDb.sl3";
            System.Data.SQLite.SQLiteConnection con3 = new System.Data.SQLite.SQLiteConnection(builder2.ConnectionString);
            con3.Open();
            System.Data.SQLite.SQLiteCommand c2 = new System.Data.SQLite.SQLiteCommand("SELECT * FROM tmpTable", con3);
            System.Data.SQLite.SQLiteDataReader d2 = c2.ExecuteReader();
            s.Restart();
            TmpTableCollection collection3 = new TmpTableCollection();
            collection3.Load(d2);
            Console.WriteLine("SQLite 3 " + s.Elapsed);
            con3.Close();*/

            s.Restart();
            Dictionary<string, TmpTable> mapped = new Dictionary<string, TmpTable>();
            colllection2.ForEach(it => mapped.Add(it.Pc4Code, it));
            mapped = mapped.Where(it => ((int)(it.Value.Lng * 10)) == 49).ToDictionary(it => it.Key, u => u.Value);
            Console.WriteLine("Dictionary " + s.Elapsed);

            Console.ReadLine();
            
          



        }
    }
}

/*
  internal static void InsertTableEntries() {
            s_table.Add(typeof(byte), OracleDbType.Byte);
            s_table.Add(typeof(byte[]), OracleDbType.Raw);
            s_table.Add(typeof(char), OracleDbType.Varchar2);
            s_table.Add(typeof(char[]), OracleDbType.Varchar2);
            s_table.Add(typeof(DateTime), OracleDbType.TimeStamp);
            s_table.Add(typeof(short), OracleDbType.Int16);
            s_table.Add(typeof(int), OracleDbType.Int32);
            s_table.Add(typeof(long), OracleDbType.Int64);
            s_table.Add(typeof(float), OracleDbType.Single);
            s_table.Add(typeof(double), OracleDbType.Double);// BINARY_FLOAT
            s_table.Add(typeof(decimal), OracleDbType.Decimal);
            s_table.Add(typeof(string), OracleDbType.Varchar2);
            s_table.Add(typeof(TimeSpan), OracleDbType.IntervalDS);
            s_table.Add(typeof(OracleBFile), OracleDbType.BFile);
            s_table.Add(typeof(OracleBinary), OracleDbType.Raw);
            s_table.Add(typeof(OracleBlob), OracleDbType.Blob);
            s_table.Add(typeof(OracleClob), OracleDbType.Clob);
            s_table.Add(typeof(OracleDate), OracleDbType.Date);
            s_table.Add(typeof(OracleDecimal), OracleDbType.Decimal);
            s_table.Add(typeof(OracleIntervalDS), OracleDbType.IntervalDS);
            s_table.Add(typeof(OracleIntervalYM), OracleDbType.IntervalYM);
            s_table.Add(typeof(OracleRefCursor), OracleDbType.RefCursor);
            s_table.Add(typeof(OracleString), OracleDbType.Varchar2);
            s_table.Add(typeof(OracleTimeStamp), OracleDbType.TimeStamp);
            s_table.Add(typeof(OracleTimeStampLTZ), OracleDbType.TimeStampLTZ);
            s_table.Add(typeof(OracleTimeStampTZ), OracleDbType.TimeStampTZ);
            s_table.Add(typeof(OracleXmlType), OracleDbType.XmlType);
            s_table.Add(typeof(OracleRef), OracleDbType.Ref);
        }
        internal static OracleDbType ConvertNumberToOraDbType(int precision, int scale) {
            OracleDbType @decimal = OracleDbType.Decimal;
            if ((scale <= 0) && ((precision - scale) < 5)) {
                return OracleDbType.Int16;
            }
            if ((scale <= 0) && ((precision - scale) < 10)) {
                return OracleDbType.Int32;
            }
            if ((scale <= 0) && ((precision - scale) < 0x13)) {
                return OracleDbType.Int64;
            }
            if ((precision < 8) && (((scale <= 0) && ((precision - scale) <= 0x26)) || ((scale > 0) && (scale <= 0x2c)))) {
                return OracleDbType.Single;
            }
            if (precision < 0x10) {
                @decimal = OracleDbType.Double;
            }
            return @decimal;
        }*/