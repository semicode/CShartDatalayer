using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace CSharpDatalayer
{
    public class EntityCollection<T> : List<T> where T : Entity<T>, new()
    {
        private static FieldsBase<T>.ActionGetValue Build(System.Reflection.PropertyInfo field, int ordinal)
        {
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
        private static System.Reflection.MethodInfo GetMethodFromType(Type t)
        {
            if (typeCode2Method == null)
            {
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

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
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
        private static readonly System.Reflection.MethodInfo DBDataReader_GetInt64 = typeof(System.Data.Common.DbDataReader).GetMethod("GetInt64");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetFloat = typeof(System.Data.Common.DbDataReader).GetMethod("GetFloat");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetDouble = typeof(System.Data.Common.DbDataReader).GetMethod("GetDouble");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetDecimal = typeof(System.Data.Common.DbDataReader).GetMethod("GetDecimal");
        private static readonly System.Reflection.MethodInfo DBDataReader_GetString = typeof(System.Data.Common.DbDataReader).GetMethod("GetString");

        public void Load(System.Data.Common.DbDataReader dr)
        {

            T prep = (T)typeof(T).New();
            //int loadingFieldCount = dr.FieldCount;
            List<FieldsBase<T>> loadingFieldBasePrep = new List<FieldsBase<T>>();


            foreach (KeyValuePair<string, FieldsBase<T>> kv in Entity<T>.m_dbFieldName2BaseField)
            {

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

            while (dr.Read())
            {
                T t = (T)typeof(T).New();
                t.Load(dr, t, loadingFieldBase, loadingFieldCount);
                this.Add(t);
            }

        }
    }
}
