using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpDatalayer {
    [AttributeUsage(AttributeTargets.Field)]
    public class DBFieldNameAttribute : Attribute {
        public DBFieldNameAttribute() {
        }
        [System.Runtime.TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public DBFieldNameAttribute(string dbFieldName) {
            this.DBFieldNameValue = dbFieldName;
        }
        public virtual string DBFieldName {
            get {
                return DBFieldNameValue;
            }
        }
        protected string DBFieldNameValue { get; set; }

        public override bool Equals(object obj) {
            return (obj is DBFieldNameAttribute) && DBFieldNameValue == (obj as DBFieldNameAttribute).DBFieldNameValue;
        }
        public override int GetHashCode() {
            return DBFieldNameValue.GetHashCode();
        }
        public override bool IsDefaultAttribute() {
            return false;
        }
    }
}
