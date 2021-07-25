using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

namespace ArcCreation
{
    public class TangetArcLine : BaseLine
    {
        int[] ThreeIndies = new int[3]; // Önceki, şimdiki, bir sonraki indexler, GC önlemek için
        const float DefaultRadius = 5;

        [SerializeField, HideInInspector] List<Anchor> anchors = new List<Anchor>();
        public bool showCircle = false;


        public float firstAngle = 180;
        public float lastAngle;

        public enum AngleSpace { Mixed, MixedReverse, Concave, Convex }
        [SerializeField] AngleSpace angleSpace;

        [HideInInspector] public Vector3 moveOnLineSegmentPosA;
        [HideInInspector] public Vector3 moveOnLineSegmentPosB;

        public Anchor GetAnchor(int i) => anchors[i];
        public int NumAnchors => anchors.Count;

        [SerializeField] private PathSpace pathSpace;
        public override PathSpace PathSpace
        {
            get { return pathSpace; }
            set
            {
                if (value == PathSpace.XYZ)
                {
                    pathSpace = nodes[0].y == 0 ? PathSpace.XZ : PathSpace.XY;
                }
                else
                {
                    pathSpace = value;
                    MovingSpace = pathSpace == PathSpace.XY ? MovingSpace.XY : MovingSpace.XZ;
                    Vector3 rotatedPoint = GetMidPointNodes();
                    if (pathSpace == PathSpace.XY && nodes[0].z != 0)
                    {
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            Vector3 node = nodes[i];
                            node = Quaternion.Euler(90, 0, 0) * (node - rotatedPoint);
                            node.z = 0;
                            nodes[i] = node;
                        }
                    }
                    else if (pathSpace == PathSpace.XZ && nodes[0].y != 0)
                    {
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            Vector3 node = nodes[i];
                            node = Quaternion.Euler(-90, 0, 0) * (node - rotatedPoint);
                            node.y = 0;
                            nodes[i] = node;
                        }
                    }
                }
            }
        }

        public override void ConvertToSmoothCurve()
        {
            smoothPoints.Clear();
            Utility.DegreeSpace degreeSpace = pathSpace == PathSpace.XY ? Utility.DegreeSpace.xy : Utility.DegreeSpace.xz;
            for (int i = 0; i < nodes.Count; i++)
            {
                SetTangentArcPoints(i, degreeSpace);
            }
        }

        void SetTangentArcPoints(int index, Utility.DegreeSpace degreeSpace)
        {
            float fromAngle = 0;
            float toAngle = 0;

            if (NumNodes == 1)
            {
                fromAngle = firstAngle;
                toAngle = firstAngle == lastAngle ? firstAngle + 360 : lastAngle;
            }
            else if (index == 0)
            {
                fromAngle = isClose ? Utility.FindDegree(nodes[LastNodeIndex] - nodes[index], degreeSpace) : firstAngle;
                toAngle = Utility.FindDegree(nodes[index + 1] - nodes[index], degreeSpace);
            }
            else if (index == LastNodeIndex)
            {
                fromAngle = Utility.FindDegree(nodes[index - 1] - nodes[index], degreeSpace);
                toAngle = isClose ? Utility.FindDegree(nodes[0] - nodes[index], degreeSpace) : lastAngle;
            }
            else
            {
                fromAngle = Utility.FindDegree(nodes[index - 1] - nodes[index], degreeSpace);
                toAngle = Utility.FindDegree(nodes[index + 1] - nodes[index], degreeSpace);
            }
            toAngle = NumNodes != 1 ? GetNewToAngleForAngleSpace(index, fromAngle, toAngle) : toAngle;

            for (int i = index == 0 ? 0 : 1; i < resolation; i++)
            {
                float angle = Mathf.Lerp(fromAngle, toAngle, i / (float)(resolation - 1));
                Vector3 rotatedPoint = degreeSpace == Utility.DegreeSpace.xy
                    ? new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * anchors[index].radius
                    : new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * anchors[index].radius;
                smoothPoints.Add(nodes[index] + rotatedPoint);
            }
        }

        float GetConcaveNewEndAngle(float startAngle, float endAngle) => startAngle <= endAngle && endAngle <= 360 ? endAngle : 360 + endAngle;
        float GetConvexNewEndAngle(float startAngle, float endAngle) => 0 <= endAngle && endAngle <= startAngle ? endAngle : endAngle - 360;
        float GetNewToAngleForAngleSpace(int index, float fromAngle, float toAngle)
        {
            switch (angleSpace)
            {
                case AngleSpace.Mixed:
                    return index % 2 == 0
                         ? GetConcaveNewEndAngle(fromAngle, toAngle)
                         : GetConvexNewEndAngle(fromAngle, toAngle);
                case AngleSpace.Concave:
                    return GetConcaveNewEndAngle(fromAngle, toAngle);
                case AngleSpace.Convex:
                    return GetConvexNewEndAngle(fromAngle, toAngle);
                case AngleSpace.MixedReverse:
                    return (index + 1) % 2 == 0
                         ? GetConcaveNewEndAngle(fromAngle, toAngle)
                         : GetConvexNewEndAngle(fromAngle, toAngle);
                default:
                    return 0;
            }
        }

        // Aradaki seçili node'u hareket ettireceğimiz  Bir önceki node'dan bir sonraki node'a olacak olan uzaklığı
        float GetNodeAToNodeCSegmentDistanceForTanget(int beforeIndex, int currIndex, int nextIndex)
        {
            float r0 = anchors[beforeIndex].radius + anchors[currIndex].radius;
            float r1 = anchors[nextIndex].radius + anchors[currIndex].radius;
            float d = Vector3.Distance(nodes[beforeIndex], nodes[nextIndex]);
            return (Mathf.Pow(r0, 2) - Mathf.Pow(r1, 2) + d * d) / (2 * d);
        }

        float GetBDerivative(int selectedNodeIndex)
        {
            return isClose
                ? GetNodeAToNodeCSegmentDistanceForTanget(ThreeIndies[0], ThreeIndies[1], ThreeIndies[2])
                : GetNodeAToNodeCSegmentDistanceForTanget(selectedNodeIndex - 1, selectedNodeIndex, selectedNodeIndex + 1);
        }

        // Orta kısımlarda kalan düğümler belirli bir hesaplama neticesinde hareket ettirir.
        public override void UpdateNode(int index, Vector3 newPos)
        {
            Anchor anchor = anchors[index];
            base.UpdateNode(index, newPos);
            if (NumNodes == 1)
            {
                return;
            }
            else if (index == 0 && !isClose)
            {
                anchor.radius = Vector3.Distance(nodes[index], nodes[index + 1]) - anchors[index + 1].radius;
            }
            else if (index == LastNodeIndex && !isClose)
            {
                anchor.radius = Vector3.Distance(nodes[index], nodes[index - 1]) - anchors[index - 1].radius;
            }
            else
            {
                GetBeforeCurrNextIndies(index);
                Vector3 dir = (nodes[ThreeIndies[2]] - nodes[ThreeIndies[0]]).normalized;
                Vector3 tangetMidPointBeforeAndNextNodes = nodes[ThreeIndies[0]] + dir * GetBDerivative(index);
                dir = pathSpace == PathSpace.XY ? dir.RotateXYPlane() : dir.RotateXZPlane();
                moveOnLineSegmentPosA = tangetMidPointBeforeAndNextNodes + dir * 150;
                moveOnLineSegmentPosB = tangetMidPointBeforeAndNextNodes - dir * 150;
                nodes[index] = Utility.ClosestPointOnLineSegment(newPos, moveOnLineSegmentPosA, moveOnLineSegmentPosB);
                anchor.radius = Vector3.Distance(nodes[ThreeIndies[0]], nodes[index]) - anchors[ThreeIndies[0]].radius;
            }
            anchors[index] = anchor;
        }

        void UpdateAllNode()
        {
            for (int i = 0; i < NumNodes; i++)
            {
                UpdateNode(i, nodes[i]);
            }
        }

        // Orta kısımlarda kalan düğümleri özgür bir şekilde hareket ettirebiliriz
        public void UpdateNodeFreedom(int index, Vector3 newPos)
        {
            base.UpdateNode(index, newPos);
            UpdateCircleRadiusRecursive(index, -1, 0);
            UpdateCircleRadiusRecursive(index, 1, NumNodes - 1);
            UpdateNode(0, nodes[0]);
        }

        public void UpdateCircleRadiusRecursive(int index, int dir, int endIndex)
        {
            if (index == endIndex)
                return;
            UpdateAnchorRadius(index + dir, Vector3.Distance(nodes[index + dir], nodes[index]) - anchors[index].radius);
            UpdateCircleRadiusRecursive(index + dir, dir, endIndex);
        }

        public void UpdateAnchorRadius(int index, float newRadius)
        {
            Anchor anchor = anchors[index];
            anchor.radius = newRadius;
            anchors[index] = anchor;
        }

        public int[] GetBeforeCurrNextIndies(int currIndex)
        {
            if (currIndex == 0)
            {
                ThreeIndies[0] = LastNodeIndex;
                ThreeIndies[1] = currIndex;
                ThreeIndies[2] = currIndex + 1;
            }
            else if (currIndex == LastNodeIndex)
            {
                ThreeIndies[0] = currIndex - 1;
                ThreeIndies[1] = currIndex;
                ThreeIndies[2] = 0;
            }
            else
            {
                ThreeIndies[0] = currIndex - 1;
                ThreeIndies[1] = currIndex;
                ThreeIndies[2] = currIndex + 1;
            }
            return ThreeIndies;
        }

        Vector3 GetMidPointNodes()
        {
            Vector3 total = Vector3.zero;
            for (int i = 0; i < nodes.Count; i++)
                total += nodes[i];
            return total / nodes.Count;
        }

        public override void Init()
        {
            base.Init();
            if (nodes.Count == 0)
            {
                anchors.Clear();
                nodes.Add(Vector3.zero);
                anchors.Add(new Anchor { radius = DefaultRadius });
                firstAngle = 180;
                lastAngle = 0;
                ConvertToSmoothCurve();
            }
        }

        public override void AddPoint(Vector3 mousePos)
        {
            if (isClose)
            {
                Debug.LogError("Ekleme işlemi için path açık olmalı");
                return;
            }
            float radius = Vector3.Distance(mousePos, nodes[LastNodeIndex]) - anchors[LastNodeIndex].radius;
            base.AddPoint(mousePos);
            anchors.Add(new Anchor { radius = radius });
        }

        public override void DeletePoint(int nodeIndex)
        {
            if (NumNodes > 1)
            {
                nodes.RemoveAt(nodeIndex);
                anchors.RemoveAt(nodeIndex);

                if (NumNodes == 2)
                {
                    isClose = false;
                    float radius = Vector3.Distance(nodes[0], nodes[1]) * .5f;
                    UpdateAnchorRadius(0, radius);
                    UpdateAnchorRadius(1, radius);
                }
                else if (NumNodes > 2)
                {
                    UpdateCircleRadiusRecursive(0, 1, NumNodes - 1);
                    UpdateAllNode();
                }
            }
            else
                Debug.Log("En az 1 düğüm olmalı");
        }

        public override void Reset()
        {
            base.Reset();
            Init();
        }

        public override void OpenPath()
        {
            if (NumNodes > 1)
            {
                base.OpenPath();
                anchors.RemoveAt(anchors.Count - 1);
                nodes.RemoveAt(LastNodeIndex);
                UpdateAllNode();
            }
        }

        public override void ClosePath()
        {
            if (NumNodes > 1)
            {
                base.ClosePath();
                nodes.Add((nodes[0] + nodes[LastNodeIndex]) * .5f);
                anchors.Add(new Anchor { radius = DefaultRadius });
                UpdateAllNode();
            }
        }

        public override void ChangeSpace() { }

        [System.Serializable]
        public struct Anchor
        {
            public Vector3 position;
            public float radius;
        }
    }
}