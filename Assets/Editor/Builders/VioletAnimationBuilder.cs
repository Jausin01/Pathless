using UnityEditor;
using UnityEditor.Animations;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds Violet's animations (6 walk frames per direction; west, north-west and south-west are mirrors of
    /// east, north-east and south-east). All the work is done by CharacterAnimationBuilder.
    /// Run it from the menu Tools > Pathless > Build Violet Animations (or through "Build Everything").
    /// </summary>
    public static class VioletAnimationBuilder
    {
        /// <summary>Name of the character (also the folder and file name prefix).</summary>
        private const string CharacterName = "Violet";

        /// <summary>Number of walk frames per direction in Violet's art.</summary>
        private const int WalkFrameCount = 6;

        /// <summary>Full path of the generated Animator Controller.</summary>
        public const string ControllerPath = "Assets/Animations/Violet/Violet.controller";

        /// <summary>Menu entry that builds only Violet's animations.</summary>
        [MenuItem("Tools/Pathless/Build Violet Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates Violet's clips and Animator Controller.</summary>
        /// <returns>The controller, or null if frames were missing.</returns>
        public static AnimatorController Build()
        {
            return CharacterAnimationBuilder.Build(CharacterName, WalkFrameCount);
        }
    }
}
