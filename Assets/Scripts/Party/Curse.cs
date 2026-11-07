using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// A curse: the special power a character received from the entity. It costs HP to use, has no cooldown, and its
    /// consequence is narrative (the story marks that it was used, see CurseLog). Only characters that own a curse
    /// can ever show a "Curse" command. Create one with Assets > Create > Pathless > Party > Curse.
    /// The numbers and descriptions are provisional.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCurse", menuName = "Pathless/Party/Curse")]
    public class Curse : ScriptableObject
    {
        /// <summary>Short stable id used to store story flags about this curse (never change it after the game ships).</summary>
        [SerializeField] private string curseId = "curse";

        /// <summary>Name of the power shown to the player.</summary>
        [SerializeField] private string displayName = "Curse";

        /// <summary>Short description shown to the player.</summary>
        [SerializeField, TextArea] private string description = "";

        /// <summary>How much HP one use costs.</summary>
        [SerializeField] private int hpCost = 10;

        /// <summary>
        /// True when the owner (and the player) do not know about this curse yet. A hidden curse exists in the data but
        /// is never offered in a menu until the story reveals it (for example Violet's).
        /// </summary>
        [SerializeField] private bool hidden;

        /// <summary>Stable id used for story flags.</summary>
        public string CurseId => curseId;

        /// <summary>Name shown to the player.</summary>
        public string DisplayName => displayName;

        /// <summary>Description shown to the player.</summary>
        public string Description => description;

        /// <summary>HP cost of one use.</summary>
        public int HpCost => hpCost;

        /// <summary>True while the curse is secret and must not be offered or listed.</summary>
        public bool Hidden => hidden;
    }
}
