using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.MMO
{
    /// <summary>
    /// Player addon that extends CorePlayerManager with MMO-specific functionality.
    /// Implements IPlayerAddon to integrate MMO features like inventory, character sheet,
    /// friends list, and other MMO UI systems into the core player system.
    /// Manages UI visibility and MMO-specific player state.
    /// </summary>
    public class MMOAddon : NetworkBehaviour, IPlayerAddon
    {
        #region Fields & Properties

        [Header("Input Events (Listening)")]
        [Tooltip("Event triggered when the player toggles the inventory.")]
        [SerializeField] private GameEvent onInventoryToggle;

        private CorePlayerManager m_PlayerManager;
        private MMOInputHandler m_MmoInputHandler;
        private CoreInputHandler m_CoreInputHandler;
        private MMOPlayerHUD m_MmoPlayerHUD;
        private bool m_IsInventoryOpen;

        #endregion

        #region IPlayerAddon Implementation

        /// <summary>
        /// Called once by CorePlayerManager in Awake to provide a reference to itself.
        /// </summary>
        public void Initialize(CorePlayerManager playerManager)
        {
            m_PlayerManager = playerManager;

            // Inventory panel is resolved at runtime from the scene
            m_IsInventoryOpen = false;
        }

        /// <summary>
        /// Called when the player's network object is spawned (OnNetworkSpawn).
        /// Registers input event listeners for the owning client.
        /// </summary>
        public void OnPlayerSpawn()
        {
            if (m_PlayerManager.IsOwner)
            {
                m_MmoInputHandler = GetComponent<MMOInputHandler>();
                m_CoreInputHandler = GetComponent<CoreInputHandler>();
                m_MmoPlayerHUD = GetComponent<MMOPlayerHUD>();
                
                // MMOPlayerHUD handles inventory initialization in its Initialize() method
                if (m_MmoPlayerHUD == null)
                {
                    Debug.LogWarning("[MMOAddon] MMOPlayerHUD component not found. Ensure the player has MMOPlayerHUD instead of CoreHUD.");
                }
                
                onInventoryToggle.RegisterListener(HandleInventoryToggle);
            }
        }

        /// <summary>
        /// Called when the player's network object is despawned (OnNetworkDespawn).
        /// Unregisters input event listeners to prevent memory leaks.
        /// </summary>
        public void OnPlayerDespawn()
        {
            if (m_PlayerManager != null && m_PlayerManager.IsOwner)
            {
                onInventoryToggle.UnregisterListener(HandleInventoryToggle);
                m_MmoInputHandler?.SetInventoryUiMode(false);
                m_CoreInputHandler?.SetGameplayInputEnabled(true);
            }
        }

        /// <summary>
        /// Called when the player's life state changes (e.g., Alive -> Eliminated, Eliminated -> Respawned).
        /// </summary>
        public void OnLifeStateChanged(PlayerLifeState previousState, PlayerLifeState newState)
        {
            // Close inventory on elimination
            if (newState == PlayerLifeState.Eliminated)
            {
                if (m_IsInventoryOpen)
                {
                    ToggleInventory();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the inventory toggle input event.
        /// </summary>
        private void HandleInventoryToggle()
        {
            Debug.Log("[MMOAddon] Inventory toggle event received!");
            ToggleInventory();
        }

        /// <summary>
        /// Toggles the inventory panel visibility.
        /// </summary>
        private void ToggleInventory()
        {
            m_IsInventoryOpen = !m_IsInventoryOpen;
            Debug.Log($"[MMOAddon] Toggled inventory. IsOpen: {m_IsInventoryOpen}");

            m_MmoInputHandler?.SetInventoryUiMode(m_IsInventoryOpen);
            m_CoreInputHandler?.SetGameplayInputEnabled(!m_IsInventoryOpen);

            if (m_MmoPlayerHUD != null && m_MmoPlayerHUD.InventoryController != null)
            {
                if (m_IsInventoryOpen)
                {
                    m_MmoPlayerHUD.InventoryController.Show();
                }
                else
                {
                    m_MmoPlayerHUD.InventoryController.Hide();
                }
            }
            else
            {
                Debug.LogWarning("[MMOAddon] MMOPlayerHUD or InventoryUIController not found on player.");
            }
        }

        /// <summary>
        /// Exposes whether the inventory is currently open to other components.
        /// </summary>
        public bool IsInventoryOpen => m_IsInventoryOpen;

        /// <summary>
        /// Opens the inventory panel.
        /// </summary>
        public void OpenInventory()
        {
            if (!m_IsInventoryOpen)
            {
                ToggleInventory();
            }
        }

        /// <summary>
        /// Closes the inventory panel.
        /// </summary>
        public void CloseInventory()
        {
            if (m_IsInventoryOpen)
            {
                ToggleInventory();
            }
        }

        #endregion
    }
}
