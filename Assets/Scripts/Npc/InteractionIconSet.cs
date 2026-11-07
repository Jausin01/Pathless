using UnityEngine;

namespace Pathless.Npc
{
    /// <summary>
    /// The sprite of each InteractionIconType, kept in one asset so every character uses the same icons.
    /// Create or refresh it with Tools > Pathless > Build Interaction Icons.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionIconSet", menuName = "Pathless/Npc/Interaction Icon Set")]
    public class InteractionIconSet : ScriptableObject
    {
        /// <summary>Icon for InteractionIconType.Talk.</summary>
        [SerializeField] private Sprite talk;

        /// <summary>Icon for InteractionIconType.Quest.</summary>
        [SerializeField] private Sprite quest;

        /// <summary>Icon for InteractionIconType.Save.</summary>
        [SerializeField] private Sprite save;

        /// <summary>Returns the sprite of an icon type.</summary>
        /// <param name="type">The icon type.</param>
        /// <returns>The sprite, or null for None.</returns>
        public Sprite GetSprite(InteractionIconType type)
        {
            switch (type)
            {
                case InteractionIconType.Talk:
                    return talk;
                case InteractionIconType.Quest:
                    return quest;
                case InteractionIconType.Save:
                    return save;
                default:
                    return null;
            }
        }
    }
}
