using UnityEditor;
using UnityEngine;

namespace Pathless.EditorTools
{
    /// <summary>
    /// Creates the Kate NPC prefab (all the work is done by CharacterPrefabBuilder).
    /// It needs KateAnimationBuilder to have run first.
    /// </summary>
    public static class KatePrefabBuilder
    {
        /// <summary>Name of the character.</summary>
        private const string CharacterName = "Kate";

        /// <summary>Where the prefab is saved.</summary>
        public static readonly string PrefabPath = CharacterPrefabBuilder.PrefabPathOf(CharacterName);

        /// <summary>Menu entry that builds only this prefab.</summary>
        [MenuItem("Tools/Pathless/Build Kate Prefab")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Builds the prefab.</summary>
        /// <returns>The prefab, or null if something is missing.</returns>
        public static GameObject Build()
        {
            return CharacterPrefabBuilder.Build(CharacterName, KateAnimationBuilder.ControllerPath);
        }
    }
}
