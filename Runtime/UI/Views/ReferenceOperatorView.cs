using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    class ReferenceOperatorView : ExVisualElement
    {
        // TODO: Replace this base64 images by actual UUIDs when the backend will support it.

        static string[] k_ProvidedPatternsBase64Encoded =
        {
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/1").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/2").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/3").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/4").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/5").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/6").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/7").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/8").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/9").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/10").EncodeToPNG()),
            Convert.ToBase64String(Resources.Load<Texture2D>("Shapes/11").EncodeToPNG()),
        };

        static readonly Texture2D[] k_PatternTextures = k_ProvidedPatternsBase64Encoded.Select(guid =>
            new Texture2D(2, 2) { name = guid, hideFlags = HideFlags.HideAndDontSave }).ToArray();

        // TODO: Source items for patterns should be simple image artifacts based on the UUID list
        //static readonly SimpleImageArtifact[] k_PatternArtifacts =
        //    k_ProvidedPatternsGuid.Select(guid => new SimpleImageArtifact(guid, 0)).ToArray();

        internal event Action dataChanged;

        VisualElement m_ShapeToolbar;

        ReferenceOperator.Mode m_Mode = ReferenceOperator.Mode.Color;

        VisualElement m_ColorToolbar;

        ActionGroup m_ModeGroup;

        TouchSliderInt m_StrengthSlider;

        Image m_PreviewImage;

        VisualElement m_DropZoneHelper;

        ActionButton m_PatternsButton;

        AppUI.UI.GridView m_PatternsView;

        Popover m_PatternsPopover;

        Texture2DDropManipulator m_DropManipulator;

        VisualElement m_DropZone;

        VisualElement m_DropzoneContextMenuAnchor;

        ActionButton m_ClearButton;

        string m_Guid;

        readonly Model m_Model;

        Texture2D m_ColorImage;

        Texture2D m_ShapeImage;

        Text m_DropzoneMessage;

        static ReferenceOperatorView()
        {
            // load textures
            for (var i = 0; i < k_ProvidedPatternsBase64Encoded.Length; i++)
            {
                var guid = k_ProvidedPatternsBase64Encoded[i];
                var img = k_PatternTextures[i];
                img.LoadImage(Convert.FromBase64String(guid));
            }
        }

        public ReferenceOperatorView(Model model)
        {
            m_Model = model;

            CreateGUI();

            SetModeWithoutNotify(ReferenceOperator.Mode.Color);
            SetColorImageWithoutNotify(null);
        }

        void CreateGUI()
        {
            passMask = Passes.Clear | Passes.OutsetShadows;

            AddToClassList("muse-node");
            AddToClassList("appui-elevation-8");
            name = "input-image-node";

            var text = new Text();
            text.text = "Input Image";
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");
            Add(text);

            var row = new VisualElement();
            row.AddToClassList("row");
            row.AddToClassList("bottom-gap");
            Add(row);

            m_ModeGroup = new ActionGroup
            {
                selectionType = SelectionType.Single,
                compact = true
            };
            m_ModeGroup.selectionChanged += OnModeChanged;
            row.Add(m_ModeGroup);

            var colorModeButton = new ActionButton();
            colorModeButton.label = "Color";
            m_ModeGroup.Add(colorModeButton);

            var shapeModeButton = new ActionButton();
            shapeModeButton.label = "Shape";
            m_ModeGroup.Add(shapeModeButton);

            var spacer = new VisualElement();
            spacer.AddToClassList("muse-spacer");
            row.Add(spacer);

            m_PatternsButton = new ActionButton();
            m_PatternsButton.AddToClassList("right-gap");
            m_PatternsButton.label = "Patterns";
            m_PatternsButton.clicked += OnPatternsButtonClicked;
            row.Add(m_PatternsButton);

            m_ClearButton = new ActionButton();
            m_ClearButton.icon = "x";
            m_ClearButton.clicked += OnClearButtonClicked;
            row.Add(m_ClearButton);

            m_DropZone = new VisualElement
            {
                pickingMode = PickingMode.Position,
                focusable = true,
            };
            m_DropZone.AddToClassList("muse-dropzone");
            m_DropZone.AddToClassList("bottom-gap");
            m_DropZone.name = "muse-dropzone";
            Add(m_DropZone);
            m_DropZone.RegisterCallback<GeometryChangedEvent>(ResizeDropZone);

            m_DropzoneContextMenuAnchor = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    width = 0,
                    height = 0,
                }
            };
            m_DropZone.Add(m_DropzoneContextMenuAnchor);
            m_DropZone.RegisterCallback<PointerDownEvent>(OnDropZonePointerDown);
            m_DropZone.RegisterCallback<KeyDownEvent>(OnDropZoneKeyDown);
            m_DropManipulator = new Texture2DDropManipulator(m_Model);
            m_DropManipulator.onDragStart += OnDragStart;
            m_DropManipulator.onDragEnd += OnDragEnd;
            m_DropManipulator.onDrop += OnDrop;
            m_DropZone.AddManipulator(m_DropManipulator);

            m_PreviewImage = new Image { pickingMode = PickingMode.Ignore };
            m_PreviewImage.AddToClassList("muse-dropzone__image");
            m_DropZone.Add(m_PreviewImage);

            m_DropZoneHelper = new VisualElement { pickingMode = PickingMode.Ignore };
            m_DropZoneHelper.AddToClassList("muse-dropzone__helper");
            m_DropZone.Add(m_DropZoneHelper);

            m_DropzoneMessage = new Text { pickingMode = PickingMode.Position, enableRichText = true };
            m_DropzoneMessage.size = TextSize.XS;
            m_DropzoneMessage.AddToClassList("muse-dropzone__message");
            m_DropzoneMessage.AddToClassList("bottom-gap");
            m_DropZoneHelper.Add(m_DropzoneMessage);

            var dropzoneButton = new ActionButton { size = Size.S };
            dropzoneButton.label = "Import";
            dropzoneButton.AddToClassList("muse-dropzone__button");
            m_DropZoneHelper.Add(dropzoneButton);
            dropzoneButton.clicked += OnImportButtonClicked;

            m_ColorToolbar = new VisualElement();
            Add(m_ColorToolbar);

            m_StrengthSlider = new TouchSliderInt();
            m_StrengthSlider.label = "Strength";
            m_StrengthSlider.lowValue = 0;
            m_StrengthSlider.highValue = 100;
            m_StrengthSlider.value = 50;
            m_StrengthSlider.AddToClassList("bottom-gap");
            m_StrengthSlider.RegisterValueChangedCallback(OnIntValueChanged);
            m_ColorToolbar.Add(m_StrengthSlider);

            m_ShapeToolbar = new VisualElement();
            Add(m_ShapeToolbar);

            m_PatternsView = new AppUI.UI.GridView
            {
                itemHeight = 100,
                selectionType = SelectionType.Single,
                columnCount = 2
            };
            m_PatternsView.AddToClassList("muse-patterns-view");

            m_PatternsView.makeItem = MakePatternItemView;
            m_PatternsView.bindItem = BindPatternItemView;
            m_PatternsView.itemsSource = k_PatternTextures;
            m_PatternsView.itemsChosen += OnPatternChosen;
        }

        void OnDropZonePointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();

                m_DropzoneContextMenuAnchor.style.left = evt.localPosition.x;
                m_DropzoneContextMenuAnchor.style.top = evt.localPosition.y;

                if (!m_PreviewImage.image)
                    return;

                var contextMenu = new Menu
                {
                    style =
                    {
                        minWidth = 128
                    }
                };

                var copyAction = new MenuItem
                {
                    label = "Copy",
                    shortcut = $"{actionKeyLabel}+C"
                };
                copyAction.clickable.clicked += CopyImageToClipboard;
                contextMenu.Add(copyAction);

                var pasteAction = new MenuItem
                {
                    label = "Paste",
                    shortcut = $"{actionKeyLabel}+V"
                };
                pasteAction.clickable.clicked += PasteImageFromClipboard;
                contextMenu.Add(pasteAction);

                var menu = MenuBuilder.Build(m_DropzoneContextMenuAnchor, contextMenu);
                menu.dismissed += (builder, type) => m_DropZone.RemoveFromClassList(Styles.focusedUssClassName);
                menu.Show();

                m_DropZone.AddToClassList(Styles.focusedUssClassName);
            }
        }

        void OnDropZoneKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey)
            {
                if (evt.keyCode == KeyCode.C)
                {
                    evt.StopImmediatePropagation();
                    evt.PreventDefault();

                    if (m_PreviewImage.image)
                        CopyImageToClipboard();
                }
                else if (evt.keyCode == KeyCode.V)
                {
                    evt.StopImmediatePropagation();
                    evt.PreventDefault();

                    PasteImageFromClipboard();
                }
            }
        }

        const string k_ArtifactMimeType = "artifact/guid;";
        const string k_ImageMimeType = "image/png;base64,";

        void PasteImageFromClipboard()
        {
            var buffer = GUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(buffer))
                return;

            if (buffer.StartsWith(k_ArtifactMimeType))
            {
                var guid = buffer.Substring(k_ArtifactMimeType.Length);
                var artifact = m_Model.AssetsData.FirstOrDefault(a => a.Guid == guid);

                if (artifact is not null && ArtifactCache.IsInCache(artifact))
                {
                    var cachedObj = ArtifactCache.Read(artifact);
                    if (cachedObj is Texture2D img)
                    {
                        SetGuidWithoutNotify(guid);
                        if (m_Mode == ReferenceOperator.Mode.Color)
                            SetColorImageWithoutNotify(img);
                        else
                            SetShapeImageWithoutNotify(img);
                        dataChanged?.Invoke();
                    }
                }
            }
            else if (buffer.StartsWith(k_ImageMimeType))
            {
                var b64String = buffer.Substring(k_ImageMimeType.Length);
                var bytes = Convert.FromBase64String(b64String);
                var img = new Texture2D(2, 2);
                img.LoadImage(bytes);
                if (m_Mode == ReferenceOperator.Mode.Color)
                    SetColorImageWithoutNotify(img);
                else
                    SetShapeImageWithoutNotify(img);
                dataChanged?.Invoke();
            }
            else
            {
                Debug.Log(buffer);
            }
        }

        void CopyImageToClipboard()
        {
            var img = (Texture2D)m_PreviewImage.image;

            if (!img && string.IsNullOrEmpty(m_Guid))
                return;

            GUIUtility.systemCopyBuffer = string.IsNullOrEmpty(m_Guid) ?
                $"{k_ImageMimeType}{Convert.ToBase64String(img.EncodeToPNG())}" : $"{k_ArtifactMimeType}{m_Guid}";

            Toast
                .Build(this, "Input Image Copied to Clipboard", NotificationDuration.Short)
                .Show();
        }

        void OnDrop(Texture2D obj)
        {
            // TODO: Support drag and drop for Shape mode
            if (m_Mode == ReferenceOperator.Mode.Shape)
            {
                Debug.LogWarning("Dropping images in shape mode is not yet supported.");
                return;
            }

            SetGuidWithoutNotify(m_DropManipulator.artifact?.Guid);

            if (m_Mode == ReferenceOperator.Mode.Shape)
                SetShapeImageWithoutNotify(obj);
            else
                SetColorImageWithoutNotify(obj);

            dataChanged?.Invoke();
        }

        void OnDragEnd()
        {
            m_DropZone.RemoveFromClassList("accept-drag");
        }

        void OnDragStart()
        {
            m_DropZone.AddToClassList("accept-drag");
        }

        static void BindPatternItemView(VisualElement el, int idx)
        {
            if (idx >= k_PatternTextures.Length || idx < 0)
                return;

            // TODO: Use PreviewImage instead of Image
            el.Q<Image>().image = k_PatternTextures[idx];
            el.userData = k_ProvidedPatternsBase64Encoded[idx];
        }

        static VisualElement MakePatternItemView()
        {
            var itemView = new VisualElement();
            itemView.AddToClassList("muse-patterns-view__item");

            // TODO: Use PreviewImage instead of Image
            var image = new Image();
            image.AddToClassList("muse-patterns-view__image");
            itemView.Add(image);

            return itemView;
        }

        static string actionKeyLabel =>
            Application.platform is RuntimePlatform.OSXEditor or
                RuntimePlatform.OSXPlayer or
                RuntimePlatform.OSXServer ? "Cmd" : "Ctrl";

        void OnPatternChosen(IEnumerable<object> selection)
        {
            using var selectionEnumerator = selection.GetEnumerator();

            // TODO: Use Artifact instead of plain textures
            if (selectionEnumerator.MoveNext() && selectionEnumerator.Current is Texture2D tex2D)
            {
                SetGuidWithoutNotify(null); // artifact.Guid
                OnChosenItemLoaded(tex2D, null, null); // artifact.GetPreview(OnChosenItemLoaded, true);
            }
        }

        void OnChosenItemLoaded(Texture2D preview, byte[] rawData, string errorMessage)
        {
            SetShapeImageWithoutNotify(preview);
            dataChanged?.Invoke();

            m_PatternsPopover?.Dismiss(DismissType.Action);
        }

        void OnPatternsButtonClicked()
        {
            m_PatternsPopover?.Dismiss(DismissType.Consecutive);

            m_PatternsPopover = Popover
                .Build(m_PatternsButton, m_PatternsView)
                .SetAnchor(m_PatternsButton)
                .SetPlacement(PopoverPlacement.BottomStart)
                .SetArrowVisible(false)
                .SetCrossOffset(-8);

            m_PatternsPopover.Show();
        }

        void OnImportButtonClicked()
        {
#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
                "Import Image",
                "",
                new[]
                {
                    "Image",
                    "png,jpg,jpeg"
                });
            if (string.IsNullOrEmpty(path))
                return;

            var img = new Texture2D(2, 2);
            img.LoadImage(System.IO.File.ReadAllBytes(path));
            if (m_Mode == ReferenceOperator.Mode.Color)
                SetColorImageWithoutNotify(img);
            else
                SetShapeImageWithoutNotify(img);
            dataChanged?.Invoke();
