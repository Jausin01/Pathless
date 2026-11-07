using Pathless.Story;
using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// The current health and special value of each party member. They live in StoryFlags (flags such as
    /// "party:messenger.hp"), so they are saved with the rest of the story. A member that was never touched has the
    /// starting values defined in its PartyMember asset.
    /// </summary>
    public static class PartyState
    {
        /// <summary>Flag name of a member's health.</summary>
        private static string HpKey(PartyMember member)
        {
            return "party:" + member.MemberId + ".hp";
        }

        /// <summary>Flag name of a member's special value.</summary>
        private static string SpecialKey(PartyMember member)
        {
            return "party:" + member.MemberId + ".special";
        }

        /// <summary>Reads the current health.</summary>
        /// <param name="member">The member.</param>
        /// <returns>Health from 0 to the maximum.</returns>
        public static int GetHp(PartyMember member)
        {
            string key = HpKey(member);
            int value = StoryFlags.Contains(key) ? Mathf.RoundToInt(StoryFlags.Get(key)) : member.StartingHp;
            return Mathf.Clamp(value, 0, member.MaxHp);
        }

        /// <summary>Changes the current health (kept between 0 and the maximum).</summary>
        /// <param name="member">The member.</param>
        /// <param name="value">New health.</param>
        public static void SetHp(PartyMember member, int value)
        {
            StoryFlags.Set(HpKey(member), Mathf.Clamp(value, 0, member.MaxHp));
        }

        /// <summary>Reads the current special value.</summary>
        /// <param name="member">The member.</param>
        /// <returns>Value from 0 to the maximum.</returns>
        public static int GetSpecial(PartyMember member)
        {
            string key = SpecialKey(member);
            int value = StoryFlags.Contains(key) ? Mathf.RoundToInt(StoryFlags.Get(key)) : member.StartingSpecial;
            return Mathf.Clamp(value, 0, member.MaxSpecial);
        }

        /// <summary>Changes the current special value (kept between 0 and the maximum).</summary>
        /// <param name="member">The member.</param>
        /// <param name="value">New value.</param>
        public static void SetSpecial(PartyMember member, int value)
        {
            StoryFlags.Set(SpecialKey(member), Mathf.Clamp(value, 0, member.MaxSpecial));
        }
    }
}
