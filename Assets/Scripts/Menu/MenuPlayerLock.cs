using System.Collections;
using Pathless.Player;
using UnityEngine;

namespace Pathless.Menu
{
    /// <summary>
    /// Freezes the player while the menu is open and releases the player afterwards.
    /// The release waits one frame so the button that closed the menu (for example A on a gamepad)
    /// cannot immediately trigger an interaction.
    /// </summary>
    public class MenuPlayerLock : MonoBehaviour
    {
        /// <summary>The menu to listen to.</summary>
        [SerializeField] private MenuController controller;

        /// <summary>The player motor to freeze. Assigned by the scene builder (the player lives in the scene).</summary>
        [SerializeField] private PlayerMotor playerMotor;

        /// <summary>Subscribes to the menu events.</summary>
        private void OnEnable()
        {
            controller.Opened += OnMenuOpened;
            controller.Closed += OnMenuClosed;
        }

        /// <summary>Unsubscribes from the menu events.</summary>
        private void OnDisable()
        {
            controller.Opened -= OnMenuOpened;
            controller.Closed -= OnMenuClosed;
        }

        /// <summary>Freezes the player.</summary>
        private void OnMenuOpened()
        {
            if (playerMotor != null)
            {
                playerMotor.SetMovementEnabled(false);
            }
        }

        /// <summary>Starts the delayed release.</summary>
        private void OnMenuClosed()
        {
            StartCoroutine(ReleaseNextFrame());
        }

        /// <summary>Waits one frame, then lets the player move again.</summary>
        private IEnumerator ReleaseNextFrame()
        {
            yield return null;

            if (playerMotor != null)
            {
                playerMotor.SetMovementEnabled(true);
            }
        }
    }
}
