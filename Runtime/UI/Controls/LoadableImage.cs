using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AppUI.UI;

namespace Unity.Muse.Common
{
    internal class LoadableImage : Image
    {
        const string k_StylePath = "uss/LoadableImageStyle";
        const string k_ClassStatusElement = "li-element";

        internal readonly GenericLoader GenericLoader;
        readonly Chip m_ResolutionChip;

        internal GenericLoader.State LoadingState => GenericLoader.LoadingState;

        protected LoadableImage(bool autoLoading = true)
        {
            styleSheets.Add(Resources.Load<StyleSheet>(k_StylePath));
            AddToClassList(k_ClassStatusElement);

            GenericLoader = new GenericLoader(autoLoading ? GenericLoader.State.Loading : GenericLoader.State.None)
            {
                style =
                {
                    position = Position.Absolute,
                    width = Length.Percent(100),
                    height = Length.Percent(100)
                }
            };

            Add(GenericLoader);

            m_ResolutionChip = new Chip
            {
                variant = Chip.Variant.Filled,
                label = "2K",
                style =
                {
                   position = Position.Absolute,
                   alignSelf = Align.FlexEnd,
                   right = 8,
                   top = 8,
                   opacity = 0.8f,
                   display = DisplayStyle.None
                }
            };

            Add(m_ResolutionChip);
        }

        protected void OnLoaded(UnityEngine.Texture texture)
        {
            image = texture;
            GenericLoader.SetState(GenericLoader.State.None);

            UpdateResolutionChip(texture);
        }

        public void OnError(string error)
        {
            GenericLoader.SetState(GenericLoader.State.Error, error);
        }

        protected void OnLoading()
        {
            image = null;
            GenericLoader.SetState(GenericLoader.State.Loading);
        }

        void UpdateResolutionChip(UnityEngine.Texture texture)
        {
            if (texture)
            {
                m_ResolutionChip.label = texture.width switch
                {
                    2048 => "2K",
                    4096 => "4K",
                    8192 => "8K",
                    _ => string.Empty
                };
                m_ResolutionChip.style.display = string.IsNullOrEmpty(m_ResolutionChip.label) ? DisplayStyle.None : DisplayStyle.Flex;
            }
            else
            {
                m_ResolutionChip.style.display = DisplayStyle.None;
            }
        }
    }
}
