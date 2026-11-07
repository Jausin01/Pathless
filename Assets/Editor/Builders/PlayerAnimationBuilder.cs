using UnityEditor;
using UnityEditor.Animations;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Builds the player's animations (8 walk frames per direction). All the work is done by
    /// CharacterAnimationBuilder; this class only says which character and how many frames.
    /// Run it from the menu Tools > Pathless > Build Player Animations (or through "Build Everything").
    /// </summary>
    public static class PlayerAnimationBuilder
    {
        /// <summary>Name of the character (also the folder and file name prefix).</summary>
        private const string CharacterName = "Player";

        /// <summary>Number of walk frames per direction in the player's art.</summary>
        private const int WalkFrameCount = 8;

        /// <summary>Full path of the generated Animator Controller.</summary>
        public const string ControllerPath = "Assets/Animations/Player/Player.controller";

        /// <summary>Menu entry that builds only the player animations.</summary>
        [MenuItem("Tools/Pathless/Build Player Animations")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Creates the player's clips and Animator Controller.</summary>
        /// <returns>The controller, or null if frames were missing.</returns>
        public static AnimatorController Build()
        {
            return CharacterAnimationBuilder.Build(CharacterName, WalkFrameCount);
        }
    }
}
