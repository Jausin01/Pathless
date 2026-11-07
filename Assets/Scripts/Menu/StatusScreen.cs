using Pathless.Party;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// The Status screen: one card per ACTIVE party slot (face, name, level, health bar and special bar). Slots without a
    /// member show an empty placeholder, so the party can grow up to PartyRoster.MaxSlots without changing the screen.
    /// </summary>
    public class StatusScreen : MenuScreen
    {
        /// <summary>The party to show.</summary>
        [SerializeField] private PartyRoster roster;

        /// <summary>The cards, one per slot, in order.</summary>
        [SerializeField] private PartyMemberCard[] cards;

        /// <summary>Fills the cards with the members of the ACTIVE party (see PartyFormation).</summary>
        protected override void OnOpening()
        {
            System.Collections.Generic.List<PartyMember> active = PartyFormation.GetActive(roster);
            for (int i = 0; i < cards.Length; i++)
            {
                if (i < active.Count)
                {
                    cards[i].Show(active[i]);
                }
                else
                {
                    cards[i].ShowEmpty();
                }
            }
        }
    }
}
