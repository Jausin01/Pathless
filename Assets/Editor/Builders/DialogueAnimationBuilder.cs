using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the two Animator Controllers used by the dialogue UI (following the project rule that
    /// animations are done with the Animator):
    ///  - DialogueBox.controller: the box slides up from below the screen (Closed -> Open -> Closing -> Closed),
    ///    driven by the bool parameter "IsOpen".
    ///  - DialogueIndicator.controller: the "continue" arrow is hidden (Hidden) or bounces (Bounce),
    ///    driven by the bool parameter "IsWaiting".
    /// Safe to run again: the generated assets are replaced.
    /// </summary>
    public static class DialogueAnimationBuilder
    {
        /// <summary>Folder where the clips and controllers are saved.</summary>
        private const string Folder = "Assets/Animations/Dialogue";

        /// <summary>Path of the box controller (used by DialogueUiBuilder).</summary>
        public const string BoxControllerPath = Folder + "/DialogueBox.controller";

        /// <summary>Path of the arrow controller (used by DialogueUiBuilder).</summary>
        public const string IndicatorControllerPath = Folder + "/DialogueIndicator.controller";

        /// <summary>Path of the name plate flame controller (used by DialogueUiBuilder).</summary>
        public const string FlameControllerPath = Folder + "/DialogueFlame.controller";

        /// <summary>Y position (canvas pixels) of the panel when the box is visible.</summary>
        public const float OpenY = 8f;

        /// <summary>Y position (canvas pixels) of the panel when the box is hidden below the screen.</summary>
        public const float ClosedY = -152f;

        /// <summary>Duration in seconds of the slide-in.</summary>
        private const float OpenDuration = 0.12f;

        /// <summary>Duration in seconds of the slide-out.</summary>
        private const float CloseDuration = 0.10f;

        /// <summary>Names of every clip created by this builder.</summary>
        private static readonly string[] ClipNames = { "Box_Closed", "Box_Open", "Box_Close", "Arrow_Hidden", "Arrow_Bounce", "Flame_Flicker" };

        /// <summary>Menu entry to rebuild only the dialogue animations.</summary>
        [MenuItem("Tools/Pathless/Dialogue/Build Dialogue Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the clips and both controllers.</summary>
        /// <returns>True when everything was created.</returns>
        public static bool Build()
        {
            EditorAssetUtility.EnsureFolder(Folder);
            DeleteOldAssets();

            BuildBoxController();
            BuildIndicatorController();
            BuildFlameController();

            // Curves added with SetEditorCurve do not always mark the clip as changed, so without this the
            // clips were saved EMPTY (no movement, the box never appeared). Mark every clip dirty explicitly.
            foreach (string clipName in ClipNames)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Folder}/{clipName}.anim");
                EditorUtility.SetDirty(clip);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("DialogueAnimationBuilder: controllers saved in " + Folder);
            return true;
        }

        /// <summary>Removes the previously generated assets so the build starts clean.</summary>
        private static void DeleteOldAssets()
        {
            AssetDatabase.DeleteAsset(BoxControllerPath);
            AssetDatabase.DeleteAsset(IndicatorControllerPath);
            AssetDatabase.DeleteAsset(FlameControllerPath);
            foreach (string clip in ClipNames)
            {
                AssetDatabase.DeleteAsset($"{Folder}/{clip}.anim");
            }
        }

        /// <summary>Creates the box clips and the Closed/Open/Closing state machine.</summary>
        private static void BuildBoxController()
        {
            EditorCurveBinding positionY = EditorCurveBinding.FloatCurve("", typeof(RectTransform), "m_AnchoredPosition.y");

            AnimationClip closed = CreateClip("Box_Closed", false);
            AnimationUtility.SetEditorCurve(closed, positionY, AnimationCurve.Constant(0f, 0f, ClosedY));

            AnimationClip open = CreateClip("Box_Open", false);
            AnimationUtility.SetEditorCurve(open, positionY, AnimationCurve.EaseInOut(0f, ClosedY, OpenDuration, OpenY));

            AnimationClip closing = CreateClip("Box_Close", false);
            AnimationUtility.SetEditorCurve(closing, positionY, AnimationCurve.EaseInOut(0f, OpenY, CloseDuration, ClosedY));

            Commit(closed);
            Commit(open);
            Commit(closing);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BoxControllerPath);
            controller.AddParameter("IsOpen", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState closedState = machine.AddState("Closed");
            closedState.motion = closed;
            AnimatorState openState = machine.AddState("Open");
            openState.motion = open;
            AnimatorState closingState = machine.AddState("Closing");
            closingState.motion = closing;
            machine.defaultState = closedState;

            // Closed -> Open as soon as IsOpen becomes true.
            AddConditionTransition(closedState, openState, "IsOpen", true);

            // Open -> Closing as soon as IsOpen becomes false.
            AddConditionTransition(openState, closingState, "IsOpen", false);

            // A new dialogue may start while the box is still closing: go back to Open.
            AddConditionTransition(closingState, openState, "IsOpen", true);

            // Closing -> Closed when the slide-out clip has finished.
            AnimatorStateTransition finished = closingState.AddTransition(closedState);
            finished.hasExitTime = true;
            finished.exitTime = 1f;
            finished.duration = 0f;

            EditorUtility.SetDirty(controller);
        }

        /// <summary>Creates the arrow clips and the Hidden/Bounce state machine.</summary>
        private static void BuildIndicatorController()
        {
            EditorCurveBinding enabled = EditorCurveBinding.FloatCurve("", typeof(Image), "m_Enabled");
            EditorCurveBinding positionY = EditorCurveBinding.FloatCurve("", typeof(RectTransform), "m_AnchoredPosition.y");

            AnimationClip hidden = CreateClip("Arrow_Hidden", false);
            AnimationUtility.SetEditorCurve(hidden, enabled, AnimationCurve.Constant(0f, 0f, 0f));

            // A 0.5 second loop: the arrow moves 2 pixels down and back up, and stays visible.
            AnimationClip bounce = CreateClip("Arrow_Bounce", true);
            AnimationUtility.SetEditorCurve(bounce, enabled, AnimationCurve.Constant(0f, 0.5f, 1f));
            AnimationCurve bobbing = new AnimationCurve(
                new Keyframe(0f, 12f, 0f, 0f),
                new Keyframe(0.25f, 10f, 0f, 0f),
                new Keyframe(0.5f, 12f, 0f, 0f));
            AnimationUtility.SetEditorCurve(bounce, positionY, bobbing);

            Commit(hidden);
            Commit(bounce);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(IndicatorControllerPath);
            controller.AddParameter("IsWaiting", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState hiddenState = machine.AddState("Hidden");
            hiddenState.motion = hidden;
            AnimatorState bounceState = machine.AddState("Bounce");
            bounceState.motion = bounce;
            machine.defaultState = hiddenState;

            AddConditionTransition(hiddenState, bounceState, "IsWaiting", true);
            AddConditionTransition(bounceState, hiddenState, "IsWaiting", false);

            EditorUtility.SetDirty(controller);
        }

        /// <summary>
        /// Creates the flame flicker: one looping state that squashes and stretches the flame in two steps
        /// (like the CSS steps(2) flicker of the design), so the flame looks alive next to the name.
        /// </summary>
        private static void BuildFlameController()
        {
            AnimationClip flicker = CreateClip("Flame_Flicker", true);

            // Constant (stepped) keys: the flame jumps between two shapes instead of blending.
            AnimationUtility.SetEditorCurve(flicker, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.x"),
                Stepped(1f, 0.9f));
            AnimationUtility.SetEditorCurve(flicker, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale.y"),
                Stepped(1f, 1.18f));

            Commit(flicker);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(FlameControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("Flicker");
            state.motion = flicker;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
        }

        /// <summary>Builds a 0.44 second looping curve that holds value A, then value B, then A again.</summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>The stepped curve.</returns>
        private static AnimationCurve Stepped(float a, float b)
        {
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, a), new Keyframe(0.22f, b), new Keyframe(0.44f, a));
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }

            return curve;
        }

        /// <summary>Creates an animation clip asset in the dialogue animation folder.</summary>
        /// <param name="clipName">File name without extension.</param>
        /// <param name="loop">True if the clip repeats forever.</param>
        /// <returns>The created clip.</returns>
        private static AnimationClip CreateClip(string clipName, bool loop)
        {
            // The clip is NOT saved yet: curves are added first and Commit() saves it afterwards.
            // (Adding curves to a clip that was already saved as an asset left the files empty.)
            AnimationClip clip = new AnimationClip { frameRate = 60f, name = clipName };

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        /// <summary>Saves a finished clip (curves already added) as an asset in the dialogue animation folder.</summary>
        /// <param name="clip">The clip created by CreateClip.</param>
        private static void Commit(AnimationClip clip)
        {
            AssetDatabase.CreateAsset(clip, $"{Folder}/{clip.name}.anim");
            EditorUtility.SetDirty(clip);
        }

        /// <summary>Adds an instant transition that fires when a bool parameter has the given value.</summary>
        /// <param name="from">Source state.</param>
        /// <param name="to">Destination state.</param>
        /// <param name="parameter">Bool parameter name.</param>
        /// <param name="value">Value that triggers the transition.</param>
        private static void AddConditionTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }
    }
}
