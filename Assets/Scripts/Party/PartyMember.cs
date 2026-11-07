using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Party
{
    /// <summary>
    /// Fixed data of one party member: name, face, level and the maximum of each bar.
    /// The CURRENT values (health, special) are not stored here but in PartyState, so they can change during the
    /// game and be saved. Create one with Assets > Create > Pathless > Party > Member.
    /// The values of the test members are provisional.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPartyMember", menuName = "Pathless/Party/Member")]
    public class PartyMember : ScriptableObject
    {
        /// <summary>Short stable id used to store this member's current values (never change it after the game ships).</summary>
        [SerializeField] private string memberId = "member";

        /// <summary>Name shown in the status screen.</summary>
        [SerializeField] private string displayName = "Member";

        /// <summary>Face shown in the status screen (a portrait sprite).</summary>
        [SerializeField] private Sprite portrait;

        /// <summary>Character level.</summary>
        [SerializeField] private int level = 1;

        /// <summary>Maximum health.</summary>
        [SerializeField] private int maxHp = 100;

        /// <summary>Health when the game starts. 0 (or more than the maximum) means full health.</summary>
        [SerializeField] private int startingHp;

        /// <summary>Name of the second bar. It is NOT SP; what it means is decided later, so it is only a label for now.</summary>
        [SerializeField] private string specialName = "Special";

        /// <summary>Maximum of the second bar.</summary>
        [SerializeField] private int maxSpecial = 50;

        /// <summary>Value of the second bar when the game starts. 0 means full.</summary>
        [SerializeField] private int startingSpecial;

        /// <summary>Color of the second bar.</summary>
        [SerializeField] private Color specialColor = new Color(0.62f, 0.38f, 0.85f, 1f);

        /// <summary>
        /// The curse this member owns (empty if none). A member without a curse never shows a "Curse" command.
        /// </summary>
        [SerializeField] private Curse curse;

        /// <summary>
        /// Id of the path (class) this member learned. Empty means no path yet, so no "Path" command is shown.
        /// Paths are not designed yet, so this is only a placeholder id.
        /// </summary>
        [SerializeField] private string pathId = "";

        /// <summary>Basic attacks and other normal skills of this member.</summary>
        [SerializeField] private List<Ability> abilities = new List<Ability>();

        /// <summary>The curse this member owns (may be null or hidden).</summary>
        public Curse Curse => curse;

        /// <summary>True if the curse exists AND is known, so the "Curse" command may be shown.</summary>
        public bool HasVisibleCurse => curse != null && !curse.Hidden;

        /// <summary>True if this member learned a path, so the "Path" command may be shown.</summary>
        public bool HasPath => !string.IsNullOrEmpty(pathId);

        /// <summary>Id of the learned path (empty if none).</summary>
        public string PathId => pathId;

        /// <summary>Basic attacks and other normal skills.</summary>
        public IReadOnlyList<Ability> Abilities => abilities;

        /// <summary>Stable id used to store current values.</summary>
        public string MemberId => memberId;

        /// <summary>Name shown in the status screen.</summary>
        public string DisplayName => displayName;

        /// <summary>Face shown in the status screen.</summary>
        public Sprite Portrait => portrait;

        /// <summary>Character level.</summary>
        public int Level => level;

        /// <summary>Maximum health.</summary>
        public int MaxHp => maxHp;

        /// <summary>Health when the game starts (maximum if not set).</summary>
        public int StartingHp => startingHp > 0 ? Mathf.Min(startingHp, maxHp) : maxHp;

        /// <summary>Name of the second bar.</summary>
        public string SpecialName => specialName;

        /// <summary>Maximum of the second bar.</summary>
        public int MaxSpecial => maxSpecial;

        /// <summary>Value of the second bar when the game starts (maximum if not set).</summary>
        public int StartingSpecial => startingSpecial > 0 ? Mathf.Min(startingSpecial, maxSpecial) : maxSpecial;

        /// <summary>Color of the second bar.</summary>
        public Color SpecialColor => specialColor;
    }
}
