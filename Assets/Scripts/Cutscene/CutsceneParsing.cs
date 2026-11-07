using System.Globalization;
using UnityEngine;

namespace Pathless.Cutscene
{
    /// <summary>
    /// Small helpers that read the arguments of cutscene commands (numbers and compass directions).
    /// Numbers always use "." as the decimal separator, whatever the language of the computer.
    /// </summary>
    public static class CutsceneParsing
    {
        /// <summary>Reads a decimal number such as "0.5" or "-3.5".</summary>
        /// <param name="text">The text to read.</param>
        /// <param name="value">The number, when the text is valid.</param>
        /// <returns>True when the text is a valid number.</returns>
        public static bool TryFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>Reads a compass direction: north, south, east, west, north-east, ... or n, s, e, w, ne, nw, se, sw.</summary>
        /// <param name="text">The direction word (not case sensitive).</param>
        /// <param name="direction">The direction as a vector (x and y are -1, 0 or 1).</param>
        /// <returns>True when the text is a known direction.</returns>
        public static bool TryDirection(string text, out Vector2 direction)
        {
            switch (text.ToLowerInvariant())
            {
                case "north": case "n": case "up": direction = new Vector2(0f, 1f); return true;
                case "south": case "s": case "down": direction = new Vector2(0f, -1f); return true;
                case "east": case "e": case "right": direction = new Vector2(1f, 0f); return true;
                case "west": case "w": case "left": direction = new Vector2(-1f, 0f); return true;
                case "north-east": case "ne": direction = new Vector2(1f, 1f); return true;
                case "north-west": case "nw": direction = new Vector2(-1f, 1f); return true;
                case "south-east": case "se": direction = new Vector2(1f, -1f); return true;
                case "south-west": case "sw": direction = new Vector2(-1f, -1f); return true;
                default: direction = Vector2.zero; return false;
            }
        }
    }
}
