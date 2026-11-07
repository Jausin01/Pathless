using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.Menu
{
    /// <summary>
    /// The visual side of the menu: builds the option rows from a MenuDefinition, shows which one is selected,
    /// and opens or closes the panel. The slide in/out is an Animator animation (bool "IsOpen"); this class only
    /// sets the parameter. It knows nothing about input or what the options do.
    /// </summary>
    public class MenuView : MonoBehaviour
    {
        /// <summary>Text of the menu title.</summary>
        [SerializeField] private TMP_Text titleLabel;

        /// <summary>Prefab of one option row.</summary>
        [SerializeField] private MenuRowView rowPrefab;

        /// <summary>Parent of the rows (it has a vertical layout group).</summary>
        [SerializeField] private RectTransform listRoot;

        /// <summary>The whole menu panel; resized by its layout when rows are added.</summary>
        [SerializeField] private RectTransform panelRect;

        /// <summary>Animator that slides the panel in and out (bool parameter "IsOpen").</summary>
        [SerializeField] private Animator panelAnimator;

        /// <summary>Animator parameter hash for the open state.</summary>
        private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

        /// <summary>The rows currently shown.</summary>
        private readonly List<MenuRowView> rows = new List<MenuRowView>();

        /// <summary>Creates the title and one row per option of the definition.</summary>
        /// <param name="definition">The menu to show.</param>
        public void Build(MenuDefinition definition)
        {
            foreach (MenuRowView row in rows)
            {
                Destroy(row.gameObject);
            }

            rows.Clear();

            // Fail with a readable message instead of a NullReferenceException when a reference is missing
            // (usually a stale scene reference after the menu assets were rebuilt).
            if (definition == null || titleLabel == null || rowPrefab == null || listRoot == null)
            {
                Debug.LogError("MenuView.Build: a reference is missing (definition, titleLabel, rowPrefab or listRoot). " +
                               "Reopen the scene or run Tools > Pathless > Build Everything.", this);
                return;
            }

            titleLabel.text = definition.Title;

            foreach (MenuDefinition.Entry entry in definition.Entries)
            {
                MenuRowView row = Instantiate(rowPrefab, listRoot);
                row.SetLabel(entry.Label);
                row.SetSelected(false);
                rows.Add(row);
            }

            // The panel grows with the number of rows, so its layout is rebuilt right away.
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        /// <summary>Slides the panel in.</summary>
        public void Open()
        {
            panelAnimator.SetBool(IsOpenHash, true);
        }

        /// <summary>Slides the panel out.</summary>
        public void Close()
        {
            panelAnimator.SetBool(IsOpenHash, false);
        }

        /// <summary>Marks one row as selected and the others as normal.</summary>
        /// <param name="index">Index of the selected option.</param>
        public void SetSelected(int index)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].SetSelected(i == index);
            }
        }
    }
}
