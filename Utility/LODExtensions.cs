using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VLib
{
    public static class LODUtils
    {
        //Return the LODGroup component with a renderer pointing to a specific GameObject. If the GameObject is not part of a LODGroup, returns null 
        public static LODGroup GetParentLODGroupComponent(GameObject GO)
        {
            LODGroup LODGroupParent = GO.GetComponentInParent<LODGroup>();
            if (LODGroupParent == null)
                return null;
            LOD[] LODs = LODGroupParent.GetLODs();

            var FoundLOD = LODs.Where(lod => lod.renderers.Where(renderer => renderer == GO.GetComponent<Renderer>()).ToArray().Any()).ToArray();
            if (FoundLOD is {Length: > 0}) // FoundLOD != null && FoundLOD.Count() > 0)
                return (LODGroupParent);

            return null;
        }


        //Return the GameObject of the LODGroup component with a renderer pointing to a specific GameObject. If the GameObject is not part of a LODGroup, returns null.
        public static GameObject GetParentLODGroupGameObject(GameObject GO)
        {
            var LODGroup = GetParentLODGroupComponent(GO);

            return LODGroup == null ? null : LODGroup.gameObject;
        }

        //Get the LOD # of a selected GameObject. If the GameObject is not part of any LODGroup returns -1.
        public static int GetLODid(GameObject GO)
        {
            LODGroup LODGroupParent = GO.GetComponentInParent<LODGroup>();
            if (LODGroupParent == null)
                return -1;
            LOD[] LODs = LODGroupParent.GetLODs();

            var index = Array.FindIndex(LODs, lod => lod.renderers.Where(renderer => renderer == GO.GetComponent<Renderer>()).ToArray().Any());
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
            if (camera == null)
                return 0f;
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

        /// <summary>
        /// Generates or updates a <see cref="LODGroup"/> on the given root object by
        /// discovering LOD levels from the names of child <see cref="Renderer"/> components.
        /// </summary>
        /// <param name="root">
        /// Root <see cref="GameObject"/> that will own the generated <see cref="LODGroup"/>.
        /// All child Renderers of this object are scanned to build LOD levels.
        /// </param>
        /// <param name="reportUnrecognizedObjects">
        /// If true, logs errors for Renderers whose names contain an "_LOD" token but do not
        /// match the expected "_LODX" pattern, where X is a non‑negative integer.
        /// </param>
        /// <remarks>
        /// Renderers are grouped into LOD sets by stripping a trailing "_LODX" token from the
        /// GameObject name; all Renderers that share the same base name are considered part of
        /// the same LOD set, and any Renderer without an "_LOD" token is treated as level 0
        /// (LOD0). The largest matching set (by number of Renderers) is selected to build the
        /// <see cref="LODGroup"/>, missing numeric LOD levels are skipped, and the remaining
        /// populated levels are remapped to consecutive indices starting at 0. Screen‑relative
        /// transition heights are generated automatically, decreasing from a high value for
        /// LOD0 down toward 0 for the final LOD level, and the resulting array is applied via
        /// <see cref="LODGroup.SetLODs(LOD[])"/> followed by <see cref="LODGroup.RecalculateBounds"/>.
        /// </remarks>
        public static void GenerateLODGroup(GameObject root, bool reportUnrecognizedObjects = true)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            var lodGroups = new Dictionary<string, Dictionary<int, List<Renderer>>>();
            var unrecognized = new List<Renderer>();

            Regex lodRegex = new Regex(@"^(.*)_LOD(\d*)$");

            foreach (Renderer r in renderers)
            {
                string name = r.gameObject.name;
                Match match = lodRegex.Match(name);
                string baseName;
                int lodLevel;

                if (match.Success)
                {
                    baseName = match.Groups[1].Value;
                    string digits = match.Groups[2].Value;
                    if (!string.IsNullOrEmpty(digits))
                    {
                        if (!int.TryParse(digits, out lodLevel))
                        {
                            unrecognized.Add(r);
                            continue;
                        }
                    }
                    else
                    {
                        lodLevel = 1;
                    }

                    AddToGroup(lodGroups, baseName, lodLevel, r);
                    continue;
                }

                if (!name.Contains("_LOD"))
                {
                    baseName = name;
                    lodLevel = 0;
                    AddToGroup(lodGroups, baseName, lodLevel, r);
                    continue;
                }

                unrecognized.Add(r);
            }

            if (reportUnrecognizedObjects && unrecognized.Count > 0)
            {
                Debug.LogError($"LODGenerator: {unrecognized.Count} unrecognized Renderers on {root.name}", root);
                foreach (Renderer u in unrecognized)
                    Debug.LogError($"- {u.gameObject.name}", u.gameObject);
            }

            if (lodGroups.Count == 0)
                return;

            // Pick largest set
            KeyValuePair<string, Dictionary<int, List<Renderer>>> mainSet = lodGroups
                .OrderByDescending(kv => kv.Value.Values.Sum(l => l?.Count ?? 0))
                .First();
            string mainBase = mainSet.Key;
            var mainLevels = mainSet.Value;

            // Collect populated levels, sorted by original LOD index
            var populatedLevels = mainLevels
                .Where(kv => kv.Value != null && kv.Value.Count > 0)
                .OrderBy(kv => kv.Key)
                .ToList();

            if (populatedLevels.Count == 0)
            {
                Debug.LogWarning($"LODGenerator: No valid LOD levels for '{mainBase}' on {root.name}", root);
                return;
            }

            int numLods = populatedLevels.Count;
            LOD[] lods = new LOD[numLods];

            // Decreasing transitions: LOD0 ~0.7f → last ~0.05f
            float maxTransition = 0.7f;
            float minTransition = 0.05f;
            float step = (maxTransition - minTransition) / Mathf.Max(1, numLods - 1);
            float currTrans = maxTransition;

            for (int i = 0; i < numLods; i++)
            {
                var levelRends = populatedLevels[i].Value;
                float transHeight = (i == numLods - 1) ? 0f : currTrans;
                lods[i] = new LOD(transHeight, levelRends.ToArray());
                currTrans = Mathf.Max(minTransition, currTrans - step);
            }

            LODGroup lodGroup = root.GetOrAddComponent<LODGroup>();
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            Debug.Log($"LODGenerator: Created {numLods}-level LODGroup (remapped) for '{mainBase}' on {root.name}", root);

            // Warn on gaps/skipped levels
            var expectedLods = Enumerable.Range(0, mainLevels.Keys.Max() + 1);
            var missing = expectedLods.Where(l => !mainLevels.ContainsKey(l) || mainLevels[l].Count == 0);
            var enumerable = missing as int[] ?? missing.ToArray();
            if (enumerable.Any())
                Debug.LogWarning($"LODGenerator: Missing/skipped levels {string.Join(", ", enumerable)} for '{mainBase}'", root);

            // Warn on other sets
            foreach (var kv in lodGroups.Where(k => k.Key != mainBase))
                Debug.LogWarning($"LODGenerator: Unused set '{kv.Key}' ({kv.Value.Keys.Count} levels)", root);
        }

        static void AddToGroup(Dictionary<string, Dictionary<int, List<Renderer>>> lodGroups, string baseName, int lodLevel, Renderer r)
        {
            if (!lodGroups.TryGetValue(baseName, out var levels))
            {
                levels = new Dictionary<int, List<Renderer>>();
                lodGroups[baseName] = levels;
            }

            if (!levels.TryGetValue(lodLevel, out var list))
            {
                list = new List<Renderer>();
                levels[lodLevel] = list;
            }

            list.Add(r);
        }
    }
}