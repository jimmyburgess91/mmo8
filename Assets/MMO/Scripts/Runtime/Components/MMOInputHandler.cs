using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.MMO
{
    /// <summary>
    /// Handles MMO-specific player input using Unity's Input System and broadcasts actions via GameEvents.
    /// This component handles inputs for MMO features like inventory, character sheet, etc.
    /// Core movement inputs (Move, Look, Jump, Sprint) are handled by CoreInputHandler.
    /// </summary>
    public class MMOInputHandler : NetworkBehaviour
    {
        #region Fields

        [Header("MMO Events")]
        [Tooltip("Raised when the inventory toggle button (I key) is pressed.")]
        [SerializeField] private GameEvent onInventoryToggle;

        private GameplayInputSystem_Actions m_InputActions;
        private MMOAddon m_MmoAddon;

        #endregion

        #region Unity Lifecycle & Network Callbacks

        private void Awake()
        {
            m_InputActions = new GameplayInputSystem_Actions();
            m_MmoAddon = GetComponent<MMOAddon>();
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[MMOInputHandler] OnNetworkSpawn - IsOwner: {IsOwner}");
            if (IsOwner)
            {
                RegisterInputActions();
                m_InputActions.Player.Enable();
                Debug.Log("[MMOInputHandler] Input actions registered and enabled");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                m_InputActions.Player.Disable();
                m_InputActions.UI.Disable();
                UnregisterInputActions();
            }
        }

        private void OnEnable()
        {
            if (IsOwner && m_InputActions != null)
            {
                m_InputActions.Player.Enable();
            }
        }

        private void OnDisable()
        {
            if (IsOwner && m_InputActions != null)
            {
                m_InputActions.Player.Disable();
            }
        }

        #endregion

        #region Input Registration

        private void RegisterInputActions()
        {
            m_InputActions.Player.Inventory.performed += HandleInventoryToggle;
            m_InputActions.UI.Click.performed += HandleInventoryClick;
        }

        private void UnregisterInputActions()
        {
            m_InputActions.Player.Inventory.performed -= HandleInventoryToggle;
            m_InputActions.UI.Click.performed -= HandleInventoryClick;
        }

        #endregion

        #region Input Handlers

        private void HandleInventoryToggle(InputAction.CallbackContext context)
        {
            Debug.Log("[MMOInputHandler] HandleInventoryToggle called!");
            onInventoryToggle?.Raise();
        }

        private void HandleInventoryClick(InputAction.CallbackContext context)
        {
            if (m_MmoAddon == null || !m_MmoAddon.IsInventoryOpen) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            var screenPos = mouse.position.ReadValue();
            if (m_MmoAddon.IsPointerOverInventory(screenPos))
            {
                Debug.Log($"[MMOInputHandler] UI Click on inventory by {context.control?.device?.name ?? "unknown"} at {screenPos}");
            }
        }

        /// <summary>
        /// Switches between gameplay and UI input modes when inventory opens/closes.
        /// </summary>
        public void SetInventoryUiMode(bool isInventoryOpen)
        {
            if (!IsOwner || m_InputActions == null) return;

            if (isInventoryOpen)
            {
                m_InputActions.Player.Disable();
                m_InputActions.UI.Enable();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                m_InputActions.UI.Disable();
                m_InputActions.Player.Enable();
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        #endregion
    }
}
