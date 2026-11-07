using UnityEditor;
using UnityEditor.Animations;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds Kate's animations. Her art has 6 walk frames per direction (7 for west and east) at 5 frames per
    /// second (200 ms per frame). All the work is done by CharacterAnimationBuilder, which counts the frames of
    /// each direction by itself. East, north-east and south-west are mirrors of west, north-west and south-east.
    /// Run it from the menu Tools > Pathless > Build Kate Animations (or through "Build Everything").
    /// </summary>
    public static class KateAnimationBuilder
    {
        /// <summary>Name of the character (also the folder and file name prefix).</summary>
        private const string CharacterName = "Kate";

        /// <summary>Walk speed: her frames last 200 ms each.</summary>
        private const float FrameRate = 5f;

        /// <summary>Full path of the generated Animator Controller.</summary>
        public const string ControllerPath = "Assets/Animations/Kate/Kate.controller";

        /// <summary>Menu entry that builds only Kate's animations.</summary>
        [MenuItem("Tools/Pathless/Build Kate Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates Kate's clips and Animator Controller.</summary>
        /// <returns>The controller, or null if frames were missing.</returns>
        public static AnimatorController Build()
        {
            // 0 = count the frames of each direction (the west and east walks have 7 frames, the others 6).
            return CharacterAnimationBuilder.Build(CharacterName, 0, FrameRate);
        }
    }
}
