using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// Base class of the full screens opened from the menu (Status, Quests...). It shows and hides the screen with its
    /// Animator (a fade, bool "IsOpen") and receives the navigation buttons from MenuScreenRouter while it is open.
    /// To make a new screen: inherit from this class, override the buttons you need, and add it to the router's list.
    /// </summary>
    public abstract class MenuScreen : MonoBehaviour
    {
        /// <summary>Id of the menu option that opens this screen (for example "status"; same id as in the MenuDefinition).</summary>
        [SerializeField] private string screenId;

        /// <summary>Animator that fades the screen in and out.</summary>
        [SerializeField] private Animator animator;

        /// <summary>Animator parameter hash for the open state.</summary>
        private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

        /// <summary>Id of the menu option that opens this screen.</summary>
        public string ScreenId => screenId;

        /// <summary>Refreshes the content and fades the screen in.</summary>
        public void Open()
        {
            OnOpening();
            animator.SetBool(IsOpenHash, true);
        }

        /// <summary>Fades the screen out.</summary>
        public void Close()
        {
            animator.SetBool(IsOpenHash, false);
        }

        /// <summary>Called just before the screen fades in; fill it with current data here.</summary>
        protected virtual void OnOpening()
        {
        }

        /// <summary>Up button pressed while the screen is open.</summary>
        public virtual void OnUp()
        {
        }

        /// <summary>Down button pressed while the screen is open.</summary>
        public virtual void OnDown()
        {
        }

        /// <summary>Left button pressed while the screen is open.</summary>
        public virtual void OnLeft()
        {
        }

        /// <summary>Right button pressed while the screen is open.</summary>
        public virtual void OnRight()
        {
        }

        /// <summary>Submit button pressed while the screen is open.</summary>
        public virtual void OnSubmit()
        {
        }
    }
}
