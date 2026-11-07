using System.Collections.Generic;
using Pathless.Story;
using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// Who is in the active party right now (up to PartyRoster.MaxSlots). The story decides it: a chapter can bring
    /// the whole group, one member alone, or force a pair. The choice is stored in StoryFlags as
    /// "party:slot.N" = roster index + 1 (0 or missing = empty slot), so it is saved with the story.
    /// Until the story sets a formation, the first MaxSlots members of the roster are the active party.
    /// </summary>
    public static class PartyFormation
    {
        /// <summary>Flag name of one slot.</summary>
        private static string SlotKey(int slot)
        {
            return "party:slot." + slot;
        }

        /// <summary>Returns the active members in slot order.</summary>
        /// <param name="roster">The roster that holds all members.</param>
        /// <returns>The active members (1 to MaxSlots, or empty if the roster is empty).</returns>
        public static List<PartyMember> GetActive(PartyRoster roster)
        {
            List<PartyMember> active = new List<PartyMember>();
            if (roster == null)
            {
                return active;
            }

            // No slot was ever set: use the default (the first members of the roster).
            if (!StoryFlags.Contains(SlotKey(0)))
            {
                for (int i = 0; i < roster.Members.Count && active.Count < PartyRoster.MaxSlots; i++)
                {
                    if (roster.Members[i] != null)
                    {
                        active.Add(roster.Members[i]);
                    }
                }

                return active;
            }

            for (int slot = 0; slot < PartyRoster.MaxSlots; slot++)
            {
                int index = Mathf.RoundToInt(StoryFlags.Get(SlotKey(slot))) - 1;
                if (index >= 0 && index < roster.Members.Count && roster.Members[index] != null)
                {
                    active.Add(roster.Members[index]);
                }
            }

            return active;
        }

        /// <summary>Sets who is in the active party (extra members beyond MaxSlots are ignored).</summary>
        /// <param name="roster">The roster that holds all members.</param>
        /// <param name="members">The members that will be active, in order.</param>
        public static void SetActive(PartyRoster roster, IReadOnlyList<PartyMember> members)
        {
            for (int slot = 0; slot < PartyRoster.MaxSlots; slot++)
            {
                int value = 0;
                if (slot < members.Count && members[slot] != null)
                {
                    value = roster.IndexOf(members[slot]) + 1;
                }

                StoryFlags.Set(SlotKey(slot), value);
            }
        }
    }
}
