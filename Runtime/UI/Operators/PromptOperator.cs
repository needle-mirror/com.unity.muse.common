using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Text = Unity.AppUI.UI.Text;

namespace Unity.Muse.Common
{
    [Serializable]
    public class PromptOperator : IOperator
    {
        public const int MinimumPromptLength = 1;
        public string OperatorName  => "PromptOperator";
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Prompt";
        event Action OnDataUpdate;

        [SerializeField]
        OperatorData m_OperatorData;

        TextArea m_PromptField;
        bool m_LastKeyReturn;
        Model m_Model;

        public PromptOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "0.0.1",  new [] { "" }, false);
        }

        public bool IsPromptValid()
        {
            return m_OperatorData.settings[0].Length >= MinimumPromptLength;
        }
        public bool IsSavable()
        {
            return true;
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("PromptOperator.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            m_PromptField?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            m_PromptField?.UnregisterValueChangingCallback(ValueChangedCallback);

            m_Model = model;
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList("muse-node");
            UI.name = "prompt-node";
            var text = new Text
            {
                text = Label,
                pickingMode = PickingMode.Position,
                tooltip = TextContent.operatorPromptTooltip
            };
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");
            UI.Add(text);

            m_PromptField = new TextArea()
            {
                name = "prompt-inputfield",
                placeholder = TextContent.promptPlaceholder
            };

            m_LastKeyReturn = false;

            var ticks = DateTime.Now.Ticks;
            m_PromptField.userData = ticks;
            m_PromptField.RegisterCallback<KeyDownEvent>(OnKeyDown,TrickleDown.TrickleDown);

            m_PromptField.SetValueWithoutNotify(m_OperatorData.settings[0]);
            model.SetCurrentPrompt(m_PromptField.value);

            m_PromptField.RegisterValueChangingCallback(ValueChangedCallback);
            UI.Add(m_PromptField);

            OnDataUpdate -= OnOnDataUpdate;
            OnDataUpdate += OnOnDataUpdate;

            return UI;
        }

        void OnOnDataUpdate()
        {
            if (m_OperatorData.settings[0] != "")
            {
                m_PromptField.value = m_OperatorData.settings[0];
                m_Model.SetCurrentPrompt(m_PromptField.value);
            }
        }

        void ValueChangedCallback(ChangingEvent<string> evt)
        {
            m_OperatorData.settings[0] = m_PromptField.value;
            m_Model.SetCurrentPrompt(m_PromptField.value);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if ((evt.keyCode == KeyCode.Tab || evt.keyCode == KeyCode.None && evt.character == '\t') && !evt.shiftKey)
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
                if (evt.character != '\t') m_PromptField.focusController.FocusNextInDirectionEx(m_PromptField, VisualElementFocusChangeDirection.right);
                return;
            }

            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                m_LastKeyReturn = true;
                return;
            }

            if (evt.keyCode == KeyCode.None && m_LastKeyReturn)
            {
                evt.StopPropagation();
                evt.PreventDefault();
                m_OperatorData.settings[0] = m_PromptField.value;
                m_Model.GenerateButtonClicked();
            }

            m_LastKeyReturn = false;
        }
        /// <summary>
        /// Gets the operator data.
        /// </summary>
        /// <returns>The operator data.</returns>
        public OperatorData GetOperatorData()
        {
            return m_OperatorData;
        }

        /// <summary>
        /// Sets the operator data.
        /// </summary>
        /// <param name="data">The data to use.</param>
        public void SetOperatorData(OperatorData data)
        {
            m_OperatorData.enabled = data.enabled;
            if (data.settings == null || data.settings.Length < 1)
                return;
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

        /// <summary>
        /// Gets the enabled state of the operator.
        /// </summary>
        /// <returns>The enabled state.</returns>
        public bool Enabled()
        {
            return m_OperatorData.enabled;
        }

        /// <summary>
        /// Sets the enabled state of the operator.
        /// </summary>
        /// <param name="enable">The new state to set.</param>
        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        public bool Hidden { get; set; }

        /// <summary>
        /// Clones the operator.
        /// </summary>
        /// <returns>The cloned operator.</returns>
        public IOperator Clone()
        {
            var result = new PromptOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        /// <summary>
        /// Registers the operator to the model events.
        /// </summary>
        /// <param name="model"></param>
        public void RegisterToEvents(Model model)
        { }

        /// <summary>
        /// Unregisters the operator from the model events.
        /// </summary>
        /// <param name="model"></param>
        public void UnregisterFromEvents(Model model)
        { }

        /// <summary>
        /// Gets the prompt for this operator.
        /// </summary>
        /// <returns>The operator's prompt.</returns>
        public string GetPrompt()
        {
            return m_OperatorData.settings[0];
        }

        /// <summary>
        /// Sets the prompt text.
        /// </summary>
        /// <param name="promptText">Prompt text</param>
        public void SetPrompt(string promptText)
        {
            m_OperatorData.settings[0] = promptText;
            if(m_PromptField != null)
                m_PromptField.value = promptText;
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            var text = GetPrompt();
            if (string.IsNullOrEmpty(text))
                return null;

            var view = new Text { text = text };
            return view;
        }
    }
}
