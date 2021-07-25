using UnityEngine;
using ArcCreation;

namespace ArcExamples
{
    public class Example01 : MonoBehaviour
    {
        public ArcLine arcLine;
        public float speed = 30;
        public float maxAngle = 30;

        int[] movingIndies = new int[] { 1, 3, 5 };

        private void Update()
        {
            for (int i = 0; i < arcLine.NumAnchors; i++)
            {
                arcLine.UpdateAnchorAxisAngle(i, Mathf.PingPong(Time.time * speed, maxAngle) - maxAngle / 2);
            }
            arcLine.UpdateWhenPathChanges();
        }
    }
}