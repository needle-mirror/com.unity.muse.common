using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Text = Unity.AppUI.UI.Text;

namespace Unity.Muse.Common
{
    [Serializable]
    public class UpscaleOperator : IOperator
    {
        public string OperatorName  => "UpscaleOperator";
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Upscale Image";

        event Action OnDataUpdate;

        [SerializeField]
        OperatorData m_OperatorData;

        public UpscaleOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "0.0.1",  new [] { "" }, false);
        }

        public bool IsSavable()
        {
            return true;
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("UpscaleOperator.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList("muse-node");
            UI.AddToClassList("appui-elevation-8");
            UI.name = "upscale-node";

            //title
            var text = new Text();
            text.text = Label;
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");
            UI.Add(text);

            //m_Image = new Image();
            // m_Image.AddToClassList("muse-ref-image");
            // m_Image.name = "muse-upscale-image-field";
            //
            // UI.Add(m_Image);
            return UI;
        }

        public OperatorData GetOperatorData()
        {
            return m_OperatorData;
        }

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

        public bool Enabled()
        {
            return m_OperatorData.enabled;
        }

        public void Enable(bool enable)
        {
            m_OperatorData.enabled = enable;
        }

        public bool Hidden { get; set; }

        public void SetParentGuid(string guid)
        {
            m_OperatorData.settings[0] = guid;
            OnDataUpdate?.Invoke();
        }
        public IOperator Clone()
        {
            var result = new UpscaleOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }
        public void RegisterToEvents(Model model)
        { }

        public void UnregisterFromEvents(Model model)
        { }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            VisualElement result = null;
            if (Enabled())
            {
                result = new VisualElement();
                result.SetEnabled(false);           // Don't allow "use" on upscale operator
            }
            return result;
        }
    }
}
