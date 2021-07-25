using UnityEngine;
using ArcCreation;

namespace ArcExamples
{
    public class ChangeablePathFollower : MonoBehaviour
    {
        public TangetArcLine tangetArcLine;
        public MoveType moveType;

        float distanceTravelled;
        public float speed = 3;
        int dir = 1;

        private void Start()
        {
            tangetArcLine.onPathChangedEvent += OnPathChanged;
        }

        private void Update()
        {
            distanceTravelled += Time.deltaTime * dir * speed;
            transform.position = tangetArcLine.GetPointAtTravelledDistance(distanceTravelled, moveType);
            Vector3 lookDir = tangetArcLine.GetDirectionAtDistanceTravelled(distanceTravelled, transform.position, transform.forward, moveType);
            transform.rotation = Quaternion.LookRotation(lookDir);

            DoAnim();
        }

        void DoAnim()
        {
            int signedDir = (int)Mathf.Sign(Mathf.PingPong(Time.time, 2) - 1);
            Vector3 moveDir = (tangetArcLine.GetNode(2) - tangetArcLine.GetNode(0)).normalized.RotateXZPlane();
            tangetArcLine.UpdateNode(1, tangetArcLine.GetNode(1) + moveDir * Time.deltaTime * 3 * signedDir);
            tangetArcLine.UpdateWhenPathChanges();
        }

        public void OnPathChanged()
        {
            if (moveType == MoveType.Stop && distanceTravelled >= tangetArcLine.TotalPathDistance)
                return;
            else if (moveType == MoveType.Reverse)
            {
                if (distanceTravelled >= tangetArcLine.TotalPathDistance)
                    dir = -1;
                else if (distanceTravelled < 0)
                    dir = 1;
            }
            distanceTravelled = tangetArcLine.GetClosestDistanceTravelled(transform.position, moveType);
        }
    }
}