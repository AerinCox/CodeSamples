using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*
    Description:
    This will display a new texture every X seconds, selecting from the list of textures provided.
    It is network synced.
*/
public class TextureCycler : NetworkBehaviour {
    [Space (10, order = 0)]
    [Header ("Required Settings", order = 1)]
    public List<Material> textures;
    public int secondsBetweenSwap;
    public bool playTexturesInOrder = false; // If FALSE, will randomly choose from the list of textures. If TRUE, will play textures in order.

    [Space (10, order = 0)]
    [Header ("Optional: Material Index (Default is 0)", order = 1)]
    public int meshRendererMaterialIndex = 0;

    private Renderer meshRenderer;
    private float timePassed = 0f;
    private List<int> possibleIndexes;

    [SyncVar] int syncedTextureIndex = 0;
    private int currTextureIndex = 0;

    private Material[] materialArray; // Note about this: You can't directly change the elements inside of the MeshRenderer's material array. But you can swap out the entire array. So we modify this array's elements locally and then swap the MeshRenderer's material array.

    void Start () {
        if (textures.Count <= 1) {
            FLLog.Error ("TextureCycler has 0 or 1 textures in its list on {0}. That doesn't make sense! Disabling it.", this.name);
            this.enabled = false;
        }
        this.meshRenderer = gameObject.GetComponent<MeshRenderer> ();
        if (meshRenderer == null) {
            FLLog.Error ("TextureCycler couldn't find a mesh renderer on {0}. Disabling it.", this.name);
            this.enabled = false;
        }
        if(meshRenderer.sharedMaterials.Length <= meshRendererMaterialIndex){
            FLLog.Error("The material index in TextureCycler for '{0}' was too big in comparison to the amount of materials in the mesh renderer. Defaulting the index to 0.", this.name);
            meshRendererMaterialIndex = 0;
        }

        if (!this.playTexturesInOrder) {
            this.possibleIndexes = new List<int>();
            for (int i = 0; i < textures.Count; i++) {
                possibleIndexes.Add (i);
            }
        }
        materialArray = meshRenderer.materials;
    }

    void Update () {
        // Client Side //
        if (!FactSheet.sharedInstance[YesNoFacts.IsVREnabled] && !Application.isEditor) {
            if(currTextureIndex != syncedTextureIndex){
                currTextureIndex = syncedTextureIndex;
                materialArray[meshRendererMaterialIndex] = textures[currTextureIndex];
                meshRenderer.materials = materialArray;
            }
        }
        // Server or Editor Side //
        else if (timePassed > secondsBetweenSwap) {
            timePassed = 0f;
            if (this.playTexturesInOrder) {
                this.currTextureIndex++;
                if(currTextureIndex == this.textures.Count){
                    currTextureIndex = 0;
                }
                syncedTextureIndex = currTextureIndex;
                materialArray[meshRendererMaterialIndex] = textures[currTextureIndex];
                meshRenderer.materials = materialArray;
            } else {
                // If randomly selecting textures, we don't want to choose the Index of the texture that's currently being displayed. So we use possibleIndexes to keep track of that.
                int someIndex = UnityEngine.Random.Range(0, possibleIndexes.Count);
                int prevIndex = currTextureIndex;
                materialArray[meshRendererMaterialIndex] = textures[possibleIndexes[someIndex]];
                meshRenderer.materials = materialArray;
                currTextureIndex = possibleIndexes[someIndex];
                possibleIndexes.RemoveAt (someIndex);
                possibleIndexes.Add (prevIndex);
                syncedTextureIndex = currTextureIndex;
            }
        } else {
            timePassed += Time.deltaTime;
        }
    }
}