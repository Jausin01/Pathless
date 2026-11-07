using Pathless.Story;

namespace Pathless.Party
{
    /// <summary>
    /// Remembers which curses were used. The consequences of a curse are narrative, so this is only a flag per curse
    /// ("curse_used:alice"); dialogues can read it with "@if curse_used:alice". It is stored in StoryFlags.
    /// </summary>
    public static class CurseLog
    {
        /// <summary>Flag name of a curse.</summary>
        public static string Key(Curse curse)
        {
            return "curse_used:" + curse.CurseId;
        }

        /// <summary>Marks the curse as used (call it when a character uses its curse).</summary>
        /// <param name="curse">The curse.</param>
        public static void MarkUsed(Curse curse)
        {
            StoryFlags.Set(Key(curse), 1f);
        }

        /// <summary>True if the curse was used at least once.</summary>
        /// <param name="curse">The curse.</param>
        public static bool WasUsed(Curse curse)
        {
            return StoryFlags.IsSet(Key(curse));
        }
    }
}
