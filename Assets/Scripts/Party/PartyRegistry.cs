using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// Gives any script access to the roster of all party members without a scene reference. The roster asset lives
    /// in a Resources folder (Assets/Resources/Party) and is loaded the first time it is needed.
    /// </summary>
    public static class PartyRegistry
    {
        /// <summary>Path of the roster inside a Resources folder, without extension.</summary>
        public const string ResourcePath = "Party/PartyRoster_Main_Claude";

        /// <summary>The loaded roster (null until first use).</summary>
        private static PartyRoster roster;

        /// <summary>The roster of all party members (null, with an error logged, if the asset is missing).</summary>
        public static PartyRoster Roster
        {
            get
            {
                if (roster == null)
                {
                    roster = Resources.Load<PartyRoster>(ResourcePath);
                    if (roster == null)
                    {
                        Debug.LogError("PartyRegistry: roster not found at Resources/" + ResourcePath + ". Run Tools > Pathless > Party > Build Party Data.");
                    }
                }

                return roster;
            }
        }
    }
}
