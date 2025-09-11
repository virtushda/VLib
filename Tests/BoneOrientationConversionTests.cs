using NUnit.Framework;
using UnityEngine;
using VLib;

namespace Libraries.VLib.Tests
{
    public class BoneOrientationConversionTests
    {
        [Test]
        public void BoneOrientationConversionPasses()
        {
            // Try convert from Unity to Blender
            {
                var unityToBlender = (Quaternion) BoneOrientation.UnityDefault.GetOrientationConversionTo(BoneOrientation.BlenderDefault);
                var unityX = Vector3.right;
                var unityY = Vector3.up;
                var unityZ = Vector3.forward;
                var blenderX = unityToBlender * unityX;
                var blenderY = unityToBlender * unityY;
                var blenderZ = unityToBlender * unityZ;
                Assert.True(Vector3.Dot(blenderX, Vector3.left) > 0.99f);
                Assert.True(Vector3.Dot(blenderY, Vector3.forward) > 0.99f);
                Assert.True(Vector3.Dot(blenderZ, Vector3.up) > 0.99f);
            }

            // Try convert from Blender to Unity
            {
                var blenderToUnity = (Quaternion) BoneOrientation.BlenderDefault.GetOrientationConversionTo(BoneOrientation.UnityDefault);
                var blenderX = Vector3.left;
                var blenderY = Vector3.forward;
                var blenderZ = Vector3.up;
                var unityX = blenderToUnity * blenderX;
                var unityY = blenderToUnity * blenderY;
                var unityZ = blenderToUnity * blenderZ;
                Assert.True(Vector3.Dot(unityX, Vector3.right) > 0.99f);
                Assert.True(Vector3.Dot(unityY, Vector3.up) > 0.99f);
                Assert.True(Vector3.Dot(unityZ, Vector3.forward) > 0.99f);
            }
            
            // Try convert from Unity to Inverse Blender
            {
                var unityToInverseBlender = (Quaternion) BoneOrientation.UnityDefault.GetOrientationConversionTo(BoneOrientation.BlenderAlt);
                var unityX = Vector3.right;
                var unityY = Vector3.up;
                var unityZ = Vector3.forward;
                var inverseBlenderX = unityToInverseBlender * unityX;
                var inverseBlenderY = unityToInverseBlender * unityY;
                var inverseBlenderZ = unityToInverseBlender * unityZ;
                Assert.True(Vector3.Dot(inverseBlenderX, Vector3.right) > 0.99f);
                Assert.True(Vector3.Dot(inverseBlenderY, Vector3.forward) > 0.99f);
                Assert.True(Vector3.Dot(inverseBlenderZ, Vector3.down) > 0.99f);
            }
            
            // Try convert from Inverse Blender to Unity
            {
                var inverseBlenderToUnity = (Quaternion) BoneOrientation.BlenderAlt.GetOrientationConversionTo(BoneOrientation.UnityDefault);
                var inverseBlenderX = Vector3.right;
                var inverseBlenderY = Vector3.forward;
                var inverseBlenderZ = Vector3.down;
                var unityX = inverseBlenderToUnity * inverseBlenderX;
                var unityY = inverseBlenderToUnity * inverseBlenderY;
                var unityZ = inverseBlenderToUnity * inverseBlenderZ;
                Assert.True(Vector3.Dot(unityX, Vector3.right) > 0.99f);
                Assert.True(Vector3.Dot(unityY, Vector3.up) > 0.99f);
                Assert.True(Vector3.Dot(unityZ, Vector3.forward) > 0.99f);
            }
            
            Assert.Pass();
        }
    }
}