using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the Hina NPC prefab (all the work is done by CharacterPrefabBuilder).
    /// It needs HinaAnimationBuilder to have run first.
    /// </summary>
    public static class HinaPrefabBuilder
    {
        /// <summary>Name of the character.</summary>
        private const string CharacterName = "Hina";

        /// <summary>Where the prefab is saved.</summary>
        public static readonly string PrefabPath = CharacterPrefabBuilder.PrefabPathOf(CharacterName);

        /// <summary>Menu entry that builds only this prefab.</summary>
        [MenuItem("Tools/Pathless/Build Hina Prefab")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Builds the prefab.</summary>
        /// <returns>The prefab, or null if something is missing.</returns>
        public static GameObject Build()
        {
            return CharacterPrefabBuilder.Build(CharacterName, HinaAnimationBuilder.ControllerPath);
        }
    }
}
