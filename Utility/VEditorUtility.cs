using UnityEditor;
using UnityEngine;

namespace VLib.Utility
{
    public static class VEditorUtility
    {
#if UNITY_EDITOR
        public static void DirtyAll(this Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                EditorUtility.SetDirty(objects[i]);
            }
        }

        /// <summary> Searches everywhere, very slow. <br/>
        /// Handy when debugging to forcibly lookup an object by type and ID to inspect it. </summary>
        public static T FindObjectWithIDSlow<T>(int instanceID)
            where T : Object
        {
            return Resources.FindObjectsOfTypeAll<T>().Find((g) => g.GetInstanceID() == instanceID);
        }
        
        /// <summary> Searches everywhere and all types, very very slow. <br/>
        /// Handy when debugging to forcibly lookup an object by ID alone to inspect it. </summary>
        public static Object FindAnyObjectWithIDSuperSlow(int instanceID)
        {
            return Resources.FindObjectsOfTypeAll<Object>().Find((g) => g.GetInstanceID() == instanceID);
        }
#endif
    }
}