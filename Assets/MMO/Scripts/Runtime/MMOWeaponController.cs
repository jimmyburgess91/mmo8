using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.MMO
{
    /// <summary>
    /// Manages weapon equipping and attachment for MMO players.
    /// Spawns weapon prefabs as network objects and attaches them to the player's armature.
    /// </summary>
    public class MMOWeaponController : NetworkBehaviour
    {
        #region Fields

        [Header("Configuration")]
        [Tooltip("Default attachment node name for right hand weapon")]
        [SerializeField] private string defaultAttachmentNodeName = "Right_Hand_Attach";

        [Header("Events")]
        [Tooltip("Event fired when a weapon is equipped from inventory")]
        [SerializeField] private GameObjectEvent onInventoryItemEquipped;
        [Tooltip("Event fired when a weapon is unequipped from inventory")]
        [SerializeField] private GameObjectEvent onInventoryItemUnequipped;

        private readonly Dictionary<string, AttachableNode> m_AttachmentNodes = new Dictionary<string, AttachableNode>();
        private AttachableBehaviour m_CurrentWeapon;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Find all AttachableNodes in the armature (Right_Hand_Attach, etc.)
            var allNodes = GetComponentsInChildren<AttachableNode>(true);
            foreach (var node in allNodes)
            {
                if (!m_AttachmentNodes.TryAdd(node.gameObject.name, node))
                {
                    Debug.LogWarning($"[MMOWeaponController] Duplicate AttachableNode name: {node.gameObject.name}", this);
                }
            }

            Debug.Log($"[MMOWeaponController] Found {m_AttachmentNodes.Count} attachment nodes");
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Subscribe to inventory events
                if (onInventoryItemEquipped != null)
                {
                    onInventoryItemEquipped.RegisterListener(HandleEquipWeapon);
                }
                if (onInventoryItemUnequipped != null)
                {
                    onInventoryItemUnequipped.RegisterListener(HandleUnequipWeapon);
                }

                Debug.Log("[MMOWeaponController] Event listeners registered");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                // Unsubscribe from inventory events
                if (onInventoryItemEquipped != null)
                {
                    onInventoryItemEquipped.UnregisterListener(HandleEquipWeapon);
                }
                if (onInventoryItemUnequipped != null)
                {
                    onInventoryItemUnequipped.UnregisterListener(HandleUnequipWeapon);
                }

                // Despawn any active weapon - check if it's still spawned first
                if (m_CurrentWeapon != null)
                {
                    // Check if the weapon's NetworkObject is still spawned before trying to detach
                    NetworkObject weaponNetObj = m_CurrentWeapon.GetComponentInParent<NetworkObject>();
                    if (weaponNetObj != null && weaponNetObj.IsSpawned)
                    {
                        UnequipWeapon();
                    }
                    else
                    {
                        Debug.Log("[MMOWeaponController] Weapon already despawned, skipping unequip");
                        m_CurrentWeapon = null;
                    }
                }

                Debug.Log("[MMOWeaponController] Event listeners unregistered");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when the inventory equip event fires. Extracts the weapon prefab from the event.
        /// </summary>
        private void HandleEquipWeapon(GameObject weaponPrefab)
        {
            Debug.Log($"[MMOWeaponController] [EVENT RECEIVED] HandleEquipWeapon - weaponPrefab: {(weaponPrefab != null ? weaponPrefab.name : "NULL")}");
            
            if (weaponPrefab != null)
            {
                Debug.Log($"[MMOWeaponController] [EVENT PROCESSING] Equipping weapon: {weaponPrefab.name}");
                EquipWeapon(weaponPrefab, defaultAttachmentNodeName);
            }
            else
            {
                Debug.LogWarning("[MMOWeaponController] [EVENT ERROR] Received null weapon prefab in equip event");
            }
        }

        /// <summary>
        /// Called when the inventory unequip event fires.
        /// </summary>
        private void HandleUnequipWeapon(GameObject obj)
        {
            Debug.Log("[MMOWeaponController] [EVENT RECEIVED] HandleUnequipWeapon");
            Debug.Log($"[MMOWeaponController] [EVENT PROCESSING] Unequipping weapon, current: {(m_CurrentWeapon != null ? m_CurrentWeapon.gameObject.name : "NONE")}");
            UnequipWeapon();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Equips a weapon prefab by spawning it as a NetworkObject and attaching to the specified node.
        /// </summary>
        /// <param name="weaponPrefab">The weapon prefab to spawn (must have NetworkObject)</param>
        /// <param name="attachmentNodeName">Name of the attachment node (defaults to Right_Hand_Attach)</param>
        public void EquipWeapon(GameObject weaponPrefab, string attachmentNodeName = null)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("[MMOWeaponController] Only the owner can equip weapons");
                return;
            }

            if (weaponPrefab == null)
            {
                Debug.LogError("[MMOWeaponController] Weapon prefab is null");
                return;
            }

            // Use default node if none specified
            string nodeName = string.IsNullOrEmpty(attachmentNodeName) ? defaultAttachmentNodeName : attachmentNodeName;

            if (!m_AttachmentNodes.TryGetValue(nodeName, out AttachableNode targetNode))
            {
                Debug.LogError($"[MMOWeaponController] AttachableNode '{nodeName}' not found. Available nodes: {string.Join(", ", m_AttachmentNodes.Keys)}");
                return;
            }

            // Unequip current weapon first
            if (m_CurrentWeapon != null)
            {
                UnequipWeapon();
            }

            // Start coroutine to handle async spawning and attachment
            StartCoroutine(EquipWeaponCoroutine(weaponPrefab, nodeName, targetNode));
        }

        /// <summary>
        /// Coroutine that spawns weapon and waits for network spawn before attaching.
        /// </summary>
        private System.Collections.IEnumerator EquipWeaponCoroutine(GameObject weaponPrefab, string nodeName, AttachableNode targetNode)
        {
            // Spawn weapon as NetworkObject
            GameObject weaponInstance = Instantiate(weaponPrefab);
            NetworkObject weaponNetObj = weaponInstance.GetComponent<NetworkObject>();

            if (weaponNetObj == null)
            {
                Debug.LogError($"[MMOWeaponController] Weapon prefab '{weaponPrefab.name}' must have NetworkObject component");
                Destroy(weaponInstance);
                yield break;
            }

            // Get AttachableBehaviour (should be on -Child GameObject)
            AttachableBehaviour attachable = weaponInstance.GetComponentInChildren<AttachableBehaviour>();
            if (attachable == null)
            {
                Debug.LogError($"[MMOWeaponController] Weapon '{weaponPrefab.name}' must have AttachableBehaviour component");
                Destroy(weaponInstance);
                yield break;
            }

            // Spawn on network with owner authority
            Debug.Log($"[MMOWeaponController] Spawning weapon '{weaponPrefab.name}' on network with ownership...");
            weaponNetObj.SpawnWithOwnership(OwnerClientId);

            // Wait for network spawn to complete
            yield return new WaitUntil(() => weaponNetObj.IsSpawned);
            Debug.Log($"[MMOWeaponController] Weapon spawned, IsSpawned={weaponNetObj.IsSpawned}, Owner={weaponNetObj.OwnerClientId}, Local={NetworkManager.LocalClientId}");

            weaponInstance.SetActive(false);

            // Wait one more frame for AttachableBehaviour to initialize
            yield return null;

            // Check if we have authority to attach
            if (!attachable.HasAuthority)
            {
                Debug.LogWarning($"[MMOWeaponController] AttachableBehaviour does not have authority, cannot attach");
                yield break;
            }

            Debug.Log($"[MMOWeaponController] Weapon NetworkObject.IsSpawned={weaponNetObj.IsSpawned}, HasAuthority={attachable.HasAuthority}");
            Debug.Log($"[MMOWeaponController] AttachableNode: {targetNode.name}, IsSpawned={targetNode.IsSpawned}, HasAuthority={targetNode.HasAuthority}");

            // Attach to player's hand
            Debug.Log($"[MMOWeaponController] Calling attachable.Attach('{nodeName}')...");
            attachable.Attach(targetNode);
            
            // Give it a frame to process the network call
            yield return null;
            
            m_CurrentWeapon = attachable;

            // Verify attachment worked
            if (attachable.transform.parent == targetNode.transform)
            {
                Debug.Log($"[MMOWeaponController] ✅ ATTACHMENT SUCCESSFUL - weapon parented to {nodeName}");
            }
            else
            {
                Debug.LogError($"[MMOWeaponController] ❌ ATTACHMENT FAILED - weapon parent is {(attachable.transform.parent != null ? attachable.transform.parent.name : "null")}, expected {nodeName}");
            }

            Debug.Log($"[MMOWeaponController] Equipped weapon '{weaponPrefab.name}' to '{nodeName}'");
        }

        /// <summary>
        /// Unequips the currently equipped weapon by despawning it.
        /// </summary>
        public void UnequipWeapon()
        {
            if (!IsOwner)
            {
                Debug.LogWarning("[MMOWeaponController] Only the owner can unequip weapons");
                return;
            }

            if (m_CurrentWeapon == null)
            {
                Debug.Log("[MMOWeaponController] No weapon to unequip");
                return;
            }

            // Detach from node
            m_CurrentWeapon.Detach();

            // Despawn the NetworkObject
            NetworkObject weaponNetObj = m_CurrentWeapon.GetComponentInParent<NetworkObject>();
            if (weaponNetObj != null && weaponNetObj.IsSpawned)
            {
                weaponNetObj.Despawn();
            }

            m_CurrentWeapon = null;
            Debug.Log("[MMOWeaponController] Unequipped weapon");
        }

        /// <summary>
        /// Gets the currently equipped weapon AttachableBehaviour.
        /// </summary>
        public AttachableBehaviour GetCurrentWeapon()
        {
            return m_CurrentWeapon;
        }

        #endregion
    }
}
