using UnityEngine;

namespace VLib
{
    public interface IInstanceTrack : ICmdTransform
    {
        Mesh Mesh { get; }
    }
}