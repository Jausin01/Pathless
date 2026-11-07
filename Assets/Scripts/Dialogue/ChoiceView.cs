using System.Collections.Generic;
using Pathless.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.Dialogue
{
    /// <summary>
    /// The list of options shown for a @choice: a framed panel above the dialogue box that slides in from the right
    /// (Animator, bool "IsOpen"). It uses the same row prefab as the menu, so both look the same.
    /// It only shows things; DialogueRunner decides what the selection means.
    /// </summary>
    public class ChoiceView : MonoBehaviour
    {
        /// <summary>Prefab of one option row (the menu row: highlight, bouncing cursor, label).</summary>
        [SerializeField] private MenuRowView rowPrefab;

        /// <summary>Object that holds the rows (a vertical layout).</summary>
        [SerializeField] private RectTransform listRoot;

        /// <summary>The panel; its layout is rebuilt when the options change.</summary>
        [SerializeField] private RectTransform panelRect;

        /// <summary>Animator that slides the panel in and out.</summary>
        [SerializeField] private Animator panelAnimator;

        /// <summary>Animator parameter hash for the open state.</summary>
        private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

        /// <summary>The rows currently shown.</summary>
        private readonly List<MenuRowView> rows = new List<MenuRowView>();

        /// <summary>Builds the rows for the given texts and slides the panel in.</summary>
        /// <param name="labels">Option texts, in order.</param>
        public void Show(IReadOnlyList<string> labels)
        {
            foreach (MenuRowView row in rows)
            {
                // Hidden first: Destroy happens at the end of the frame and the layout must not count the old rows.
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }

            rows.Clear();

            foreach (string label in labels)
            {
                MenuRowView row = Instantiate(rowPrefab, listRoot);
                row.SetLabel(label);
                row.SetSelected(false);
                rows.Add(row);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            panelAnimator.SetBool(IsOpenHash, true);
        }

        /// <summary>Highlights one option.</summary>
        /// <param name="index">Index of the option to highlight.</param>
        public void SetSelected(int index)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].SetSelected(i == index);
            }
        }

        /// <summary>Slides the panel out.</summary>
        public void Hide()
        {
            panelAnimator.SetBool(IsOpenHash, false);
        }
    }
}
