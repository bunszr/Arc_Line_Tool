using UnityEngine;
using UnityEditor;
using ArcCreation;

namespace ArcEditor
{
    [CustomEditor(typeof(TangetArcLine))]
    public class TangetArcLineEditor : BaseLineEditor
    {
        TangetArcLine tangetArcLine;
        float shiftPressedAnchorPerpendicularDst;
        bool openLastSelectedAnchorAndNodeInfo = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            tangetArcLine = (TangetArcLine)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            openLastSelectedAnchorAndNodeInfo = EditorGUILayout.Foldout(openLastSelectedAnchorAndNodeInfo, "Show Anchor & Node Setting");
            if (openLastSelectedAnchorAndNodeInfo)
            {
                DrawLastSelectedNodeIndex();
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    info.lastSelectedNodeIndex = Mathf.Clamp(info.lastSelectedNodeIndex, 0, tangetArcLine.NumAnchors - 1);
                    float radius = EditorGUILayout.FloatField("    Radius", tangetArcLine.GetAnchor(info.lastSelectedNodeIndex).radius);
                    if (GUI.changed)
                    {
                        tangetArcLine.UpdateAnchorRadius(info.lastSelectedNodeIndex, radius);
                        tangetArcLine.UpdateCircleRadiusRecursive(info.lastSelectedNodeIndex, -1, 0);
                        tangetArcLine.UpdateCircleRadiusRecursive(info.lastSelectedNodeIndex, 1, tangetArcLine.NumNodes - 1);
                        isRepaint = true;
                    }
                }
            }

            DrawInspResetAndOpenCloseButton();
            GUIChanged();
        }

        protected override void Draw()
        {
            base.Draw();

            Handles.color = Color.black;
            if (tangetArcLine.showCircle)
            {
                Vector3 axis = tangetArcLine.PathSpace == PathSpace.XY ? Vector3.forward : Vector3.down;
                for (int i = 0; i < tangetArcLine.NumAnchors; i++)
                {
                    Handles.DrawWireDisc(tangetArcLine.GetNode(i), axis, tangetArcLine.GetAnchor(i).radius);
                }
            }

            if (0 < info.selectedNodeIndex && info.selectedNodeIndex < tangetArcLine.NumNodes - 1)
            {
                Handles.DrawLine(tangetArcLine.moveOnLineSegmentPosA, tangetArcLine.moveOnLineSegmentPosB);
            }
        }

        protected override void LeftMouseDrag(Vector3 mousePos, Ray ray)
        {
            Undo.RecordObject(baseLine, "Move Point");
            if (info.selectedNodeIndex != -1)
            {
                if (guiEvent.modifiers == EventModifiers.Shift)
                {
                    tangetArcLine.UpdateNodeFreedom(info.selectedNodeIndex, GetHitPoint(mousePos, ray));
                }
                else
                {
                    tangetArcLine.UpdateNode(info.selectedNodeIndex, GetHitPoint(mousePos, ray));
                }
            }
            isRepaint = true;
        }
    }
}