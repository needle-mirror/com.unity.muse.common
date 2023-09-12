using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public class ContextMenu : VisualElement, IControl
    {
        public new class UxmlFactory : UxmlFactory<ContextMenu, UxmlTraits> { }

        public void SetModel(Model model)
        {
           // throw new System.NotImplementedException();
        }

        public void UpdateView()
        {
            throw new System.NotImplementedException();
        }
    }
}
