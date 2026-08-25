#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace VN.Editor
{
    public abstract class VNStoryNodeView : Node
    {
        public string NodeID;
        public VNStoryNodeType NodeType { get; protected set; }

        protected static readonly Color TriggerColor = new(0.2f, 0.55f, 0.25f);
        protected static readonly Color ConditionColor = new(0.7f, 0.6f, 0.15f);
        protected static readonly Color ActionColor = new(0.2f, 0.35f, 0.6f);
        protected static readonly Color DialogueColor = new(0.5f, 0.2f, 0.6f);

        protected VNStoryNodeView(string title, Color color, VNStoryNodeType type)
        {
            NodeID = Guid.NewGuid().ToString();
            NodeType = type;
            this.title = title;
            style.width = 280;
            titleContainer.style.backgroundColor = new StyleColor(color);
        }

        protected Port CreateInputPort(string name = "In")
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            port.portName = name;
            inputContainer.Add(port);
            return port;
        }

        protected Port CreateOutputPort(string name = "Out", Port.Capacity capacity = Port.Capacity.Multi)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(bool));
            port.portName = name;
            outputContainer.Add(port);
            return port;
        }

        public virtual VNStoryNodeData ToData()
        {
            var data = new VNStoryNodeData();
            data.id = NodeID;
            data.type = NodeType;
            var pos = GetPosition();
            data.posX = pos.x;
            data.posY = pos.y;
            return data;
        }

        public virtual void LoadFromData(VNStoryNodeData data)
        {
            NodeID = data.id;
        }
    }
}
#endif
