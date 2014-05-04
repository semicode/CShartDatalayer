using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpDatalayer
{
    public class FieldsBase<T> where T : Entity<T>{
        public delegate void ActionGetValue(T instance, System.Data.Common.DbDataReader dr);
        #region Members
        private Enum m_enumItem;
        private string m_dbFieldName;
        private ulong m_modificationFlag = 0;
        private int m_ordinal = -1;
        public System.Reflection.PropertyInfo PropertyInfo { get; set; }
        public System.Reflection.FieldInfo FieldInfo { get; set; }
        public ActionGetValue ActionGetValueSetter { get; set; }
        #endregion Members

        #region Proporties
        public int Ordinal
        {
            get
            {
                if (m_ordinal == -1)
                {
                    m_ordinal = Array.IndexOf(Enum.GetValues(m_enumItem.GetType()), m_enumItem);


                }
                return m_ordinal;
            }
        }
        public string DbFieldName
        {
            get
            {
                return m_dbFieldName;
            }
            set
            {
                m_dbFieldName = value;
            }
        }
        public ulong ModificationFlags
        {
            get
            {
                if (m_modificationFlag == 0)
                {
                    m_modificationFlag = 0x01UL << Ordinal;
                }
                return m_modificationFlag;
            }
        }
        public Enum EnumItem
        {
            get
            {
                return m_enumItem;
            }
        }
        #endregion Proporties

        #region Constructors
        public FieldsBase()
        {
        }
        public FieldsBase(string dbFieldName, Enum el)
        {
            this.m_dbFieldName = dbFieldName;
            this.m_enumItem = el;
        }
        #endregion Constructors

        #region Methods
        public override string ToString()
        {
            return m_enumItem.ToString();
        }
        #endregion Methods

    }
}
