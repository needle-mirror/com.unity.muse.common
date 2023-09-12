using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    class MuseShortcutHandler : Manipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            var keyCode = evt.keyCode;
            var keyModifier = GetModifier(evt);

            var shortcuts = MuseShortcuts.GetShortcuts(keyCode, keyModifier);
            if (shortcuts != null)
            {
                foreach (var shortcut in shortcuts)
                {
                    if (evt.target is VisualElement element && element.panel != shortcut.source?.panel)
                        continue;
                    shortcut.action?.Invoke();
                }

                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        static KeyModifier GetModifier(IKeyboardEvent evt)
        {
            if (evt.altKey)
                return KeyModifier.Alt;
            if (evt.ctrlKey)
                return KeyModifier.Control;
            if (evt.actionKey)
                return KeyModifier.Action;
            if (evt.shiftKey)
                return KeyModifier.Shift;

            return KeyModifier.None;
        }
    }
}
