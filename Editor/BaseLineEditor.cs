using UnityEngine;
using UnityEditor;
using ArcCreation;

namespace ArcEditor
{
    [CustomEditor(typeof(BaseLine))]
    public class BaseLineEditor : Editor
    {
        protected Vector3 mousePos;

        protected Event guiEvent;
        protected BaseLine baseLine;
        protected Info info;
        protected bool isRepaint = false;
        protected bool isRepaintInspector = false;
        protected Vector3 pressedPoint;
        private Vector3 nodesOffsetEditor;
        private Vector3 oldNodesOffsetEditor;


        protected PathSpace pathSpaceEditor;

        protected virtual void OnEnable()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            baseLine = target as BaseLine;
            pathSpaceEditor = baseLine.PathSpace;
            info = new Info();
            baseLine.Init();
        }

        public override void OnInspectorGUI()
        {
            string helpMessage = "Ctrl+Sol Tık = Düğümü Sil || Shift+Sol Tık = Yeni düğüm ekle || Sağ Tık = Düğüm ya da Anchor seç";
            EditorGUILayout.HelpBox(helpMessage, MessageType.Info);
            base.OnInspectorGUI();

            if (pathSpaceEditor != baseLine.PathSpace)
            {
                pathSpaceEditor = baseLine.PathSpace;
                baseLine.PathSpace = baseLine.PathSpace;
            }

            nodesOffsetEditor = EditorGUILayout.Vector3Field("Nodes Offset", nodesOffsetEditor);
            if (nodesOffsetEditor != oldNodesOffsetEditor)
            {
                Undo.RecordObject(baseLine, "Offset Nodes");
                baseLine.DoOffsetNodes(nodesOffsetEditor - oldNodesOffsetEditor);
                oldNodesOffsetEditor = nodesOffsetEditor;
            }

        }

        protected void DrawInspResetAndOpenCloseButton()
        {

            GUILayout.BeginHorizontal();
            string closeOpenString = baseLine.IsClose ? "Open Path" : "Close Path";
            if (GUILayout.Button(closeOpenString))
            {
                info.lastSelectedNodeIndex = 0;
                info.lastSelectedAnchorIndex = 0;
                if (baseLine.IsClose)
                    baseLine.OpenPath();
                else
                    baseLine.ClosePath();
            }

            if (GUILayout.Button("Reset"))
            {
                info.lastSelectedNodeIndex = 0;
                info.lastSelectedAnchorIndex = 0;
                baseLine.Reset();
                SceneView.RepaintAll();
            }
            GUILayout.EndHorizontal();
        }

        protected void DrawLastSelectedNodeIndex()
        {
            GUILayout.Label("    Last Selected Node Index : " + info.lastSelectedNodeIndex);
        }

        protected void GUIChanged()
        {
            if (GUI.changed)
            {
                baseLine.ConvertToSmoothCurve();
                SceneView.RepaintAll();
            }
        }

