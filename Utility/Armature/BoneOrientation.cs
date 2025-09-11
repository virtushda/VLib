public enum BoneOrientation : byte
{
    /// <summary> Typical bone orientation coming from Blender. </summary>
    XLeftYForwardZUp = 0,
    /// <summary> Typical transform orientation in Unity. </summary>
    XRightYUpZForward = 1,
    /// <summary> 180 rolled version of typical Blender orientation. </summary>
    XRightYForwardZDown = 2,
    
    UnityDefault = XRightYUpZForward,
    BlenderDefault = XLeftYForwardZUp,
    BlenderAlt = XRightYForwardZDown,
}