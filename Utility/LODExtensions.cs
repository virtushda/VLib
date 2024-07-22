using System;
using System.Linq;
using UnityEngine;

namespace VLib
{
    public static class LODExtendedUtility
    {
        //Return the LODGroup component with a renderer pointing to a specific GameObject. If the GameObject is not part of a LODGroup, returns null 
        static public LODGroup GetParentLODGroupComponent(GameObject GO)
        {
            LODGroup LODGroupParent = GO.GetComponentInParent<LODGroup>();
            if (LODGroupParent == null)
                return null;
            LOD[] LODs = LODGroupParent.GetLODs();

            var FoundLOD = LODs.Where(lod => lod.renderers.Where(renderer => renderer == GO.GetComponent<Renderer>()).ToArray().Count() > 0).ToArray();
            if (FoundLOD != null && FoundLOD.Count() > 0)
                return (LODGroupParent);

            return null;
        }


        //Return the GameObject of the LODGroup component with a renderer pointing to a specific GameObject. If the GameObject is not part of a LODGroup, returns null.
        static public GameObject GetParentLODGroupGameObject(GameObject GO)
        {
            var LODGroup = GetParentLODGroupComponent(GO);

            return LODGroup == null ? null : LODGroup.gameObject;
        }

        //Get the LOD # of a selected GameObject. If the GameObject is not part of any LODGroup returns -1.
        static public int GetLODid(GameObject GO)
        {
            LODGroup LODGroupParent = GO.GetComponentInParent<LODGroup>();
            if (LODGroupParent == null)
                return -1;
            LOD[] LODs = LODGroupParent.GetLODs();

            var index = Array.FindIndex(LODs, lod => lod.renderers.Where(renderer => renderer == GO.GetComponent<Renderer>()).ToArray().Count() > 0);
            return index;
        }


        //returns the currently visible LOD level of a specific LODGroup, from a specific camera. If no camera is define, uses the Camera.current.
        public static int GetVisibleLOD(this LODGroup lodGroup, Camera camera)
        {
            var lods = lodGroup.GetLODs();
            var relativeHeight = GetRelativeHeight(lodGroup, camera);

            int lodIndex = GetMaxLOD(lodGroup);
            for (var i = 0; i < lods.Length; i++)
            {
                var lod = lods[i];

                if (relativeHeight >= lod.screenRelativeTransitionHeight)
                {
                    lodIndex = i;
                    break;
                }
            }

            return lodIndex;
        }

        //returns the currently visible LOD level of a specific LODGroup, from a the SceneView Camera.
        /*public static int GetVisibleLODSceneView(LODGroup lodGroup)
        {
            Camera camera = SceneView.lastActiveSceneView.camera;
            return GetVisibleLOD(lodGroup, camera);
        }*/

        static float GetRelativeHeight(this LODGroup lodGroup, Camera camera)
        {
            if (camera == null) return 0f;
            var distance = (lodGroup.transform.TransformPoint(lodGroup.localReferencePoint) - camera.transform.position).magnitude;
            return DistanceToRelativeHeight(camera, (distance / QualitySettings.lodBias), GetWorldSpaceSize(lodGroup));
        }

        static float DistanceToRelativeHeight(Camera camera, float distance, float size)
        {
            if (camera.orthographic)
                return size * 0.5F / camera.orthographicSize;

            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            var relativeHeight = size * 0.5F / (distance * halfAngle);
            return relativeHeight;
        }
        public static int GetMaxLOD(this LODGroup lodGroup)
        {
            return lodGroup.lodCount - 1;
        }
        public static float GetWorldSpaceSize(this LODGroup lodGroup)
        {
            return GetWorldSpaceScale(lodGroup.transform) * lodGroup.size;
        }
        static float GetWorldSpaceScale(Transform t)
        {
            var scale = t.lossyScale;
            float largestAxis = Mathf.Abs(scale.x);
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
            return largestAxis;
        }
    }
}