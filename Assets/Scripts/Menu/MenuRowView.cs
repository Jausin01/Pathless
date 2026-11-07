using TMPro;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// The visual of one menu option row: the text, the highlight bar and the bouncing cursor.
    /// The cursor bounce itself is an Animator animation; this class only shows or hides it.
    /// </summary>
    public class MenuRowView : MonoBehaviour
    {
        /// <summary>The option text.</summary>
        [SerializeField] private TMP_Text label;

        /// <summary>Highlight bar behind the selected row.</summary>
        [SerializeField] private GameObject highlight;

        /// <summary>Cursor arrow shown at the left of the selected row (has its own bounce Animator).</summary>
        [SerializeField] private GameObject cursor;

        /// <summary>Text color of the selected row.</summary>
        [SerializeField] private Color selectedColor = new Color(0.953f, 0.824f, 0.478f, 1f);

        /// <summary>Text color of the other rows.</summary>
        [SerializeField] private Color normalColor = new Color(0.937f, 0.890f, 0.769f, 1f);

        /// <summary>Sets the text of the row.</summary>
        /// <param name="text">The option label.</param>
        public void SetLabel(string text)
        {
            label.text = text;
        }

        /// <summary>Shows or hides the selection look.</summary>
        /// <param name="selected">True when this row is the selected option.</param>
        public void SetSelected(bool selected)
        {
            highlight.SetActive(selected);
            cursor.SetActive(selected);
            label.color = selected ? selectedColor : normalColor;
        }
    }
}
