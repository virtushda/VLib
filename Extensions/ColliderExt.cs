using UnityEngine;

namespace VLib
{
    public static class ColliderExtensions
    {
        /// <summary>
        /// Returns the collider's bounds in local space.
        /// Works on prefabs and disabled objects (unlike Collider.bounds).
        /// </summary>
        public static Bounds GetLocalBounds(this Collider collider)
        {
            if (collider == null)
                throw new System.ArgumentNullException(nameof(collider));

            switch (collider)
            {
                case BoxCollider box:
                    return new Bounds(box.center, box.size);

                case SphereCollider sphere:
                    return SphereToBounds(sphere.center, sphere.radius);

                case CapsuleCollider capsule:
                    return CapsuleToBounds(capsule.center, capsule.radius, capsule.height, capsule.direction);

                case MeshCollider mesh:
                    // Mesh bounds are already in local space  
                    return mesh.sharedMesh != null 
                        ? mesh.sharedMesh.bounds 
                        : new Bounds(Vector3.zero, Vector3.zero);

                case CharacterController character:
                    // CharacterController is a capsule standing on Y axis
                    return CapsuleToBounds(character.center, character.radius, character.height, 1);

                case TerrainCollider terrain:
                    // Terrain dimensions are in local space, origin at bottom-left
                    var size = terrain.terrainData?.size ?? Vector3.zero;
                    return new Bounds(new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f), size);

                default:
                    // Fallback: attempt to compute from world bounds (only valid if in scene)
                    Debug.LogWarning($"GetLocalBounds: Unsupported collider type {collider.GetType()}. " +
                                   "Consider adding a case for this type.");
                    var worldToLocal = collider.transform.worldToLocalMatrix;
                    var localBounds = collider.bounds;
                    localBounds.center = worldToLocal.MultiplyPoint3x4(localBounds.center);
                    localBounds.size = worldToLocal.MultiplyVector(localBounds.size);
                    return localBounds;
            }
        }

        private static Bounds SphereToBounds(Vector3 center, float radius)
        {
            float diameter = radius * 2f;
            return new Bounds(center, new Vector3(diameter, diameter, diameter));
        }

        private static Bounds CapsuleToBounds(Vector3 center, float radius, float height, int direction)
        {
            // Capsule is a cylinder with hemisphere caps
            // Height includes the caps, so cylinder height = height - 2*radius
            float diameter = radius * 2f;
            Vector3 size;

            switch (direction)
            {
                case 0: // X-axis
                    size = new Vector3(height, diameter, diameter);
                    break;
                case 2: // Z-axis
                    size = new Vector3(diameter, diameter, height);
                    break;
                default: // 1 = Y-axis (default)
                    size = new Vector3(diameter, height, diameter);
                    break;
            }

            return new Bounds(center, size);
        }
    }
}