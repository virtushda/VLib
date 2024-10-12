using UnityEngine;

namespace VLib
{
    /// <summary> Interface that connects with the gatherer to provide transform access request information on-demand. </summary>
    public interface ITransformAccessRequestor
    {
        /// <summary> Passes a list reference around to gather up transforms to be built into a transform tree! </summary>
        void GatherTransforms(ref HashList<Transform> gatheredTransforms, ref HashList<Transform> gatheredWriteTransforms);

        /// <summary> After constructing a virtual transform tree from the input Unity transforms, send the tree back out to the requestors so they can query it. </summary>
        void ReceiveVirtualTransformTree(VirtualTransformTree virtualTransformTree);

        /// <summary> After constructing a virtual transform tree from the input Unity transforms, send the tree back out to the requestors so they can query it.
        /// Native version for burst support </summary>
        void ReceiveVirtualTransformTreeNative(VirtualValueTransformTree virtualTransformTree);
    }
}