using UnityEngine;

namespace Pathless.Core
{
    /// <summary>
    /// Static helper that snaps any 2D direction to one of the 8 compass directions
    /// (E, NE, N, NW, W, SW, S, SE) supported by the character art.
    /// Usage: Vector2 facing = Direction8.Snap(someInputVector);
    /// </summary>
    public static class Direction8
    {
        /// <summary>
        /// The 8 snapped directions, ordered counter-clockwise starting at East.
        /// Index i corresponds to an angle of i * 45 degrees.
        /// Diagonals are intentionally NOT normalized: their components are exactly -1, 0 or 1,
        /// so the result can be fed straight into the Animator blend tree as MoveX / MoveY.
        /// </summary>
        private static readonly Vector2[] Directions =
        {
            new Vector2(1f, 0f),    // 0: East
            new Vector2(1f, 1f),    // 1: North-East
            new Vector2(0f, 1f),    // 2: North
            new Vector2(-1f, 1f),   // 3: North-West
            new Vector2(-1f, 0f),   // 4: West
            new Vector2(-1f, -1f),  // 5: South-West
            new Vector2(0f, -1f),   // 6: South
            new Vector2(1f, -1f)    // 7: South-East
        };

        /// <summary>
        /// Returns the compass direction closest to the given vector.
        /// Returns Vector2.zero when the input is (almost) zero, so callers can tell "no direction".
        /// </summary>
        /// <param name="direction">Any 2D vector, for example the raw movement input.</param>
        /// <returns>A vector whose x and y are each -1, 0 or 1.</returns>
        public static Vector2 Snap(Vector2 direction)
        {
            // Ignore tiny vectors (stick drift or no input) so we never return a random direction.
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            // Angle in degrees, from -180 to 180, where 0 is East and 90 is North.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Each compass sector is 45 degrees wide, so rounding angle / 45 gives the sector index (-4 to 4).
            int index = Mathf.RoundToInt(angle / 45f);

            // Wrap negative indices (and 8) into the 0..7 range used by the Directions table.
            index = ((index % 8) + 8) % 8;

            return Directions[index];
        }
    }
}
