using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the Animator Controller shared by the full menu screens (Status, Quests...): the whole screen fades
    /// in and out through the CanvasGroup alpha, driven by the bool "IsOpen" (states Closed -> Open -> Closing -> Closed).
    /// Animations are done with the Animator, per the project rules. Clips are fully built before they are saved
    /// (saving first and adding curves later left clips empty before). Safe to run again: the assets are replaced.
    /// </summary>
    public static class MenuScreenAnimationBuilder
    {
        /// <summary>Folder where the clips and controller are saved.</summary>
        private const string Folder = "Assets/Animations/Menu";

        /// <summary>Path of the controller (used by MenuScreensBuilder).</summary>
        public const string ControllerPath = Folder + "/MenuScreen.controller";

        /// <summary>Duration in seconds of the fade-in.</summary>
        private const float OpenDuration = 0.15f;

        /// <summary>Duration in seconds of the fade-out.</summary>
        private const float CloseDuration = 0.10f;

        /// <summary>Menu entry to rebuild only this controller.</summary>
        [MenuItem("Tools/Pathless/Menu/Build Menu Screen Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the clips and the controller.</summary>
        /// <returns>True when everything was created.</returns>
        public static bool Build()
        {
            EditorAssetUtility.EnsureFolder(Folder);
            foreach (string name in new[] { "Screen_Closed", "Screen_Open", "Screen_Close" })
            {
                AssetDatabase.DeleteAsset($"{Folder}/{name}.anim");
            }

            AssetDatabase.DeleteAsset(ControllerPath);

            EditorCurveBinding alpha = EditorCurveBinding.FloatCurve("", typeof(CanvasGroup), "m_Alpha");

            AnimationClip closed = NewClip("Screen_Closed");
            AnimationUtility.SetEditorCurve(closed, alpha, AnimationCurve.Constant(0f, 0f, 0f));
            Save(closed);

            AnimationClip open = NewClip("Screen_Open");
            AnimationUtility.SetEditorCurve(open, alpha, AnimationCurve.EaseInOut(0f, 0f, OpenDuration, 1f));
            Save(open);

            AnimationClip closing = NewClip("Screen_Close");
            AnimationUtility.SetEditorCurve(closing, alpha, AnimationCurve.EaseInOut(0f, 1f, CloseDuration, 0f));
            Save(closing);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("IsOpen", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState closedState = machine.AddState("Closed");
            closedState.motion = closed;
            AnimatorState openState = machine.AddState("Open");
            openState.motion = open;
            AnimatorState closingState = machine.AddState("Closing");
            closingState.motion = closing;
            machine.defaultState = closedState;

            AddBoolTransition(closedState, openState, true);
            AddBoolTransition(openState, closingState, false);
            AddBoolTransition(closingState, openState, true);

            AnimatorStateTransition finished = closingState.AddTransition(closedState);
            finished.hasExitTime = true;
            finished.exitTime = 1f;
            finished.duration = 0f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("MenuScreenAnimationBuilder: controller saved at " + ControllerPath);
            return true;
        }

        /// <summary>Creates an in-memory, non-looping clip (not saved yet).</summary>
        /// <param name="clipName">Clip name (also the file name).</param>
        /// <returns>The new clip.</returns>
        private static AnimationClip NewClip(string clipName)
        {
            AnimationClip clip = new AnimationClip { frameRate = 60f, name = clipName };
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        /// <summary>Saves a finished clip as an asset.</summary>
        /// <param name="clip">The clip with its curves already added.</param>
        private static void Save(AnimationClip clip)
        {
            AssetDatabase.CreateAsset(clip, $"{Folder}/{clip.name}.anim");
            EditorUtility.SetDirty(clip);
        }

        /// <summary>Adds an instant transition that fires when IsOpen has the given value.</summary>
        /// <param name="from">Source state.</param>
        /// <param name="to">Destination state.</param>
        /// <param name="value">Value of IsOpen that triggers it.</param>
        private static void AddBoolTransition(AnimatorState from, AnimatorState to, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsOpen");
        }
    }
}
