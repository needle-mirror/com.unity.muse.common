using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public class PaintingManipulator : Manipulator
    {
        PaintingElement m_PaintingElement;
        Model m_Model;
        public bool Seamless { get; private set; }
        public bool WrapAround { get; private set; }

        public PaintingManipulator(bool seamless, bool wrapAround = false)
        {
            SetMaskSeamless(seamless);
            WrapAround = wrapAround;
        }

        public void SetRadius(float radius)
        {
            if(m_PaintingElement != null)
                m_PaintingElement.PaintRadius = radius;
        }

        public float GetRadius()
        {
            return m_PaintingElement?.PaintRadius ?? 5.0f;
        }

        public void SetEraserMode(bool erase)
        {
            if(m_PaintingElement != null)
                m_PaintingElement.EraseMode = erase;
        }

        public void ClearPainting()
        {
            if(m_PaintingElement != null)
                m_PaintingElement.ClearPainting();
        }
        public void SetMaskSeamless(bool value)
        {
            Seamless = value;
            m_PaintingElement?.SetMaskSeamless(Seamless);
        }
        protected override void RegisterCallbacksOnTarget()
        {
            Texture baseTexture = null;
            if (target is Image imageTarget)
            {
                baseTexture = imageTarget.image;
            }
            else
            {
                baseTexture = target.style.backgroundImage.value.texture;
            }

            m_PaintingElement = new PaintingElement() { WrapAround = WrapAround };
            m_PaintingElement.SetMaskSeamless(Seamless);

            m_PaintingElement.SetModel(m_Model);
            target.Add(m_PaintingElement);
            m_PaintingElement.InitializeImage(baseTexture, target);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            if (m_PaintingElement == null)
                return;

            m_PaintingElement.Dispose();
            target.Remove(m_PaintingElement);
            m_PaintingElement = null;
        }

        public RenderTexture GetTexture()
        {
            return m_PaintingElement?.Export();
        }
        public void SetModel(Model model)
        {
            m_Model = model;
        }
    }
}
