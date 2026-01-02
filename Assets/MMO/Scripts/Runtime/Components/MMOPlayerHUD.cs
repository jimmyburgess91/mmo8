using UnityEngine;
using UnityEngine.UIElements;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.MMO
{
    /// <summary>
    /// Extends CoreHUD with MMO-specific UI elements like inventory, character sheet, and friends list.
    /// Manages the MMO player's HUD using Unity's UI Toolkit (UIElements).
    /// Follows the Blocks framework pattern: one UIDocument per player with all UI elements in one UXML file.
    /// </summary>
    public class MMOPlayerHUD : CoreHUD
    {
        #region Fields & Properties

        [Header("MMO Components")]
        [Tooltip("Controller for the inventory UI panel.")]
        private InventoryUIController m_InventoryUIController;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Called by CoreHUD after base initialization completes.
        /// Used to initialize MMO-specific UI elements.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Get the inventory controller component
            m_InventoryUIController = GetComponent<InventoryUIController>();
            if (m_InventoryUIController != null)
            {
                // Initialize inventory with the UIDocument root element
                var uiDocument = GetComponent<UIDocument>();
                if (uiDocument != null)
                {
                    m_InventoryUIController.Initialize(uiDocument.rootVisualElement);
                    Debug.Log("[MMOPlayerHUD] Inventory UI initialized successfully.");
                }
                else
                {
                    Debug.LogWarning("[MMOPlayerHUD] UIDocument component not found on player.");
                }
            }
            else
            {
                Debug.LogWarning("[MMOPlayerHUD] InventoryUIController component not found on player.");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the inventory controller for external access (e.g., from MMOAddon).
        /// </summary>
        public InventoryUIController InventoryController => m_InventoryUIController;

        #endregion
    }
}
