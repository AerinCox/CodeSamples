using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*
    Description:
    This will display a new Material every X seconds, selecting from the list of materials provided.
    It is network synced.
*/
public class MaterialCycler : NetworkBehaviour {
    [Space (10, order = 0)]
    [Header ("Required Settings", order = 1)]
    public List<Material> materials;
    public int secondsBetweenSwap;
    public bool playMaterialsInOrder = false; // If FALSE, will randomly choose from the list of materials. If TRUE, will play materials in order.

    [Space (10, order = 0)]
    [Header ("Optional: Material Index (Default is 0)", order = 1)]
    public int matIndex = 0;

    private Renderer meshRenderer;
    private float timePassed = 0f;
    private List<int> possibleIndexes;

    [SyncVar] int syncedMaterialIndex = 0;
    private int currMaterialIndex = 0;

    private Material[] tempMatArray; // Note about this: You can't directly change the elements inside of the MeshRenderer's material array. But you can swap out the entire array. So we modify this array's elements locally and then swap the MeshRenderer's material array.

    void Start () {
        if (materials.Count <= 1) {
            FLLog.Error ("MaterialCycler has 0 or 1 materials in its list on {0}. That doesn't make sense! Disabling it.", this.name);
            this.enabled = false;
        }
        this.meshRenderer = gameObject.GetComponent<MeshRenderer> ();
        if (meshRenderer == null) {
            FLLog.Error ("MaterialCycler couldn't find a mesh renderer on {0}. Disabling it.", this.name);
            this.enabled = false;
        }
        if(meshRenderer.sharedMaterials.Length <= matIndex){
            FLLog.Error("The material index in MaterialCycler for '{0}' was too big in comparison to the amount of materials in the mesh renderer. Defaulting the index to 0.", this.name);
            matIndex = 0;
        }

        if (!this.playMaterialsInOrder) {
            this.possibleIndexes = new List<int>();
            for (int i = 0; i < materials.Count; i++) {
                possibleIndexes.Add (i);
            }
        }
        tempMatArray = meshRenderer.materials;
    }

    void Update () {
        // Client Side //
        if (!FactSheet.sharedInstance[YesNoFacts.IsVREnabled] && !Application.isEditor) {
            if(currMaterialIndex != syncedMaterialIndex){
                currMaterialIndex = syncedMaterialIndex;
                tempMatArray[matIndex] = materials[currMaterialIndex];
                meshRenderer.materials = tempMatArray;
            }
        }
        // Server or Editor Side //
        else if (timePassed > secondsBetweenSwap) {
            timePassed = 0f;
            if (this.playMaterialsInOrder) {
                this.currMaterialIndex++;
                if(currMaterialIndex == this.materials.Count){
                    currMaterialIndex = 0;
                }
                syncedMaterialIndex = currMaterialIndex;
                tempMatArray[matIndex] = materials[currMaterialIndex];
                meshRenderer.materials = tempMatArray;
            } else {
                // If randomly selecting materials, we don't want to choose the Index of the Material that's currently being displayed. So we use possibleIndexes to keep track of that.
                int someIndex = UnityEngine.Random.Range(0, possibleIndexes.Count);
                int prevIndex = currMaterialIndex;
                tempMatArray[matIndex] = materials[possibleIndexes[someIndex]];
                meshRenderer.materials = tempMatArray;
                currMaterialIndex = possibleIndexes[someIndex];
                possibleIndexes.RemoveAt (someIndex);
                possibleIndexes.Add (prevIndex);
                syncedMaterialIndex = currMaterialIndex;
            }
        } else {
            timePassed += Time.deltaTime;
        }
    }
}