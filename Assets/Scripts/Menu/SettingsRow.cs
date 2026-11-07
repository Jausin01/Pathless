using TMPro;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>One line of the Settings screen: the option name on the left and its current value on the right.</summary>
    public class SettingsRow : MonoBehaviour
    {
        /// <summary>Name of the option.</summary>
        [SerializeField] private TMP_Text nameLabel;

        /// <summary>Current value (text, for example "70%  [#######---]").</summary>
        [SerializeField] private TMP_Text valueLabel;

        /// <summary>Color of the selected row.</summary>
        [SerializeField] private Color selectedColor = new Color(0.953f, 0.824f, 0.478f, 1f);

        /// <summary>Color of the other rows.</summary>
        [SerializeField] private Color normalColor = new Color(0.937f, 0.890f, 0.769f, 1f);

        /// <summary>Shows the value text.</summary>
        /// <param name="text">The text to show.</param>
        public void SetValue(string text)
        {
            valueLabel.text = text;
        }

        /// <summary>Highlights the row (gold, with an arrow before the name) or returns it to normal.</summary>
        /// <param name="selected">True when the player has this row selected.</param>
        public void SetSelected(bool selected)
        {
            Color color = selected ? selectedColor : normalColor;
            nameLabel.color = color;
            valueLabel.color = color;
            nameLabel.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            nameLabel.text = (selected ? "> " : "  ") + RawName;
        }

        /// <summary>Sets the option name (without the selection arrow).</summary>
        /// <param name="optionName">The name to show.</param>
        public void SetName(string optionName)
        {
            RawName = optionName;
            nameLabel.text = "  " + optionName;
        }

        /// <summary>The option name without the arrow.</summary>
        private string RawName { get; set; } = "";
    }
}
