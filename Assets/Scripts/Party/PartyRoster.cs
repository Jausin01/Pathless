using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// Every member of the group that exists in the game, in a fixed order (the story has nine friends).
    /// Who is actually WITH the player right now is decided by PartyFormation, which can hold at most MaxSlots.
    /// Create one with Assets > Create > Pathless > Party > Roster.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPartyRoster", menuName = "Pathless/Party/Roster")]
    public class PartyRoster : ScriptableObject
    {
        /// <summary>How many members can be in the active party (and have a card in the status screen) at once.</summary>
        public const int MaxSlots = 4;

        /// <summary>All the members, in a fixed order.</summary>
        [SerializeField] private List<PartyMember> members = new List<PartyMember>();

        /// <summary>All the members, in a fixed order.</summary>
        public IReadOnlyList<PartyMember> Members => members;

        /// <summary>Finds the position of a member in the list.</summary>
        /// <param name="member">The member.</param>
        /// <returns>The index, or -1 if the member is not in this roster.</returns>
        public int IndexOf(PartyMember member)
        {
            return members.IndexOf(member);
        }
    }
}
