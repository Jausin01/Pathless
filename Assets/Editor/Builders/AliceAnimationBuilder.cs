using UnityEditor;
using UnityEditor.Animations;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds Alice's animations. Her art has 8 walk frames per direction (7 for north) at 5 frames per
    /// second (200 ms per frame). All the work is done by CharacterAnimationBuilder, which counts the frames of
    /// each direction by itself. North-west, west and south-east are mirrors of north-east, east and south-west.
    /// Run it from the menu Tools > Pathless > Build Alice Animations (or through "Build Everything").
    /// </summary>
    public static class AliceAnimationBuilder
    {
        /// <summary>Name of the character (also the folder and file name prefix).</summary>
        private const string CharacterName = "Alice";

        /// <summary>Walk speed: her frames last 200 ms each.</summary>
        private const float FrameRate = 5f;

        /// <summary>Full path of the generated Animator Controller.</summary>
        public const string ControllerPath = "Assets/Animations/Alice/Alice.controller";

        /// <summary>Menu entry that builds only Alice's animations.</summary>
        [MenuItem("Tools/Pathless/Build Alice Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates Alice's clips and Animator Controller.</summary>
        /// <returns>The controller, or null if frames were missing.</returns>
        public static AnimatorController Build()
        {
            // 0 = count the frames of each direction (the north walk has 7 frames, the others 8).
            return CharacterAnimationBuilder.Build(CharacterName, 0, FrameRate);
        }
    }
}
