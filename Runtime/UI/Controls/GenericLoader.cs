using System;
using UnityEngine;
using UnityEngine.UIElements;
using AppUI = Unity.AppUI.UI;

namespace Unity.Muse.Common
{
    public class GenericLoader : VisualElement
    {
        const string k_StyleSheetPath = "uss/GradientLoaderStyle";

        const string k_GradientLoaderClass = "genai-loader-gradient";
        const string k_LoadingStateClass = "genai-loader-state-loading";
        const string k_NoneStateClass = "genai-loader-state-none";

        readonly AppUI.UI.CircularProgress m_Progress;
        readonly AppUI.UI.Text m_ProgressLabel;
        
        internal State LoadingState { get; private set; }
        internal event Action<State> OnLoadingStateChanged; 

        public new class UxmlFactory : UxmlFactory<GenericLoader, UxmlTraits>
        {
        }

        public GenericLoader()
            : this(State.Loading)
        {
        }

        public GenericLoader(State state, bool withProgress = false)
        {
            m_Progress = new AppUI.UI.CircularProgress();
            if (withProgress)
            {
                m_Progress.variant = AppUI.UI.Progress.Variant.Determinate;

                m_ProgressLabel = new AppUI.UI.Text
                {
                    size = AppUI.UI.TextSize.XS
                };

                m_Progress.Add(m_ProgressLabel);
            }

            Add(m_Progress);

            InitializeStyle();

            SetState(state);
        }


        void InitializeStyle()
        {
            var styleSheet = Resources.Load<StyleSheet>(k_StyleSheetPath);
            Debug.Assert(k_StyleSheetPath != null, $"Could not find stylesheet at path: {k_StyleSheetPath}");

            styleSheets.Add(styleSheet);

            AddToClassList(k_GradientLoaderClass);
        }

        public void SetState(State state)
        {
            RemoveFromClassList(k_NoneStateClass);
            RemoveFromClassList(k_LoadingStateClass);

            switch (state)
            {
                case State.None:
                    AddToClassList(k_NoneStateClass);
                    break;
                case State.Loading:
                    AddToClassList(k_LoadingStateClass);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
            
            var preChangedState = LoadingState;
            
            LoadingState = state;
            
            if (preChangedState != state)
                OnLoadingStateChanged?.Invoke(state);
        }

        public void SetProgress(float progress)
        {
            progress /= 100f;
            m_Progress.value = progress;

            if(m_ProgressLabel != null)
                m_ProgressLabel.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        public enum State
        {
            None,
            Loading
        }
    }
}