using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
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

        public static List<T> GetComponentsInAssetsPath<T>(string path)
        {
            var filesInPath = Directory.GetFiles(Path.Combine(Application.dataPath, path), "*.prefab",
                SearchOption.AllDirectories);
            List<T> components = new List<T>();
            foreach (var file in filesInPath)
            {
#if UNITY_EDITOR
                string p = "Assets" + file.Replace(Application.dataPath, "");
                var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(p, typeof(GameObject));
                var c = prefab.GetComponent<T>();
                if (c != null)
                    components.Add(c);
#endif
            }

            return components;
        }
    }
}