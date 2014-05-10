using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace CSharpDatalayer {
    public abstract class Entity<T>
        where T : Entity<T> {
        private ulong m_modificationFlags = 0L;
        private string m_tableName;
        private static FieldsBase<T> m_primaryField;
        public static List<FieldsBase<T>> m_fields = null;
        public static SortedDictionary<string, FieldsBase<T>> m_dbFieldName2BaseField = null;



        protected abstract Type GetEnumType();

        public static List<FieldsBase<T>> FieldsInfo {
            get {
                return m_fields;
            }
        }
        public object GetValue(Enum field) {
            FieldsBase<T> fieldInfo = m_fields.FirstOrDefault(it => StringComparer.InvariantCulture.Compare(it.EnumItem.ToString(), field.ToString()) == 0);
            return fieldInfo.PropertyInfo.GetValue(this, new object[0]);
        }


        private static T GetDefault<W>() {
            return default(T);
        }

        public static Q GetAttributeOfType<Q>(Enum enumVal) where Q : System.Attribute {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(Q), false);
            return (Q)attributes[0];
        }
        public static bool HasAttributeOfType<Q>(Enum enumVal) where Q : System.Attribute {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(Q), false);
            return attributes.Length > 0;
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
        public Entity(string tableName) {
       
            //this.m_primaryField = primaryField;
            if (Entity<T>.m_fields == null) {
                this.m_tableName = tableName;
                InializeFields(GetEnumType());
            
                Enum primKey = null;

                foreach (Enum f in Enum.GetValues(GetEnumType())) {
                    if (HasAttributeOfType<PrimaryKeyAttribute>(f)) {
                        primKey = f;
                    }
                }
                if (primKey != null) {
                    Entity<T>.m_primaryField = m_fields.FirstOrDefault(it => StringComparer.InvariantCulture.Compare(it.EnumItem.ToString(), primKey.ToString()) == 0);
                }
            }
        }
        public void SetFields(string tableName, List<FieldsBase<T>> fields, FieldsBase<T> primaryField) {
            this.m_tableName = tableName;
        }
        public void Load(System.Data.Common.DbDataReader dr, T obj, FieldsBase<T>[] m_loadingFieldBase, int m_loadingFieldCount) {

            for (int i = 0; i < m_loadingFieldCount; i++) {
                FieldsBase<T> el = m_loadingFieldBase[i];
                el.ActionGetValueSetter((T)this, dr);
            }
            m_modificationFlags = 0L;
        }
        public static DbType GetDBType(Type t) {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
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
                if ((m_modificationFlags & el.ModificationFlags) > 0) {
                    object val = el.PropertyInfo.GetValue(this, null);

                    if (paramIndex > 0) {
                        updateStatement.Append(", ");
                    }
                    if (val == null) {
                        updateStatement.Append(el.DbFieldName).Append(" = NULL");
                    } else {

                        IDbDataParameter param = cmd.CreateParameter();

                        param.Value = val;
                        //if (param.Value == null){
                        //    param.DbType = DbType
                        //} else{
                        param.DbType = GetDBType(el.PropertyInfo.PropertyType);
                        //}
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
            foreach (FieldsBase<T> el in elements.Where(it => it != m_primaryField)) {
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


            m_modificationFlags |= m_fields.FirstOrDefault(it => StringComparer.InvariantCulture.Compare(it.EnumItem.ToString(), field.ToString()) == 0).ModificationFlags;
        }

    }

}
