using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Text = Unity.AppUI.UI.Text;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class MaskOperator : IOperator
    {
        public string OperatorName  => "MaskOperator";
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Mask Image";

        [SerializeField]
        OperatorData m_OperatorData;

        event Action OnDataUpdate;
        public Texture2D GetMask()
        {
            var b64String = m_OperatorData.settings[0];
            var bytes = Convert.FromBase64String(b64String);
            var maskTexture = TextureUtils.Create();
            maskTexture.LoadImage(bytes);
            return maskTexture;
        }
        public MaskOperator()
        {
            m_OperatorData = new OperatorData("MaskOperator", "0.0.1", new[]{"","True"}, false);
        }
        public bool IsSavable()
        {
            return true;
        }
        public bool GetSeamless()
        {
            return bool.Parse(m_OperatorData.settings[1]);
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("MaskOperator.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList("muse-node");
            UI.AddToClassList("appui-elevation-8");
            UI.name = "mask-node";

            //title
            var text = new Text();
            text.text = Label;
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");
            UI.Add(text);

            //Probably need to create a new class for Mask
            var image = GetImageUI();

            var imageText = new Text();
            imageText.text = "No Mask";
            imageText.AddToClassList("muse-ref-image__text");
            if (m_OperatorData.settings[0] != "")
                imageText.text = "";

            image.Add(imageText);
            UI.Add(image);

            var inputLabel = new InputLabel("Seamless inpainting");
            inputLabel.inputAlignment = Align.FlexEnd;
            UI.Add(inputLabel);

            m_OperatorData.settings[1] = "True";

            OnDataUpdate += () =>
            {
                if (m_OperatorData.settings[0] != "")
                {
                    image.image = GetMask();
                    imageText.text = "";
                }
            };

            return UI;
        }

        Image GetImageUI()
        {
            var image = new Image();
            image.AddToClassList("muse-ref-image");
            image.name = "muse-reference-image-field";

            if (m_OperatorData.settings[0] != "")
                image.image = GetMask();

            image.AddToClassList("bottom-gap");

            return image;
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
             return new[] { m_OperatorData.settings[0], m_OperatorData.settings[1] };
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
            var result = new MaskOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }

        static TextureFormat FindEquivalentTextureFormat(GraphicsFormat graphicsFormat)
        {
            // Perform mapping to find equivalent TextureFormat
            return graphicsFormat switch
            {
                GraphicsFormat.R8_UNorm => TextureFormat.R8,
                GraphicsFormat.R8G8B8A8_UNorm => TextureFormat.RGBA32,
                _ => TextureFormat.RGBA32
            };
        }

        void OnMaskPaintDone(Texture2D texture)
        {
            m_OperatorData.settings[0] = Convert.ToBase64String(texture.EncodeToPNG());
            OnDataUpdate?.Invoke();
        }

        public void RegisterToEvents(Model model)
        {
            if (!model.CurrentOperators.Contains(this))
                return;         // Only register to paint event for the current operator and not the selected artifact's operator

            model.OnMaskPaintDone += OnMaskPaintDone;
        }

        public void UnregisterFromEvents(Model model)
        {
            model.OnMaskPaintDone -= OnMaskPaintDone;
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            if (string.IsNullOrEmpty(m_OperatorData.settings[0]))
                return null;
            return GetImageUI();
        }
    }
}
