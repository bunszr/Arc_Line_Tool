using UnityEngine;
using System.Collections.Generic;
using System;

namespace ArcCreation
{
    public abstract class BaseLine : MonoBehaviour
    {
        public Color pathColor = Color.green;
        public Action onPathChangedEvent;
        const int SplitSegmentCount = 10;
        const float eps = .01f;

        public MovingSpace MovingSpace { get; protected set; }
        public virtual PathSpace PathSpace { get; set; }

        [SerializeField, HideInInspector] protected bool isClose;
        [SerializeField, HideInInspector] float totalPathDistance;
        [SerializeField, Range(2, 40)] protected int resolation = 18;
        [SerializeField, HideInInspector] protected List<Vector3> nodes = new List<Vector3>();
        [SerializeField, HideInInspector] protected List<Vector3> smoothPoints = new List<Vector3>();
        [SerializeField, HideInInspector] protected List<PathInfo> pathInfos = new List<PathInfo>();

        public Vector3 GetNode(int i) => nodes[i];
        public Vector3 GetSmooth(int i) => smoothPoints[i];
        public int NumNodes => nodes.Count;
        public int NumSmoothPoints => smoothPoints.Count;
        public int LastNodeIndex => nodes.Count - 1;
        public int Resolation => resolation;
        public bool IsClose => isClose;
        public float TotalPathDistance => totalPathDistance;

        public abstract void ConvertToSmoothCurve();
        public abstract void ChangeSpace();

        public VisualSetting visualSetting;

        private void Awake()
        {
            UpdatePathInfos();
        }

        public virtual void Init()
        {
            MovingSpace = PathSpace == PathSpace.XY ? MovingSpace.XY : MovingSpace.XZ;
        }

        public virtual void DeletePoint(int index)
        {
        }

        public virtual void OpenPath()
        {
            isClose = false;
        }

        public virtual void ClosePath()
        {
            isClose = true;
        }

        // Path üzerinde bir değişiklik yapıldıysa bunun çağrılması gerekir.
        public void UpdateWhenPathChanges()
        {
            ConvertToSmoothCurve();
            UpdatePathInfos();
            onPathChangedEvent?.Invoke();
        }

        protected void UpdatePathInfos()
        {
            totalPathDistance = GetTotalPathDistance();
            pathInfos.Clear();
            int numPathInfo = smoothPoints.Count / SplitSegmentCount;
            if (numPathInfo <= 2)
            {
                pathInfos.Add(new PathInfo(0, 0, 0));
                pathInfos.Add(new PathInfo(1, TotalPathDistance, smoothPoints.Count - 1));
                return;
            }

            float totalDst = 0;
            int indexOfPathInfo = 0;
            for (int i = 0; i < smoothPoints.Count - 1; i++)
            {
                if (i % SplitSegmentCount == 0)
                {
                    float percent = totalDst / TotalPathDistance;
                    pathInfos.Add(new PathInfo(percent, totalDst, i));
                    indexOfPathInfo++;
                }

                totalDst += Vector3.Distance(smoothPoints[i], smoothPoints[i + 1]);
            }
            pathInfos.Add(new PathInfo(1, TotalPathDistance, smoothPoints.Count - 1));
        }

        float GetTotalPathDistance()
        {
            float totalPathDistance = 0;
            for (int i = 0; i < smoothPoints.Count - 1; i++)
            {
                totalPathDistance += Vector3.Distance(smoothPoints[i + 1], smoothPoints[i]);
            }
            return totalPathDistance;
        }

        public Vector3 GetPointAtTime(float time, MoveType moveType = MoveType.Stop)
        {
            time = time.GetNewTimeWithMoveType(moveType);
            return PathUtility.GetPointAtTime(time, pathInfos, smoothPoints, TotalPathDistance, moveType);
        }

        public Vector3 GetPointAtTravelledDistance(float dst, MoveType moveType = MoveType.Stop)
        {
            float t = dst / TotalPathDistance;
            return GetPointAtTime(t, moveType);
        }

        public Vector3 GetDirectionAtTime(float currentTime, Vector3 currentPos, Vector3 transformForward, MoveType moveType = MoveType.Stop)
        {
            currentTime = currentTime.GetNewTimeWithMoveType(moveType);
            Vector3 dir = currentTime == 0 ? GetPointAtTime(eps) - nodes[0]
                        : currentTime == 1 ? smoothPoints[smoothPoints.Count - 1] - GetPointAtTime(1 - eps)
                        : GetPointAtTime(Mathf.Clamp01(currentTime + eps)) - GetPointAtTime(Mathf.Clamp01(currentTime - eps));
            dir.Normalize();
            return dir;
        }

        public Vector3 GetDirectionAtDistanceTravelled(float currentDstTravelled, Vector3 currentPos, Vector3 transformForward, MoveType moveType = MoveType.Stop)
        {
            float t = currentDstTravelled / TotalPathDistance;
            return GetDirectionAtTime(t, currentPos, transformForward, moveType);
        }

        public float GetClosestTimeOnPath(Vector3 worldPoint, MoveType moveType)
        {
            return GetClosestDistanceTravelled(worldPoint, moveType) / TotalPathDistance;
        }

        public float GetClosestDistanceTravelled(Vector3 worldPoint, MoveType moveType)
        {
            return PathUtility.GetClosestDistanceTravelled(worldPoint, pathInfos, smoothPoints, TotalPathDistance);
        }

        public int GetClosestNodeIndex(Ray ray)
        {
            Vector3 a = ray.origin;
            Vector3 b = ray.GetPoint(1000);
            for (int i = 0; i < nodes.Count; i++)
            {
                Vector3 closestPoint = Utility.ClosestPointOnLineSegment(nodes[i], a, b);
                if ((closestPoint - nodes[i]).sqrMagnitude < visualSetting.nodeRadius / 2f)
                {
                    return i;
                }
            }
            return -1;
        }

        public virtual void AddPoint(Vector3 mousePos)
        {
            nodes.Add(mousePos);
        }

        public virtual void UpdateNode(int index, Vector3 newPos)
        {
            nodes[index] = newPos;
        }

        public virtual void Reset()
        {
            nodes.Clear();
            isClose = false;
        }
        
        public void DoOffsetNodes(Vector3 moveAmount)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i] += moveAmount;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (UnityEditor.Selection.activeGameObject != gameObject && visualSetting.showPathNotSelected)
            {
                Gizmos.color = pathColor;
                for (int i = 0; i < smoothPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(smoothPoints[i], smoothPoints[i + 1]);
                }
            }
        }
#endif
        [System.Serializable]
        public class VisualSetting
        {
            public float nodeRadius = .5f;
            public float anchorRadius = .3f;
            public bool showPathNotSelected = true;
        }
    }
}