using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class TransformExt
    {
        public static float3x3 GetDirsXYZ(this Transform t) => new float3x3(t.right, t.up, t.forward);

        public static Transform[] GetChildTransforms(this Transform t)
        {
            if (t.childCount == 0)
                return Array.Empty<Transform>();

            var array = new Transform[t.childCount];

            for (int i = 0; i < array.Length; i++)
                array[i] = t.GetChild(i);

            return array;
        }

        public static List<Transform> FindChain(this Transform root, string name)
        {
            var nodes = new List<Transform>();
            var s = FindRobust(root, name);
            if (s == null)
                s = root;
            else
                nodes.Add(s);
            int idx = 1;
            for (int i = 0; i < 20; i++)
            {
                var ns = FindRobust(s, string.Format("{0}.{1}", name, idx.ToString().PadLeft(3, '0')));
                if (ns != null)
                {
                    s = ns;
                    nodes.Add(s);
                }

                idx++;
            }

            return nodes;
        }

        public static Transform FindRobust(this Transform n, string name)
        {
            if (n == null)
                return null;
            var ret = n.Find(name);
            if (ret == null)
                ret = n.Find(name.Replace(".", "_"));
            return ret;
        }

        public static Transform FindPartial(this Transform n, string name)
        {
            if (n == null)
                return null;
            for (int i = 0; i < n.childCount; i++)
            {
                if (n.GetChild(i).name.ToLower().Contains(name.ToLower()))
                    return n.GetChild(i);
            }

            var ret = n.Find(name);
            if (ret == null)
                ret = n.Find(name.Replace(".", "_"));
            return ret;
        }

        public static Transform FindInChain(this Transform[] nodes, string name)
        {
            if (nodes == null || nodes.Length <= 0)
                return null;
            foreach (var n in nodes)
            {
                var ret = n.Find(name);
                if (ret == null)
                    ret = n.Find(name.Replace(".", "_"));
                if (ret != null)
                    return ret;
            }

            return null;
        }
    }
}