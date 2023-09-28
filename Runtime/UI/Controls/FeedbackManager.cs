using System;
using Unity.Muse.Common.Utils;
using UnityEngine;

namespace Unity.Muse.Common
{
    [Serializable]
    internal class FeedbackManager : IModelData
    {
        public event Action OnModified;

        public event Action<Artifact> OnDislike;

        [SerializeField]
        SerializedHashSet<string> m_Disliked;

        public FeedbackManager()
        {
            m_Disliked = new SerializedHashSet<string>();
        }

        public void Dislike(Artifact artifact)
        {
            m_Disliked.Add(artifact.Guid);

            OnDislike?.Invoke(artifact);

            OnModified?.Invoke();
        }

        public void ToggleDislike(Artifact artifact)
        {
            var guid = artifact.Guid;
            if (m_Disliked.Contains(guid))
                m_Disliked.Remove(guid);
            else
                m_Disliked.Add(guid);

            OnDislike?.Invoke(artifact);

            OnModified?.Invoke();
        }

        public bool IsDisliked(Artifact artifact) => m_Disliked.Contains(artifact.Guid);
    }
}
