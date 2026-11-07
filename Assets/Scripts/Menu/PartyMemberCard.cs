using Pathless.Party;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pathless.Menu
{
    /// <summary>
    /// One slot of the status screen: a party member's face, name, level, health bar and special bar, or an empty
    /// slot placeholder when there is no member. It only shows data given to it.
    /// </summary>
    public class PartyMemberCard : MonoBehaviour
    {
        /// <summary>Parent of everything shown for a member (hidden when the slot is empty).</summary>
        [SerializeField] private GameObject contentRoot;

        /// <summary>Placeholder shown when the slot is empty.</summary>
        [SerializeField] private GameObject emptyRoot;

        /// <summary>The face.</summary>
        [SerializeField] private Image portrait;

        /// <summary>Member name.</summary>
        [SerializeField] private TMP_Text nameLabel;

        /// <summary>Level text, for example "Lv 5".</summary>
        [SerializeField] private TMP_Text levelLabel;

        /// <summary>Colored part of the health bar; its width is changed to show the amount.</summary>
        [SerializeField] private RectTransform hpFill;

        /// <summary>Health numbers, for example "42/60".</summary>
        [SerializeField] private TMP_Text hpValueLabel;

        /// <summary>Name of the second bar.</summary>
        [SerializeField] private TMP_Text specialNameLabel;

        /// <summary>Colored part of the second bar.</summary>
        [SerializeField] private RectTransform specialFill;

        /// <summary>Image of the second bar's colored part (its color comes from the member).</summary>
        [SerializeField] private Image specialFillImage;

        /// <summary>Numbers of the second bar.</summary>
        [SerializeField] private TMP_Text specialValueLabel;

        /// <summary>Shows a party member.</summary>
        /// <param name="member">The member to show.</param>
        public void Show(PartyMember member)
        {
            contentRoot.SetActive(true);
            emptyRoot.SetActive(false);

            portrait.sprite = member.Portrait;
            portrait.enabled = member.Portrait != null;
            nameLabel.text = member.DisplayName;
            levelLabel.text = "Lv " + member.Level;

            int hp = PartyState.GetHp(member);
            SetBar(hpFill, hp, member.MaxHp);
            hpValueLabel.text = hp + "/" + member.MaxHp;

            int special = PartyState.GetSpecial(member);
            specialNameLabel.text = member.SpecialName;
            specialFillImage.color = member.SpecialColor;
            SetBar(specialFill, special, member.MaxSpecial);
            specialValueLabel.text = special + "/" + member.MaxSpecial;
        }

        /// <summary>Shows the empty slot placeholder.</summary>
        public void ShowEmpty()
        {
            contentRoot.SetActive(false);
            emptyRoot.SetActive(true);
        }

        /// <summary>Fills a bar by changing the right edge of its colored part.</summary>
        /// <param name="fill">The colored part (anchored to stretch inside its background).</param>
        /// <param name="value">Current value.</param>
        /// <param name="max">Maximum value.</param>
        private static void SetBar(RectTransform fill, int value, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)value / max) : 0f;
            fill.anchorMax = new Vector2(ratio, 1f);
        }
    }
}
