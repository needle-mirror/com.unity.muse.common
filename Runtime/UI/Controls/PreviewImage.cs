using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public class PreviewImage: LoadableImage
    {
        Artifact m_CurrentArtifact;
        public event Action<Artifact> OnSelected;
        public event Action OnLoadedPreview;

        public PreviewImage()
        {
            RegisterCallback<PointerDownEvent>(OnPointerDown);
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
                artifact.OnGenerationDone += () => SetAsset(artifact);
            }
        }

        void OnArtifactReceived(Texture2D artifactInstance, byte[] rawData, string errorMessage)
        {
            OnLoaded(artifactInstance);
            OnLoadedPreview?.Invoke();
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