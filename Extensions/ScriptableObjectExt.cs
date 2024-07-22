using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace VLib
{
    public static class ScriptableObjectExt
    {
#if UNITY_EDITOR
        public static bool TryDeleteAsset(this ScriptableObject obj)
        {
            if (obj == null)
                return false;
            var path = AssetDatabase.GetAssetPath(obj);
            if (path.IsNullOrWhitespace())
                return false;
            AssetDatabase.DeleteAsset(path);
            return true;
        }
#endif
    }
}