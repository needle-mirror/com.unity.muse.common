namespace Unity.Muse.Common
{
    internal static class TextContent
    {
        public static string defaultAssetName(string modeTitle = "Muse") => $"New {modeTitle}";

        public static readonly string yes = "Yes";
        public static readonly string no = "No";
        public static readonly string cancel = "Cancel";
        public static readonly string savePopupTitle = "Unsaved changes";
        public static readonly string savePopupMessage = "There are some unsaved changes, would you like to save them?";
        public static readonly string deleteDialogTitle = "Delete Generations";
        public static readonly string deleteDialogMessage = "You are about to delete generated assets, would you like to continue?";
        public static readonly string deleteDialogOk = "Delete Selected";
        public static readonly string deleteDialogOkDontShowAgain = "Don't ask again";
        public static readonly string exportSingle = "Export";
        public static readonly string exportMultiple = "Export Selected";
        public static readonly string deleteSingle = "Delete";
        public static readonly string deleteMultiple = "Delete Selected";
        public static readonly string starSingle = "Star";
        public static readonly string unStarSingle = "Remove Star";
        public static readonly string starMultiple = "Star Selected";
        public static readonly string unStarMultiple = "Unstar Selected";
        public static readonly string refineSingle = "Refine";
        public static readonly string thumbnailSizeSliderTooltip = "Adjust thumbnails size";
        public static readonly string assetRemovedFromProjectTitle = "Delete selected generator?";
        public static readonly string assetRemovedFromProjectMessage = "'{0}'\n\nYou cannot undo the delete action.";
        public static readonly string saveGeneratorAsset = "Save Generator Asset";
        public static readonly string bookmarkTooltip = "Shows only starred artifacts";
        public static readonly string bookmarkButtonTooltip = "Add to favourites";
        public static readonly string operatorGenerateNumberTooltip = "Sets the number of images to be generated.";
        public static readonly string operatorPromptTooltip = "Enter the text to describe the things that you want to generate...";
        public static readonly string operatorNegativePromptTooltip = "Enter the text to describe the things you want to exclude...";
        public static readonly string saveTooltip = "Saves selected generation(s) into project.";
        public static readonly string refineTooltip = "Refines image in canvas.";
        public static readonly string undoTooltip = "Undo";
        public static readonly string redoTooltip = "Redo";
        public static readonly string backButtonTooltip = "Snaps back the Generations panel, hides the canvas, and exits the Refine mode.";
        public static readonly string promptPlaceholder = "Enter what to generate...";
        public static readonly string removeReference = "Remove Reference Image";
        public static readonly string signUpBetaDialogTitle = "Unity AI Beta Program";
        public static readonly string signUpBetaDialogMessage = "Would you like to register to the Unity AI Beta Program?";
        public static readonly string signUpBeta = "Register";
        public static readonly string savePanelTitle = "Save";
        public static readonly string savePanelMessage = "Save changes to an asset in the Project.";
        public static readonly string dragAndDropColorImageMessage = "Drag and Drop or Import an image to guide your generation";
        public static readonly string dragAndDropShapeImageMessage = "Drag and Drop or Import a <b>black and white <u><a href=\"https://en.wikipedia.org/wiki/Canny_edge_detector\">canny</a></u> image</b> to guide your generation";
        public static readonly string dislike = "Dislike";
        public static readonly string removeDislike = "Remove Dislike";
        public static readonly string dislikeTooltip = "Dislike this";
    }
}
