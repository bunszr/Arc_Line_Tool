using UnityEngine;
using UnityEditor;
using ArcCreation;

namespace ArcEditor
{
    [CustomEditor(typeof(ArcLine))]
    public class ArcLineEditor : BaseLineEditor
    {
        ArcLine arcLine;
        float shiftPressedAnchorPerpendicularDst;

        bool openLastSelectedAnchorAndNodeInfo = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            arcLine = (ArcLine)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            arcLine.useSnap = EditorGUILayout.Toggle("Use Snap", arcLine.useSnap);
            if (arcLine.useSnap)
            {
                arcLine.snapValue = EditorGUILayout.FloatField("Snap Value", arcLine.snapValue);
            }

            openLastSelectedAnchorAndNodeInfo = EditorGUILayout.Foldout(openLastSelectedAnchorAndNodeInfo, "Show Anchor & Node Setting");
            if (openLastSelectedAnchorAndNodeInfo)
            {
                DrawLastSelectedNodeIndex();
                GUILayout.Label("    Last Selected Anchor Index : " + info.lastSelectedAnchorIndex);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    float axisAngle = EditorGUILayout.FloatField("    Axis Angle", arcLine.GetAnchor(info.lastSelectedAnchorIndex).axisAngleNodeAToNodeB);
                    float perpDst = EditorGUILayout.FloatField("    Perp Dst", arcLine.GetAnchor(info.lastSelectedAnchorIndex).perpendicularDst);
                    if (GUI.changed)
                    {
                        ArcLine.Anchor anchor = arcLine.GetAnchor(info.lastSelectedAnchorIndex);
                        anchor.axisAngleNodeAToNodeB = axisAngle;
                        anchor.perpendicularDst = perpDst;
                        arcLine.SetAnchor(info.lastSelectedAnchorIndex, anchor);
                    }
                }

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    Vector3 nodePos = EditorGUILayout.Vector3Field("    Node Pos", arcLine.GetNode(info.lastSelectedNodeIndex));
                    if (GUI.changed)
                    {
                        arcLine.UpdateNode(info.lastSelectedNodeIndex, nodePos);
                    }
                }
            }

            DrawInspResetAndOpenCloseButton();
            GUIChanged();
        }

        protected override void RightMouseDown(Vector3 mousePos)
        {
            base.RightMouseDown(mousePos);
            info.lastSelectedAnchorIndex = info.mouseOverAnchorIndex != -1 ? info.mouseOverAnchorIndex : 0;
        }

        protected override void Draw()
        {
            base.Draw();

            for (int i = 0; i < arcLine.NumAnchors; i++)
            {
                ArcLine.Anchor anc = arcLine.GetAnchor(i);
                Handles.color = info.mouseOverAnchorIndex == i ? Color.red : Color.black;
                Vector3 midPointNodeAToB = (arcLine.GetNode(i) + arcLine.GetNode((i + 1) % arcLine.NumNodes)) * .5f;
                Handles.DrawLine(anc.position, anc.center);
                Handles.SphereHandleCap(i, anc.position, Quaternion.identity, arcLine.visualSetting.anchorRadius, EventType.Repaint);
            }
        }

        protected override void MouseMove(Ray ray)
        {
            base.MouseMove(ray);
            info.mouseOverAnchorIndex = GetClosestAnchorIndex(ray);
        }

        protected override void LeftMouseDrag(Vector3 mousePos, Ray ray)
        {
            base.LeftMouseDrag(mousePos, ray);

            if (info.selectedAnchorIndex != -1)
            {
                Plane plane = new Plane(ray.direction, arcLine.GetAnchor(info.selectedAnchorIndex).position);
                float enter = 0;
                if (plane.Raycast(ray, out enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    if (Event.current.modifiers == EventModifiers.Shift)
                    {
                        UpdateAnchorAngle(info.selectedAnchorIndex, -Event.current.delta.y);
                    }
                    else
                    {
                        ArcLine.Anchor anchor = arcLine.GetAnchor(info.selectedAnchorIndex);
                        float dstArcCenterToPlaneHitPoint = (anchor.twoNodeMidPoint - hitPoint).magnitude;
                        Vector3 dirAnchorPos2CenterPos = anchor.center - anchor.position;
                        arcLine.UpdateAnchorPerpendicalarDstToMidPoint(info.selectedAnchorIndex, dstArcCenterToPlaneHitPoint);
                    }
                }
            }
            isRepaint = true;
        }

        protected override void LeftMouseDown(Vector3 mousePos)
        {
            base.LeftMouseDown(mousePos);
            if (info.mouseOverAnchorIndex != -1)
            {
                pressedPoint = arcLine.GetAnchor(info.mouseOverAnchorIndex).position;
            }
        }

        void UpdateAnchorAngle(int anchorIndex, float stepAngle)
        {
            ArcLine.Anchor anc = arcLine.GetAnchor(anchorIndex);
            anc.axisAngleNodeAToNodeB += stepAngle;
            arcLine.SetAnchor(anchorIndex, anc);
        }

        int GetClosestAnchorIndex(Ray ray)
        {
            Vector3 a = ray.origin;
            Vector3 b = ray.GetPoint(1000);
            for (int i = 0; i < arcLine.NumAnchors; i++)
            {
                Vector3 closestPoint = Utility.ClosestPointOnLineSegment(arcLine.GetAnchor(i).position, a, b);
                if ((closestPoint - arcLine.GetAnchor(i).position).sqrMagnitude < arcLine.visualSetting.anchorRadius / 2)
                    return i;
            }
            return -1;
        }

    }
}