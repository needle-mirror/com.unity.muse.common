using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    public partial class PaintingElement
    {
        public const string rootClass = "ng-paintelement-image-root";
        public const string maskClass = "ng-paintelement-image-mask";

        const string k_StyleResourcePath = "PaintingElementStyle";

        public new class UxmlFactory : UxmlFactory<PaintingElement, UxmlTraits> { }

        public new class UxmlTraits : Image.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }
}
