using Pathless.Dialogue;
using Pathless.Story;
using UnityEngine;

namespace Pathless.Npc
{
    /// <summary>
    /// Shows a small icon above a character to tell the player what it offers (talk, quest, save).
    /// The icon is a child SpriteRenderer (created by InteractionIconBuilder). It is hidden while a dialogue is
    /// running, and also once the optional "hideWhenFlag" story flag is set (for example after the quest was accepted).
    /// </summary>
    public class InteractionIcon : MonoBehaviour
    {
        /// <summary>The sprites of all icon types.</summary>
        [SerializeField] private InteractionIconSet iconSet;

        /// <summary>Which icon this character shows.</summary>
        [SerializeField] private InteractionIconType type = InteractionIconType.Talk;

        /// <summary>The renderer of the icon (a child object, placed above the character's head).</summary>
        [SerializeField] private SpriteRenderer iconRenderer;

        /// <summary>The dialogue runner; the icon hides while it is running.</summary>
        [SerializeField] private DialogueRunner runner;

        /// <summary>Optional story flag name: once it is set (not 0) the icon disappears. Empty = never hides by itself.</summary>
        [SerializeField] private string hideWhenFlag = "";

        /// <summary>The icon type shown now.</summary>
        public InteractionIconType Type => type;

        /// <summary>Changes the icon (for example from Quest to Talk after a quest is accepted).</summary>
        /// <param name="newType">The new type.</param>
        public void SetType(InteractionIconType newType)
        {
            type = newType;
            Refresh();
        }

        /// <summary>Applies the sprite at the start.</summary>
        private void Start()
        {
            Refresh();
        }

        /// <summary>Checks every frame whether the icon should be visible (a dialogue running or a flag can change it).</summary>
        private void Update()
        {
            Refresh();
        }

        /// <summary>Sets the sprite and the visibility of the icon renderer.</summary>
        private void Refresh()
        {
            if (iconRenderer == null)
            {
                return;
            }

            Sprite sprite = iconSet != null ? iconSet.GetSprite(type) : null;
            bool dialogueRunning = runner != null && runner.IsRunning;
            bool hiddenByFlag = !string.IsNullOrEmpty(hideWhenFlag) && StoryFlags.IsSet(hideWhenFlag);
            bool visible = sprite != null && !dialogueRunning && !hiddenByFlag;

            if (iconRenderer.sprite != sprite)
            {
                iconRenderer.sprite = sprite;
            }

            if (iconRenderer.enabled != visible)
            {
                iconRenderer.enabled = visible;
            }
        }
    }
}
