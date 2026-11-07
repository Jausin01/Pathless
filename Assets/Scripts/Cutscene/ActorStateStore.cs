using Pathless.Story;
using UnityEngine;

namespace Pathless.Cutscene
{
    /// <summary>
    /// Remembers where cutscenes left each actor, per scene, so a character that walked somewhere stays there
    /// after the scene is left and entered again (a scene is rebuilt from scratch every time it loads).
    /// Commands marked "temp" (for example "@move Messenger 1 2 temp") are not stored.
    /// The memory is kept in StoryFlags (flags such as "actor:Test_Movement/messenger.x"), so it is saved together
    /// with the rest of the story.
    /// </summary>
    public static class ActorStateStore
    {
        /// <summary>Builds the flag name prefix of an actor.</summary>
        /// <param name="sceneName">Scene the actor is in.</param>
        /// <param name="actorName">Actor name used in commands.</param>
        /// <returns>The prefix; a suffix (".x", ".y"...) is added for each value.</returns>
        private static string Prefix(string sceneName, string actorName)
        {
            return "actor:" + sceneName + "/" + actorName.ToLowerInvariant();
        }

        /// <summary>Stores the position an actor ended at.</summary>
        /// <param name="sceneName">Scene the actor is in.</param>
        /// <param name="actorName">Actor name.</param>
        /// <param name="position">World position to remember.</param>
        public static void SetPosition(string sceneName, string actorName, Vector2 position)
        {
            string prefix = Prefix(sceneName, actorName);
            StoryFlags.Set(prefix + ".hasPos", 1f);
            StoryFlags.Set(prefix + ".x", position.x);
            StoryFlags.Set(prefix + ".y", position.y);
        }

        /// <summary>Stores the direction an actor ended facing.</summary>
        /// <param name="sceneName">Scene the actor is in.</param>
        /// <param name="actorName">Actor name.</param>
        /// <param name="facing">Direction to remember.</param>
        public static void SetFacing(string sceneName, string actorName, Vector2 facing)
        {
            string prefix = Prefix(sceneName, actorName);
            StoryFlags.Set(prefix + ".hasFacing", 1f);
            StoryFlags.Set(prefix + ".fx", facing.x);
            StoryFlags.Set(prefix + ".fy", facing.y);
        }

        /// <summary>Reads the stored position of an actor.</summary>
        /// <param name="sceneName">Scene the actor is in.</param>
        /// <param name="actorName">Actor name.</param>
        /// <param name="position">The stored position, if any.</param>
        /// <returns>True if a position was stored.</returns>
        public static bool TryGetPosition(string sceneName, string actorName, out Vector2 position)
        {
            string prefix = Prefix(sceneName, actorName);
            bool found = StoryFlags.IsSet(prefix + ".hasPos");
            position = found ? new Vector2(StoryFlags.Get(prefix + ".x"), StoryFlags.Get(prefix + ".y")) : Vector2.zero;
            return found;
        }

        /// <summary>Reads the stored facing of an actor.</summary>
        /// <param name="sceneName">Scene the actor is in.</param>
        /// <param name="actorName">Actor name.</param>
        /// <param name="facing">The stored direction, if any.</param>
        /// <returns>True if a facing was stored.</returns>
        public static bool TryGetFacing(string sceneName, string actorName, out Vector2 facing)
        {
            string prefix = Prefix(sceneName, actorName);
            bool found = StoryFlags.IsSet(prefix + ".hasFacing");
            facing = found ? new Vector2(StoryFlags.Get(prefix + ".fx"), StoryFlags.Get(prefix + ".fy")) : Vector2.zero;
            return found;
        }
    }
}
