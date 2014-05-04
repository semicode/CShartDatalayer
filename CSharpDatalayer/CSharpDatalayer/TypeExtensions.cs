using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace CSharpDatalayer
{
    public static class TypeExtensions
    {
        public delegate object ObjectActivator(params object[] args);
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
}
