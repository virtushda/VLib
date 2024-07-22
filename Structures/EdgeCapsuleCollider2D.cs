using Unity.Collections;
using Unity.Mathematics;

namespace VLib
{
    [GenerateTestsForBurstCompatibility]
    public readonly struct EdgeCapsuleCollider2D
    {
        public readonly float2 vertA;
        public readonly float2 vertB;
        public readonly float2 heightMinMax;
        public readonly float2 normal;
        public readonly float radius;

        // Store extra data so computations can be more efficient
        readonly float2 center;

        readonly float2 centerOuterRejectPlane;
        //float length;
        readonly float lengthSq;
        //float lengthPlusRadius;
        readonly float halfLengthPlusRadiusSq;

        readonly float lengthHalfSq;
        //float2 a2bDir;

        public float3 VertA3 => new float3(vertA.x, heightMinMax.Average(), vertA.y);
        public float3 VertB3 => new float3(vertB.x, heightMinMax.Average(), vertB.y);
        
        public float3 Center3 => new float3(center.x, heightMinMax.Average(), center.y);
        
        public float3 Normal3 => new float3(normal.x, 0, normal.y);

        public EdgeCapsuleCollider2D(float3 vertA, float3 vertB, float radius, float heightMargin = 1)
        {
            this.vertA = vertA.xz;
            this.vertB = vertB.xz;
            this.radius = radius;

            float2 vertAToVertB = this.vertB - this.vertA;
            this.normal = VMath.Rotate90CW(math.normalize(vertAToVertB));
            this.center = (this.vertA + this.vertB) * .5f;
            float length = math.length(vertAToVertB);
            this.lengthSq = length * length;
            float lengthHalf = length * .5f;
            this.lengthHalfSq = lengthHalf * lengthHalf;
            float halfLengthPlusRadius = lengthHalf + radius;
            this.halfLengthPlusRadiusSq = halfLengthPlusRadius * halfLengthPlusRadius;

            float minHeight = math.min(vertA.y, vertB.y);
            float maxHeight = math.max(vertA.y, vertB.y);
            this.heightMinMax = new float2(minHeight - heightMargin, maxHeight + heightMargin);

            this.centerOuterRejectPlane = center + normal * this.radius;
        }

        public EdgeCapsuleCollider2D(float3x2 edge, float radius, float heightMargin = 1) => this = new EdgeCapsuleCollider2D(edge.c0, edge.c1, radius, heightMargin);

        public bool IsInHeightRange(float3 point) => !(point.y < heightMinMax.x || point.y > heightMinMax.y);

        public bool PointIsOutsidePlane(float3 point) => math.dot(point.xz - center, normal) > 0;

        public bool PointIsOutsideOuterPlane(float3 point) => math.dot(point.xz - centerOuterRejectPlane, normal) > 0;

        public bool RejectPenetratingPoint(float3 point, out float3 adjustedPoint, float addRejectRadius = 0)
        {
            if (PointIsOutsideOuterPlane(point))
            {
                adjustedPoint = point;
                return false;
            }
            
            if (!IsInHeightRange(point))
            {
                adjustedPoint = point;
                return false;
            }

            var pointToLinePoint = center - point.xz;
            var pointToLineDot = math.dot(normal, pointToLinePoint);
            var pointOnLine = point.xz + pointToLineDot * normal;
            
            float2 centerToPointOnLine = pointOnLine - center;
            float centerToPointOnLineDistSq = math.lengthsq(centerToPointOnLine);
            // Check if point on line segment is outside segment length + radius
            if (centerToPointOnLineDistSq >= halfLengthPlusRadiusSq)
            {
                adjustedPoint = point;
                return false;
            }

            // Reject differently if in center portion of capsule, vs inside an end cap area
            if (centerToPointOnLineDistSq <= lengthSq + .01f) // Inside portion of capsule
            {
                var rejectedPoint2D = pointOnLine + normal * (radius + addRejectRadius);
                adjustedPoint = new float3(rejectedPoint2D.x, point.y, rejectedPoint2D.y);
                return true;
            }
            else // Inside an end cap
            {
                // Default values to vert A for cap side check
                var vertToPointOnLine = pointOnLine - vertA;
                var distFromVertSq = math.lengthsq(vertToPointOnLine);
                
                // Which end cap?
                var rejectionVert = vertA;
                if (distFromVertSq < lengthHalfSq) // Closer to vert a
                    rejectionVert = vertA;
                else // Closer to vert b
                {
                    rejectionVert = vertB;
                    vertToPointOnLine = pointOnLine - vertB;
                    distFromVertSq = math.lengthsq(vertToPointOnLine);
                }

                // Check if point is inside plane and segment checks, but outside the end cap (rare)
                if (distFromVertSq > radius * radius)
                {
                    adjustedPoint = point;
                    return false;
                }

                // Project point out of circle and along edge normal
                float distAlongCapZenith = math.sqrt(distFromVertSq) / radius;
                //float distAlongNormal = math.sqrt(radius - distAlongCapZenith * distAlongCapZenith);
                //float2 circProjectedPoint = pointOnLine
                float2 rejectDir = math.normalize(math.lerp(normal, math.normalize(vertToPointOnLine), distAlongCapZenith));
                float2 circProjectedPoint = rejectDir * (radius + addRejectRadius);
                adjustedPoint = new float3(circProjectedPoint.x, point.y, circProjectedPoint.y);
                return true;
            }
        }
    }
}