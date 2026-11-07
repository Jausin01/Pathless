using UnityEngine;

namespace Pathless.SceneFlow
{
    /// <summary>
    /// A named place where the player can appear when entering this scene (a door, a road entrance...).
    /// Put it on an empty object; its position is the player's feet position. Other scenes refer to it by Spawn Id,
    /// for example "@scene Test_House door" or a SceneTransitionZone with target spawn "door".
    /// </summary>
    public class SceneSpawnPoint : MonoBehaviour
    {
        /// <summary>Name used by transitions to find this point (unique inside the scene).</summary>
        [SerializeField] private string spawnId = "default";

        /// <summary>Direction the player faces on arrival (default south, looking at the camera).</summary>
        [SerializeField] private Vector2 facing = Vector2.down;

        /// <summary>Name used by transitions.</summary>
        public string SpawnId => spawnId;

        /// <summary>Direction the player faces on arrival.</summary>
        public Vector2 Facing => facing;

        /// <summary>Draws a small marker in the Scene view so the point is easy to see.</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(facing.normalized * 0.5f));
        }
    }
}
