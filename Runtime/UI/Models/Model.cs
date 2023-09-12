using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Core;
using Unity.Muse.Common.Analytics;
using UnityEngine;

namespace Unity.Muse.Common
{
    /// <summary>
    /// Custom model data.
    /// </summary>
    public interface IModelData
    {
        /// <summary>
        /// Event raised when the data was modified.
        /// </summary>
        event Action OnModified;
    }

    delegate IEnumerable<IOperator> SetOperatorDefault(IEnumerable<IOperator> currentOperators);

    [Serializable]
    [Icon(IconHelper.assetIconPath)]
    public class Model : ScriptableObject, IContext
    {
        /// <summary>
        /// Event raised when the model was modified.
        /// </summary>
        public event Action OnModified;
        public event Action<Artifact> OnArtifactAdded;
        public event Action<Artifact[]> OnArtifactRemoved;
        public event Action<string, IList<Artifact>> OnEditorDragStart;
        public event Action<IList<(string name, IList<Artifact> artifacts)>> OnEditorMultiDragStart;
        public event Action<IEnumerable<Artifact>, Vector3> OnItemsDropped;
        public event Action<Artifact> OnArtifactSelected;
        public event Action<ICanvasTool> OnActiveToolChanged;
        public event Action<Texture2D> OnMaskPaintDone;
        public event Action<string> OnCurrentPromptChanged;
        public event Action<IEnumerable<IOperator>, bool> OnOperatorUpdated;
        /// <summary>
        /// Called when removing an operator
        /// </summary>
        public event Action<IEnumerable<IOperator>> OnOperatorRemoved;
        public event Action OnGenerateButtonClicked;
        public event Action<Artifact> OnExportArtifact;
        public event Action<IList<ArtifactView>> OnMultiExport;
        public event Action OnDeselectAll;
        public event Action<bool> OnSetMaskSeamless;
        public event Action<int> OnModeChanged;
        public event Action<bool> OnLoggedInStateChanged;
        public event Action<Artifact> OnFrameArtifactRequested;
        public event Action OnDispose;
        public event Action<Artifact> OnRefineArtifact;
        public event Action<Artifact> OnCanvasRefineArtifact;
        public event Action<Artifact> OnFinishRefineArtifact;
        public event Action<Artifact> OnSetReferenceOperator;
        internal event SetOperatorDefault OnSetOperatorDefaults;
        internal event Action OnForbiddenAccess;

        void OnEnable()
        {
            if (string.IsNullOrEmpty(currentMode))
            {
                var mode = PlayerPrefs.GetInt("Muse.Mode", 0);
                currentMode = ModesFactory.GetModeKeyFromIndex(mode);
            }

            foreach (var modelData in m_Data)
                modelData.OnModified += () => OnModified?.Invoke();
        }

        public List<Artifact> AssetsData
        {
            get => isRefineMode ? refinedArtifact.history : assetsData;
            private set => assetsData = value;
        }

        public List<Artifact> DraggedArtifacts { get; private set; } = new List<Artifact>();

        public string CurrentMode
        {
            get => currentMode;
            private set => currentMode = value;
        }

        public ICanvasTool ActiveTool { get; private set; }

        public void DeselectAll()
        {
            OnDeselectAll?.Invoke();
        }

        [SerializeReference]
        List<Artifact> assetsData;

        [SerializeField]
        string currentMode;

        [SerializeReference]
        List<IModelData> m_Data = new List<IModelData>();

        [SerializeReference]
        Artifact preRefinedArtifact;        // The selected artifact prior to entering refine mode

        [SerializeReference]
        Artifact refinedArtifact;

        [SerializeReference]
        Artifact selectedArtifact;

        [SerializeReference]
        List<IOperator> m_Operators;

        [SerializeReference]
        List<IOperator> m_PreRefineOperators;

        /// <summary>
        /// Get the list of operators currently being used
        /// </summary>
        public List<IOperator> CurrentOperators => currentOperators.ToList();

