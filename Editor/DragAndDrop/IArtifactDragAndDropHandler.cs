using UnityEngine;

namespace Unity.Muse.Common
{
    public interface IArtifactDragAndDropHandler
    {
        bool CanDropSceneView(GameObject dropUpon, Vector3 worldPosition);
        void HandleDropSceneView(GameObject dropUpon, Vector3 worldPosition);

        bool CanDropHierarchy(GameObject dropUpon);
        void HandleDropHierarchy(GameObject dropUpon);

        bool CanDropProject(string path);
        void HandleDropProject(string path);
    }
}
