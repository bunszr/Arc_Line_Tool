using UnityEngine;
using ArcCreation;

namespace ArcExamples
{
    public class Example00 : MonoBehaviour
    {
        public TangetArcLine tangetArcLine;

        int[] movingIndies = new int[] { 1, 3, 5 };

        private void Update()
        {
            int signedDir = (int)Mathf.Sign(Mathf.PingPong(Time.time * 2, 5) - 2.5f);
            for (int i = 0; i < movingIndies.Length; i++)
            {
                int[] threeIndies = tangetArcLine.GetBeforeCurrNextIndies(movingIndies[i]);
                Vector3 moveDir = (tangetArcLine.GetNode(threeIndies[0]) - tangetArcLine.GetNode(threeIndies[2])).normalized.RotateXYPlane();
                tangetArcLine.UpdateNode(movingIndies[i], tangetArcLine.GetNode(movingIndies[i]) + moveDir * Time.deltaTime * 3 * signedDir);
            }
            tangetArcLine.UpdateWhenPathChanges();
        }
    }
}