        /// <summary>
        /// The artifact currently being refined.
        /// </summary>
        public Artifact RefinedArtifact => refinedArtifact;

        public Artifact SelectedArtifact => selectedArtifact;

        public bool isRefineMode => refinedArtifact != null;

        public T GetData<T>() where T: IModelData, new()
        {
            var data = m_Data.Find(d => d is T);
            if (data == null)
            {
                data = new T();
                m_Data.Add(data);

                data.OnModified += () => OnModified?.Invoke();
            }

            return (T)data;
        }

        public void DeleteData<T>()
        {
            var index = m_Data.FindIndex(d => d is T);
            if(index > 0)
                m_Data.RemoveAt(index);
        }

        public void GenerateButtonClicked()
        {
            OnGenerateButtonClicked?.Invoke();
        }

        List<IOperator> modeDefaultOperators => ModesFactory.GetMode(currentMode).Select(op => op.Clone()).ToList();
        IEnumerable<IOperator> currentOperators => m_Operators ??= modeDefaultOperators;

        /// <summary>
        /// Set or replace operators in the nodes list.
        /// </summary>
        /// <param name="operators">Operators to update.</param>
        /// <param name="set">Set or update the operators</param>
        public void UpdateOperators(IEnumerable<IOperator> operators, bool set = false)
        {
            if (set)
            {
                m_Operators = operators == null ? currentOperators.ToList() : operators.ToList();
            }
            else
            {
                foreach (var op in operators)
                {
                    var index = m_Operators.FindIndex(o => o.GetType() == op.GetType());
                    if (index >= 0)
                        m_Operators[index] = op;
                    else
                        m_Operators.Add(op);
                }
            }

            OnOperatorUpdated?.Invoke(m_Operators, set);
        }

        /// <summary>
        /// Set or replace operators in the nodes list.
        /// </summary>
        /// <param name="operators">Operators to update.</param>
        public void UpdateOperators(params IOperator[] operators)
        {
            UpdateOperators(operators, false);
        }

        /// <summary>
        /// Remove operators in the nodes list.
        /// </summary>
        /// <param name="operators">Operators to remove.</param>
        public void RemoveOperators(params IOperator[] operators)
        {
            m_Operators = m_Operators.Where(op => !operators.Contains(op)).ToList();
            OnOperatorRemoved?.Invoke(operators);
        }

        /// <summary>
        /// Sets the selected artifact.
        /// </summary>
        /// <param name="artifact">The artifact to select.</param>
        /// <param name="force">Force the selection change even if the artifact is the same as current selection.</param>
        public void ArtifactSelected(Artifact artifact, bool force = false)
        {
            if (SelectedArtifact == artifact && !force)
                return;

            SelectedArtifact?.UnregisterFromEvents(this);
            selectedArtifact = artifact;
            SelectedArtifact?.RegisterToEvents(this);
            OnArtifactSelected?.Invoke(SelectedArtifact);

            SetOperatorDefaults();
        }

        public void AddAsset(Artifact artifact)
        {
            AssetsData ??= new List<Artifact>();

            if (!string.IsNullOrEmpty(artifact.Guid) && AssetsData.Contains(artifact)) return;

            AssetsData.Add(artifact);

            OnArtifactAdded?.Invoke(artifact);
            OnModified?.Invoke();
        }

        bool IsArtifactUnused(Artifact artifact)
        {
            return !assetsData.Any(assetsArtifact =>
                !ReferenceEquals(assetsArtifact, artifact)        // Don't check the artifact itself
                && assetsArtifact.history.Contains(artifact));    // Check if the artifact is present in any history
        }

