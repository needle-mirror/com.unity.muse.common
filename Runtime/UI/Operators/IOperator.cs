using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public interface IOperator
    {
        public string OperatorName { get; }
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label { get; }
        public bool Enabled();
        public void Enable(bool enable);
        public VisualElement GetCanvasView();
        public VisualElement GetOperatorView(Model model);
        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView();

        public OperatorData GetOperatorData();

        public void SetOperatorData(OperatorData data);

        public IOperator Clone();

        public void RegisterToEvents(Model model);
        public void UnregisterFromEvents(Model model);
        public bool IsSavable();
    }
}
