using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// A normal combat skill of a character (basic attacks and improvisations that are not curses).
    /// Curses and path skills have their own types, so a character that has none of them simply has nothing to show.
    /// Create one with Assets > Create > Pathless > Party > Ability. No battle rules exist yet: this is data only.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Pathless/Party/Ability")]
    public class Ability : ScriptableObject
    {
        /// <summary>Short stable id (never change it after the game ships).</summary>
        [SerializeField] private string abilityId = "ability";

        /// <summary>Name shown to the player.</summary>
        [SerializeField] private string displayName = "Ability";

        /// <summary>Short description shown to the player.</summary>
        [SerializeField, TextArea] private string description = "";

        /// <summary>Stable id of the ability.</summary>
        public string AbilityId => abilityId;

        /// <summary>Name shown to the player.</summary>
        public string DisplayName => displayName;

        /// <summary>Description shown to the player.</summary>
        public string Description => description;
    }
}
