using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Text = Unity.AppUI.UI.Text;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class NegativePromptOperator : IOperator
    {
        public string OperatorName  => "NegativePromptOperator";
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Negative Prompt";

        [SerializeField]
        OperatorData m_OperatorData;

        event Action OnDataUpdate;

        public NegativePromptOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "0.0.1",  new [] { "" }, false);
        }

        public bool IsSavable()
        {
            return true;
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("NegativePromptOperator.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList("muse-node");
            UI.name = "prompt-node";
            var text = new Text
            {
                text = Label,
                tooltip = TextContent.operatorNegativePromptTooltip,
                pickingMode = PickingMode.Position
            };

            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");

            UI.Add(text);

            var negativePromptField = new TextArea
            {
                name = "neg-prompt-inputfield"
            };

            var lastKeyReturn = false;
            negativePromptField.RegisterCallback((KeyDownEvent evt) =>
            {
                if ((evt.keyCode == KeyCode.Tab || evt.keyCode == KeyCode.None && evt.character == '\t') && !evt.shiftKey)
                {
                    evt.StopImmediatePropagation();
                    evt.PreventDefault();
                    if (evt.character != '\t')
                        negativePromptField.focusController.FocusNextInDirectionEx(negativePromptField, VisualElementFocusChangeDirection.right);
                    return;
                }

                if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                {
                    lastKeyReturn = true;
                    return;
                }

                if (evt.keyCode == KeyCode.None && lastKeyReturn)
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    m_OperatorData.settings[0] = negativePromptField.value;
                    model.GenerateButtonClicked();
                }

                lastKeyReturn = false;
            },TrickleDown.TrickleDown);

            negativePromptField.SetValueWithoutNotify(m_OperatorData.settings[0]);
            negativePromptField.RegisterValueChangedCallback((evt) =>
            {
                m_OperatorData.settings[0] = negativePromptField.value;
            });
            UI.Add(negativePromptField);

            OnDataUpdate += () =>
            {
                if (m_OperatorData.settings[0] != "")
                    negativePromptField.value = m_OperatorData.settings[0];
            };

            return UI;
        }

        public OperatorData GetOperatorData()
        {
            return m_OperatorData;
        }

        public void SetOperatorData(OperatorData data)
        {
            m_OperatorData.type = data.type;
            m_OperatorData.version = data.version;
            m_OperatorData.enabled = data.enabled;
            if(data.settings != null && data.settings.Length == 1)
                m_OperatorData.settings = data.settings;

            OnDataUpdate?.Invoke();
        }
        void SetSettings(IReadOnlyList<string> settings)
        {
            m_OperatorData.settings[0] = settings[0];
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
            var result = new NegativePromptOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }
        public void RegisterToEvents(Model model)
        { }

        public void UnregisterFromEvents(Model model)
        { }

        public string GetNegativePrompt()
        {
            return m_OperatorData.settings[0];
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            var text = GetNegativePrompt();
            if (string.IsNullOrEmpty(text))
                return null;

            var view = new Text { text = text };
            return view;
        }
    }
}
