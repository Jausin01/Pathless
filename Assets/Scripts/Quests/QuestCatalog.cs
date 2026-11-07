using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Quests
{
    /// <summary>
    /// The list of all quests of the game: id, title and description. Whether a quest is active or completed is
    /// not stored here but in QuestLog (changed by "@quest start id" / "@quest complete id" in .dialogue files).
    /// Create one with Assets > Create > Pathless > Quests > Catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestCatalog", menuName = "Pathless/Quests/Catalog")]
    public class QuestCatalog : ScriptableObject
    {
        /// <summary>One quest.</summary>
        [Serializable]
        public class Quest
        {
            /// <summary>Id used in "@quest start id" (letters, digits and _ ; never change it after the game ships).</summary>
            [SerializeField] private string id;

            /// <summary>Title shown in the quest list.</summary>
            [SerializeField] private string title;

            /// <summary>Text shown when the quest is selected.</summary>
            [SerializeField, TextArea(2, 6)] private string description;

            /// <summary>Id used in commands.</summary>
            public string Id => id;

            /// <summary>Title shown in the list.</summary>
            public string Title => title;

            /// <summary>Description shown when selected.</summary>
            public string Description => description;
        }

        /// <summary>All quests.</summary>
        [SerializeField] private List<Quest> quests = new List<Quest>();

        /// <summary>All quests.</summary>
        public IReadOnlyList<Quest> Quests => quests;
    }
}
