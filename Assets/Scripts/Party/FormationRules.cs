using System.Collections.Generic;
using Pathless.Story;

namespace Pathless.Party
{
    /// <summary>
    /// The story's rules for the formation screen: who MUST go in the party (required) and who is not available
    /// right now (absent). The protagonist is always required, because the story is his. The rules are kept in
    /// StoryFlags ("formation:required.id" and "formation:absent.id"), so they are saved with the story.
    /// This class only stores and applies the rules; the screen that uses them comes later.
    /// </summary>
    public static class FormationRules
    {
        /// <summary>Id of the protagonist, who can never be left out.</summary>
        public const string ProtagonistId = "protagonist";

        /// <summary>Flag name of the "required" rule of a member.</summary>
        private static string RequiredKey(string memberId)
        {
            return "formation:required." + memberId;
        }

        /// <summary>Flag name of the "absent" rule of a member.</summary>
        private static string AbsentKey(string memberId)
        {
            return "formation:absent." + memberId;
        }

        /// <summary>True when the member must be in the party (the protagonist always is).</summary>
        public static bool IsRequired(PartyMember member)
        {
            return member != null && (member.MemberId == ProtagonistId || StoryFlags.IsSet(RequiredKey(member.MemberId)));
        }

        /// <summary>True when the member cannot be chosen now. The protagonist is never absent.</summary>
        public static bool IsAbsent(PartyMember member)
        {
            return member != null && member.MemberId != ProtagonistId && StoryFlags.IsSet(AbsentKey(member.MemberId));
        }

        /// <summary>Makes a member required (true) or free again (false).</summary>
        public static void SetRequired(string memberId, bool required)
        {
            if (required)
            {
                StoryFlags.Set(RequiredKey(memberId), 1f);
                StoryFlags.Remove(AbsentKey(memberId));
            }
            else
            {
                StoryFlags.Remove(RequiredKey(memberId));
            }
        }

        /// <summary>Makes a member absent (true) or available again (false). An absent member is no longer required.</summary>
        public static void SetAbsent(string memberId, bool absent)
        {
            if (absent)
            {
                StoryFlags.Set(AbsentKey(memberId), 1f);
                StoryFlags.Remove(RequiredKey(memberId));
            }
            else
            {
                StoryFlags.Remove(AbsentKey(memberId));
            }
        }

        /// <summary>Removes every required/absent rule of every member of the roster.</summary>
        public static void ClearAll(PartyRoster roster)
        {
            foreach (PartyMember member in roster.Members)
            {
                if (member != null)
                {
                    StoryFlags.Remove(RequiredKey(member.MemberId));
                    StoryFlags.Remove(AbsentKey(member.MemberId));
                }
            }
        }

        /// <summary>
        /// Turns any wished party into a valid one: absent members are dropped, required members are added
        /// (even if that pushes out the last chosen ones), the total never goes over MaxSlots.
        /// Required members come first, then the others in the order given.
        /// </summary>
        /// <param name="roster">All members.</param>
        /// <param name="wished">The members the player picked.</param>
        /// <returns>The valid party (always has at least one member if the roster has the protagonist).</returns>
        public static List<PartyMember> Apply(PartyRoster roster, IReadOnlyList<PartyMember> wished)
        {
            List<PartyMember> result = new List<PartyMember>();

            foreach (PartyMember member in roster.Members)
            {
                if (member != null && IsRequired(member) && result.Count < PartyRoster.MaxSlots)
                {
                    result.Add(member);
                }
            }

            foreach (PartyMember member in wished)
            {
                if (member != null && !IsAbsent(member) && !result.Contains(member) && result.Count < PartyRoster.MaxSlots)
                {
                    result.Add(member);
                }
            }

            return result;
        }
    }
}
