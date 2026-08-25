#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VN.Editor
{
    public class VNStoryGraphWindow : EditorWindow
    {
        private VNStoryGraphView _graphView;
        private string _exportFolder = "Assets";
        private ObjectField _soField;
        private VNStoryGraphData _loadedAsset;
        private TextField _graphNameField;
        private string _graphName = "";

        [MenuItem("Tools/VN Story Graph")]
        public static void Open()
        {
            var window = GetWindow<VNStoryGraphWindow>();
            window.titleContent = new GUIContent("VN Story Graph");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
        }

        private void OnDisable()
        {
            if (_graphView != null)
                rootVisualElement.Remove(_graphView);
        }

        private void ConstructGraphView()
        {
            _graphView = new VNStoryGraphView();
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            _graphNameField = new TextField("Name:");
            _graphNameField.style.width = 200;
            _graphNameField.RegisterValueChangedCallback(evt => _graphName = evt.newValue);
            toolbar.Add(_graphNameField);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            _soField = new ObjectField("Edit SO:");
            _soField.objectType = typeof(VNStoryGraphData);
            _soField.style.width = 250;
            _soField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is VNStoryGraphData asset)
                    LoadSOAsset(asset);
            });
            toolbar.Add(_soField);

            toolbar.Add(new Button(() => VNStorySaveUtility.ExportGraphSO(_graphView, _exportFolder))
            { text = "Export SO" });

            toolbar.Add(new Button(() => SaveToLoadedAsset())
            { text = "Save to SO" });

            toolbar.Add(new Button(() => ClearGraph())
            { text = "Clear" });

            rootVisualElement.Add(toolbar);
        }

        private void LoadSOAsset(VNStoryGraphData asset)
        {
            _loadedAsset = asset;
            _graphName = asset.graphName;
            _graphNameField.SetValueWithoutNotify(asset.graphName);
            _soField.SetValueWithoutNotify(asset);
            VNStorySaveUtility.LoadFromSO(_graphView, asset);
        }

        private void SaveToLoadedAsset()
        {
            if (_loadedAsset == null)
            {
                EditorUtility.DisplayDialog("No Asset Loaded", "Select or load a VNStoryGraphData asset first.", "OK");
                return;
            }

            var builtData = VNStorySaveUtility.BuildGraphData(_graphView);

            Undo.RecordObject(_loadedAsset, "Update VN Story Graph");
            _loadedAsset.graphName = _graphName;
            _loadedAsset.nodes = builtData.nodes;
            _loadedAsset.links = builtData.links;
            EditorUtility.SetDirty(_loadedAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[VNStoryGraphWindow] Saved to '{_loadedAsset.name}'");
        }

        private void ClearGraph()
        {
            if (EditorUtility.DisplayDialog("Clear Graph", "Are you sure you want to clear all nodes?", "Clear", "Cancel"))
            {
                _graphView.DeleteElements(_graphView.graphElements.ToList());
                _loadedAsset = null;
                _soField.SetValueWithoutNotify(null);
                _graphNameField.SetValueWithoutNotify("");
                _graphName = "";
            }
        }
    }
}
#endif