#else
            Debug.LogError("Importing images is not supported in builds");
#endif
        }

        void OnIntValueChanged(ChangeEvent<int> evt)
        {
            dataChanged?.Invoke();
        }

        void OnClearButtonClicked()
        {
            if (m_Mode == ReferenceOperator.Mode.Color)
                SetColorImageWithoutNotify(null);
            else
                SetShapeImageWithoutNotify(null);
            dataChanged?.Invoke();
        }

        void OnModeChanged(IEnumerable<int> indices)
        {
            using var enumerator = indices.GetEnumerator();
            enumerator.MoveNext();

            var mode = (ReferenceOperator.Mode)enumerator.Current;
            SetModeWithoutNotify(mode);
            dataChanged?.Invoke();
        }

        internal void SetModeWithoutNotify(ReferenceOperator.Mode mode)
        {
            m_Mode = mode;
            m_ColorToolbar.EnableInClassList(Styles.hiddenUssClassName, m_Mode != ReferenceOperator.Mode.Color);
            m_ShapeToolbar.EnableInClassList(Styles.hiddenUssClassName, m_Mode != ReferenceOperator.Mode.Shape);
            m_ModeGroup.SetSelectionWithoutNotify(new []{(int)m_Mode});
            m_PatternsButton.EnableInClassList(Styles.hiddenUssClassName, m_Mode != ReferenceOperator.Mode.Shape);
            RefreshPreview();
        }

        internal ReferenceOperator.Mode GetMode()
        {
            return m_Mode;
        }

        internal void SetGuidWithoutNotify(string guid)
        {
            m_Guid = guid;
        }

        internal void SetColorImageWithoutNotify(Texture2D img)
        {
            if (!Validate(img))
                return;

            m_ColorImage = img;
            RefreshPreview();
        }

        internal void SetShapeImageWithoutNotify(Texture2D img)
        {
            if (!Validate(img))
                return;

            m_ShapeImage = img;
            RefreshPreview();
        }

        static bool Validate(Texture2D img)
        {
            if (img && !img.isReadable)
            {
                Debug.LogError("<b>[Muse]</b> Input image must be readable, please enable read/write in the import settings");
                return false;
            }

            if (img && IsTextureCompressed(img))
            {
                Debug.LogError($"<b>[Muse]</b> Input image must be not be compressed. Please remove compression from the import settings.");
                return false;
            }

            return true;
        }

        static bool IsTextureCompressed(Texture2D texture)
        {
            var format = texture.format;

            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT5:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ASTC_4x4:
                    return true;
                default:
                    return false;
            }
        }

        void RefreshPreview()
        {
            m_PreviewImage.image = m_Mode switch
            {
                ReferenceOperator.Mode.Color => GetColorImage(),
                ReferenceOperator.Mode.Shape => GetShapeImage(),
                _ => null
            };
            m_DropzoneMessage.text = m_Mode switch
            {
                ReferenceOperator.Mode.Color => TextContent.dragAndDropColorImageMessage,
                ReferenceOperator.Mode.Shape => TextContent.dragAndDropShapeImageMessage,
                _ => ""
            };
            // TODO: Show DropZone helper in Shape mode when no image is set
            m_DropZoneHelper.EnableInClassList(Styles.hiddenUssClassName, /*m_Mode == ReferenceOperator.Mode.Shape ||*/ m_PreviewImage.image);
            m_ClearButton.SetEnabled(m_PreviewImage.image);
        }

        internal string GetGuid()
        {
            return m_Guid;
        }

        internal Texture2D GetColorImage()
        {
            return m_ColorImage;
        }

        internal Texture2D GetShapeImage()
        {
            return m_ShapeImage;
        }

        internal void SetStrengthWithoutNotify(int strength)
        {
            m_StrengthSlider.SetValueWithoutNotify(strength);
        }

        internal int GetStrength()
        {
            return m_StrengthSlider.value;
        }

        void ResizeDropZone(GeometryChangedEvent evt)
        {
            var dropZone = (VisualElement)evt.target;
            var size = dropZone.resolvedStyle.width;

            if (!Mathf.Approximately(dropZone.resolvedStyle.height, size))
                dropZone.style.height = size;
        }
    }
}
