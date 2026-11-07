using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Pathless.Story
{
    /// <summary>
    /// The memory of the story: named values that remember what already happened ("helped_elder", "elder_trust"...).
    /// A flag is a number: 0 means "not set", anything else means "set"; yes/no flags use 1 and 0, counters use any number.
    /// Dialogue files change them with "@set" and "@add" and read them with "@if" (see StoryCondition).
    /// Other systems keep their memory here too (played cutscenes, where actors stand), so a future save system only
    /// needs to save and load this one dictionary (see All and Restore).
    /// It lives only while the game runs.
    /// </summary>
    public static class StoryFlags
    {
        /// <summary>All stored values by name.</summary>
        private static readonly Dictionary<string, float> Values = new Dictionary<string, float>();

        /// <summary>A valid flag name: starts with a letter or "_", then letters, digits and _ . : / -</summary>
        private static readonly Regex NameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_.:/\-]*$");

        /// <summary>Every stored flag (read only). A save system will write this.</summary>
        public static IReadOnlyDictionary<string, float> All => Values;

        /// <summary>Reads a flag.</summary>
        /// <param name="name">Flag name.</param>
        /// <returns>Its value, or 0 if it was never set.</returns>
        public static float Get(string name)
        {
            return Values.TryGetValue(name, out float value) ? value : 0f;
        }

        /// <summary>Checks if a flag was ever stored (even with the value 0).</summary>
        /// <param name="name">Flag name.</param>
        /// <returns>True if the flag exists.</returns>
        public static bool Contains(string name)
        {
            return Values.ContainsKey(name);
        }

        /// <summary>Checks if a flag is set (any value other than 0).</summary>
        /// <param name="name">Flag name.</param>
        /// <returns>True if the flag is set.</returns>
        public static bool IsSet(string name)
        {
            return Get(name) != 0f;
        }

        /// <summary>Stores a value.</summary>
        /// <param name="name">Flag name.</param>
        /// <param name="value">New value.</param>
        public static void Set(string name, float value)
        {
            Values[name] = value;
        }

        /// <summary>Replaces everything with saved data (used by a future save system).</summary>
        /// <param name="saved">The saved flags.</param>
        public static void Restore(IDictionary<string, float> saved)
        {
            Values.Clear();
            foreach (KeyValuePair<string, float> pair in saved)
            {
                Values[pair.Key] = pair.Value;
            }
        }

        /// <summary>Checks that a name can be used as a flag name.</summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if valid.</returns>
        public static bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && NameRegex.IsMatch(name);
        }

        /// <summary>
        /// Checks a flag command without running it.
        /// Commands: "set name [value]" (value is a number, true or false; default 1), "add name [amount]" (default 1)
        /// and "quest start id" / "quest complete id" (they set the flag "quest:id" to 1 / 2; see QuestLog).
        /// </summary>
        /// <param name="command">The command without the "@".</param>
        /// <param name="error">What is wrong, when the result is false.</param>
        /// <returns>True if the command is valid.</returns>
        public static bool Validate(string command, out string error)
        {
            return TryParse(command, out _, out _, out _, out error);
        }

        /// <summary>Runs a "set" or "add" command.</summary>
        /// <param name="command">The command without the "@".</param>
        /// <param name="error">What is wrong, when the result is false.</param>
        /// <returns>True if the command was valid and applied.</returns>
        public static bool TryApply(string command, out string error)
        {
            if (!TryParse(command, out bool isAdd, out string name, out float value, out error))
            {
                return false;
            }

            // Starting a quest that is already active or completed must not send it back to "active".
            if (name.StartsWith("quest:") && value == 1f && Get(name) >= 1f)
            {
                return true;
            }

            Values[name] = isAdd ? Get(name) + value : value;
            return true;
        }

        /// <summary>Reads the words of a flag command.</summary>
        private static bool TryParse(string command, out bool isAdd, out string name, out float value, out string error)
        {
            isAdd = false;
            name = "";
            value = 1f;
            error = "";

            string[] words = command.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (words.Length > 0 && words[0].ToLowerInvariant() == "quest")
            {
                string action = words.Length == 3 ? words[1].ToLowerInvariant() : "";
                if ((action != "start" && action != "complete") || !IsValidName(words[2]))
                {
                    error = "use '@quest start quest_id' or '@quest complete quest_id'.";
                    return false;
                }

                name = "quest:" + words[2];
                value = action == "start" ? 1f : 2f;
                return true;
            }

            if (words.Length < 2 || words.Length > 3 || (words[0].ToLowerInvariant() != "set" && words[0].ToLowerInvariant() != "add"))
            {
                error = "use '@set name [value]' or '@add name [amount]'.";
                return false;
            }

            isAdd = words[0].ToLowerInvariant() == "add";
            name = words[1];
            if (!IsValidName(name))
            {
                error = $"'{name}' is not a valid flag name (letters, digits and _ only, not starting with a digit).";
                return false;
            }

            if (words.Length == 3)
            {
                string valueText = words[2].ToLowerInvariant();
                if (valueText == "true")
                {
                    value = 1f;
                }
                else if (valueText == "false")
                {
                    value = 0f;
                }
                else if (!float.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    error = $"'{words[2]}' is not a number.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>Clears the memory when Play mode starts (static data can survive between Play sessions).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Values.Clear();
        }
    }
}
