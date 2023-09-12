using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public partial class PaintingElement : Image, IDisposable
    {
        bool Seamless { get; set; }
        public bool WrapAround { get; set; }

        Model m_Model;
        public RenderTexture MaskTexture => m_MaskTexture;

        public float PaintRadius
        {
            get => m_Radius;
            set => m_Radius = value;
        }

        public void SetMaskSeamless(bool value)
        {
            Seamless = value;
            if (m_MaskTexture != null)
            {
                PaintMaterial(null);
            }
        }

        public bool EraseMode { get; set; }

        const string k_MaterialPaintResourcePath = "PaintingMaterial";
        const string k_MaterialExportResourcePath = "MaskExportMaterial";

        static Material s_PaintingMaterial;
        static Material s_ExportMaterial;

        static readonly int k_RadiusShaderProperty = Shader.PropertyToID("_Radius");
        static readonly int k_PaintPosXShaderProperty = Shader.PropertyToID("_PaintPosX");
        static readonly int k_PaintPosYShaderProperty = Shader.PropertyToID("_PaintPosY");
        static readonly int k_IsErasingShaderProperty = Shader.PropertyToID("_IsErasing");
        static readonly int k_SeamlessShaderProperty = Shader.PropertyToID("_IsSeamless");
        static readonly int k_WrapAroundShaderProperty = Shader.PropertyToID("_WrapAround");

        Image m_MaskImage;
        RenderTexture m_MaskTexture;

        bool m_IsPainting;
        float m_Radius = 5f;

        public void SetModel(Model model)
        {
            m_Model = model;
        }
        public void InitializeImage(Texture baseTexture, VisualElement imageContainer)
        {
            InitializeMaskTexture(baseTexture);

            if (m_MaskImage == null)
            {
                m_MaskImage = new Image();

                m_MaskImage.AddToClassList(maskClass);

                m_MaskImage.RegisterCallback<PointerDownEvent>(OnPointerDown);
                m_MaskImage.RegisterCallback<PointerUpEvent>(OnPointerUp);

                panel.visualTree.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                panel.visualTree.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
                panel.visualTree.RegisterCallback<PointerUpEvent>(OnPointerUp);

                //We are stopping the events used by the graph
                m_MaskImage.RegisterCallback<MouseDownEvent>((evt) => evt.StopImmediatePropagation());
                m_MaskImage.RegisterCallback<MouseUpEvent>((evt) => evt.StopImmediatePropagation());
                m_MaskImage.RegisterCallback<MouseMoveEvent>((evt) => evt.StopImmediatePropagation());

                Add(m_MaskImage);

                m_MaskImage.parent.RegisterCallback<GeometryChangedEvent>((_) => SetMaskElementBounds(imageContainer));
                styleSheets.Add(Resources.Load<StyleSheet>(k_StyleResourcePath));
            }

            m_MaskImage.image = m_MaskTexture;
            SetMaskElementBounds(imageContainer);
        }

        void InitializeMaskTexture(Texture baseTexture)
        {
            Dispose(); //We avoid texture leak
            m_MaskTexture = new RenderTexture(baseTexture.width, baseTexture.height, 0, RenderTextureFormat.ARGB32);

            var restoreRT = RenderTexture.active;

            RenderTexture.active = m_MaskTexture;
            GL.Clear(true, true, new Color(1f, 1f, 1f, 0f));
            RenderTexture.active = restoreRT;
        }

        void OnPaint(Vector2 localPosition)
        {
            if (!m_IsPainting)
                return;

            localPosition = new Vector2(
                Math.Clamp(localPosition.x, 0, m_MaskImage.localBound.width),
                Math.Clamp(localPosition.y, 0, m_MaskImage.localBound.height));

            localPosition.y = m_MaskImage.localBound.height - localPosition.y;

            var uvPos = ConvertCoordinateFromPixelToUVSpace(localPosition, new Vector2(m_MaskImage.localBound.width, m_MaskImage.localBound.height));

            PaintMaterial(uvPos);
        }

        void PaintMaterial(Vector2? uvPos)
        {
            if (s_PaintingMaterial == null)
            {
                var baseMaterial = Resources.Load<Material>(k_MaterialPaintResourcePath);
                s_PaintingMaterial = new Material(baseMaterial);
            }

            if (uvPos != null)
            {
                s_PaintingMaterial.SetFloat(k_RadiusShaderProperty, m_Radius / 100f);
                s_PaintingMaterial.SetFloat(k_PaintPosXShaderProperty, uvPos.Value.x);
                s_PaintingMaterial.SetFloat(k_PaintPosYShaderProperty, uvPos.Value.y);
            }
            else
            {
                s_PaintingMaterial.SetFloat(k_RadiusShaderProperty, 0f);
            }

            s_PaintingMaterial.SetFloat(k_IsErasingShaderProperty, EraseMode ? 1f : 0f);
            s_PaintingMaterial.SetFloat(k_SeamlessShaderProperty, Seamless ? 1f : 0f);
            s_PaintingMaterial.SetFloat(k_WrapAroundShaderProperty, WrapAround ? 1f : 0f);

            var restoreRT = RenderTexture.active;

            RenderTexture.active = m_MaskTexture;
            var temporary = RenderTexture.GetTemporary(m_MaskTexture.descriptor);

            Graphics.Blit(m_MaskTexture, temporary);
            Graphics.Blit(temporary, m_MaskTexture, s_PaintingMaterial);

            RenderTexture.ReleaseTemporary(temporary);

            RenderTexture.active = restoreRT;
        }

        static Vector2 ConvertCoordinateFromPixelToUVSpace(Vector2 coordinate, Vector2 textureSize)
        {
            return new Vector2(coordinate.x / textureSize.x, coordinate.y / textureSize.y);
        }

        public RenderTexture Export()
        {
            if (s_ExportMaterial == null)
            {
                s_ExportMaterial = Resources.Load<Material>(k_MaterialExportResourcePath);
            }

            var exportTexture = new RenderTexture(m_MaskTexture.width, m_MaskTexture.height, 0, m_MaskTexture.format);

            var restoreRT = RenderTexture.active;
            RenderTexture.active = exportTexture;
            Graphics.Blit(m_MaskTexture, exportTexture, s_ExportMaterial);
            RenderTexture.active = restoreRT;

            return exportTexture;
        }

        void SetMaskElementBounds(VisualElement imageContainer)
        {
            m_MaskImage.style.width = imageContainer.localBound.width;
            m_MaskImage.style.height = imageContainer.localBound.height;
            m_MaskImage.transform.position = imageContainer.localBound.position;

            m_MaskImage.style.left = (m_MaskImage.parent.localBound.width - m_MaskImage.style.width.value.value) / 2f;
        }

        #region Pointer Events

        void OnPointerDown(PointerDownEvent @event)
        {
            m_IsPainting = true;
            OnPaint(@event.localPosition);
            @event.StopImmediatePropagation();
        }

        void OnPointerUp(PointerUpEvent @event)
        {
            if (!m_IsPainting || !m_Model) return;
            m_Model.MaskPaintDone(Export().ToTexture2D());
            m_IsPainting = false;
        }

        void OnPointerLeave(PointerLeaveEvent @event)
        {
            OnPointerUp(null);
        }

        void OnPointerMove(PointerMoveEvent @event)
        {
            if (panel is null)
                return;
            var position = m_MaskImage.WorldToLocal(panel.visualTree.LocalToWorld(@event.localPosition));
            if (m_IsPainting)
                OnPaint(position);
        }
        #endregion

        public void Dispose()
        {
            if (panel is not null)
            {
                panel.visualTree.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                panel.visualTree.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
                panel.visualTree.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            }

            if (m_MaskTexture != null)
                m_MaskTexture.SafeDestroy();
        }

        public void ClearPainting()
        {
            var restoreRT = RenderTexture.active;

            RenderTexture.active = m_MaskTexture;
            GL.Clear(true, true, new Color(1f, 1f, 1f, 0f));
            RenderTexture.active = restoreRT;
            m_Model?.MaskPaintDone(Export().ToTexture2D());
        }
    }
}