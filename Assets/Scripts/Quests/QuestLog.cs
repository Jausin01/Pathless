using Pathless.Story;

namespace Pathless.Quests
{
    /// <summary>The state of a quest.</summary>
    public enum QuestState
    {
        /// <summary>Not started yet (not shown in the quest screen).</summary>
        NotStarted,

        /// <summary>Started, not finished.</summary>
        Active,

        /// <summary>Finished.</summary>
        Completed
    }

    /// <summary>
    /// Reads the state of quests. The state is a story flag named "quest:id" (0 not started, 1 active, 2 completed),
    /// changed by "@quest start id" / "@quest complete id" in .dialogue files. Because it is a normal flag,
    /// dialogues can also test it: "@if quest:find_bell == 2".
    /// </summary>
    public static class QuestLog
    {
        /// <summary>Reads the state of a quest.</summary>
        /// <param name="questId">The quest id.</param>
        /// <returns>The state.</returns>
        public static QuestState GetState(string questId)
        {
            float value = StoryFlags.Get("quest:" + questId);
            if (value >= 2f)
            {
                return QuestState.Completed;
            }

            return value >= 1f ? QuestState.Active : QuestState.NotStarted;
        }
    }
}
