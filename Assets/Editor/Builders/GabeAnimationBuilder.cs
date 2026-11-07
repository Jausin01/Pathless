using UnityEditor;
using UnityEditor.Animations;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds Gabe's animations. His art has 6 walk frames per direction at 5 frames per
    /// second (200 ms per frame). All the work is done by CharacterAnimationBuilder, which counts the frames of
    /// each direction by itself. North-west, south-west and east are mirrors of north-east, south-east and west.
    /// Run it from the menu Tools > Pathless > Build Gabe Animations (or through "Build Everything").
    /// </summary>
    public static class GabeAnimationBuilder
    {
        /// <summary>Name of the character (also the folder and file name prefix).</summary>
        private const string CharacterName = "Gabe";

        /// <summary>Walk speed: his frames last 200 ms each.</summary>
        private const float FrameRate = 5f;

        /// <summary>Full path of the generated Animator Controller.</summary>
        public const string ControllerPath = "Assets/Animations/Gabe/Gabe.controller";

        /// <summary>Menu entry that builds only Gabe's animations.</summary>
        [MenuItem("Tools/Pathless/Build Gabe Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates Gabe's clips and Animator Controller.</summary>
        /// <returns>The controller, or null if frames were missing.</returns>
        public static AnimatorController Build()
        {
            // 0 = count the frames of each direction (all directions have 6 frames).
            return CharacterAnimationBuilder.Build(CharacterName, 0, FrameRate);
        }
    }
}