        /// <summary>
        /// Remove give artifacts from this model.
        /// </summary>
        /// <param name="artifacts">Artifacts to remove from model.</param>
        public void RemoveAssets(params Artifact[] artifacts)
        {
            List<Artifact> removeFromCache = new();
            Artifact selected = null;
            Artifact setAsThumbnail = null;
            int setAsThumbnailIndex = RefinedArtifactGenerationsIndex;  // Keep the generations index we might be replacing
            var finishRefine = false;

            // Remove from cache (will only actually be removed from cache if unused elsewhere
            if (isRefineMode)
                removeFromCache.AddRange(artifacts);
            else
                removeFromCache.AddRange(artifacts.SelectMany(a => a.history)); // Clear history when removing from generations

            // Delete from generation or refinement list (AssetsData)
            foreach (var artifact in artifacts)
            {
                // Check by artifact reference rather then guid otherwise we might delete multiple top level items that have
                // previously been branched off.
                AssetsData.RemoveAll(a => ReferenceEquals(a, artifact));
            }

            // After everything has been deleted, check if we need to set a new thumbnail or select a new item
            // Otherwise we might select an item that will end up being deleted.
            if (isRefineMode && artifacts.Contains(refinedArtifact))
            {
                // If we're deleting the root artifact, we need to set a new root artifact that will appear in the Generations list
                if (refinedArtifact.history.Count <= 1)
                {
                    assetsData.RemoveAll(a => ReferenceEquals(a, refinedArtifact));
                    finishRefine = true;
                }
                else
                {
                    setAsThumbnail = AssetsData.Last();
                }
            }

            if (artifacts.Contains(SelectedArtifact) && !finishRefine && AssetsData.Count > 0)
                selected = AssetsData.Last();

            ArtifactCache.Delete(removeFromCache.Where(IsArtifactUnused));

            OnArtifactRemoved?.Invoke(artifacts.ToArray());
            OnModified?.Invoke();

            // Set new state
            if (finishRefine)
                FinishRefineArtifact();
            else if (setAsThumbnail != null)
                SetAsThumbnail(setAsThumbnail, setAsThumbnailIndex);
            else
                ArtifactSelected(selected);
        }

        public void EditorStartDrag(string type, IList<Artifact> artifact)
        {
            OnEditorDragStart?.Invoke(type, artifact);
        }

        public void EditorStartMultiDrag(IList<(string name, IList<Artifact> artifacts)> artifactsList)
        {
            OnEditorMultiDragStart?.Invoke(artifactsList);
        }

        public void DragStart(IEnumerable<Artifact> artifacts)
        {
            DraggedArtifacts.Clear();
            DraggedArtifacts.AddRange(artifacts);
        }

        public void DragEnd()
        {
            DraggedArtifacts.Clear();
        }

        public void DragEnd(IEnumerable<Artifact> artifacts)
        {
            foreach (var artifact in artifacts)
            {
                DraggedArtifacts.Remove(artifact);
            }
        }

        public void DropItems(IEnumerable<Artifact> artifacts, Vector3 position)
        {
            OnItemsDropped?.Invoke(artifacts, position);
        }

        public void SetActiveTool(ICanvasTool tool)
        {
            ActiveTool = tool;
            OnActiveToolChanged?.Invoke(ActiveTool);
        }

        public void MaskPaintDone(Texture2D texture)
        {
            OnMaskPaintDone?.Invoke(texture);
        }

        public void SetMaskSeamless(bool seamless)
        {
            OnSetMaskSeamless?.Invoke(seamless);
        }

        public void ExportArtifact(Artifact artifact)
        {
            OnExportArtifact?.Invoke(artifact);
        }

        public void MultiExport(IList<ArtifactView> artifactViews)
        {
            OnMultiExport?.Invoke(artifactViews);
        }

        public void ModeChanged(int mode)
        {
            if(mode < 0 )
                return;
            currentMode = ModesFactory.GetModeKeyFromIndex(mode);
            OnModeChanged?.Invoke(mode);
        }

        public void LoggedInStateChanged(bool loggedIn)
        {
            OnLoggedInStateChanged?.Invoke(loggedIn);
        }

        public void RequestFrameArtifact(Artifact artifact)
        {
            OnFrameArtifactRequested?.Invoke(artifact);
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }

