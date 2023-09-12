using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Muse.Common
{
    internal class BookmarkManager : IModelData, ISerializationCallbackReceiver
    {
        public event Action OnModified;

        [SerializeField]
        string[] m_Bookmarks = Array.Empty<string>();

        HashSet<string> m_BookmarkedArtifacts;

        [SerializeField]
        bool m_IsFilterEnabled;

        public bool isFilterEnabled => m_IsFilterEnabled;

        public BookmarkManager()
        {
            InitializeBookmarks();
        }

        public bool IsBookmarked(Artifact artifact) => artifact != null && m_BookmarkedArtifacts.Contains(artifact.Guid);

        public void Bookmark(Artifact artifact, bool bookmark = true)
        {
            if (artifact == null)
                return;

            var guid = artifact.Guid;
            if (bookmark)
                m_BookmarkedArtifacts.Add(guid);
            else
                m_BookmarkedArtifacts.Remove(guid);

            OnModified?.Invoke();
        }

        public void SetFilter(bool enabled)
        {
            m_IsFilterEnabled = enabled;

            OnModified?.Invoke();
        }

        void InitializeBookmarks()
        {
            m_BookmarkedArtifacts = new HashSet<string>();
        }

        public void OnBeforeSerialize() => m_Bookmarks = m_BookmarkedArtifacts.ToArray();
        public void OnAfterDeserialize() => m_BookmarkedArtifacts = new HashSet<string>(m_Bookmarks);
    }
}
