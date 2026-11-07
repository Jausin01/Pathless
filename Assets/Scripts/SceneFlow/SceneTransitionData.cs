using UnityEngine;

namespace Pathless.SceneFlow
{
    /// <summary>
    /// The only thing that travels between two scenes: where the player must appear in the new scene and which
    /// conversation (cutscene) must continue there. It is static because every object of the old scene is destroyed
    /// when the new scene loads. SceneLoader writes it, ScreenFader peeks at it, SceneArrival reads and clears it.
    /// </summary>
    public static class SceneTransitionData
    {
        /// <summary>True when a transition is in progress (the new scene must start black and place the player).</summary>
        public static bool HasPending { get; private set; }

        /// <summary>Id of the SceneSpawnPoint where the player appears (empty = leave the player where the scene put it).</summary>
        public static string SpawnId { get; private set; } = "";

        /// <summary>Id of the conversation to play after arriving (empty = none).</summary>
        public static string ContinueId { get; private set; } = "";

        /// <summary>Stores a transition request. Called right before loading the new scene.</summary>
        /// <param name="spawnId">Spawn point id in the new scene.</param>
        /// <param name="continueId">Conversation id in the new scene's arrival script, or empty.</param>
        public static void Set(string spawnId, string continueId)
        {
            HasPending = true;
            SpawnId = spawnId ?? "";
            ContinueId = continueId ?? "";
        }

        /// <summary>Marks the transition as handled so it does not repeat in later scenes.</summary>
        public static void Clear()
        {
            HasPending = false;
            SpawnId = "";
            ContinueId = "";
        }

        /// <summary>Clears stale data when Play mode starts (static data can survive between Play sessions).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Clear();
        }
    }
}
