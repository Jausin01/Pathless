using UnityEditor;
using UnityEditor.Animations;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds Hina's animations. His art has 6 walk frames per direction at 5 frames per
    /// second (200 ms per frame). All the work is done by CharacterAnimationBuilder, which counts the frames of
    /// each direction by itself. South-west, north-east and west are mirrors of south-east, north-west and east.
    /// Run it from the menu Tools > Pathless > Build Hina Animations (or through "Build Everything").
    /// </summary>
    public static class HinaAnimationBuilder
    {
        /// <summary>Name of the character (also the folder and file name prefix).</summary>
        private const string CharacterName = "Hina";

        /// <summary>Walk speed: his frames last 200 ms each.</summary>
        private const float FrameRate = 5f;

        /// <summary>Full path of the generated Animator Controller.</summary>
        public const string ControllerPath = "Assets/Animations/Hina/Hina.controller";

        /// <summary>Menu entry that builds only Hina's animations.</summary>
        [MenuItem("Tools/Pathless/Build Hina Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates Hina's clips and Animator Controller.</summary>
        /// <returns>The controller, or null if frames were missing.</returns>
        public static AnimatorController Build()
        {
            // 0 = count the frames of each direction (all directions have 6 frames).
            return CharacterAnimationBuilder.Build(CharacterName, 0, FrameRate);
        }
    }
}
