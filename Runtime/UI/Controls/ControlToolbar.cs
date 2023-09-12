using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public class ControlToolbar : VisualElement, IControl
    {
        const string k_USSClassName = "muse-controltoolbar";

        const string k_ActionGroupUssClassName = k_USSClassName + "__actiongroup";

        Model m_Model;
        VisualElement m_Settings;
        bool m_Initialized;

        ActionGroup m_ActionGroup;
        List<ICanvasTool> m_Tools;
        Dictionary<ICanvasTool, ActionButton> m_ActionButtons;

        public new class UxmlFactory : UxmlFactory<ControlToolbar, UxmlTraits> { }

        public ControlToolbar()
        {
            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));
        }

        public void SetModel(Model model)
        {
            if (m_Model == model)
                return;

            Unbind();
            m_Model = model;
            Bind();
        }

        void Bind()
        {
            if (!m_Initialized)
                Init();

            m_Model.OnArtifactSelected += OnArtifactSelected;
            m_Model.OnRefineArtifact += OnArtifactSelected;
            m_Model.OnFinishRefineArtifact += OnFinishRefineArtifact;
            m_Model.OnDispose += Unbind;

            UpdateView();
        }

        void OnArtifactSelected(Artifact artifact)
        {
            if (artifact is null)
            {
                CleanToolbar();
            }

            UpdateView();
        }

        void Init()
        {
            m_ActionGroup ??= this.Q<ActionGroup>(k_ActionGroupUssClassName);
            m_ActionGroup.Clear();

            m_Tools ??= new List<ICanvasTool>();
            m_Tools.Clear();

            m_Tools.AddRange(AvailableToolsFactory.GetAvailableTools(m_Model));

            foreach (var tool in m_Tools)
            {
                var buttonData = tool.GetToolData();
                var button = new ActionButton
                {
                    name = buttonData.Name,
                    label = buttonData.Label,
                    icon = buttonData.Icon,
                    tooltip = buttonData.Tooltip,
                    quiet = true
                };

                button.AddToClassList("muse-controltoolbar__actionbutton");

                button.clickable.clicked += () =>
                {

                    if (button.selected)
                    {
                        CleanToolbar();
                        UpdateView();
                        return;
                    }
                    tool.ActivateOperators();
                    m_Model?.SetActiveTool(tool);
                    m_Settings ??= tool.GetSettings();
                    if(m_Settings !=null)
                        Add(m_Settings);
                    UpdateView();

                };
                m_ActionButtons ??= new Dictionary<ICanvasTool, ActionButton>();

                m_ActionButtons.Add(tool, button);
                m_ActionGroup.Add(button);
            }

            UpdateView();
            m_Initialized = true;
        }

        void OnFinishRefineArtifact(Artifact artifact)
        {
            CleanToolbar();
            UpdateView();
        }

        void CleanToolbar()
        {
            RemoveSettings();
            if (m_Model != null)
            {
                m_Model.SetActiveTool(null);
            }
        }

        void RemoveSettings()
        {
            if (m_Settings != null)
            {
                Remove(m_Settings);
                m_Settings = null;
            }
        }

        void Unbind()
        {
            if(m_Model == null) return;

            m_Model.OnArtifactSelected -= OnArtifactSelected;
            m_Model.OnRefineArtifact -= OnArtifactSelected;
            m_Model.OnFinishRefineArtifact -= OnFinishRefineArtifact;
            m_Model.OnDispose -= Unbind;
        }


        public void UpdateView()
        {
            foreach (var kvp in m_ActionButtons)
            {
                kvp.Value.EnableInClassList(Styles.hiddenUssClassName, !kvp.Key.EvaluateEnableState(m_Model?.SelectedArtifact));
                kvp.Value.selected = m_Model?.ActiveTool == kvp.Key;
            }
        }
    }
}
