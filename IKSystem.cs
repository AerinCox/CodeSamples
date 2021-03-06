using System.Collections.Generic;
using UnityEngine;

//****** Unity Component UI Code ******************************************/
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor (typeof (IKSystem))]
public class IKSystemEditor : Editor {

    private IKSystem source;
    private int bodyPartSelection;
    private int onOffSelection;
    private int trackSelection;

    public override void OnInspectorGUI () {
        source = target as IKSystem;

        source.nameOfTarget = EditorGUILayout.TextField ("Name of In-Scene Target", source.nameOfTarget);
        source.offset = EditorGUILayout.Vector3Field ("Offset", source.offset);

        GUILayout.Label ("IK Target");
        string[] bodyParts = new string[] { "  Head", "  Left Hand", " Right Hand" };
        bodyPartSelection = (int) source.bodyPart;
        bodyPartSelection = GUILayout.SelectionGrid (bodyPartSelection, bodyParts, 1, EditorStyles.radioButton);
        source.bodyPart = (IKSystem.BodyPart) bodyPartSelection;

        source.speed = EditorGUILayout.Slider ("Speed", source.speed, 0f, 20f);

        GUILayout.Label ("Turn IK ON/OFF");
        string[] onOff = new string[] { "  ON", "  OFF" };
        onOffSelection = source.shouldActivateIK ? 0 : 1;
        onOffSelection = GUILayout.SelectionGrid (onOffSelection, onOff, 1, EditorStyles.radioButton);
        source.shouldActivateIK = onOffSelection == 0 ? true : false;

        source.shouldContinuouslyTrack =  EditorGUILayout.Toggle("Should Continuously Track Target", source.shouldContinuouslyTrack);
    }
}
#endif

//****** IKSystem Code *************************************************/
/*
    Description:
    This is a IK System for NPC animators. The default Unity IK system does not allow for smooth transitions between new IK targets.
    IKSystem utilizes in-scene gameobjects called "IKRail"s as IK targets. So instead of a body part targeting a new IK target, the part will always target the IKRail, and the IKRail moves/teleports around.

    Since the IKSystem needs to know the activity/location of the IKRails across multiple animationController nodes,
    and we need multiple characters to be able to look/point at once,
    individual IKRails are stored in a global dictionary called 'IKRails' utilizing the character's unique instanceID.
*/
public class IKSystem : StateMachineBehaviour {
    // Global Variables //
    protected static Dictionary<int, IKRail> IKRails;

    protected class IKRail {
        public Transform headIKRail;
        public Transform lHandIKRail;
        public Transform rHandIKRail;
        public bool headIKActive = false;
        public bool lHandIKActive = false;
        public bool rHandIKActive = false;
    }

    // Instance Variables //
    public enum BodyPart {
        Head = 0,
        LeftHand = 1,
        RightHand = 2,
    }

    public string nameOfTarget = ""; // Name of object in the scene that our IK will point to.
    public float speed = 10; // How fast are we pointing? Speed 10 --> 1 second to point. Speed 1 --> 0.1 second to point.
    public Vector3 offset = Vector3.zero;
    public BodyPart bodyPart = BodyPart.Head;
    public bool shouldActivateIK = false; // Are we turning this IK System on or off?
    public bool shouldContinuouslyTrack = false; // If the target moves, should the ik move with it?

    private int myID; // ID of the NPC we're attached to. Used for the Global Dictionary.
    private GameObject target; // Gameobject of the in-scene object we're pointing at.
    private Vector3 targetPosition; // Position of the in-scene object we're pointing at.
    private bool wasIKActiveOnEntry; // What was the "IKActive" status in the global dictionary when we entered this instance? On or off?
    private float weightDelta = 0f;
    private Vector3 startRailPosition;

    override public void OnStateEnter (Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (shouldActivateIK) {
            // Finding our Target in the scene.
            if (nameOfTarget.ToLower () == "player" || nameOfTarget.ToLower () == "player container" || nameOfTarget.ToLower () == "player(clone)") {
                target = GameObject.FindGameObjectWithTag ("Player");
                if (target == null) {
                    target = GameObject.FindGameObjectWithTag ("MainCamera");
                }
            } else {
                target = GameObject.Find (nameOfTarget);
            }
            if (target == null) {
                FLLog.Error ("Could not find object named {0} for IKSystem in {1}. Destroying IK system.", nameOfTarget, this.name);
                Destroy (this);
            } else {
                targetPosition = target.transform.position + offset;
            }
        }

        // Finding this character's ID
        this.myID = animator.transform.gameObject.GetInstanceID ();

        // Creating + Adding this character's IK Rails to the Global Dictionary if we haven't already.
        if (IKRails == null) {
            IKRails = new Dictionary<int, IKRail> ();
        }
        if (!IKRails.ContainsKey (this.myID)) {
            IKRails.Add (this.myID, new IKRail ());
        }

        // Setting up the IKRail's active status and position.
        switch (bodyPart) {
            case (BodyPart.Head):
                if (IKRails[myID].headIKRail == null) {
                    IKRails[myID].headIKRail = new GameObject ("HeadIKRail_" + myID).transform;
                }
                wasIKActiveOnEntry = IKRails[myID].headIKActive;
                IKRails[myID].headIKActive = shouldActivateIK;
                if (wasIKActiveOnEntry) {
                    startRailPosition = IKRails[myID].headIKRail.position;
                } else if (shouldActivateIK) {
                    IKRails[myID].headIKRail.position = targetPosition;
                }
                break;
            case (BodyPart.LeftHand):
                if (IKRails[myID].lHandIKRail == null) {
                    IKRails[myID].lHandIKRail = new GameObject ("LeftHandIKRail_" + myID).transform;
                }
                wasIKActiveOnEntry = IKRails[myID].lHandIKActive;
                IKRails[myID].lHandIKActive = shouldActivateIK;
                if (wasIKActiveOnEntry) {
                    startRailPosition = IKRails[myID].lHandIKRail.position;
                } else if (shouldActivateIK) {
                    IKRails[myID].lHandIKRail.position = targetPosition;
                }
                break;
            case (BodyPart.RightHand):
                if (IKRails[myID].rHandIKRail == null) {
                    IKRails[myID].rHandIKRail = new GameObject ("RightHandIKRail_" + myID).transform;
                }
                wasIKActiveOnEntry = IKRails[myID].rHandIKActive;
                IKRails[myID].rHandIKActive = shouldActivateIK;
                if (wasIKActiveOnEntry) {
                    startRailPosition = IKRails[myID].rHandIKRail.position;
                } else if (shouldActivateIK) {
                    IKRails[myID].rHandIKRail.position = targetPosition;
                }
                break;
            default:
                FLLog.Error ("Something is very wrong in IKSystem..");
                break;
        }

        // Refreshing Variables
        weightDelta = shouldActivateIK ? 0f : 1f;
    }

