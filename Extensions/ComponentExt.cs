using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VLib
{
    public static class ComponentExt
    {
        public static T GetOrAddComponent<T>(this Component c)
            where T : Component
        {
            if (c.GetComponent<T>() is var retrievedComponent && retrievedComponent != null)
                return retrievedComponent;
            else
                return c.gameObject.AddComponent<T>();
        }

        public static void ResetFields<T>(this T obj,
                                 BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Default | BindingFlags.NonPublic)
            where T : Object, new()
        {
            var newT = new T();
            
            foreach (FieldInfo field in obj.GetType().GetFields(bindingFlags))
            { 
                ResetField(obj, field);
                //To prefab template logic:
                //field.SetValue(field, prefab.GetComponent(c.GetType()).GetType().GetField(field.Name).GetValue(c));
            }
        }
        
        public static void ResetField<T>(T obj, FieldInfo field)
            where T : Object =>
            field.SetValue(obj, field.FieldType.GetDefaultValue());
    }
}