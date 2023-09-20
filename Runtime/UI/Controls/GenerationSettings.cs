using System;
using Unity.AppUI.UI;
using Unity.Muse.Common.Utils;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    class GenerationSettings : VisualElement
    {
        const string k_UseAll = "use-all";
        Model m_CurrentModel;
        Artifact m_Artifact;
        Action m_Dismiss;

        internal static void ShowGenerationSettings(Artifact artifact, VisualElement parent, Model currentModel)
        {
            var settings = new GenerationSettings(artifact);
            var modal = Popover.Build(parent, settings);
            settings.m_Dismiss += modal.Dismiss;
            modal.SetAnchor(parent);
            modal.SetPlacement(currentModel.isRefineMode ? PopoverPlacement.Left : PopoverPlacement.Right);
            modal.Show();
        }

        public GenerationSettings(Artifact artifact)
        {
            this.ApplyTemplate("uxml/GenerationSettings");
            var operatorContainer = this.Q<VisualElement>(classes: "operators");
            this.Q<ActionButton>(k_UseAll).clicked += UseAll;
            m_Artifact = artifact;

            this.RegisterContextChangedCallback<Model>(context => SetModel(context.context));

            foreach (var op in artifact.GetOperators())
            {
                var view = op.GetSettingsView();
                if (view is null || !op.Enabled())
                    continue;

                operatorContainer.Add(CreateView(op.Label, view, () => Use(op)));
            }
        }

        void SetModel(Model model)
        {
            m_CurrentModel = model;
        }

        static VisualElement CreateView(string label, VisualElement view, Action useAction)
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            row.AddToClassList("operator-settings");

            var center = new VisualElement {name = "content"};
            center.Add(view);

            row.Add(new Text(label) {name = "label"});
            row.Add(center);
            row.Add(new ActionButton(useAction) {name = "use", label = "Use"});

            if (!view.enabledSelf)
                row.SetEnabled(false);

            return row;
        }

        void Use(IOperator op)
        {
            m_CurrentModel.UpdateOperators(op.Clone());
            m_Dismiss?.Invoke();
        }

        void UseAll()
        {
            m_CurrentModel.UpdateOperators(m_Artifact.CloneOperators().ToArray(), true);
            m_Dismiss?.Invoke();
        }
    }
}
