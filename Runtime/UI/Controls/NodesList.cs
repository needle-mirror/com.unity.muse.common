using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using Unity.Muse.Common.Utils;
using Dragger = Unity.Muse.Common.Baryon.UI.Manipulators.Dragger;

namespace Unity.Muse.Common
{
    public class NodesList : VisualElement, IControl
    {
        const string k_USSClassName = "muse-nodeslist";
        const string k_DragBarUssClassName = k_USSClassName + "__dragbar";
        const string k_ContentUssClassName = k_USSClassName + "__content";

        VisualElement m_Container;
        ScrollView m_ScrollView;

        Dictionary<IOperator, VisualElement> m_OperatorMap = new(); // Maps operator to their UI
        List<IOperator> m_Operators;

        Model m_CurrentModel;

        internal VisualElement draggerElement { get; private set; }

        Dragger m_HorizontalDraggable;

        VisualElement m_VerticalScrollerDragContainer;
        int m_CurrentMode;

        public event Action OnResized;
        public new class UxmlFactory : UxmlFactory<NodesList, UxmlTraits> { }

        private DateTime? m_LastGenerateTime;

        /// <summary>
        /// Generation Cooldown time in seconds.
        /// </summary>
        public static float GenerateCooldownTime = 1.5f;

        public NodesList()
        {
            this.ApplyTemplate("uxml/NodesList");
            Init();
        }

