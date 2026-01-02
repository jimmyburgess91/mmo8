using UnityEngine;
using UnityEngine.UI;

namespace Blocks.Gameplay.MMO.UI
{
    /// <summary>
    /// Simple slot view that logs clicks; meant to be attached to the InventorySlot prefab.
    /// Button.onClick should be wired to HandleClick in the inspector.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private Image icon;

        /// <summary>
        /// Invoked by the Button on this slot. Add via the inspector.
        /// </summary>
        public void HandleClick()
        {
            Debug.Log($"[InventorySlotView] Slot {slotIndex} clicked");
        }

        /// <summary>
        /// Optional helper to set icon at runtime.
        /// </summary>
        public void SetIcon(Sprite sprite, bool visible = true)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.enabled = visible && sprite != null;
        }

        /// <summary>
        /// Optional helper to set slot index at runtime (if instantiated in code).
        /// </summary>
        public void SetSlotIndex(int index)
        {
            slotIndex = index;
        }
    }
}
