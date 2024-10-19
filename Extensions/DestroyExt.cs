using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace VLib
{
    public static class DestroyExt
    {
        public static bool TryDestroy<T>(this T obj, float delay = 0, bool logging = false)
            where T : Object
        {
            if (!obj)
            {
                if (logging)
                    Debug.LogError("Could not destroy object, ref twas null my good sir!");
                return false;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (logging)
                    Debug.LogError("TryDestroy is not an editor method, and only works in play mode.");
                return false;
            }
#endif

            if (obj is RenderTexture rTex)
                rTex.Release();

            Object.Destroy(obj, delay);

            return obj == null;
        }

        /// <summary> Automagically destroys an object without forcing the user to care about internal Unity architectural problems. :) </summary>
        /// <returns> False if null, or DestroyImmediate was called. True if regular Destroy was called. </returns>
        public static bool JustDestroyItUnity<T>(this T obj)
            where T : Object
        {
            if (!obj)
                return false;

            if (Application.isPlaying)
            {
                Object.Destroy(obj);
                Assert.IsNull(obj);
                return true;
            }
            else
            {
                Object.DestroyImmediate(obj);
                Assert.IsNull(obj);
                return false;
            }
        }
    }
}