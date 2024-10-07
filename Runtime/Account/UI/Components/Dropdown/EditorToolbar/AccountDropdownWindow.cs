#if UNITY_EDITOR
using System;
using Unity.Muse.AppUI.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common.Account.UI
{
    class AccountDropdownWindow : EditorWindow
    {
        internal static Func<Rect> toolbarPosition;
        internal static VisualElement toolbarButton;
        static float s_LastPopupHeight = 1;

        /// <summary>
        /// Show the account settings without it being clicked by the user.
        /// </summary>
        internal static void ShowMuseAccountSettingsAsPopup()
        {
            var rect = toolbarButton.worldBound;
            rect.position = VisualElementUtility.GetScreenPosition(toolbarPosition(), toolbarButton);
            ShowMuseAccountSettingsAsPopupInternal(rect);
        }

        /// <summary>
        /// Show muse account settings
        /// </summary>
        /// <param name="rect">Element bounds/Rect relative to its EditorWindow</param>
        internal static void ShowMuseAccountSettingsAsPopup(Rect rect) =>
            ShowMuseAccountSettingsAsPopupInternal(GUIUtility.GUIToScreenRect(rect));

        static void ShowMuseAccountSettingsAsPopupInternal(Rect buttonRect)
        {
            ClearPreviousWindows();
            var popup = CreateInstance<AccountDropdownWindow>();
            popup.hideFlags = HideFlags.DontSave;
            popup.ShowAsDropDown(buttonRect, Vector2.zero);
            const int minSizeX = 300;

            if (Mathf.Approximately(popup.minSize.y, 0f))
            {
                popup.minSize = new Vector2(minSizeX, s_LastPopupHeight);
            }
            var content = popup.rootVisualElement.Q<AccountDropdownContent>();
            content.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                const int heightOffset = 4;

                var height = evt.newRect.height + heightOffset;

                if (!Mathf.Approximately(s_LastPopupHeight, height))
                {
                    s_LastPopupHeight = height;
                    // Can't change the popup.minSize while in a GeometryChangedEvent as it will give an error and the
                    // popup won't appear the first time it's clicked after a domain reload.
                    EditorApplication.delayCall += () => ShowMuseAccountSettingsAsPopupInternal(buttonRect);
                }
            });
        }

        static void ClearPreviousWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<AccountDropdownWindow>();
            foreach (var window in windows)
            {
                try
                {
                    window.Close();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        void CreateGUI()
        {
            var panel = new Panel();
            panel.AddToClassList("dropdown-editor-window");
            rootVisualElement.Add(panel);

            AccountController.Register(this);

            var scrollView = new ScrollView(); // Wrap in a scrollview to be certain all content will always be shown.
            var content = new AccountDropdownContent { OnAction = Close };
            scrollView.Add(content);
            panel.Add(scrollView);
        }
    }
}
#endif
