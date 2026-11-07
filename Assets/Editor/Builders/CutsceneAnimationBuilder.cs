using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the Animator Controller of the screen fade (animations are done with the Animator, per the project rules).
    /// States: Clear (transparent) -> FadeOut (alpha 0 to 1) -> Black -> FadeIn (alpha 1 to 0) -> Clear,
    /// driven by the bool "IsBlack". Every fade clip lasts exactly 1 second; ScreenFader changes the Animator speed
    /// to make a fade shorter or longer.
    /// Clips are fully built before they are saved (saving first and adding curves later left clips empty before).
    /// Safe to run again: the generated assets are replaced.
    /// </summary>
    public static class CutsceneAnimationBuilder
    {
        /// <summary>Folder where the clips and controller are saved.</summary>
        private const string Folder = "Assets/Animations/Cutscene";

        /// <summary>Path of the fade controller (used by CutsceneSystemBuilder).</summary>
        public const string FadeControllerPath = Folder + "/ScreenFade.controller";

        /// <summary>Menu entry to rebuild only the cutscene animations.</summary>
        [MenuItem("Tools/Pathless/Cutscene/Build Cutscene Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the clips and the controller.</summary>
        /// <returns>True when everything was created.</returns>
        public static bool Build()
        {
            EditorAssetUtility.EnsureFolder(Folder);
            foreach (string name in new[] { "Fade_Clear", "Fade_Out", "Fade_Black", "Fade_In" })
            {
                AssetDatabase.DeleteAsset($"{Folder}/{name}.anim");
            }

            AssetDatabase.DeleteAsset(FadeControllerPath);

            // The alpha of the black Image's color is the only thing the fade animates.
            EditorCurveBinding alpha = EditorCurveBinding.FloatCurve("", typeof(Image), "m_Color.a");

            AnimationClip clear = NewClip("Fade_Clear");
            AnimationUtility.SetEditorCurve(clear, alpha, AnimationCurve.Constant(0f, 0f, 0f));
            Save(clear);

            AnimationClip fadeOut = NewClip("Fade_Out");
            AnimationUtility.SetEditorCurve(fadeOut, alpha, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            Save(fadeOut);

            AnimationClip black = NewClip("Fade_Black");
            AnimationUtility.SetEditorCurve(black, alpha, AnimationCurve.Constant(0f, 0f, 1f));
            Save(black);

            AnimationClip fadeIn = NewClip("Fade_In");
            AnimationUtility.SetEditorCurve(fadeIn, alpha, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Save(fadeIn);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(FadeControllerPath);
            controller.AddParameter("IsBlack", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState clearState = machine.AddState("Clear");
            clearState.motion = clear;
            AnimatorState outState = machine.AddState("FadeOut");
            outState.motion = fadeOut;
            AnimatorState blackState = machine.AddState("Black");
            blackState.motion = black;
            AnimatorState inState = machine.AddState("FadeIn");
            inState.motion = fadeIn;
            machine.defaultState = clearState;

            AddBoolTransition(clearState, outState, true);
            AddExitTimeTransition(outState, blackState);
            AddBoolTransition(blackState, inState, false);
            AddExitTimeTransition(inState, clearState);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("CutsceneAnimationBuilder: fade controller saved in " + Folder);
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

        /// <summary>Adds an instant transition that fires when IsBlack has the given value.</summary>
        /// <param name="from">Source state.</param>
        /// <param name="to">Destination state.</param>
        /// <param name="value">Value of IsBlack that triggers it.</param>
        private static void AddBoolTransition(AnimatorState from, AnimatorState to, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsBlack");
        }

        /// <summary>Adds an instant transition that fires when the source clip has finished playing.</summary>
        /// <param name="from">Source state.</param>
        /// <param name="to">Destination state.</param>
        private static void AddExitTimeTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
        }
    }
}
