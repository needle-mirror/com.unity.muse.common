using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;

namespace Unity.Muse.Common
{
    /// <summary>
    /// Common UI for Canvas Tools
    /// </summary>
    internal class MuseToolbar : VisualElement
    {
        /// <summary>
        /// Pan Button
        /// </summary>
        public Button PanBtn { get; private set; }
        /// <summary>
        /// Paint Button
        /// </summary>
        public Button PaintBtn { get; private set; }
        /// <summary>
        /// Erase Button
        /// </summary>
        public Button EraseBtn { get; private set; }
        /// <summary>
        /// Delete Button
        /// </summary>
        public IconButton DeleteBtn { get; private set; }
        /// <summary>
        /// Size Slider
        /// </summary>
        public TouchSliderFloat SizeSlider { get; private set; }
        
        const string k_SelectedClassName = "muse-toolbar--selected";

        public MuseToolbar()
        {
            InitializeVisualTree();
        }

        void InitializeVisualTree()
        {
            var styleSheet = ResourceManager.Load<StyleSheet>(PackageResources.toolbarStyleSheet);
            styleSheets.Add(styleSheet);

            var actionGroup = new ActionGroup()
            {
                compact = true,
                justified = false,
                style =
                {
                    flexGrow = 0f
                }
            };

            PanBtn = new Button()
            {
                name = "PanBtn",
                tooltip = "Pan",
                trailingIcon = "pan--regular"
            };
            actionGroup.Add(PanBtn);

            PaintBtn = new Button()
            {
                name = "PaintBtn",
                tooltip = "Paint",
                trailingIcon = "paint-brush--regular"
            };
            actionGroup.Add(PaintBtn);

            EraseBtn = new Button()
            {
                name = "EraseBtn",
                tooltip = "Erase",
                trailingIcon = "eraser--regular"
            };
            actionGroup.Add(EraseBtn);

            Add(actionGroup);

            DeleteBtn = new IconButton("delete--regular")
            {
                tooltip = "Clear"
            };
            
            Add(DeleteBtn);

            SizeSlider = new TouchSliderFloat()
            {
                label = "Size",
                value = 5,
                lowValue = 0,
                highValue = 10,
                style =
                {
                    width = 138
                }
            }; 
            Add(SizeSlider);
            
            PanBtn.clickable.clicked += OnPanClicked;
            PaintBtn.clickable.clicked += OnPaintClicked;
            EraseBtn.clickable.clicked += OnEraseClicked;

            SelectButton(PanBtn);
        }
        
        /// <summary>
        /// Set the button's selected state
        /// </summary>
        /// <param name="button">The specific button</param>
        public void SelectButton(Button button)
        {
            PaintBtn.EnableInClassList(k_SelectedClassName, button == PaintBtn);
            EraseBtn.EnableInClassList(k_SelectedClassName, button == EraseBtn);
            PanBtn.EnableInClassList(k_SelectedClassName, button == PanBtn);
            
            DeleteBtn.SetEnabled(button == PaintBtn || button == EraseBtn);
            SizeSlider.SetEnabled(button == PaintBtn || button == EraseBtn);
        }

        void OnPaintClicked()
        {
            SelectButton(PaintBtn);
        }

        void OnEraseClicked()
        {
            SelectButton(EraseBtn);
        }
        
        void OnPanClicked()
        {
            SelectButton(PanBtn);
        }
    }
}