using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// A skill two friends use together (a combo). It is defined for ONE specific pair, needs both improvisation bars
    /// full during battle, and needs the bond between the two to reach a minimum (see PartyBonds).
    /// Create one with Assets > Create > Pathless > Party > Improvised Skill. No battle rules exist yet: data only.
    /// </summary>
    [CreateAssetMenu(fileName = "NewImprovisedSkill", menuName = "Pathless/Party/Improvised Skill")]
    public class ImprovisedSkill : ScriptableObject
    {
        /// <summary>First member of the pair.</summary>
        [SerializeField] private PartyMember memberA;

        /// <summary>Second member of the pair (order does not matter).</summary>
        [SerializeField] private PartyMember memberB;

        /// <summary>Name shown to the player.</summary>
        [SerializeField] private string displayName = "Improvised Skill";

        /// <summary>Short description shown to the player.</summary>
        [SerializeField, TextArea] private string description = "";

        /// <summary>Minimum bond between the two members needed to use the skill.</summary>
        [SerializeField] private int minBond = 1;

        /// <summary>First member of the pair.</summary>
        public PartyMember MemberA => memberA;

        /// <summary>Second member of the pair.</summary>
        public PartyMember MemberB => memberB;

        /// <summary>Name shown to the player.</summary>
        public string DisplayName => displayName;

        /// <summary>Description shown to the player.</summary>
        public string Description => description;

        /// <summary>Minimum bond needed.</summary>
        public int MinBond => minBond;

        /// <summary>True if the given two members are this skill's pair (in any order).</summary>
        /// <param name="first">One member.</param>
        /// <param name="second">The other member.</param>
        public bool IsPair(PartyMember first, PartyMember second)
        {
            return (memberA == first && memberB == second) || (memberA == second && memberB == first);
        }
    }
}
