using UnityEngine;

namespace Unity.Muse.Common
{
    public interface IControl
    {
        public void SetModel(Model model);

        public void UpdateView();
    }
}
