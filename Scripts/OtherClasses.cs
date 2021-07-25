using UnityEditor;
using UnityEngine;

namespace ArcCreation
{
    public static class MouseUtility
    {
        public static Vector3 GetMousePos(Ray ray, Vector2 guiMousePos, MovingSpace movingSpace, float planeHeight)
        {
            float dst = 0;
            if (movingSpace == MovingSpace.XY)
            {
                dst = (planeHeight - ray.origin.z) / ray.direction.z;
                return ray.origin + ray.direction * dst;
            }
            dst = (planeHeight - ray.origin.y) / ray.direction.y;
            return ray.origin + ray.direction * dst;
        }
    }

    public static class Utility
    {
        public static Vector3 ToRound(this Vector3 v3, float mul) => new Vector3(RoundTo(v3.x, mul), RoundTo(v3.y, mul), RoundTo(v3.z, mul));
        public static float RoundTo(this float value, float mul = 1) => Mathf.Round(value / mul) * mul;
        public static Vector3 RotateXYPlane(this Vector3 vector) => new Vector3(vector.y, -vector.x, 0);
        public static Vector3 RotateXZPlane(this Vector3 vector) => new Vector3(vector.z, 0, -vector.x);
        public static Vector3 RotateYZPlane(this Vector3 vector) => new Vector3(0, vector.z, -vector.y);

        public static float FindDegree(Vector3 v, DegreeSpace space = DegreeSpace.xy)
        {
            float angle = space == DegreeSpace.xy ? Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg : Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
            if (angle < 0)
                angle += 360;
            return angle;
        }

        // Sebastian Lague'e aittir.
        public static Vector3 ClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 aB = b - a;
            Vector3 aP = p - a;
            float sqrLenAB = aB.sqrMagnitude;

            if (sqrLenAB == 0)
                return a;

            float t = Mathf.Clamp01(Vector3.Dot(aP, aB) / sqrLenAB);
            return a + aB * t;
        }

        public enum DegreeSpace { xy, xz }
    }

    [System.Serializable]
    public struct PathInfo
    {
        public float percent;
        public float distance;
        public int smoothIndex;

        public PathInfo(float percent, float distance, int smoothIndex)
        {
            this.percent = percent;
            this.distance = distance;
            this.smoothIndex = smoothIndex;
        }
    }

    public enum PathSpace { XY = 0, XZ = 1, XYZ = 2 }
    public enum MovingSpace { XY = 0, XZ = 1 }
    public enum MoveType { Stop, Loop, Reverse, None }
}