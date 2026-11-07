using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the animation assets of ONE 8-direction character from its imported frames:
    /// 8 Walk clips + 8 Idle clips (one per compass direction) and an Animator Controller that plays them through
    /// two 2D blend trees (Idle and Walk). It is generic: PlayerAnimationBuilder and VioletAnimationBuilder only say
    /// which character to build. Frames must be at Assets/Art/Characters/(name)/Frames/(direction)/(name)_Walk_(direction)_NN.png.
    /// Output goes to Assets/Animations/(name). Running it again REPLACES the generated clips and controller.
    /// </summary>
    public static class CharacterAnimationBuilder
    {
        /// <summary>Default walk animation speed in frames per second (8 frames at 10 fps = 0.8 s per step cycle).</summary>
        public const float DefaultFrameRate = 10f;

        /// <summary>Animator parameter holding the horizontal facing (-1, 0 or 1).</summary>
        private const string MoveXParameter = "MoveX";

        /// <summary>Animator parameter holding the vertical facing (-1, 0 or 1).</summary>
        private const string MoveYParameter = "MoveY";

        /// <summary>Animator parameter that is true while the player is walking.</summary>
        private const string IsMovingParameter = "IsMoving";

        /// <summary>
        /// The 8 directions: the name of the frame sub-folder and the blend tree position of that direction.
        /// The position must match what Direction8 sends to the Animator (MoveX, MoveY).
        /// </summary>
        private static readonly (string folder, Vector2 position)[] Directions =
        {
            ("east", new Vector2(1f, 0f)),
            ("north-east", new Vector2(1f, 1f)),
            ("north", new Vector2(0f, 1f)),
            ("north-west", new Vector2(-1f, 1f)),
            ("west", new Vector2(-1f, 0f)),
            ("south-west", new Vector2(-1f, -1f)),
            ("south", new Vector2(0f, -1f)),
            ("south-east", new Vector2(1f, -1f))
        };

        /// <summary>Folder that holds the frames of a character, one sub-folder per direction.</summary>
        private static string FramesRoot(string character) => $"Assets/Art/Characters/{character}/Frames";

        /// <summary>Folder where the Animator Controller of a character is saved.</summary>
        private static string OutputFolder(string character) => $"Assets/Animations/{character}";

        /// <summary>Folder where the generated animation clips of a character are saved.</summary>
        private static string ClipsFolder(string character) => $"Assets/Animations/{character}/Clips";

        /// <summary>Full path of the Animator Controller of a character.</summary>
        public static string ControllerPathOf(string character) => $"{OutputFolder(character)}/{character}.controller";

        /// <summary>
        /// Creates the clips and the Animator Controller of one character.
        /// </summary>
        /// <param name="character">Character name, for example "Player" or "Violet" (also the file name prefix).</param>
        /// <param name="walkFrameCount">
        /// How many walk frames each direction has in the source art. Use 0 to count the files of each direction
        /// (directions may then have different numbers of frames).
        /// </param>
        /// <param name="frameRate">Walk animation speed in frames per second.</param>
        /// <returns>The new controller, or null if some frames were missing (an error is logged).</returns>
        public static AnimatorController Build(string character, int walkFrameCount, float frameRate = DefaultFrameRate)
        {
            EditorAssetUtility.EnsureFolder("Assets/Animations");
            EditorAssetUtility.EnsureFolder(OutputFolder(character));
            EditorAssetUtility.EnsureFolder(ClipsFolder(character));

            // Clips per direction, paired with their blend tree position.
            List<(AnimationClip clip, Vector2 position)> idleClips = new List<(AnimationClip, Vector2)>();
            List<(AnimationClip clip, Vector2 position)> walkClips = new List<(AnimationClip, Vector2)>();

            foreach ((string folder, Vector2 position) direction in Directions)
            {
                Sprite[] walkSprites = LoadWalkSprites(character, direction.folder, walkFrameCount);
                if (walkSprites == null)
                {
                    return null;
                }

                // Idle uses the first walk frame, which is the neutral standing pose.
                AnimationClip idle = CreateClip(character, $"{character}_Idle_{direction.folder}", new[] { walkSprites[0] }, frameRate);
                AnimationClip walk = CreateClip(character, $"{character}_Walk_{direction.folder}", walkSprites, frameRate);

                idleClips.Add((idle, direction.position));
                walkClips.Add((walk, direction.position));
            }

            AnimatorController controller = CreateController(character, idleClips, walkClips);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CharacterAnimationBuilder: {character}.controller and 16 clips created in {OutputFolder(character)}");
            return controller;
        }

        /// <summary>Loads the walk sprites of one direction, in frame order.</summary>
        /// <param name="character">Character name.</param>
        /// <param name="directionFolder">Direction name, for example "south-east".</param>
        /// <param name="walkFrameCount">How many frames to load; 0 loads every consecutive frame that exists (at least one).</param>
        /// <returns>The sprites, or null (with an error logged) if a frame is missing.</returns>
        private static Sprite[] LoadWalkSprites(string character, string directionFolder, int walkFrameCount)
        {
            List<Sprite> sprites = new List<Sprite>();

            for (int i = 0; walkFrameCount <= 0 || i < walkFrameCount; i++)
            {
                string path = $"{FramesRoot(character)}/{directionFolder}/{character}_Walk_{directionFolder}_{i:00}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (sprite == null)
                {
                    // In auto mode the first missing file just ends the animation (but frame 00 must exist).
                    if (walkFrameCount <= 0 && i > 0)
                    {
                        break;
                    }

                    Debug.LogError($"CharacterAnimationBuilder: sprite not found at {path}");
                    return null;
                }

                sprites.Add(sprite);
            }

            return sprites.ToArray();
        }

        /// <summary>
        /// Creates a looping sprite animation clip and saves it as an asset (replacing any old one).
        /// </summary>
        /// <param name="character">Character name (decides the output folder).</param>
        /// <param name="clipName">Asset name, for example "Player_Walk_south".</param>
        /// <param name="sprites">The frames in order; a single sprite makes a static (idle) clip.</param>
        /// <param name="frameRate">Frames per second.</param>
        /// <returns>The saved clip asset.</returns>
        private static AnimationClip CreateClip(string character, string clipName, Sprite[] sprites, float frameRate)
        {
            AnimationClip clip = new AnimationClip { name = clipName, frameRate = frameRate };

            // One keyframe per sprite, plus a closing keyframe that repeats the last sprite so it
            // keeps its full duration before the loop restarts.
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };
            }

            keyframes[sprites.Length] = new ObjectReferenceKeyframe
            {
                time = sprites.Length / frameRate,
                value = sprites[sprites.Length - 1]
            };

            // The curve changes the "Sprite" property of the SpriteRenderer on the animated object.
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string path = $"{ClipsFolder(character)}/{clipName}.anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        /// <summary>
        /// Creates the Animator Controller with the parameters, the Idle and Walk states and their transitions.
        /// </summary>
        /// <param name="character">Character name (decides the controller path).</param>
        /// <param name="idleClips">Idle clips with their blend tree positions.</param>
        /// <param name="walkClips">Walk clips with their blend tree positions.</param>
        /// <returns>The saved controller asset.</returns>
        private static AnimatorController CreateController(
            string character,
            List<(AnimationClip clip, Vector2 position)> idleClips,
            List<(AnimationClip clip, Vector2 position)> walkClips)
        {
            string controllerPath = ControllerPathOf(character);
            AssetDatabase.DeleteAsset(controllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter(MoveXParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveYParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(IsMovingParameter, AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            // The first state added becomes the default state, so the player starts in Idle.
            AnimatorState idleState = stateMachine.AddState("Idle");
            idleState.motion = CreateBlendTree("Idle", controller, idleClips);

            AnimatorState walkState = stateMachine.AddState("Walk");
            walkState.motion = CreateBlendTree("Walk", controller, walkClips);

            // Idle -> Walk when IsMoving becomes true (instant, no exit time).
            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.hasFixedDuration = true;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, IsMovingParameter);

            // Walk -> Idle when IsMoving becomes false.
            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.hasFixedDuration = true;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, IsMovingParameter);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// Creates a 2D Simple Directional blend tree inside the controller asset.
        /// The tree picks the clip whose position matches the (MoveX, MoveY) facing direction.
        /// </summary>
        /// <param name="treeName">Name of the blend tree (also its sub-asset name).</param>
        /// <param name="controller">The controller that stores the tree as a sub-asset.</param>
        /// <param name="clips">Clips with their positions (one per compass direction).</param>
        /// <returns>The configured blend tree.</returns>
        private static BlendTree CreateBlendTree(
            string treeName,
            AnimatorController controller,
            List<(AnimationClip clip, Vector2 position)> clips)
        {
            BlendTree tree = new BlendTree
            {
                name = treeName,
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = MoveXParameter,
                blendParameterY = MoveYParameter,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };

            // A blend tree must be stored inside an asset, otherwise it is lost when the editor reloads.
            AssetDatabase.AddObjectToAsset(tree, controller);

            foreach ((AnimationClip clip, Vector2 position) entry in clips)
            {
                tree.AddChild(entry.clip, entry.position);
            }

            return tree;
        }
    }
}
