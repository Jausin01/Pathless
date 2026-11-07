using Pathless.Story;

namespace Pathless.Dialogue
{
    /// <summary>
    /// Remembers which forced conversations already played, across scene changes (a scene is rebuilt every time it
    /// loads, so the "already triggered" memory of a zone inside it would be lost).
    /// The memory is kept in StoryFlags (flag "played:scene/conversation"), so it is saved together with the rest of the story.
    /// </summary>
    public static class DialogueProgress
    {
        /// <summary>Prefix of the flag names used for played conversations.</summary>
        private const string Prefix = "played:";

        /// <summary>Checks if a conversation already played.</summary>
        /// <param name="key">Unique key, see MakeKey.</param>
        /// <returns>True if it was marked as played.</returns>
        public static bool HasPlayed(string key)
        {
            return StoryFlags.IsSet(Prefix + key);
        }

        /// <summary>Marks a conversation as played.</summary>
        /// <param name="key">Unique key, see MakeKey.</param>
        public static void MarkPlayed(string key)
        {
            StoryFlags.Set(Prefix + key, 1f);
        }

        /// <summary>Builds the key of a conversation: the scene name plus the conversation id.</summary>
        /// <param name="sceneName">Scene where the conversation is placed.</param>
        /// <param name="sequenceId">Conversation id (the name of the sequence asset).</param>
        /// <returns>The key.</returns>
        public static string MakeKey(string sceneName, string sequenceId)
        {
            return sceneName + "/" + sequenceId;
        }
    }
}
