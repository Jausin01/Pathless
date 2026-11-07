using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// Data asset that defines one menu: its title and its options, in order.
    /// Edit the list in the Inspector to add, remove or rename options; no code change is needed.
    /// Create one with Assets > Create > Pathless > Menu > Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMenu", menuName = "Pathless/Menu/Definition")]
    public class MenuDefinition : ScriptableObject
    {
        /// <summary>One option of the menu.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>Short id sent to the listeners when this option is chosen (for example "items"). Keep it stable.</summary>
            [SerializeField] private string id;

            /// <summary>Text shown in the menu.</summary>
            [SerializeField] private string label;

            /// <summary>Id sent when the option is chosen.</summary>
            public string Id => id;

            /// <summary>Text shown in the menu.</summary>
            public string Label => label;
        }

        /// <summary>Title shown at the top of the menu.</summary>
        [SerializeField] private string title = "MENU";

        /// <summary>The options in the order they are listed.</summary>
        [SerializeField] private List<Entry> entries = new List<Entry>();

        /// <summary>Title shown at the top of the menu.</summary>
        public string Title => title;

        /// <summary>The options in order.</summary>
        public IReadOnlyList<Entry> Entries => entries;
    }
}
