using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;
using Text = Unity.AppUI.UI.Text;

namespace Unity.Muse.Common
{
    [Serializable]
    public class ReferenceOperator : IOperator
    {
        public string OperatorName  => "ReferenceOperator";
        /// <summary>
        /// Human-readable label for the operator.
        /// </summary>
        public string Label => "Reference Image";

        event Action OnDataUpdate;

        [SerializeField]
        OperatorData m_OperatorData;


        public ReferenceOperator()
        {
            m_OperatorData = new OperatorData(OperatorName, "0.0.1",  new [] { "", "" }, false);
        }

        public bool IsSavable()
        {
            return true;
        }

        public VisualElement GetCanvasView()
        {
            Debug.Log("ReferenceOperator.GetCanvasView()");
            return new VisualElement();
        }

        public VisualElement GetOperatorView(Model model)
        {
            var UI = new ExVisualElement { passMask = ExVisualElement.Passes.Clear | ExVisualElement.Passes.OutsetShadows };
            UI.AddToClassList("muse-node");
            UI.name = "reference-node";

            var titleRow = new VisualElement();
            titleRow.AddToClassList("muse-title-row");
            titleRow.AddToClassList("bottom-gap");

            //title
            var text = new Text();
            text.text = Label;
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");
            titleRow.Add(text);

            var deleteButton = new Button(() => OnDeleteClicked(model)) { leadingIcon = "x", size = Size.S, tooltip = TextContent.removeReference };
            titleRow.Add(deleteButton);

            UI.Add(titleRow);


            var image = GetImageUI();

            // var textImage = new Text();
            // textImage.text = "No image selected";
            // textImage.AddToClassList("muse-ref-image__text");
            // m_Image.Add(textImage);

            UI.Add(image);

            var progress = new CircularProgress();
            progress.AddToClassList("muse-ref-image__progress");
            progress.StretchToParentSize();
            progress.style.position = Position.Relative;
            progress.style.alignSelf = Align.Center;
            image.style.justifyContent = Justify.Center;
            image.Add(progress);

            OnDataUpdate += () =>
            {
                if (m_OperatorData.settings[1] != "")
                {
                    image.image = GetTexture();
                    progress.style.display = DisplayStyle.None;
                }
            };

            OnDataUpdate?.Invoke();

            return UI;
        }

        void OnDeleteClicked(Model model)
        {
            model.RemoveOperators(this);
        }

        Image GetImageUI()
        {
            var texture = GetTexture();
            if (texture is null)
                return null;

            var image = new Image();
            image.AddToClassList("muse-ref-image");
            image.name = "muse-reference-image-field";
            image.image = texture;

            return image;
        }

        Texture GetTexture()
        {
            if (m_OperatorData.settings[1] == "")
                return null;

            var texture = TextureUtils.Create();
            texture.LoadImage(Convert.FromBase64String(m_OperatorData.settings[1]));
            return texture;
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
            var result = new ReferenceOperator();
            var operatorData = new OperatorData();
            operatorData.FromJson(GetOperatorData().ToJson());
            result.SetOperatorData(operatorData);
            return result;
        }
        public void RegisterToEvents(Model model)
        { }

        public void UnregisterFromEvents(Model model)
        { }


        public void SetReferenceImage(Artifact artifact)
        {
            m_OperatorData.enabled = true;
            m_OperatorData.settings[0] = artifact.Guid;

            //Todo, we should implement a get preview in the artifact class
            if (artifact is Artifact<Texture2D> textureArtifact)
            {
                textureArtifact.GetArtifact(OnArtifactReceived, true);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void OnArtifactReceived(Texture2D artifactInstance, byte[] rawData, string errorMessage)
        {
            m_OperatorData.settings[1] = Convert.ToBase64String(artifactInstance.EncodeToPNG());
            OnDataUpdate?.Invoke();
        }

        /// <summary>
        /// Get the settings view for this operator.
        /// </summary>
        /// <returns> UI for the operator. Set to Null if the operator should not be displayed in the settings view. Disable the returned VisualElement if you want it to be displayed but not usable.</returns>
        public VisualElement GetSettingsView()
        {
            return GetImageUI();
        }
    }
}
