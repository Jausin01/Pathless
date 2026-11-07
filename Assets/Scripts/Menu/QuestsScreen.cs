using System.Collections.Generic;
using Pathless.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.Menu
{
    /// <summary>
    /// The Quests screen: two tabs (Active and Completed, switched with left/right), the list of quests of the
    /// current tab on the left (up/down) and the description of the selected quest on the right.
    /// Quests appear when "@quest start id" runs and move to Completed with "@quest complete id".
    /// </summary>
    public class QuestsScreen : MenuScreen
    {
        /// <summary>The list of all quests.</summary>
        [SerializeField] private QuestCatalog catalog;

        /// <summary>Prefab of one list row (the menu row).</summary>
        [SerializeField] private MenuRowView rowPrefab;

        /// <summary>Object that holds the rows (a vertical layout).</summary>
        [SerializeField] private RectTransform listRoot;

        /// <summary>Title of the selected quest.</summary>
        [SerializeField] private TMP_Text titleLabel;

        /// <summary>Description of the selected quest.</summary>
        [SerializeField] private TMP_Text descriptionLabel;

        /// <summary>Text shown when the current tab has no quests.</summary>
        [SerializeField] private GameObject emptyLabel;

        /// <summary>The two tab labels: Active first, Completed second.</summary>
        [SerializeField] private TMP_Text[] tabLabels;

        /// <summary>Color of the selected tab.</summary>
        [SerializeField] private Color selectedTabColor = new Color(0.953f, 0.824f, 0.478f, 1f);

        /// <summary>Color of the other tab.</summary>
        [SerializeField] private Color normalTabColor = new Color(0.55f, 0.50f, 0.42f, 1f);

        /// <summary>The rows currently shown.</summary>
        private readonly List<MenuRowView> rows = new List<MenuRowView>();

        /// <summary>The quests shown in the current tab, in row order.</summary>
        private readonly List<QuestCatalog.Quest> shownQuests = new List<QuestCatalog.Quest>();

        /// <summary>Current tab: 0 = Active, 1 = Completed.</summary>
        private int tab;

        /// <summary>Index of the selected row.</summary>
        private int selected;

        /// <summary>Starts on the Active tab with the first quest selected.</summary>
        protected override void OnOpening()
        {
            tab = 0;
            selected = 0;
            Refresh();
        }

        /// <summary>Selects the previous quest.</summary>
        public override void OnUp()
        {
            MoveSelection(-1);
        }

        /// <summary>Selects the next quest.</summary>
        public override void OnDown()
        {
            MoveSelection(1);
        }

        /// <summary>Goes to the Active tab.</summary>
        public override void OnLeft()
        {
            SwitchTab(0);
        }

        /// <summary>Goes to the Completed tab.</summary>
        public override void OnRight()
        {
            SwitchTab(1);
        }

        /// <summary>Changes the tab and rebuilds the list.</summary>
        /// <param name="newTab">0 for Active, 1 for Completed.</param>
        private void SwitchTab(int newTab)
        {
            if (newTab == tab)
            {
                return;
            }

            tab = newTab;
            selected = 0;
            Refresh();
        }

        /// <summary>Moves the selection, wrapping around.</summary>
        /// <param name="direction">-1 up, 1 down.</param>
        private void MoveSelection(int direction)
        {
            if (shownQuests.Count == 0)
            {
                return;
            }

            selected = (selected + direction + shownQuests.Count) % shownQuests.Count;
            RefreshSelection();
        }

        /// <summary>Rebuilds the list for the current tab and updates the tab colors.</summary>
        private void Refresh()
        {
            foreach (MenuRowView row in rows)
            {
                // Hidden first: Destroy happens at the end of the frame and the layout must not count old rows.
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }

            rows.Clear();
            shownQuests.Clear();

            QuestState wanted = tab == 0 ? QuestState.Active : QuestState.Completed;
            if (catalog != null)
            {
                foreach (QuestCatalog.Quest quest in catalog.Quests)
                {
                    if (QuestLog.GetState(quest.Id) == wanted)
                    {
                        shownQuests.Add(quest);
                        MenuRowView row = Instantiate(rowPrefab, listRoot);
                        row.SetLabel(quest.Title);
                        row.SetSelected(false);
                        rows.Add(row);
                    }
                }
            }

            for (int i = 0; i < tabLabels.Length; i++)
            {
                tabLabels[i].color = i == tab ? selectedTabColor : normalTabColor;
            }

            emptyLabel.SetActive(shownQuests.Count == 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
            RefreshSelection();
        }

        /// <summary>Highlights the selected row and shows its description.</summary>
        private void RefreshSelection()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].SetSelected(i == selected);
            }

            bool hasQuest = selected < shownQuests.Count;
            titleLabel.text = hasQuest ? shownQuests[selected].Title : "";
            descriptionLabel.text = hasQuest ? shownQuests[selected].Description : "";
        }
    }
}