        void Init()
        {
            m_Operators = new List<IOperator>();
            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            m_ScrollView = this.Q<ScrollView>();
            m_ScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            m_ScrollView.verticalScroller.style.opacity = 0;
            m_VerticalScrollerDragContainer = m_ScrollView.verticalScroller.slider.Q(classes: BaseSlider<float>.dragContainerUssClassName);
            m_Container = this.Q<VisualElement>(classes: k_ContentUssClassName);
            draggerElement = this.Q<VisualElement>(k_DragBarUssClassName);
            m_HorizontalDraggable = new Dragger(OnResizeBarClicked, OnResizeBarDrag, OnResizeBarUp, OnResizeBarDown);
            draggerElement.AddManipulator(m_HorizontalDraggable);

            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));
            RegisterCallback<AttachToPanelEvent>(_ => SubscribeToEvents());
            RegisterCallback<DetachFromPanelEvent>(_ => UnsubscribeFromEvents());
        }

        void SubscribeToEvents()
        {
            if (m_CurrentModel == null)
                return;
            UnsubscribeFromEvents();

            m_CurrentModel.OnDispose += OnModelDispose;
            m_CurrentModel.OnOperatorUpdated += OnOperatorUpdated;
            m_CurrentModel.OnOperatorRemoved += OnOperatorRemoved;
            m_CurrentModel.OnGenerateButtonClicked += OnGenerateButtonClicked;
            m_CurrentModel.OnModeChanged += OnModeChanged;
            m_CurrentModel.OnSetReferenceOperator += OnSetReferenceOperator;
        }

        void UnsubscribeFromEvents()
        {
            if (m_CurrentModel == null)
                return;

            m_CurrentModel.OnDispose -= OnModelDispose;
            m_CurrentModel.OnOperatorUpdated -= OnOperatorUpdated;
            m_CurrentModel.OnOperatorRemoved -= OnOperatorRemoved;
            m_CurrentModel.OnGenerateButtonClicked -= OnGenerateButtonClicked;
            m_CurrentModel.OnModeChanged -= OnModeChanged;
            m_CurrentModel.OnSetReferenceOperator -= OnSetReferenceOperator;
        }

        void OnModelDispose()
        {
            SetModel(null);
        }

        void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (m_VerticalScrollerDragContainer.HasPointerCapture(evt.pointerId))
                return;

            m_ScrollView.verticalScroller.experimental.animation.Start(m_ScrollView.verticalScroller.resolvedStyle.opacity, 0f, 120,
                (element, f) => element.style.opacity = f);
        }

        void OnPointerEnter(PointerEnterEvent evt)
        {
            if (draggerElement.HasPointerCapture(evt.pointerId))
                return;

            m_ScrollView.verticalScroller.experimental.animation.Start(m_ScrollView.verticalScroller.resolvedStyle.opacity, 1f, 120,
                (element, f) => element.style.opacity = f);
        }

        void OnResizeBarDown(Dragger manipulator) { }

        void OnResizeBarUp(Dragger manipulator) { }

        void OnResizeBarDrag(Dragger manipulator)
        {
            var width = resolvedStyle.width;
            width += manipulator.deltaPos.x;
            style.width = width;
            OnResized?.Invoke();
        }

        void OnResizeBarClicked() { }

        void SetDefaultOperators()
        {
            m_CurrentModel.UpdateOperators(null, true);
        }

        /// <summary>
        /// Set the operators to be displayed in the list, clearing any previous ones.
        /// </summary>
        /// <param name="operators">The operators to display.</param>
        public void ResetOperators(IEnumerable<IOperator> operators)
        {
            foreach (var op in m_Operators)
            {
                op.UnregisterFromEvents(m_CurrentModel);
            }
            var previousScrollOffset = m_ScrollView.scrollOffset;
            m_Operators.Clear();
            m_Container.Clear();
            foreach (var op in operators)
            {
                if (m_CurrentMode >= 0 && op is GenerateOperator generateOperator)
                {
                    generateOperator.SetDropdownValue(m_CurrentMode);
                }

                SetOperator(op);
            }

            schedule.Execute(() => { m_ScrollView.scrollOffset = previousScrollOffset; });
        }

        /// <summary>
        /// Adds or replace, if it exists, the provided operator.
        /// </summary>
        void SetOperator(IOperator op)
        {
            // Replace operator if it exists
            RemoveOperator(op, out var index, out var insertAtIndex);
            if (index >= 0)
                m_Operators.Insert(index, op);
            else
                m_Operators.Add(op);

            op.RegisterToEvents(m_CurrentModel);

            if (!op.Enabled() || op.Hidden)
                return;
            if (insertAtIndex == -1)
                insertAtIndex = m_Container.childCount;         // Insert at the end by default

            var operatorView = op.GetOperatorView(m_CurrentModel);
            m_OperatorMap[op] = operatorView;
            m_Container.Insert(insertAtIndex, operatorView);
        }

        void RemoveOperator(IOperator op, out int foundIndex, out int removedAtIndex)
        {
            foundIndex = m_Operators.FindIndex(o => o.GetType() == op.GetType());
            removedAtIndex = -1;
            if (foundIndex >= 0)
            {
                op.UnregisterFromEvents(m_CurrentModel);
                m_OperatorMap.TryGetValue(m_Operators[foundIndex], out var view);
                if (view != null)
                {
                    removedAtIndex = m_Container.IndexOf(view);
                    if (removedAtIndex >= 0)
                        m_Container.RemoveAt(removedAtIndex);
                }

                m_Operators.Remove(op);
            }
        }

        void SetView()
        {
            SetDefaultOperators();
        }

        void OnModeChanged(int modeIndex)
        {
            m_CurrentMode = modeIndex;
            SetView();
        }

        void OnGenerateButtonClicked()
        {
            var currentTime = DateTime.Now;

            if(m_LastGenerateTime != null && (currentTime - m_LastGenerateTime.Value).TotalSeconds < GenerateCooldownTime)
                return;

            m_LastGenerateTime = currentTime;

            var referenceOp = m_Operators.GetOperator<ReferenceOperator>();
            var isVariation = referenceOp != null && referenceOp.Enabled();
            var generateOperator = m_Operators.GetOperator<GenerateOperator>();
            var promptOperator = m_Operators.GetOperator<PromptOperator>();

            if(promptOperator.IsPromptValid() == false)
                return;

            var modeType = ModesFactory.GetModeKeyFromIndex(m_CurrentMode);
            var groupArtifact = ArtifactFactory.CreateArtifact(modeType);
            groupArtifact.SetOperators(m_Operators);
            groupArtifact.StartGenerate(m_CurrentModel);

            for (var i = 0; i < generateOperator?.GetCount(); i++)
            {
                var artifact = ArtifactFactory.CreateArtifact(modeType);
                artifact.SetOperators(m_Operators);

                if (isVariation == false)
                    artifact.Generate(m_CurrentModel);
                else
                    artifact.Variate(m_Operators);

                m_CurrentModel.AddAsset(artifact);
            }
        }

        public void SetModel(Model model)
        {
            if(model == m_CurrentModel)
                return;

            UnsubscribeFromEvents();

            m_CurrentModel = model;

            if(m_CurrentModel == null)
                return;

            var currentMode = ModesFactory.GetModeIndexFromKey(model.CurrentMode);
            if (currentMode >= 0)
            {
                m_CurrentMode = currentMode;
            }

            SubscribeToEvents();
            SetView();
        }

        public void UpdateView()
        {
            //Here we would switch.
            throw new NotImplementedException();
        }

        void OnOperatorUpdated(IEnumerable<IOperator> operators, bool set)
        {
            if (set)
            {
                ResetOperators(operators);
            }
            else
            {
                foreach (var op in operators)
                    SetOperator(op);
            }
        }

        void OnOperatorRemoved(IEnumerable<IOperator> operators)
        {
            foreach (var op in operators)
                RemoveOperator(op, out _, out _);
        }

        void OnSetReferenceOperator(Artifact artifact)
        {
            var referenceOp = m_Operators.GetOperator<ReferenceOperator>();
            if (referenceOp is null)
            {
                referenceOp = new ReferenceOperator();
                m_Operators.Add(referenceOp);
            }
            referenceOp.SetReferenceImage(artifact);

            m_CurrentModel.UpdateOperators(referenceOp);
            m_CurrentModel.SetActiveTool(null);
        }
    }
}
