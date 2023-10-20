using System;
using UnityEngine.UIElements;
using Unity.AppUI.UI;
using UnityEngine;

namespace Unity.Muse.Common
{
    /// <summary>
    /// View Displaying an error message and a retry button
    /// </summary>
    internal class ErrorView : VisualElement
    {
        /// <summary>
        /// Error Visual Element
        /// </summary>
        Text m_ErrorText;

        /// <summary>
        /// Retry Button
        /// </summary>
        ActionButton m_RetryButton;

        /// <summary>
        /// Delete Button
        /// </summary>
        IconButton m_DeleteButton;

        /// <summary>
        /// Current Error string
        /// </summary>
        string m_Error;

        /// <summary>
        /// Style sheet path
        /// </summary>
        const string k_StyleSheetPath = "uss/ErrorView";

        /// <summary>
        /// Error Text Class
        /// </summary>
        const string k_ErrorTextClass = "muse-errorview--text";

        /// <summary>
        /// Retry Button Class
        /// </summary>
        const string k_RetryButtonClass = "muse-errorview--retry-button";

        /// <summary>
        /// Delete Button Class
        /// </summary>
        const string k_DeleteButtonClass = "muse-errorview--delete-button";

        /// <summary>
        /// Delete Button Parent Class
        /// </summary>
        const string k_DeleteButtonParentClass = "muse-errorview--delete-button-parent";


        /// <summary>
        /// On Retry event
        /// </summary>
        public event Action OnRetry;

        /// <summary>
        /// On Delete event
        /// </summary>
        public event Action OnDelete;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ErrorView()
        {
            InitializeView();
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        /// <summary>
        /// Adding the different views to the ErrorView
        /// </summary>
        void InitializeView()
        {
            var styleSheet = Resources.Load<StyleSheet>(k_StyleSheetPath);
            styleSheets.Add(styleSheet);

            var errorText = new Text();
            errorText.AddToClassList(k_ErrorTextClass);
            Add(errorText);

            var retryButton = new ActionButton();
            retryButton.AddToClassList(k_RetryButtonClass);
            retryButton.label = "Retry";
            Add(retryButton);

            var deleteButtonParent = new VisualElement();
            deleteButtonParent.AddToClassList(k_DeleteButtonParentClass);
            deleteButtonParent.pickingMode = PickingMode.Ignore;
            Add(deleteButtonParent);
            
            var deleteButton = new IconButton();
            deleteButton.AddToClassList(k_DeleteButtonClass);
            deleteButton.icon = "delete--regular";
            deleteButtonParent.Add(deleteButton);
        }

        /// <summary>
        /// event handler for the AttachToPanelEvent
        /// </summary>
        /// <param name="evt">On attach event</param>
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_ErrorText = this.Q<Text>(classes: k_ErrorTextClass);
            m_RetryButton = this.Q<ActionButton>(classes: k_RetryButtonClass);
            m_DeleteButton = this.Q<IconButton>(classes: k_DeleteButtonClass);

            m_RetryButton.clickable.clicked += OnRetry;
            m_DeleteButton.clickable.clicked += OnDelete;

            RefreshErrorText();

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// event handler for the DetachFromPanelEvent
        /// </summary>
        /// <param name="evt">Detach from panel event</param>
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_RetryButton.clickable.clicked -= OnRetry;
            m_DeleteButton.clickable.clicked -= OnDelete;

            m_ErrorText = null;
            m_RetryButton = null;
            m_DeleteButton = null;

            UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// Setting the error message
        /// </summary>
        /// <param name="error">Error message</param>
        public void SetError(string error)
        {
            m_Error = error;
            RefreshErrorText();
        }

        /// <summary>
        /// Refreshing the Text view with the error message
        /// </summary>
        void RefreshErrorText()
        {
            if (m_ErrorText == null)
                return;

            m_ErrorText.text = m_Error;
        }
    }
}