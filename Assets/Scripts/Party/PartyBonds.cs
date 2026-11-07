using System;
using Pathless.Story;
using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// The bond (friendship) between two party members. One number per pair, stored in StoryFlags
    /// (for example "bond:alice+gabe"), so it is saved with the story and can be changed from dialogue with
    /// "@add bond:alice+gabe 1". The order of the two ids does not matter.
    /// </summary>
    public static class PartyBonds
    {
        /// <summary>Builds the flag name of a pair (ids sorted so both orders give the same name).</summary>
        /// <param name="first">One member.</param>
        /// <param name="second">The other member.</param>
        public static string Key(PartyMember first, PartyMember second)
        {
            string a = first.MemberId;
            string b = second.MemberId;
            return string.CompareOrdinal(a, b) <= 0 ? "bond:" + a + "+" + b : "bond:" + b + "+" + a;
        }

        /// <summary>Reads the bond of a pair (0 when nothing happened between them yet).</summary>
        /// <param name="first">One member.</param>
        /// <param name="second">The other member.</param>
        public static int Get(PartyMember first, PartyMember second)
        {
            return Mathf.RoundToInt(StoryFlags.Get(Key(first, second)));
        }

        /// <summary>Changes the bond of a pair by an amount (never below 0).</summary>
        /// <param name="first">One member.</param>
        /// <param name="second">The other member.</param>
        /// <param name="amount">Positive or negative amount.</param>
        public static void Add(PartyMember first, PartyMember second, int amount)
        {
            StoryFlags.Set(Key(first, second), Math.Max(0, Get(first, second) + amount));
        }

        /// <summary>True when the pair can use the given improvised skill (bond high enough).</summary>
        /// <param name="skill">The skill.</param>
        public static bool CanUse(ImprovisedSkill skill)
        {
            return skill.MemberA != null && skill.MemberB != null && Get(skill.MemberA, skill.MemberB) >= skill.MinBond;
        }
    }
}
