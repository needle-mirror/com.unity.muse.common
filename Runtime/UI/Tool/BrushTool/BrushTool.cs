using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common.Tools
{
    class BrushTool<T> : ICanvasTool where T: class, IMaskOperator
    {
        protected Model m_CurrentModel;
        PaintCanvasToolManipulator<T> m_CurrentManipulator;
        PaintingManipulatorSettings<T> m_Settings;
        MuseToolbar m_Toolbar;

        public CanvasManipulator GetToolManipulator()
        {
            Initialize();
            return m_CurrentManipulator;
        }

        public void SetModel(Model model)
        {
            m_CurrentModel = model;
            Initialize();
            m_Settings?.SetModel(m_CurrentModel);
        }

        protected virtual void Initialize()
        {
            m_CurrentManipulator ??= new PaintCanvasToolManipulator<T>(m_CurrentModel, new Vector2Int(2, 2));
            m_Settings ??= new PaintingManipulatorSettings<T>(this, m_CurrentManipulator);
        }

        public virtual bool EvaluateEnableState(Artifact artifact)
        {
            return m_CurrentModel.isRefineMode && ArtifactCache.IsInCache(artifact);
        }

        public void ActivateOperators()
        {
            if (m_CurrentModel == null) return;

            var opMask = m_CurrentModel.CurrentOperators.Find(x => x.GetType() == typeof(T)) ??
                m_CurrentModel.AddOperator<T>();

            if (opMask != null && !opMask.Enabled())
            {
                opMask.Enable(true);
                m_CurrentModel.UpdateOperators(opMask);
            }
        }

        public VisualElement GetToolView()
        {
            return m_Settings?.GetSettings();
        }

        public ICanvasTool.ToolButtonData GetToolData()
        {
            return new ICanvasTool.ToolButtonData()
            {
                Name = "muse-brush-tool-button",
                Label = "",
                Icon = "paint-brush",
                Tooltip = TextContent.controlMaskToolTooltip
            };
        }

        public VisualElement GetSettings()
        {
            return m_Settings?.GetSettings();
        }

        public float radius
        {
            get => m_CurrentManipulator.radius;
            set => m_CurrentManipulator.radius = value;
        }

        public void SetEraserMode(bool isEraser)
        {
            m_CurrentManipulator?.SetEraserMode(isEraser);
        }

        public void Clear()
        {
            m_CurrentManipulator?.ClearPainting();
        }
    }

    internal class PaintingManipulatorSettings<T> where T: class, IMaskOperator
    {
        MuseToolbar m_Root;
        BrushTool<T> m_BrushTool;
        PaintCanvasToolManipulator<T> m_ToolManipulator;
        bool m_IsInitialized;
        List<MuseShortcut> m_Shortcuts;
        Model m_CurrentModel;

        private PaintingManipulatorSettings() { }

        public PaintingManipulatorSettings(BrushTool<T> brushTool, PaintCanvasToolManipulator<T> paintManipulator)
        {
            m_BrushTool = brushTool;
            m_ToolManipulator = paintManipulator;
            Init();
        }

        public void SetModel(Model model)
        {
           m_CurrentModel = model;
        }

        void Init()
        {
            if (m_IsInitialized)
                return;

            m_Root = new MuseToolbar();

            m_Root.SizeSlider.label = "Radius";
            m_Root.SizeSlider.tooltip = TextContent.controlMaskBrushSizeTooltip;
            m_Root.SizeSlider.incrementFactor = 0.1f;
            m_Root.SizeSlider.formatString = "F1";
            m_Root.SizeSlider.lowValue = 3.0f;
            m_Root.SizeSlider.highValue = 50.0f;
            m_Root.SizeSlider.value = m_BrushTool.radius;
            m_Root.SizeSlider.style.width = 150.0f;

            m_Root.SizeSlider.RegisterValueChangedCallback(evt =>
            {
                m_BrushTool.radius = evt.newValue;
            });

            m_Root.EraseBtn.clickable.clicked += () =>
            {
                if (m_CurrentModel != null)
                    m_CurrentModel.SetActiveTool(m_BrushTool);

                m_BrushTool.SetEraserMode(true);

                m_BrushTool.ActivateOperators();
                m_BrushTool.radius = m_Root.SizeSlider.value;
            };

            m_Root.PaintBtn.clickable.clicked += () =>
            {
                if (m_CurrentModel != null)
                    m_CurrentModel.SetActiveTool(m_BrushTool);

                m_BrushTool.SetEraserMode(false);

               m_BrushTool.ActivateOperators();
               m_BrushTool.radius = m_Root.SizeSlider.value;
            };

            m_Root.DeleteBtn.clickable.clicked += () =>
            {
                m_BrushTool.Clear();
                m_BrushTool.radius = m_Root.SizeSlider.value;
            };

            m_Root.PanBtn.clickable.clicked += () =>
            {
                m_BrushTool.SetEraserMode(false);

                if (m_CurrentModel != null)
                    m_CurrentModel.SetActiveTool(null);
            };

            m_Root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            m_Root.RegisterCallback<DetachFromPanelEvent>(OnDetach);

            m_IsInitialized = true;
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            m_CurrentModel.OnActiveToolChanged += OnActiveToolChanged;
        }

        internal virtual void OnActiveToolChanged(ICanvasTool obj)
        {
            if (obj is BrushTool<T>)
                AddShortcuts();
            else
            {
                RemoveShortcuts();
                m_Root.SelectButton(m_Root.PanBtn);
            }
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            RemoveShortcuts();

            m_CurrentModel.OnActiveToolChanged -= OnActiveToolChanged;
        }

        void AddShortcuts()
        {
            RemoveShortcuts();

            m_Shortcuts = new List<MuseShortcut>
            {
                new("Increase Brush Size", OnIncreaseBrushSize, KeyCode.RightBracket, source: m_Root),
                new("Decrease Brush Size", OnDecreaseBrushSize, KeyCode.LeftBracket, source: m_Root),
                new("Toggle Brush", ToggleBrush, KeyCode.B, source: m_Root),
                new("Toggle Eraser", ToggleEraser, KeyCode.E, source: m_Root)
            };
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                m_Shortcuts.Add(new MuseShortcut("Clear", ClearDoodle, KeyCode.Backspace, KeyModifier.Action, source: m_Root));
            else
                m_Shortcuts.Add(new MuseShortcut("Clear", ClearDoodle, KeyCode.Delete, source: m_Root));

            foreach (var shortcut in m_Shortcuts)
                MuseShortcuts.AddShortcut(shortcut);
        }

        void RemoveShortcuts()
        {
            if (m_Shortcuts != null)
            {
                foreach (var shortcut in m_Shortcuts)
                    MuseShortcuts.RemoveShortcut(shortcut);
            }
        }

        public VisualElement GetSettings()
        {
            return m_Root;
        }

        void OnIncreaseBrushSize()
        {
            if (!isFocused)
                return;

            m_Root.SizeSlider.value += k_RadiusStep;
        }

        const float k_RadiusStep = 3f;

        void OnDecreaseBrushSize()
        {
            if (!isFocused)
                return;

            m_Root.SizeSlider.value -= k_RadiusStep;
        }

        void ToggleBrush()
        {
            if (!isFocused)
                return;

            m_Root.SelectButton(m_Root.PaintBtn);
            m_BrushTool.SetEraserMode(false);
        }

        void ToggleEraser()
        {
            if (!isFocused)
                return;

            m_Root.SelectButton(m_Root.EraseBtn);
            m_BrushTool.SetEraserMode(true);
        }

        void ClearDoodle()
        {
            if (!isFocused)
                return;

            m_BrushTool.Clear();
        }

        bool isFocused
        {
            get
            {
                var focusedElement = m_ToolManipulator?.target?.panel?.focusController?.focusedElement;
                return focusedElement == m_ToolManipulator?.target;
            }
        }
    }
}
