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
        public static readonly string deleteDialogOkDontShowAgain = "Delete, and don't ask again";
        public static readonly string exportSingle = "Export";
        public static readonly string exportMultiple = "Export All";
        public static readonly string deleteSingle = "Delete";
        public static readonly string deleteMultiple = "Delete All";
        public static readonly string starMultiple = "Star All";
        public static readonly string unStarMultiple = "Remove All Stars";
        public static readonly string thumbnailSizeSliderTooltip = "Adjust thumbnails size";
        public static readonly string assetRemovedFromProjectTitle = "Muse Generator asset removed";
        public static readonly string assetRemovedFromProjectMessage = "The file '{0}' has been deleted or removed from the project folder.\nWould you like to save your generations?";
        public static readonly string assetSaveAs = "Save As...";
        public static readonly string discardAndClose = "Discard Generations and Close Window";
        public static readonly string saveGeneratorAsset = "Save Generator Asset";
        public static readonly string bookmarkTooltip = "Shows only starred artifacts";
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
    }
}