        protected virtual void OnSceneGUI()
        {
            guiEvent = Event.current;

            if (guiEvent.type == EventType.Repaint)
            {
                Draw();
            }
            else if (guiEvent.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else
            {
                HandleInput(guiEvent);

                if (isRepaint)
                {
                    baseLine.ConvertToSmoothCurve();
                    HandleUtility.Repaint();
                    isRepaint = false;
                }

                if (isRepaintInspector)
                {
                    Repaint();
                    isRepaintInspector = false;
                }
            }
        }

        protected virtual void Draw()
        {
            Vector3 diskDirection = baseLine.MovingSpace == MovingSpace.XY ? Vector3.forward : Vector3.up;
            for (int i = 0; i < baseLine.NumNodes; i++)
            {
                Handles.color = info.mouseOverNodeIndex == i ? Color.red : Color.white;
                Handles.SphereHandleCap(i, baseLine.GetNode(i), Quaternion.identity, baseLine.visualSetting.nodeRadius, EventType.Repaint);
                Handles.color = Color.black;
            }

            Handles.color = baseLine.pathColor;
            for (int i = 0; i < baseLine.NumSmoothPoints - 1; i++)
            {
                Handles.DrawLine(baseLine.GetSmooth(i), baseLine.GetSmooth(i + 1));
            }

            if (info.selectedNodeIndex != -1)
            {
                Handles.DrawWireCube(pressedPoint, baseLine.MovingSpace == MovingSpace.XY ? new Vector3(5, 5, .2f) : new Vector3(5, .2f, 5));
            }
        }

        void HandleInput(Event guiEvent)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float planeHeight = 0;
            if (baseLine.MovingSpace == MovingSpace.XY)
                planeHeight = baseLine.GetNode(baseLine.LastNodeIndex).z;
            else
                planeHeight = baseLine.GetNode(baseLine.LastNodeIndex).y;
            mousePos = MouseUtility.GetMousePos(ray, guiEvent.mousePosition, baseLine.MovingSpace, planeHeight);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
            {
                ShiftLeftMouseDown(mousePos);
            }
            else if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Control)
            {
                CtrlLeftMouseDown(mousePos);
            }
            else if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
            {
                LeftMouseDown(mousePos);
            }
            else if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0)
            {
                LeftMouseDrag(mousePos, ray);
            }
            else if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
            {
                LeftMouseUp();
            }
            else if (guiEvent.type == EventType.MouseMove)
            {
                MouseMove(ray);
            }
            else if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Space)
            {
                baseLine.ChangeSpace();
                if (info.selectedNodeIndex != -1)
                {
                    pressedPoint = baseLine.GetNode(info.selectedNodeIndex);
                }
            }
            else if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
            {
                RightMouseDown(mousePos);
            }
        }

        void ShiftLeftMouseDown(Vector3 mousePos)
        {
            Undo.RecordObject(baseLine, "Add Point");
            baseLine.AddPoint(mousePos);
            isRepaint = true;
        }

        protected virtual void CtrlLeftMouseDown(Vector3 mousePos)
        {
            if (info.mouseOverNodeIndex != -1)
            {
                Undo.RecordObject(baseLine, "Remove Point");
                baseLine.DeletePoint(info.mouseOverNodeIndex);
                info.lastSelectedAnchorIndex = 0;
                info.lastSelectedNodeIndex = 0;
                isRepaint = true;
            }
        }

        protected virtual void LeftMouseDown(Vector3 mousePos)
        {
            if (info.mouseOverNodeIndex != -1)
            {
                info.selectedNodeIndex = info.mouseOverNodeIndex;
                info.lastSelectedNodeIndex = info.mouseOverNodeIndex;
                pressedPoint = baseLine.GetNode(info.selectedNodeIndex);
            }
            if (info.mouseOverAnchorIndex != -1)
            {
                info.selectedAnchorIndex = info.mouseOverAnchorIndex;
                info.lastSelectedAnchorIndex = info.mouseOverAnchorIndex;
            }
            isRepaint = true;
        }

        protected virtual void MouseMove(Ray ray)
        {
            info.mouseOverNodeIndex = baseLine.GetClosestNodeIndex(ray);
            isRepaint = true;
        }

        protected virtual void LeftMouseDrag(Vector3 mousePos, Ray ray)
        {
            Undo.RecordObject(baseLine, "Move Point");
            if (info.selectedNodeIndex != -1)
            {
                baseLine.UpdateNode(info.selectedNodeIndex, GetHitPoint(mousePos, ray));
            }
        }

        protected virtual void RightMouseDown(Vector3 mousePos)
        {
            info.lastSelectedNodeIndex = info.mouseOverNodeIndex != -1 ? info.mouseOverNodeIndex : 0;
            isRepaintInspector = true;
        }

        protected Vector3 GetHitPoint(Vector3 mousePos, Ray ray)
        {
            float enter = 0;
            Vector3 inNormal = baseLine.MovingSpace == MovingSpace.XY ? Vector3.forward : Vector3.up;
            Plane plane = new Plane(inNormal, pressedPoint);
            Handles.DrawWireCube(pressedPoint, baseLine.MovingSpace == MovingSpace.XY ? new Vector3(5, 5, .5f) : new Vector3(5, .5f, 5));
            plane.Raycast(ray, out enter);
            return ray.GetPoint(enter);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            baseLine.UpdateWhenPathChanges();
            if (baseLine != null)
            {
                EditorUtility.SetDirty(baseLine);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(baseLine.gameObject.scene);
            }
        }

        void LeftMouseUp()
        {
            info.selectedNodeIndex = -1;
            info.selectedAnchorIndex = -1;
        }

        void UndoRedoPerformed()
        {
            info.lastSelectedNodeIndex = 0;
            info.lastSelectedAnchorIndex = 0;
        }

        public class Info
        {
            public int selectedNodeIndex = -1;
            public int mouseOverNodeIndex = -1;

            public int selectedAnchorIndex = -1;
            public int mouseOverAnchorIndex = -1;

            public int lastSelectedNodeIndex;
            public int lastSelectedAnchorIndex;
        }
    }
}