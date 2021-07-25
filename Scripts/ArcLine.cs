using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace ArcCreation
{
    public class ArcLine : BaseLine
    {
        const float DefaultPercendicularDst = 1.5f;
        [SerializeField, HideInInspector] List<Anchor> anchors = new List<Anchor>();

        [HideInInspector] public bool useSnap;
        [HideInInspector] public float snapValue = .5f;

        public Anchor GetAnchor(int i) => anchors[i];
        public void SetAnchor(int index, Anchor newAnchor) => anchors[index] = newAnchor;
        public int NumAnchors => anchors.Count;

        [SerializeField] private PathSpace pathSpace;
        public override PathSpace PathSpace
        {
            get { return pathSpace; }
            set
            {
                pathSpace = value;
                if (pathSpace != PathSpace.XYZ)
                {
                    MovingSpace = pathSpace == PathSpace.XY ? MovingSpace.XY : MovingSpace.XZ;
                    for (int i = 0; i < NumNodes; i++)
                    {
                        nodes[i] = pathSpace == PathSpace.XY ? new Vector3(nodes[i].x, nodes[i].y, 0) : new Vector3(nodes[i].x, 0, nodes[i].z);
                        if (i < anchors.Count)
                            UpdateAnchorAxisAngle(i, 0);
                    }
                }
            }
        }

        public override void ConvertToSmoothCurve()
        {
            smoothPoints.Clear();
            int iterate = isClose ? nodes.Count : nodes.Count - 1;
            for (int i = 0; i < iterate; i++)
            {
                SetArcPoints(nodes[i], nodes[(i + 1) % NumNodes], i);
            }
        }

        void SetArcPoints(Vector3 nodeA, Vector3 nodeB, int anchorIndex)
        {
            Quaternion anchorRotation = Quaternion.AngleAxis(anchors[anchorIndex].axisAngleNodeAToNodeB, nodeB - nodeA);

            Vector3 dirAToB = nodeB - nodeA;
            Vector3 midPoint = (nodeA + nodeB) * .5f;
            Vector3 downVectorAToB = GetDownVector(dirAToB, anchorRotation);

            Vector3 anchorPos = midPoint + downVectorAToB.normalized * anchors[anchorIndex].perpendicularDst;
            float cosAngle = Vector3.Angle(nodeA - anchorPos, midPoint - anchorPos);
            float radius = (nodeA - anchorPos).magnitude * .5f / Mathf.Cos(cosAngle * Mathf.Deg2Rad);
            Vector3 center = anchorPos + (midPoint - anchorPos).normalized * radius;

            Vector3 axis = Vector3.Cross(nodeB - anchorPos, nodeA - anchorPos);
            float angle = Vector3.Angle(nodeA - center, nodeB - center);
            bool isObtuseAngle = Vector3.SignedAngle(nodeA - center, nodeB - center, axis) < 0; // Geniş açı ise angle'ı tekrar hesaplamalıyız
            angle = isObtuseAngle ? 360 - angle : angle;
            float stepAngle = angle / (resolation - 1);
            Vector3 startRotDir = (nodeA - center);
            for (int i = anchorIndex == 0 ? 0 : 1; i < resolation; i++)
            {
                float currAngle = i * stepAngle;
                Vector3 rotatedPoint = center + Quaternion.AngleAxis(currAngle, axis) * startRotDir;
                smoothPoints.Add(rotatedPoint);
            }
            anchors[anchorIndex] = new Anchor(center, radius, anchors[anchorIndex].perpendicularDst, anchorPos, midPoint, anchors[anchorIndex].axisAngleNodeAToNodeB); // Editor üzerinden çizim yaparken gerekli bilgileri tutuyoruz. Bu sayede tekrar hesaplamakla uğraşmıyoruz
        }

        Vector3 GetDownVector(Vector3 dirAToB, Quaternion anchorRotation)
        {
            return pathSpace == PathSpace.XY ? anchorRotation * dirAToB.RotateXYPlane()
                : pathSpace == PathSpace.XZ ? anchorRotation * dirAToB.RotateXZPlane()
                : anchorRotation * Quaternion.LookRotation(dirAToB) * Quaternion.Euler(-90, 0, 0) * Vector3.forward;
        }

        public override void Init()
        {
            base.Init();
            if (nodes.Count == 0)
            {
                anchors.Clear();
                const float dst = 5;
                nodes.Add(Vector3.left * dst);
                nodes.Add(Vector3.right * dst);
                anchors.Add(new Anchor { perpendicularDst = DefaultPercendicularDst });
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
            mousePos = useSnap ? mousePos.ToRound(snapValue) : mousePos;
            base.AddPoint(mousePos);
            anchors.Add(new Anchor { perpendicularDst = DefaultPercendicularDst });
        }

        public override void UpdateNode(int index, Vector3 newPos)
        {
            if (pathSpace == PathSpace.XY)
                newPos.z = 0;
            else if (pathSpace == PathSpace.XZ)
                newPos.y = 0;

            nodes[index] = useSnap ? newPos.ToRound(snapValue) : newPos;
        }

        public override void DeletePoint(int nodeIndex)
        {
            if (NumNodes > 2)
            {
                int deletingAnchorIndex = nodeIndex - 1;
                if (nodeIndex == 0)
                    deletingAnchorIndex = 0;
                else if (nodeIndex == LastNodeIndex)
                    deletingAnchorIndex = anchors.Count - 1;

                nodes.RemoveAt(nodeIndex);
                anchors.RemoveAt(deletingAnchorIndex);
            }
            else
                Debug.Log("En az 2 düğüm olmalı");
        }

        public override void Reset()
        {
            base.Reset();
            PathSpace = PathSpace.XY;
            Init();
        }

        public void UpdateAnchorPerpendicalarDstToMidPoint(int anchorIndex, float newPerpendicalarDstToMidPoint)
        {
            Anchor tempAnchor = anchors[anchorIndex];
            tempAnchor.perpendicularDst = newPerpendicalarDstToMidPoint;
            anchors[anchorIndex] = tempAnchor;
        }

        public void UpdateAnchorAxisAngle(int anchorIndex, float angle)
        {
            Anchor anc = anchors[anchorIndex];
            anc.axisAngleNodeAToNodeB = angle;
            anchors[anchorIndex] = anc;
        }

        public override void OpenPath()
        {
            base.OpenPath();
            anchors.RemoveAt(anchors.Count - 1);
        }

        public override void ClosePath()
        {
            base.ClosePath();
            anchors.Add(new Anchor { perpendicularDst = DefaultPercendicularDst });
        }

        public override void ChangeSpace()
        {
            if (pathSpace == PathSpace.XYZ)
            {
                MovingSpace = (MovingSpace)(1 - (int)MovingSpace);
            }
        }


        [System.Serializable]
        public struct Anchor
        {
            public readonly Vector3 center;
            public float radius;
            public float perpendicularDst;
            public readonly Vector3 position;
            public readonly Vector3 twoNodeMidPoint;
            public float axisAngleNodeAToNodeB;

            public Anchor(Vector3 center, float radius, float perpendicularDst, Vector3 position, Vector3 twoNodeMidPoint, float angle)
            {
                this.center = center;
                this.radius = radius;
                this.perpendicularDst = perpendicularDst;
                this.position = position;
                this.twoNodeMidPoint = twoNodeMidPoint;
                this.axisAngleNodeAToNodeB = angle;
            }
        }
    }
}