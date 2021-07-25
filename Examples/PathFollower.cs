using UnityEngine;
using ArcCreation;

namespace ArcExamples
{
    public class PathFollower : MonoBehaviour
    {
        public BaseLine baseLine;
        public MoveType moveType;

        float distanceTravelled;
        public float speed = 3;

        private void Update()
        {
            distanceTravelled += Time.deltaTime * speed;
            transform.position = baseLine.GetPointAtTravelledDistance(distanceTravelled, moveType);
            Vector3 lookDir = baseLine.GetDirectionAtDistanceTravelled(distanceTravelled, transform.position, transform.forward, moveType);
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}