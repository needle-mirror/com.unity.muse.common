using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Muse.Common.Editor
{
    internal class AssetDeleteWatcher : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Model>(assetPath);
            if (asset != null)
            {
                var editors = Resources.FindObjectsOfTypeAll<MuseEditor>().Where(w => w.CurrentModel == asset).ToArray();
                if (editors.Length > 0)
                {
                    if (EditorUtility.DisplayDialog(TextContent.assetRemovedFromProjectTitle, string.Format(TextContent.assetRemovedFromProjectMessage, asset.name), TextContent.assetSaveAs, TextContent.discardAndClose))
                    {
                        var savePath = EditorUtility.SaveFilePanel(TextContent.saveGeneratorAsset, Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath), "asset");
                        if (!string.IsNullOrEmpty(savePath) && savePath.StartsWith(Application.dataPath))
                        {
                            savePath = "Assets" + savePath.Substring(Application.dataPath.Length);

                            AssetDatabase.MoveAsset(assetPath, savePath);

                            foreach (var museEditor in editors)
                                museEditor.AssetMoved(savePath);

                            return AssetDeleteResult.DidDelete;
                        }

                        return AssetDeleteResult.DidDelete;
                    }
                }
                foreach (var museEditor in editors)
                    museEditor.Close();

                ArtifactCache.Delete(asset.AssetsData);
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
