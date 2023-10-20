using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    internal class Canvas : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Canvas, UxmlTraits> { }

        Model m_CurrentModel;

        CanvasManipulator m_CurrentToolManipulator;

        readonly AppUI.UI.Canvas m_Canvas;

        Artifact m_RefinedArtifact;

        public Artifact refinedArtifact
        {
            get => m_RefinedArtifact;
            private set
            {
                if (m_RefinedArtifact == value)
                    return;

                m_RefinedArtifact = value;
                UpdateView();
            }
        }

        public override VisualElement contentContainer => m_Canvas.contentContainer;

        public Canvas()
        {
            m_Canvas = new AppUI.UI.Canvas
            {
                frameMargin = 24f
            };
            hierarchy.Add(m_Canvas);
            m_Canvas.StretchToParentSize();
            this.StretchToParentSize();

            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));
        }

        void FrameArtifact(Artifact artifact)
        {
            // we just have one node in the canvas so we just need to check the first child
            if (m_Canvas.childCount == 0 || m_Canvas.ElementAt(0) is not ArtifactNode node || node.artifact != artifact)
                return;

            FrameAll();
        }

        public void FrameAll()
        {
            m_Canvas.FrameAll();
        }
        public void SetModel(Model model)
        {
            UnSubscribeToModelEvents();
            m_CurrentModel = model;
            SubscribeToModelEvents();
        }

        void SubscribeToModelEvents()
        {
            if (m_CurrentModel == null)
                return;

            m_CurrentModel.OnDispose += OnModelDispose;
            m_CurrentModel.OnActiveToolChanged += OnActiveToolChanged;
            m_CurrentModel.OnFrameArtifactRequested += FrameArtifact;
            m_CurrentModel.OnDispose += UnSubscribeToModelEvents;
            m_CurrentModel.OnCanvasRefineArtifact += OnCanvasRefineArtifact;
            m_CurrentModel.OnArtifactSelected += OnArtifactSelected;
            m_CurrentModel.OnFinishRefineArtifact += OnFinishRefineArtifact;
        }

        private void OnFinishRefineArtifact(Artifact obj)
        {
            refinedArtifact = null;
            UpdateView();
        }

        void OnArtifactSelected(Artifact artifact)
        {
            if (m_CurrentModel == null || !m_CurrentModel.isRefineMode)
                return;

            OnActiveToolChanged(m_CurrentModel.ActiveTool);
            OnCanvasRefineArtifact(artifact);
        }

        void OnCanvasRefineArtifact(Artifact artifact)
        {
            if (m_CurrentModel == null)
                return;

            refinedArtifact = artifact;
        }

        void UnSubscribeToModelEvents()
        {
            if (m_CurrentModel == null)
                return;

            m_CurrentModel.OnDispose -= OnModelDispose;
            m_CurrentModel.OnActiveToolChanged -= OnActiveToolChanged;
            m_CurrentModel.OnFrameArtifactRequested -= FrameArtifact;
            m_CurrentModel.OnCanvasRefineArtifact -= OnCanvasRefineArtifact;
            m_CurrentModel.OnArtifactSelected -= OnArtifactSelected;
            m_CurrentModel.OnFinishRefineArtifact -= OnFinishRefineArtifact;
        }

        void OnModelDispose()
        {
            SetModel(null);
        }

        void OnActiveToolChanged(ICanvasTool tool)
        {
            if (m_CurrentToolManipulator != null)
                m_Canvas.RemoveManipulator(m_CurrentToolManipulator);

            if (tool == null)
                return;

            m_CurrentToolManipulator = tool.GetToolManipulator();
            m_Canvas.AddManipulator(m_CurrentToolManipulator);
        }

        public void UpdateView()
        {
            Clear();
            if (m_CurrentModel == null || refinedArtifact is null)
                return;

            var node = new ArtifactNode
            {
                artifact = refinedArtifact
            };
            Add(node);

            schedule.Execute(FrameAll).ExecuteLater(32L);
        }
    }
}