        public void RefineArtifact(Artifact artifact)
        {
            if (artifact?.Guid == refinedArtifact?.Guid)
                return;

            preRefinedArtifact = selectedArtifact;
            m_PreRefineOperators = m_Operators.ToList();
            refinedArtifact = artifact;
            m_Operators = modeDefaultOperators.ToList();

            ArtifactSelected(refinedArtifact);

            OnRefineArtifact?.Invoke(artifact);
        }

        public void FinishRefineArtifact()
        {
            if (refinedArtifact is null)
                return;

            var previousArtifact = refinedArtifact;
            refinedArtifact = null;
            m_Operators = m_PreRefineOperators.ToList();

            ArtifactSelected(preRefinedArtifact);

            preRefinedArtifact = null;
            m_PreRefineOperators = null;

            OnFinishRefineArtifact?.Invoke(previousArtifact);
        }

        public void CanvasRefineArtifact(Artifact artifact)
        {
            OnCanvasRefineArtifact?.Invoke(artifact);
        }

        public void SetReferenceOperator(Artifact artifact)
        {
            OnSetReferenceOperator?.Invoke(artifact);
        }

        public void SetCurrentPrompt(string prompt)
        {
            OnCurrentPromptChanged?.Invoke(prompt);
        }

        IEnumerable<IOperator> m_StaticOperators;

        internal IEnumerable<IOperator> SetOperatorDefaults()
        {
            // Cloning the operators as we can not modify a generated artifact's operators.
            var operators = currentOperators;

            // Keep static operators for UX consistency (such as generate so that the user does not lose its settings in and out of refine mode)
            if (m_StaticOperators is null || !m_StaticOperators.Any())
                m_StaticOperators = operators.Where(o => o is GenerateOperator);

            foreach (var staticOperator in m_StaticOperators ??  Array.Empty<IOperator>())
            {
                operators = currentOperators?.Select(o =>
                    o.GetType() == staticOperator.GetType() ? staticOperator : o).ToList();
            }

            operators = OnSetOperatorDefaults?.Invoke(operators) ?? operators;
            UpdateOperators(operators?.ToArray(), true);

            return operators;
        }

        int RefinedArtifactGenerationsIndex => assetsData.FindIndex(a => a == refinedArtifact);

        /// <summary>
        /// Set the thumbnail of the generations list to the given artifact.
        /// </summary>
        /// <param name="artifact">New artifact to be used in the generations list.</param>
        /// <param name="indexToReplace">Index in the generations list to be replaced.</param>
        void SetAsThumbnailInternal(Artifact artifact, int indexToReplace)
        {
            // Swap with the previous parent
            artifact.history = refinedArtifact.history.ToList();
            refinedArtifact.history.Clear();
            assetsData[indexToReplace] = artifact;
        }

        /// <summary>
        /// Set the thumbnail of the generations list to the given artifact.
        /// </summary>
        /// <param name="artifact">Artifact to set.</param>
        /// <param name="indexToReplace">The index in the generations list to replace. (optional)</param>
        public void SetAsThumbnail(Artifact artifact, int? indexToReplace = null)
        {
            indexToReplace ??= RefinedArtifactGenerationsIndex;

            SetAsThumbnailInternal(artifact, indexToReplace.Value);
            RefineArtifact(artifact);
        }

        /// <summary>
        /// Branch off the given artifact and add it to the generations list as a new generations.
        /// </summary>
        /// <param name="artifact">The artifact to branch off.</param>
        public void Branch(Artifact artifact)
        {
            var clone = artifact.Clone(currentMode);
            assetsData.Add(clone);
            clone.history.Clear();
            clone.history = new() {clone};
            RefineArtifact(clone);
        }

        internal static Action<string, object, int> OnAnalytics;

        internal static void SendAnalytics(IAnalyticsData data)
        {
            SendAnalytics(data.EventName, data, data.Version);
        }

        internal static void SendAnalytics(string eventName, object parameters, int version)
        {
            OnAnalytics?.Invoke(eventName, parameters, version);
        }

        internal void ForbiddenAccess()
        {
            OnForbiddenAccess?.Invoke();
        }
    }
}

