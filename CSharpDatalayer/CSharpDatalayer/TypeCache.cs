using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CSharpDatalayer
{
    public class TypeCache
    {
        internal static IDictionary Cache;

        static TypeCache()
        {
            Cache = new Hashtable();
        }
    }
}
