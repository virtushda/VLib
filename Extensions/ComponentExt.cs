using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
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

        public static T[] GetComponentsInAllChildren<T>(this Component c)
        {
            List<T> components = new List<T>();
            foreach (Transform t in c.transform.GetDescendants())
            {
                var comp = t.GetComponent<T>();
                if (comp != null) components.Add(comp);
            }

            return components.ToArray();
        }
        
        public static T GetComponentInParents<T>(this Component c)
        {
            foreach (Transform t in c.transform.GetAncestors())
            {
                var comp = t.GetComponent<T>();
                if (comp != null) return comp;
            }

            return default(T);
        }

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