using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public class PreviewImage: LoadableImage
    {
        Artifact m_CurrentArtifact;
        public event Action<Artifact> OnSelected;
        public event Action OnLoadedPreview;
        public event Action OnDelete;

        public PreviewImage()
        {
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            GenericLoader.OnRetry += OnRetry;
            GenericLoader.OnDelete += OnDeleteClicked;
        }

        private void OnRetry()
        {
            if (!m_CurrentArtifact.IsValid())
            {
                m_CurrentArtifact.RetryGenerate(this.GetContext<Model>());
            }
            
            SetAsset(m_CurrentArtifact);
        }

        private void OnDeleteClicked()
        {
            OnDelete?.Invoke();
        }

        public void SetAsset(Artifact artifact)
        {
            m_CurrentArtifact = artifact;

            if (artifact.IsValid())
            {
                OnLoading();
                artifact.GetPreview(OnArtifactReceived, true);
            }
            else
            {
                OnLoading();
                artifact.OnGenerationDone += OnArtifactGenerationDone;
            }
        }

        void OnArtifactGenerationDone(Artifact artifact, string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                SetAsset(artifact);
                return;
            }
            
            OnError("Generation failed.");
        }

        void OnArtifactReceived(Texture2D artifactInstance, byte[] rawData, string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                OnLoaded(artifactInstance);
                OnLoadedPreview?.Invoke(); 
                return;
            }
            
            OnError("Failed to retrieve artifact.");
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if(evt.clickCount == 2)
            {
                OnSelected?.Invoke(m_CurrentArtifact);
            }
        }
    }
}