using Unity.AppUI.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Text = Unity.AppUI.UI.Text;
using TouchSliderInt = Unity.AppUI.UI.TouchSliderInt;
using Button = Unity.AppUI.UI.Button;
#if UNITY_WEBGL && !UNITY_EDITOR
using Dropdown = Unity.AppUI.UI.Dropdown;
#endif

namespace Unity.Muse.Common
{
    [Serializable]
    public class GenerateOperator : IOperator
    {
        public string OperatorName => "GenerateOperator";
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Generate";
        event Action OnDataUpdate;

        [SerializeField]
        OperatorData m_OperatorData;

        internal CooldownManipulator<PointerDownEvent> m_GenerateButtonCooldown;

        public GenerateOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "0.0.1", new[] { "TextToImage", "4" }, false);
        }

        public bool IsSavable()
        {
            return true;
        }

        public int GetCount()
        {
            return int.Parse(m_OperatorData.settings[1]);
        }

        public void SetDropdownValue(int mode)
        {
            m_OperatorData.settings[0] = ModesFactory.GetModeKeyFromIndex(mode);
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("PromptOperator.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var ui = new GenerateOperatorUI(model, m_OperatorData, OnDataUpdate);

            return ui;
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            return null;
        }

        public OperatorData GetOperatorData()
        {
            return m_OperatorData;
        }

        public void SetOperatorData(OperatorData data)
        {
            m_OperatorData.enabled = data.enabled;
            if (data.settings == null || data.settings.Length < 2)
                return;
            m_OperatorData.settings = data.settings;
            OnDataUpdate?.Invoke();
        }

        void SetSettings(IReadOnlyList<string> settings)
        {
            m_OperatorData.settings[0] = settings[0];
            m_OperatorData.settings[1] = settings[1];

            OnDataUpdate?.Invoke();
        }

        string[] GetSettings()
        {
            return m_OperatorData.settings;
        }

        public bool Enabled()
        {
            return m_OperatorData.enabled;
        }

        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        public bool Hidden { get; set; }

        public IOperator Clone()
        {
            var result = new GenerateOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        public void RegisterToEvents(Model model) { }

        public void UnregisterFromEvents(Model model) { }

        class GenerateOperatorUI : ExVisualElement
        {
            Model m_CurrentModel;
            Button m_CurrentGenerateButton;

            public GenerateOperatorUI(Model model, OperatorData operatorData, Action OnDataUpdate)
            {
                m_CurrentModel = model;
                passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows;

                AddToClassList("muse-node");
                name = "generate-node";
                var text = new Text();
                text.text = "Generation";
                text.AddToClassList("muse-node__title");
                text.AddToClassList("bottom-gap");
                Add(text);

                //Dropdown
                var modes = ModesFactory.GetModes();
#if UNITY_WEBGL && !UNITY_EDITOR
                var dropdown = new Dropdown();
                dropdown.name = "generation-type-dropdown";
                dropdown.AddToClassList("bottom-gap");

                //Need to get Labels...
                dropdown.bindItem = (item, i) => item.label = modes[i];
                dropdown.sourceItems = modes;
                dropdown.SetValueWithoutNotify(new[] {ModesFactory.GetModeIndexFromKey(operatorData.settings[0])});
                dropdown.RegisterValueChangedCallback((evt) =>
                {
                    operatorData.settings[0] = ModesFactory.GetModeKeyFromIndex(evt.newValue.FirstOrDefault());
                    model.ModeChanged(evt.newValue.FirstOrDefault());
                });

                Add(dropdown);
#endif

                var imageCountSlider = new TouchSliderInt { tooltip = TextContent.operatorGenerateNumberTooltip };
                imageCountSlider.name = "image-count-slider";
                imageCountSlider.AddToClassList("bottom-gap");
                imageCountSlider.label = "Images";
                imageCountSlider.lowValue = 1;
                imageCountSlider.highValue = 10;
                imageCountSlider.value = int.Parse(operatorData.settings[1]);
                imageCountSlider.RegisterValueChangedCallback(evt =>
                {
                    operatorData.settings[1] = evt.newValue.ToString();
                });
                Add(imageCountSlider);

                m_CurrentGenerateButton = new Button();
                m_CurrentGenerateButton.name = "generate-button";
                m_CurrentGenerateButton.title = "Generate";

                var m_GenerateButtonCooldown = new CooldownManipulator<PointerDownEvent>(true, NodesList.GenerateCooldownTime);
                m_CurrentGenerateButton.AddManipulator(m_GenerateButtonCooldown);

                model.OnGenerateButtonClicked += m_GenerateButtonCooldown.ForceCooldown;

                m_CurrentGenerateButton.AddToClassList("muse-node__button");
                m_CurrentGenerateButton.primary = true;

                m_CurrentGenerateButton.clicked += model.GenerateButtonClicked;

                m_CurrentGenerateButton.SetEnabled(false);

                Add(m_CurrentGenerateButton);

                OnDataUpdate += () =>
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (operatorData.settings[0] != "")
                {
                    dropdown.SetValueWithoutNotify(new[] {ModesFactory.GetModeIndexFromKey(operatorData.settings[0])});
                }
#endif

                    if (operatorData.settings[1] != "")
                    {
                        imageCountSlider.value = int.Parse(operatorData.settings[1]);
                    }
                };

                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                m_CurrentModel.OnCurrentPromptChanged -= OnPromptChanged;
            }

            void OnAttachToPanel(AttachToPanelEvent evt)
            {
                m_CurrentModel.OnCurrentPromptChanged += OnPromptChanged;
            }

            void OnPromptChanged(string prompt)
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    m_CurrentGenerateButton?.SetEnabled(false);
                else
                    m_CurrentGenerateButton?.SetEnabled(prompt.Length >= PromptOperator.MinimumPromptLength);
            }
        }
    }
}