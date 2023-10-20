using System;
using System.Linq;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    internal class MainUI : VisualElement, IControl
    {
        [Serializable]
        class UISize : IModelData
        {
            public event Action OnModified;

            [SerializeField]
            float m_NodeListWidth = k_NodeListMinWidth;

            [SerializeField]
            float m_NodeListRefineWidth = k_NodeListMinWidth;

            [SerializeField]
            float m_AssetListRefineWidth = k_AssetListMinWidth;

            public float nodeListWidth
            {
                get => m_NodeListWidth;
                set
                {
                    m_NodeListWidth = value;
                    OnModified?.Invoke();
                }
            }

            public float nodeListRefineWidth
            {
                get => m_NodeListRefineWidth;
                set
                {
                    m_NodeListRefineWidth = value;
                    OnModified?.Invoke();
                }
            }

            public float assetListRefineWidth
            {
                get => m_AssetListRefineWidth;
                set
                {
                    m_AssetListRefineWidth = value;
                    OnModified?.Invoke();
                }
            }
        }

        public new class UxmlFactory : UxmlFactory<MainUI, UxmlTraits> { }

        bool m_Initialized;
        Canvas m_Canvas;
        ControlToolbar m_ControlToolbar;
        NodesList m_NodesList;
        AssetsList m_AssetsList;
        ScopeToolbar m_ScopeToolbar;
        SignIn m_SignIn;

        int m_Mode;

        IUIMode m_UIMode;

        ActionButton m_CloseButton;

        const float k_AssetListLeftMargin = 10f;
        const float k_NodeListMinWidth = 300;
        const float k_AssetListMinWidth = 200;
        UISize m_UISize;

        Artifact m_ArtifactToBeRefined;


        public MainUI()
        {
            this.AddManipulator(new MuseShortcutHandler());
            this.RegisterContextChangedCallback<Model>(context =>
            {
                if (context.context != null)
                    SetModel(context.context);
            });
        }

        public void SetModel(Model model)
        {
            if(model == this.model)
                return;

            Init();
            this.model = model;
            this.model.OnLoggedInStateChanged += OnLoggedInStateChanged;
            this.model.OnModeChanged += OnModeChanged;
            model.ModeChanged(ModesFactory.GetModeIndexFromKey(model.CurrentMode));
            UpdateView();
        }

        void OnModeChanged(int newMode)
        {
            if (newMode == m_Mode)
                return;

            m_Mode = newMode;

            UpdateView();
        }

        public Canvas canvas => m_Canvas;
        public ControlToolbar controlToolbar => m_ControlToolbar;
        public NodesList nodesList => m_NodesList;
        public AssetsList assetsList => m_AssetsList;
        public ScopeToolbar scopeToolbar => m_ScopeToolbar;
        public Model model { get; private set; }

        public void UpdateView()
        {
            PreUpdateView();

            m_UIMode?.Deactivate();
            m_UIMode = UIModeFactory.GetUIMode(ModesFactory.GetModeKeyFromIndex(m_Mode));
            m_UIMode?.Activate(this);

            PostUpdateView();
        }


        void PreUpdateView()
        {
            UnregisterCallback<GeometryChangedEvent>(OnMainUIGeometryChanged);
            if(m_CloseButton != null)
                nodesList.Remove(m_CloseButton);
            assetsList.OnResized -= AssetListResized;
            nodesList.OnResized -= NodeListResized;

            RemoveModelListeners();
        }

        void PostUpdateView()
        {
            m_UISize = model.GetData<UISize>();
            AddModelListeners();

            assetsList.content.style.minWidth = k_AssetListMinWidth;

            MaximiseAssetList();
            assetsList.MarkDirtyRepaint();

            RegisterCallback<GeometryChangedEvent>(OnMainUIGeometryChanged);
            assetsList.OnResized += AssetListResized;
            nodesList.OnResized += NodeListResized;

            m_CloseButton = new() { name = "close", icon = "caret-left", label = "Generations", tooltip = TextContent.backButtonTooltip };
            m_CloseButton.clicked += OnCloseRefining;
            nodesList.Add(m_CloseButton);

            UpdateCloseButton();
            UpdateCanvasVisibility();
        }

        void Init()
        {
            if(m_Initialized) return;
            m_Canvas = this.Q<Canvas>();
            m_ControlToolbar = this.Q<ControlToolbar>();
            m_NodesList = this.Q<NodesList>();
            m_AssetsList = this.Q<AssetsList>();
            m_ScopeToolbar = this.Q<ScopeToolbar>();
            m_SignIn = this.Q<SignIn>();
            m_Initialized = true;
        }

        void AddModelListeners()
        {
			model.OnRefineArtifact += SelectArtifact;
            model.OnFinishRefineArtifact += OnFinishRefineArtifact;
            model.OnDispose += OnDispose;
            model.OnArtifactSelected += OnArtifactSelected;
            model.OnLoggedInStateChanged += OnLoggedInStateChanged;
            model.OnModeChanged += OnModeChanged;
            model.OnForbiddenAccess += OnForbiddenAccess;
            GenerativeAIBackend.OnForbiddenAccess += OnForbiddenAccess;
        }

        void RemoveModelListeners()
        {
			model.OnRefineArtifact -= SelectArtifact;
            model.OnFinishRefineArtifact -= OnFinishRefineArtifact;
            model.OnDispose -= OnDispose;
            model.OnArtifactSelected -= OnArtifactSelected;
            model.OnLoggedInStateChanged -= OnLoggedInStateChanged;
            model.OnModeChanged -= OnModeChanged;
            model.OnForbiddenAccess -= OnForbiddenAccess;
            GenerativeAIBackend.OnForbiddenAccess -= OnForbiddenAccess;
        }

        void OnLoggedInStateChanged(bool show)
        {
            m_SignIn.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            m_Canvas.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_ControlToolbar.style.display =show ? DisplayStyle.Flex : DisplayStyle.None;
            m_NodesList.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_AssetsList.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnMainUIGeometryChanged(GeometryChangedEvent evt)
        {
            if(!model.isRefineMode)
                MaximiseAssetList();
        }

        void UpdateCloseButton()
        {
            m_CloseButton.style.display = model.isRefineMode ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void UpdateCanvasVisibility()
        {
            canvas.style.display = model.isRefineMode ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefineModeAssetList()
        {
            assetsList.content.style.width = m_UISize.assetListRefineWidth;
            nodesList.style.width = m_UISize.nodeListRefineWidth;
            nodesList.draggerElement.RemoveFromClassList(Styles.hiddenUssClassName);
        }

        void MaximiseAssetList()
        {
            assetsList.content.style.maxWidth = resolvedStyle.width - 300 - k_AssetListLeftMargin;
            assetsList.content.style.width = resolvedStyle.width - m_UISize.nodeListWidth - k_AssetListLeftMargin;
            nodesList.style.width = m_UISize.nodeListWidth;
            nodesList.draggerElement.AddToClassList(Styles.hiddenUssClassName);
        }

        void OnCloseRefining()
        {
            model.FinishRefineArtifact();
        }

        void NodeListResized()
        {
            if (!model.isRefineMode)
            {
                m_UISize.nodeListWidth = nodesList.style.width.value.value;
                MaximiseAssetList();
            }
            else
            {
                m_UISize.nodeListRefineWidth = nodesList.style.width.value.value;
            }
        }

        void AssetListResized()
        {
            var assetListWidth = assetsList.content.style.width.value.value;
            if (model.isRefineMode)
            {
                m_UISize.assetListRefineWidth = assetListWidth;
            }
            else
            {
                m_UISize.nodeListWidth = Mathf.Min(nodesList.resolvedStyle.maxWidth.value,
                    resolvedStyle.width - k_AssetListLeftMargin - assetListWidth);
                MaximiseAssetList();
            }
        }

        void SelectArtifact(Artifact artifact)
        {
            model.CanvasRefineArtifact(artifact);
            RefineModeAssetList();
            UpdateCanvasVisibility();
            UpdateCloseButton();
            schedule.Execute(() => assetsList.ScrollToItem(artifact.Guid)).ExecuteLater(1L);
        }

        void OnFinishRefineArtifact(Artifact artifact)
        {
            MaximiseAssetList();
            UpdateCanvasVisibility();
            UpdateCloseButton();
        }

        void OnArtifactSelected(Artifact artifact)
        {
            if (artifact is null)
                return;

            if (model.isRefineMode && canvas.refinedArtifact?.Guid != artifact.Guid)
                SelectArtifact(artifact);
        }

        void OnDispose()
        {
            PreUpdateView();
        }

        const string k_BetaUrl = "https://create.unity.com/ai-beta";
        Modal m_BetaModal;


        void OnForbiddenAccess()
        {
            if (m_BetaModal != null)
            {
                m_BetaModal.Show();
                return;
            }

            var dialog = new AlertDialog
            {
                title = TextContent.signUpBetaDialogTitle,
                description = TextContent.signUpBetaDialogMessage,
                variant = AlertSemantic.Destructive
            };
            dialog.SetPrimaryAction(1, TextContent.signUpBeta, SignUpBeta);
            dialog.SetSecondaryAction(0, TextContent.cancel, OnCancelBeta);

            m_BetaModal = Modal.Build(this, dialog);
            m_BetaModal.Show();
        }

        void OnCancelBeta()
        {
            m_BetaModal = null;
        }

        void SignUpBeta()
        {
            m_BetaModal = null;
            Application.OpenURL($"{k_BetaUrl}");
        }
    }
}
