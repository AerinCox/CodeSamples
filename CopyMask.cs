using UnityEditor;
using UnityEngine;

/*
    Description:
    Want to apply a avatar mask to multiple FBX files? Well this is the tool for you!
    Copy/paste the path of the mask you wish to use for copying into pathOfMaskObject and save.
    Select the .FBX objects in the project view that you'd like to copy-to.
    Run the tool.
*/
public class CopyMask {
    [MenuItem ("🌷ArtistTools/Copy Animation Masks %#f")]
    private static void CopyMasks () {

        string pathOfMaskObject = "Assets/somePathToAFile/MyFile.fbx"; // Put the path of the object that has the mask you want here!!

        ModelImporter copyThisMask = AssetImporter.GetAtPath (pathOfMaskObject) as ModelImporter;

        foreach (var obj in Selection.GetFiltered<Object> (SelectionMode.Assets)) {
            string selectedPath = AssetDatabase.GetAssetPath (obj);
            Debug.Log ("Copying mask at " + pathOfMaskObject + " to everything under: " + selectedPath);
            string[] guids = AssetDatabase.FindAssets ("t:AnimationClip", new [] { selectedPath });
            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath (guid);
                if (AssetImporter.GetAtPath (path) as ModelImporter != null) {
                    ModelImporter targetMI = AssetImporter.GetAtPath (path) as ModelImporter;

                    ModelImporterClipAnimation[] clipAnimations = targetMI.clipAnimations;

                    for (int i = 0; i < clipAnimations.Length; i++) {
                        Debug.Log ("Replacing Source Mask on: " + clipAnimations[i].name);
                        clipAnimations[i].maskType = ClipAnimationMaskType.CopyFromOther;
                        clipAnimations[i].maskSource = copyThisMask.clipAnimations[0].maskSource;
                    }

                    targetMI.clipAnimations = clipAnimations;
                    //Reimporting the asset.
                    AssetDatabase.ImportAsset (path);
                }
            }
        }
    }
}