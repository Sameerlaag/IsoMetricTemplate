#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace VN.Editor
{
    public class VNStoryGraphView : GraphView
    {
        private VNStorySearchWindow _searchWindow;

        
        public VNStoryGraphView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            AddSearchWindow();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatible = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port) return;
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;
                compatible.Add(port);
            });

            return compatible;
        }

        private void AddSearchWindow()
        {
            _searchWindow = ScriptableObject.CreateInstance<VNStorySearchWindow>();
            _searchWindow.graphView = this;

            nodeCreationRequest = ctx =>
            {
                // Pass host window reference if available, or rely on focusedWindow
                _searchWindow.window = EditorWindow.focusedWindow;

                SearchWindow.Open(
                    new SearchWindowContext(ctx.screenMousePosition),
                    _searchWindow
                );
            };
        }
    }
}
#endif
