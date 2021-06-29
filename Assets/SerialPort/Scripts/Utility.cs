using System;
using System.Collections.Generic;

namespace SerialPortUtility
{
    public static class Utility
    {
        public static class Assembly
        {
            private static readonly System.Reflection.Assembly[] s_Assemblies;
            private static readonly Dictionary<string, System.Type> s_CachedTypes;
            static Assembly()
            {
                Utility.Assembly.s_Assemblies = null;
                Utility.Assembly.s_CachedTypes = new Dictionary<string, System.Type>();
                Utility.Assembly.s_Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            public static System.Reflection.Assembly[] GetAssemblies()
            {
                return Utility.Assembly.s_Assemblies;
            }
            public static System.Type[] GetTypes()
            {
                List<System.Type> list = new List<System.Type>();
                for (int i = 0; i < Utility.Assembly.s_Assemblies.Length; i++)
                {
                    list.AddRange(Utility.Assembly.s_Assemblies[i].GetTypes());
                }
                return list.ToArray();
            }
            public static System.Type GetType(string typeName)
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    throw new Exception("Type name is invalid.");
                }
                System.Type type = null;
                if (Utility.Assembly.s_CachedTypes.TryGetValue(typeName, out type))
                {
                    return type;
                }
                type = System.Type.GetType(typeName);
                if (type != null)
                {
                    Utility.Assembly.s_CachedTypes.Add(typeName, type);
                    return type;
                }
                System.Reflection.Assembly[] array = Utility.Assembly.s_Assemblies;
                for (int i = 0; i < array.Length; i++)
                {
                    System.Reflection.Assembly assembly = array[i];
                    type = System.Type.GetType(string.Format("{0}, {1}", typeName, assembly.FullName));
                    if (type != null)
                    {
                        Utility.Assembly.s_CachedTypes.Add(typeName, type);
                        return type;
                    }
                }
                return null;
            }
        }
    }
}
