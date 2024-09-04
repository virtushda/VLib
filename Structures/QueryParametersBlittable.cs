using System.Runtime.InteropServices;
using UnityEngine;

namespace VLib
{
    /// <summary> A copy of QueryParameters that is blittable according to the burst function ruleset. </summary>
    public struct QueryParametersBlittable
    {
        /// <summary>
        ///   <para>A LayerMask that is used to selectively ignore Colliders when casting a ray.</para>
        /// </summary>
        public int layerMask;
        /// <summary>
        ///   <para>Whether raycast batch query should hit multiple faces.</para>
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool hitMultipleFaces;
        /// <summary>
        ///   <para>Whether queries hit Triggers by default.</para>
        /// </summary>
        public QueryTriggerInteraction hitTriggers;
        /// <summary>
        ///   <para>Whether physics queries should hit back-face triangles.</para>
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool hitBackfaces;
        
        public QueryParametersBlittable(int layerMask = -5, bool hitMultipleFaces = false, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal, bool hitBackfaces = false)
        {
            this.layerMask = layerMask;
            this.hitMultipleFaces = hitMultipleFaces;
            this.hitTriggers = hitTriggers;
            this.hitBackfaces = hitBackfaces;
        }

        public readonly QueryParameters AsUnity() => this;
        
        public static implicit operator QueryParametersBlittable(QueryParameters qp) => new QueryParametersBlittable(qp.layerMask, qp.hitMultipleFaces, qp.hitTriggers, qp.hitBackfaces);
        public static implicit operator QueryParameters(QueryParametersBlittable bqp) => new QueryParameters(bqp.layerMask, bqp.hitMultipleFaces, bqp.hitTriggers, bqp.hitBackfaces);
    }
}