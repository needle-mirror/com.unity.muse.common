using System;
using System.Collections.Generic;
using System.IO;
using Unity.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common.Editor
{
    internal class MuseEditor : EditorWindow
    {
        public MainUI mainUI;
        IPanel m_Panel;

        public Model CurrentModel;
        public Model DiscardModel;

        [SerializeField]
        string m_AssetPath;
        [SerializeField]
        string m_Mode;

        MuseShortcut m_SaveShortcut;
        Vector2 m_MinSizeWindow = new(500f, 350f);

        string defaultWindowTitle => TextContent.defaultAssetName(ModesFactory.GetModeData(m_Mode)?.title ?? "Muse Generator");
        
        bool m_Maximized;

        void OnEnable()
        {
            if (CurrentModel is not null)
                m_Mode = CurrentModel.CurrentMode;

            m_SaveShortcut = new MuseShortcut("Save Changes", SaveChanges, KeyCode.S, KeyModifier.Action, source: rootVisualElement);
            MuseShortcuts.AddShortcut(m_SaveShortcut);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            UpdateTitle();
            minSize = m_MinSizeWindow;
        }

        void OnDisable()
        {
            MuseShortcuts.RemoveShortcut(m_SaveShortcut);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (CurrentModel)
                CurrentModel.Dispose();
        }

        void CreateGUI()
        {
            if (!CurrentModel && !string.IsNullOrEmpty(m_AssetPath))
                CurrentModel = AssetDatabase.LoadAssetAtPath<Model>(m_AssetPath);

            if (!CurrentModel)
            {
                if (!string.IsNullOrEmpty(m_Mode))
                {
                    CurrentModel = CreateInstance<Model>();
                    CurrentModel.Initialize();
                    CurrentModel.ModeChanged(ModesFactory.GetModeIndexFromKey(m_Mode));
                }

                if (!CurrentModel)
                {
                    Close();
                    return;
                }
            }

            // If the model has been upgraded to a new version, we need to save it.
            if (CurrentModel.CheckForUpgrade())
            {
                EditorUtility.SetDirty(CurrentModel);
                AssetDatabase.SaveAssetIfDirty(CurrentModel);
            }

            m_Mode = CurrentModel.CurrentMode;
            m_Panel = rootVisualElement.panel;
            DiscardModel = Instantiate(CurrentModel);

            CurrentModel.OnEditorDragStart += EditorDragStart;
            CurrentModel.OnEditorMultiDragStart += EditorMultiDragStart;
            CurrentModel.OnExportArtifact += OnExportArtifact;
            CurrentModel.OnMultiExport += OnMultiExport;
            CurrentModel.OnModified += OnModelDataModified;
            CurrentModel.OnGenerateButtonClicked += OnGenerateButtonClicked;

            rootVisualElement.ProvideContext(CurrentModel);
            var mainui = ResourceManager.Load<VisualTreeAsset>(PackageResources.mainUITemplate);
            mainui.CloneTree(rootVisualElement);
            mainUI = rootVisualElement.Q<MainUI>();
            var museRoot = rootVisualElement.Q<Panel>("muse-root");
            museRoot.theme = EditorGUIUtility.isProSkin ? "dark" : "light";
            mainUI.AddToClassList("unity-editor");

            if (!string.IsNullOrEmpty(m_Mode))
            {
                int mode = ModesFactory.GetModeIndexFromKey(m_Mode);
                CurrentModel.ModeChanged(mode);
            }

            CurrentModel.OnModeChanged += OnModeChanged;
        }

        internal void OnModelDataModified()
        {
            if (Preferences.autoSave)
            {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(CurrentModel)))
                    hasUnsavedChanges = true;
                else
                    SaveChanges();
            }
            else
                hasUnsavedChanges = true;
        }

        void OnModeChanged(int obj)
        {
            m_Mode = ModesFactory.GetModeKeyFromIndex(obj);
        }

        void OnDestroy()
        {
            if (!CurrentModel)
                return;

            CurrentModel.Dispose();
            EditorUtility.SetDirty(CurrentModel);
            AssetDatabase.SaveAssetIfDirty(CurrentModel);
            ArtifactCache.Dispose();
            ReleaseTextures();

            CurrentModel.OnEditorDragStart -= EditorDragStart;
            CurrentModel.OnEditorMultiDragStart -= EditorMultiDragStart;
            CurrentModel.OnExportArtifact -= OnExportArtifact;
            CurrentModel.OnMultiExport -= OnMultiExport;
            CurrentModel.OnModified -= OnModelDataModified;
            CurrentModel.OnGenerateButtonClicked -= OnGenerateButtonClicked;
        }

        void ReleaseTextures()
        {
            ObjectUtils.Release(m_Panel);
            m_Panel = null;
        }

        static void EditorDragStart(string type, IList<Artifact> artifacts)
        {
            if (artifacts == null)
                return;

            foreach (var artifact in artifacts)
            {
                if (!ArtifactCache.IsInCache(artifact))
                    return;
            }

            var handler = DragAndDropFactory.CreateHandler(type, artifacts);
            if (handler == null)
                return;

            ArtifactDragAndDropHandler.StartDrag(handler, type);
        }

        static void EditorMultiDragStart(IList<(string name, IList<Artifact> artifacts)> items)
        {
            if (items == null || items.Count == 0)
                return;

            var handlers = new List<IArtifactDragAndDropHandler>();
            foreach (var item in items)
            {
                foreach (var artifact in item.artifacts)
                {
                    if (!ArtifactCache.IsInCache(artifact))
                        return;
                }

                var h = DragAndDropFactory.CreateHandler(item.name, item.artifacts);
                if (h != null)
                    handlers.Add(h);
            }

            var handler = DragAndDropFactory.CreateMultiHandler(handlers);
            ArtifactDragAndDropHandler.StartDrag(handler, "Multiple Elements");
        }

        public void SetContext(Model model)
        {
            m_AssetPath = AssetDatabase.GetAssetPath(model);
            CurrentModel = model;

            if (string.IsNullOrEmpty(m_AssetPath))
                hasUnsavedChanges = true;

            UpdateTitle();
        }

        static void OnExportArtifact(Artifact artifact)
        {
            if (artifact == null)
                return;

            var exporter = ArtifactExporterFactory.instance.GetExporterForType(artifact.GetType());
            if (exporter == null)
            {
                Debug.Log($"Couldn't find exporter for {artifact.GetType()} type.");
                return;
            }

            var extension = exporter.Extension;
            var artifactName = exporter.GetSaveFileName(artifact);
            var directory = Application.dataPath;
            var path = ExporterHelpers.GetUniquePath(directory, artifactName, extension);
            path = EditorUtility.SaveFilePanel("Save Artifact", Application.dataPath, Path.GetFileNameWithoutExtension(path), extension);
            if (string.IsNullOrEmpty(path))
                return;

            artifact.ExportToPath(path);
        }

        static void OnMultiExport(IList<ArtifactView> artifactViews)
        {
            if (artifactViews == null)
                return;

            if (artifactViews.Count == 1)
            {
                OnExportArtifact(artifactViews[0].Artifact);
                return;
            }

            var directory = EditorUtility.SaveFolderPanel("Save Generator Assets", Application.dataPath, "");
            if (string.IsNullOrEmpty(directory))
                return;

            foreach (var artifactView in artifactViews)
            {
                if (!artifactView.TrySaveAsset(directory))
                    ExporterHelpers.ExportToDirectory(artifactView.Artifact, directory);
            }
        }

        public override void SaveChanges()
        {
            if (string.IsNullOrEmpty(m_AssetPath))
            {
                var path = EditorModelAssetEditor.GetSavePath(CurrentModel, true);
                if (string.IsNullOrEmpty(path))
                {
                    base.SaveChanges();
                    return;
                }

                AssetDatabase.CreateAsset(CurrentModel, ExporterHelpers.GetPathRelativeToRoot(path));
            }

            EditorUtility.SetDirty(CurrentModel);
            AssetDatabase.SaveAssetIfDirty(CurrentModel);
            UpdateTitle();

            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            m_AssetPath = AssetDatabase.GetAssetPath(CurrentModel);

            if (!string.IsNullOrEmpty(m_AssetPath))
            {
                List<Artifact> deltaArtifacts;
                if (!string.IsNullOrEmpty(m_AssetPath))
                {
                    deltaArtifacts = CurrentModel.AssetsData.FindAll(x => !DiscardModel.AssetsData.Contains(x));

                    AssetDatabase.DeleteAsset(m_AssetPath);
                    AssetDatabase.CreateAsset(DiscardModel, m_AssetPath);
                }
                else
                {
                    deltaArtifacts = CurrentModel.AssetsData;
                }

                ArtifactCache.Delete(deltaArtifacts);
            }

            base.DiscardChanges();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                HandleUnsavedChanges();
        }

        void HandleUnsavedChanges()
        {
            if (hasUnsavedChanges)
            {
                if (EditorUtility.DisplayDialog(TextContent.savePopupTitle, TextContent.savePopupMessage, TextContent.yes, TextContent.no))
                    SaveChanges();
                else
                    DiscardChanges();
            }
        }

        internal void AssetMoved(string destinationPath)
        {
            m_AssetPath = destinationPath;

            UpdateTitle();
            SaveChanges();
        }

        void OnGenerateButtonClicked()
        {
            if (!string.IsNullOrEmpty(m_AssetPath))
                return;

            var path = EditorModelAssetEditor.GetSavePath(CurrentModel, false);
            path = path.Replace("\\", "/");
            m_AssetPath = ExporterHelpers.GetPathRelativeToRoot(path);
            AssetDatabase.CreateAsset(CurrentModel, m_AssetPath);
            CurrentModel = AssetDatabase.LoadAssetAtPath<Model>(m_AssetPath);

            SaveChanges();

            EditorGUIUtility.PingObject(CurrentModel);
        }

        void UpdateTitle()
        {
            if (string.IsNullOrEmpty(m_AssetPath))
                m_AssetPath = AssetDatabase.GetAssetPath(CurrentModel);

            var titleString = Path.GetFileNameWithoutExtension(m_AssetPath);
            if (string.IsNullOrEmpty(titleString))
                titleString = defaultWindowTitle;
            titleContent = new GUIContent(titleString, IconHelper.windowIcon);
        }

        private void OnGUI()
        {
            CheckMaximized();
        }

        void CheckMaximized()
        {
            if (maximized == m_Maximized) return;
            
            m_Maximized = maximized;
            mainUI?.UpdateView();
            if (!maximized)
            {
                rootVisualElement.ProvideContext(CurrentModel); 
            }
        }
    }
}
