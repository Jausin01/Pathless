using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the two Animator Controllers of the menu (animations are done with the Animator, per the project rules):
    ///  - MenuPanel.controller: the panel slides in from the right edge and back out, driven by the bool "IsOpen"
    ///    (states Closed -> Open -> Closing -> Closed).
    ///  - MenuCursor.controller: the selection cursor bounces left and right in a loop.
    /// Clips are fully built before they are saved (saving first and adding curves later left clips empty before).
    /// Safe to run again: the generated assets are replaced.
    /// </summary>
    public static class MenuAnimationBuilder
    {
        /// <summary>Folder where the clips and controllers are saved.</summary>
        private const string Folder = "Assets/Animations/Menu";

        /// <summary>Path of the panel controller (used by MenuUiBuilder).</summary>
        public const string PanelControllerPath = Folder + "/MenuPanel.controller";

        /// <summary>Path of the cursor controller (used by MenuUiBuilder).</summary>
        public const string CursorControllerPath = Folder + "/MenuCursor.controller";

        /// <summary>Panel X position (canvas pixels, anchored to the right edge) when the menu is visible.</summary>
        public const float OpenX = -14f;

        /// <summary>Panel X position when the menu is hidden beyond the right edge of the screen.</summary>
        public const float ClosedX = 180f;

        /// <summary>Resting X position of the cursor inside its row (the bounce moves it a few pixels right).</summary>
        public const float CursorRestX = 4f;

        /// <summary>Duration in seconds of the slide-in.</summary>
        private const float OpenDuration = 0.14f;

        /// <summary>Duration in seconds of the slide-out.</summary>
        private const float CloseDuration = 0.10f;

        /// <summary>Menu entry to rebuild only the menu animations.</summary>
        [MenuItem("Tools/Pathless/Menu/Build Menu Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the clips and both controllers.</summary>
        /// <returns>True when everything was created.</returns>
        public static bool Build()
        {
            EditorAssetUtility.EnsureFolder(Folder);
            foreach (string name in new[] { "Menu_Closed", "Menu_Open", "Menu_Close", "Cursor_Bounce" })
            {
                AssetDatabase.DeleteAsset($"{Folder}/{name}.anim");
            }

            AssetDatabase.DeleteAsset(PanelControllerPath);
            AssetDatabase.DeleteAsset(CursorControllerPath);

            BuildPanelController();
            BuildCursorController();

            AssetDatabase.SaveAssets();
            Debug.Log("MenuAnimationBuilder: controllers saved in " + Folder);
            return true;
        }

        /// <summary>Creates the panel clips and the Closed/Open/Closing state machine.</summary>
        private static void BuildPanelController()
        {
            EditorCurveBinding positionX = EditorCurveBinding.FloatCurve("", typeof(RectTransform), "m_AnchoredPosition.x");

            AnimationClip closed = NewClip("Menu_Closed", false);
            AnimationUtility.SetEditorCurve(closed, positionX, AnimationCurve.Constant(0f, 0f, ClosedX));
            Save(closed);

            AnimationClip open = NewClip("Menu_Open", false);
            AnimationUtility.SetEditorCurve(open, positionX, AnimationCurve.EaseInOut(0f, ClosedX, OpenDuration, OpenX));
            Save(open);

            AnimationClip closing = NewClip("Menu_Close", false);
            AnimationUtility.SetEditorCurve(closing, positionX, AnimationCurve.EaseInOut(0f, OpenX, CloseDuration, ClosedX));
            Save(closing);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(PanelControllerPath);
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

            // Closing -> Closed when the slide-out has finished.
            AnimatorStateTransition finished = closingState.AddTransition(closedState);
            finished.hasExitTime = true;
            finished.exitTime = 1f;
            finished.duration = 0f;

            EditorUtility.SetDirty(controller);
        }

        /// <summary>Creates the looping cursor bounce (a 0.9 second loop that moves the cursor 3 pixels right and back).</summary>
        private static void BuildCursorController()
        {
            AnimationClip bounce = NewClip("Cursor_Bounce", true);
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, CursorRestX),
                new Keyframe(0.45f, CursorRestX + 3f),
                new Keyframe(0.9f, CursorRestX));
            AnimationUtility.SetEditorCurve(bounce, EditorCurveBinding.FloatCurve("", typeof(RectTransform), "m_AnchoredPosition.x"), curve);
            Save(bounce);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(CursorControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("Bounce");
            state.motion = bounce;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
        }

        /// <summary>Creates an in-memory clip (not saved yet).</summary>
        /// <param name="clipName">Clip name (also the file name).</param>
        /// <param name="loop">True if the clip repeats forever.</param>
        /// <returns>The new clip.</returns>
        private static AnimationClip NewClip(string clipName, bool loop)
        {
            AnimationClip clip = new AnimationClip { frameRate = 60f, name = clipName };
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
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

        /// <summary>Adds an instant transition that fires when the IsOpen bool has the given value.</summary>
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