    override public void OnStateIK (Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (shouldContinuouslyTrack) {
            targetPosition = target.transform.position + offset;
        }

        // Case 0: Component says turn IK OFF and our Rail was active. Turn IK System off and transition the NPC part to its original position.
        if (!this.shouldActivateIK && wasIKActiveOnEntry) {
            if (weightDelta <= 0) {
                return;
            }
            switch (bodyPart) {
                case (BodyPart.Head):
                    weightDelta -= (Time.deltaTime / 10) * speed;
                    animator.SetLookAtPosition (IKRails[myID].headIKRail.position);
                    animator.SetLookAtWeight (weightDelta, 0, 1, 1, 1);
                    break;
                case (BodyPart.LeftHand):
                    weightDelta -= (Time.deltaTime / 10) * speed;
                    animator.SetIKPosition (AvatarIKGoal.LeftHand, IKRails[myID].lHandIKRail.position);
                    animator.SetIKPositionWeight (AvatarIKGoal.LeftHand, weightDelta);
                    break;
                case (BodyPart.RightHand):
                    weightDelta -= (Time.deltaTime / 10) * speed;
                    animator.SetIKPosition (AvatarIKGoal.RightHand, IKRails[myID].rHandIKRail.position);
                    animator.SetIKPositionWeight (AvatarIKGoal.RightHand, weightDelta);
                    break;
                default:
                    FLLog.Error ("Something is very wrong in IKSystem..");
                    break;
            }
            return;
        }
        // Case 1: Component says turn IK OFF and our Rail was inactive. Do nothing!
        else if (!this.shouldActivateIK) {
            return;
        }
        // Case 2: Component says turn IK ON. Rail system was active on entry. Keep IK at MAX weight, and lerp IKRail to target.
        if (wasIKActiveOnEntry) {
            switch (bodyPart) {
                case (BodyPart.Head):
                    weightDelta += (Time.deltaTime / 10) * speed;
                    IKRails[myID].headIKRail.position = Vector3.Lerp (startRailPosition, targetPosition, weightDelta);
                    animator.SetLookAtPosition (IKRails[myID].headIKRail.position);
                    animator.SetLookAtWeight (1, 0, 1, 1, 0.5f);
                    break;
                case (BodyPart.LeftHand):
                    weightDelta += (Time.deltaTime / 10) * speed;
                    IKRails[myID].lHandIKRail.position = Vector3.Lerp (startRailPosition, targetPosition, weightDelta);
                    animator.SetIKPosition (AvatarIKGoal.LeftHand, IKRails[myID].lHandIKRail.position);
                    animator.SetIKPositionWeight (AvatarIKGoal.LeftHand, 1);
                    break;
                case (BodyPart.RightHand):
                    weightDelta += (Time.deltaTime / 10) * speed;
                    IKRails[myID].rHandIKRail.position = Vector3.Lerp (startRailPosition, targetPosition, weightDelta);
                    animator.SetIKPosition (AvatarIKGoal.RightHand, IKRails[myID].rHandIKRail.position);
                    animator.SetIKPositionWeight (AvatarIKGoal.RightHand, 1);
                    break;
                default:
                    FLLog.Error ("Something is very wrong in IKSystem..");
                    break;
            }
        }
        // Case 3: Component says Turn IK ON. Our rail was inactive on entry. Teleported IKRail object to target (in OnStateEnter), and then lerp IK here.
        else {
            switch (bodyPart) {
                case (BodyPart.Head):
                    animator.SetLookAtPosition (IKRails[myID].headIKRail.position);
                    weightDelta += (Time.deltaTime / 10) * speed;
                    animator.SetLookAtWeight (weightDelta, 0, 1, 1, 0.5f);
                    break;
                case (BodyPart.LeftHand):
                    animator.SetIKPosition (AvatarIKGoal.LeftHand, IKRails[myID].lHandIKRail.position);
                    weightDelta += (Time.deltaTime / 10) * speed;
                    animator.SetIKPositionWeight (AvatarIKGoal.LeftHand, weightDelta);
                    break;
                case (BodyPart.RightHand):
                    animator.SetIKPosition (AvatarIKGoal.RightHand, IKRails[myID].rHandIKRail.position);
                    weightDelta += (Time.deltaTime / 10) * speed;
                    animator.SetIKPositionWeight (AvatarIKGoal.RightHand, weightDelta);
                    break;
                default:
                    FLLog.Error ("Something is very wrong in IKSystem..");
                    break;
            }
        }
    }

}