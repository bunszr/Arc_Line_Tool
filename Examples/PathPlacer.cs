using UnityEngine;
using ArcCreation;

namespace ArcExamples
{
    [ExecuteInEditMode]
    public class PathPlacer : MonoBehaviour
    {
        public BaseLine baseLine;
        public int numObject = 20;
        public float radius = .5f;

        private void OnDrawGizmosSelected()
        {
            if (baseLine)
            {
                Gizmos.color = Color.yellow;

                float stepPercent = 1f / numObject;
                for (int i = 0; i < numObject; i++)
                {
                    Vector3 pos = baseLine.GetPointAtTime(stepPercent * i); ;
                    Gizmos.DrawSphere(pos, radius);
                }
            }
        }
    }
}