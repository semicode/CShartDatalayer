using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpDatalayer {
    [AttributeUsage(AttributeTargets.Field)]
    public class PrimaryKeyAttribute : Attribute {
        [System.Runtime.TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public PrimaryKeyAttribute() {
        }
    }
}
