using UnityEngine;
using UnityEngine.UIElements;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.MMO
{
    /// <summary>
    /// Manages the inventory UI using Unity's UI Toolkit (UIElements).
    /// Creates and manages a grid of inventory slots that players can click to interact with items.
    /// Integrates with the parent UIDocument and renders as part of the same PanelSettings layer.
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        #region Fields & Properties

        [Header("Inventory Configuration")]
        [Tooltip("Number of inventory slots to create (e.g., 100 for 10x10 grid)")]
        [SerializeField] private int inventorySlotCount = 100;

        [Tooltip("Number of columns in the inventory grid")]
        [SerializeField] private int gridColumns = 10;

        [Header("Component Dependencies")]
        [Tooltip("Reference to the MMOAddon that owns this inventory")]
        private MMOAddon m_MmoAddon;

        // UI Element References
        private VisualElement m_InventoryPanel;
        private VisualElement m_SlotsContainer;
        private Button m_CloseButton;
        private Button[] m_SlotButtons;

        // State
        private bool m_IsVisible = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            m_MmoAddon = GetComponent<MMOAddon>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the inventory UI by querying elements from the root VisualElement
        /// and creating slot buttons. Must be called after the UIDocument loads the UXML.
        /// </summary>
        public void Initialize(VisualElement rootElement)
        {
            // Query for the main inventory panel
            m_InventoryPanel = rootElement.Q<VisualElement>("inventory-panel");
            if (m_InventoryPanel == null)
            {
                Debug.LogError("[InventoryUIController] Failed to find 'inventory-panel' element in UXML.");
                return;
            }

            // Query for the slots container
            m_SlotsContainer = m_InventoryPanel.Q<VisualElement>("slots-container");
            if (m_SlotsContainer == null)
            {
                Debug.LogError("[InventoryUIController] Failed to find 'slots-container' element in UXML.");
                return;
            }

            // Query for the close button
            m_CloseButton = m_InventoryPanel.Q<Button>("close-inventory-button");
            if (m_CloseButton != null)
            {
                m_CloseButton.RegisterCallback<ClickEvent>(OnCloseButtonClicked);
            }

            // Create inventory slot buttons
            CreateInventorySlots();

            // Start hidden
            Hide();

            Debug.Log("[InventoryUIController] Inventory UI initialized successfully.");
        }

        /// <summary>
        /// Shows the inventory panel.
        /// </summary>
        public void Show()
        {
            if (m_InventoryPanel == null) return;

            m_InventoryPanel.style.display = DisplayStyle.Flex;
            m_IsVisible = true;
            Debug.Log("[InventoryUIController] Inventory shown.");
        }

        /// <summary>
        /// Hides the inventory panel.
        /// </summary>
        public void Hide()
        {
            if (m_InventoryPanel == null) return;

            m_InventoryPanel.style.display = DisplayStyle.None;
            m_IsVisible = false;
            Debug.Log("[InventoryUIController] Inventory hidden.");
        }

        /// <summary>
        /// Toggles the inventory visibility.
        /// </summary>
        public void Toggle()
        {
            if (m_IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Gets the current visibility state of the inventory.
        /// </summary>
        public bool IsVisible => m_IsVisible;

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the inventory slot buttons dynamically in the UI Toolkit slots container.
        /// </summary>
        private void CreateInventorySlots()
        {
            if (m_SlotsContainer == null) return;

            m_SlotButtons = new Button[inventorySlotCount];

            for (int i = 0; i < inventorySlotCount; i++)
            {
                Button slotButton = new Button();
                slotButton.AddToClassList("inventory-slot");
                slotButton.name = $"inventory-slot-{i}";
                slotButton.text = string.Empty;

                // Capture the slot index for the click handler
                int slotIndex = i;
                slotButton.RegisterCallback<ClickEvent>(_ => OnSlotClicked(slotIndex));

                m_SlotsContainer.Add(slotButton);
                m_SlotButtons[i] = slotButton;
            }

            Debug.Log($"[InventoryUIController] Created {inventorySlotCount} inventory slots.");
        }

        /// <summary>
        /// Called when a slot button is clicked.
        /// </summary>
        private void OnSlotClicked(int slotIndex)
        {
            Debug.Log($"[InventoryUIController] Slot {slotIndex} clicked!");
            // TODO: Implement inventory item interaction logic here
        }

        /// <summary>
        /// Called when the close button is clicked.
        /// </summary>
        private void OnCloseButtonClicked(ClickEvent evt)
        {
            Debug.Log("[InventoryUIController] Close button clicked.");
            Hide();

            // Signal MMOAddon to close the inventory
            if (m_MmoAddon != null)
            {
                m_MmoAddon.CloseInventory();
            }
        }

        /// <summary>
        /// Sets the icon for a specific inventory slot (for future use).
        /// </summary>
        public void SetSlotIcon(int slotIndex, Sprite icon)
        {
            if (slotIndex < 0 || slotIndex >= m_SlotButtons.Length)
            {
                Debug.LogWarning($"[InventoryUIController] Slot index {slotIndex} out of range.");
                return;
            }

            // For now, just log it. UI Toolkit Image elements can be added if needed.
            Debug.Log($"[InventoryUIController] Setting icon for slot {slotIndex}");
        }

        /// <summary>
        /// Clears all slot icons (for future use).
        /// </summary>
        public void ClearAllSlots()
        {
            for (int i = 0; i < m_SlotButtons.Length; i++)
            {
                m_SlotButtons[i].text = string.Empty;
            }

            Debug.Log("[InventoryUIController] All slots cleared.");
        }

        #endregion
    }
}
