using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace shackles
{
    public static class HackMan
    {
        public static T GetField<T>(this object instance, string fieldname)
        {
            return (T)AccessTools.Field(instance.GetType(), fieldname).GetValue(instance);
        }

        public static T GetProperty<T>(this object instance, string fieldname)
        {
            return (T)AccessTools.Property(instance.GetType(), fieldname).GetValue(instance);
        }

        public static object CreateInstance(this Type type)
        {
            return AccessTools.CreateInstance(type);
        }

        public static T[] GetFields<T>(this object instance)
        {
            List<T> list = new List<T>();
            foreach (FieldInfo item in AccessTools.GetDeclaredFields(instance.GetType())?.Where((FieldInfo t) => t.FieldType == typeof(T)))
            {
                list.Add(GetField<T>(instance, item.Name));
            }
            return list.ToArray();
        }

        public static void SetField(this object instance, string fieldname, object setVal)
        {
            AccessTools.Field(instance.GetType(), fieldname).SetValue(instance, setVal);
        }

        public static void CallMethod(this object instance, string method)
        {
            CallMethod(instance, method, null);
        }

        public static void CallMethod(this object instance, string method, params object[] args)
        {
            CallMethod<object>(instance, method, args);
            
        }

        public static T CallMethod<T>(this object instance, string method)
        {
            return (T)CallMethod<object>(instance, method, null);
        }

        public static T CallMethod<T>(this object instance, string method, params object[] args)
        {
            Type[] array = null;
            if (args != null)
            {
                array = ((args.Length != 0) ? new Type[args.Length] : null);
                for (int i = 0; i < args.Length; i++)
                {
                    array[i] = args[i].GetType();
                }
            }
            
            return (T)(GetMethod(instance, method, array).Invoke(instance, args));
        }

        public static MethodInfo GetMethod(this object instance, string method, params Type[] parameters)
        {
            return GetMethod(instance, method, parameters, null);
        }

        public static MethodInfo GetMethod(this object instance, string method, Type[] parameters = null, Type[] generics = null)
        {
            return AccessTools.Method(instance.GetType(), method, parameters, generics);
        }
    }
}
