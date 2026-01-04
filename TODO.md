# MMO Weapon Equip System - Implementation Plan

## Overview
Implementing a system where equipped inventory items (starting with a sword) appear in the player's hand, following the Blocks framework's Shooter weapon patterns.

**Asset Location**: `Assets/Danvil/Rusty Sword`

---

## 1. CODE COMPONENTS

### A. WeaponController-style Component (for MMO)
**Purpose**: Manages weapon spawning, equipping, and state synchronization

**Key responsibilities:**
- Track equipped weapon index
- Spawn weapon prefabs as NetworkObjects
- Handle equip/unequip logic
- Synchronize weapon state across network
- Manage attachment to hand bones

**Implementation Notes:**
- Create `MMOWeaponController.cs` component
- Based on `Assets/Shooter/Scripts/Runtime/Components/WeaponController.cs` pattern
- Add to MMO player prefab

### B. InventoryUIController Enhancement
**Current State**: Generic equip system with visual border feedback

**Additions Needed:**
- Add event/callback when slot is equipped → triggers WeaponController
- Communication flow: `EquipSlot()` → raise GameEvent → WeaponController listens
- Create `OnInventoryItemEquipped` GameEvent asset

**File**: `Assets/MMO/Scripts/Runtime/UI/InventoryUIController.cs`

### C. Weapon Prefab Scripts
**Required Components:**
1. **ModularWeapon** (or create MMOWeapon): Core weapon behavior
2. **AttachableBehaviour** component: Netcode system for parenting objects across network
3. **WeaponData ScriptableObject**: Configuration (model path, attachment points, animations)

**Pattern from Shooter:**
- Reference: `Assets/Shooter/Prefabs/Weapons/Weapon_HandGun.prefab`
- Reference: `Assets/Shooter/Scripts/Runtime/Components/ModularWeapon.cs`
- Reference: `Assets/Shooter/Scripts/Runtime/Framework/Weapons/WeaponData.cs`

### D. Network Synchronization
**Netcode Integration:**
- `NetworkVariable<int>` for equipped weapon index
- RPC calls for equip actions if needed
- AttachableBehaviour handles transform parenting automatically

**Pattern Reference:**
```csharp
private readonly NetworkVariable<PlayerWeaponState> m_PlayerWeaponState = 
    new NetworkVariable<PlayerWeaponState>(
        new PlayerWeaponState(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
```

---

## 2. UNITY EDITOR SETUP

### A. Player Armature (Skeleton) - AttachableNode Components
**Required Attachment Points:**
- **Right Hand Bone**: For holding weapon while aiming/attacking
  - Component: `AttachableNode`
  - Name: "RightHandAttachNode"
  
- **Hip/Back Bone** (optional): For holstered weapons when not equipped
  - Component: `AttachableNode`
  - Name: "IdleWeaponAttachNode"

**Shooter Reference:**
- Player prefab: `Assets/Shooter/Prefabs/[BB] ShooterPlayer.prefab`
- Inspect skeleton hierarchy for AttachableNode placement examples

### B. Weapon Prefab Structure
**Hierarchy Pattern:**
```
Weapon_Sword (GameObject)
├── NetworkObject component
├── ModularWeapon (or MMOWeapon) component
└── SwordMesh (child GameObject)
    ├── AttachableBehaviour component (on root or mesh)
    └── Mesh Renderer (the actual sword visual from Danvil asset)
```

**Steps:**
1. Create prefab from Danvil Rusty Sword mesh
2. Add NetworkObject component
3. Add weapon behavior script
4. Add AttachableBehaviour component
5. Save to `Assets/MMO/Prefabs/Weapons/Weapon_Sword.prefab`

### C. Animation Setup
**Animator Controller on Player:**
- New animation layer for weapon holding
- Blend trees for idle/equipped states
- `weaponTypeID` parameter (int) to switch between weapon types

**Required Animation Clips:**
- Idle with sword (standing still, sword in hand)
- Walking with sword
- Attacking with sword (future)

**Shooter Reference:**
- Animator: Located on `[BB] ShooterPlayer` prefab
- Check parameter setup and layer structure

### D. Animation Rigging (Modern Unity Feature)
**Components Needed:**
- **Rig Builder** component on player
- **Multi-Aim Constraint**: Makes hand IK follow weapon grip point
- **Two Bone IK Constraint**: Realistic arm bending to grip weapon

**Purpose:**
This is how the Shooter makes hands wrap around gun handles realistically without baking hand positions into every animation.

**Benefits:**
- One set of animations works with any weapon size/shape
- Procedural hand positioning
- Network-synchronized

---

## 3. MODERN UNITY BEST PRACTICES

### Animation Rigging (Unity 2020+)
**Why**: Procedural animation adjustments without baking clips
**How**: Constraints (Multi-Aim, Two-Bone IK) dynamically position hands
**Benefit**: One set of animations works with any weapon size/shape

**Unity Docs**: Animation Rigging package

### ScriptableObjects for Data
**Pattern**: WeaponData.asset stores all weapon config
**Benefits:**
- Reusable: Multiple weapon instances share same data
- Inspector-friendly: Artists/designers can configure weapons without code
- No hardcoded values in scripts

**Example Structure:**
```csharp
[CreateAssetMenu(fileName = "WeaponData", menuName = "MMO/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public int weaponTypeID;
    public Sprite weaponIcon;
    public string handAttachmentNodeName;
    public string idleAttachmentNodeName;
    // etc...
}
```

### Netcode AttachableBehaviour
**Feature**: Built-in Netcode component for parent/child relationships
**Benefits:**
- Automatically syncs parent/child relationships across network
- Network-safe: All clients see weapon attached to correct bone
- Authority: Only owner can attach/detach

**Usage:**
```csharp
AttachableBehaviour weaponAttachable = weapon.GetComponent<AttachableBehaviour>();
AttachableNode handNode = rightHand.GetComponent<AttachableNode>();
weaponAttachable.Attach(handNode);
```

### Prefab Workflows
**Best Practices:**
- Nested prefabs: Weapon visual mesh as prefab variant
- Prefab overrides: Adjust attachment points per weapon without breaking connections
- Prefab isolation mode: Edit weapons without scene clutter

---

## 4. BLOCKS FRAMEWORK PATTERNS TO FOLLOW

### A. Shooter Weapon System Architecture
```
WeaponController (on player)
├── Spawns weapon prefabs from WeaponLoadout (ScriptableObject list)
├── Manages AttachableBehaviour.Attach(AttachableNode)
└── Synchronizes via NetworkVariable<PlayerWeaponState>

WeaponLoadout.asset
├── List of weapon prefabs
└── Default weapon index

Weapon_HandGun.prefab
├── ModularWeapon component
├── AttachableBehaviour
└── WeaponData reference (ScriptableObject)

AttachableNodes (on player skeleton)
├── RightHandAttachNode (Transform with AttachableNode component)
└── IdleWeaponAttachNode (Transform with AttachableNode component)
```

**Key Files to Reference:**
- `Assets/Shooter/Scripts/Runtime/Components/WeaponController.cs`
- `Assets/Shooter/Scripts/Runtime/Framework/Weapons/WeaponData.cs`
- `Assets/Shooter/Scripts/Runtime/Components/ModularWeapon.cs`
- `Assets/Shooter/Prefabs/[BB] ShooterPlayer.prefab`
- `Assets/Shooter/Prefabs/Weapons/Weapon_HandGun.prefab`

### B. GameEvent System Pattern
**Flow**: UI → GameEvent → Controller listens
**Example**: 
```
InventoryUIController.EquipSlot(slotIndex)
  ↓
OnInventoryItemEquipped.Raise(slotIndex)
  ↓
MMOWeaponController.HandleItemEquipped(slotIndex)
```

**Benefit**: Decoupling - UI doesn't directly reference weapon system

**Implementation:**
1. Create `OnInventoryItemEquipped` GameEvent asset
2. Add to InventoryUIController serialized field
3. Raise event in EquipSlot()
4. MMOWeaponController registers listener in OnPlayerSpawn()

### C. IPlayerAddon Pattern
**Current**: MMOAddon manages inventory UI state
**Expansion**: MMOAddon should manage high-level weapon state

**New Component Needed**: MMOWeaponController (similar to ShooterAddon.WeaponController)

**Integration Points:**
- MMOAddon.OnPlayerSpawn() → initializes weapon controller
- MMOWeaponController is separate component on same player GameObject
- MMOAddon can query weapon state via public properties

### D. Animation Integration
**Animator Parameters:**
- `weaponTypeID` (int): Switches between weapon animation sets (0 = unarmed, 1 = sword, 2 = bow, etc.)
- `IsAttacking` (bool): Triggers attack animations
- `IsSwitchingWeapon` (bool): Weapon equip/unequip transition

**Layer Weights:**
- Set weapon layer to 1.0 when equipped, 0.0 when not
- Smooth transition with lerp

**Blend Trees:**
- Separate trees for idle vs aiming (Shooter uses AimingBlendTree, IdleBlendTree)
- Each weapon type has entry in blend tree matched by weaponTypeID

---

## 5. HIGH-LEVEL IMPLEMENTATION STEPS

### ✅ COMPLETED: Sword Prefab & Attachment Points
- [x] Created Weapon_SwordDefault.prefab in Assets/MMO/Resources/Prefabs/Weapons/
- [x] Set up prefab structure: Root → Child (AttachableBehaviour) → Visual (mesh)
- [x] Added AttachableNode to Right_Hand_Attach bone in Armature_MMO
- [x] Configured ComponentController with mesh renderer
- [x] Set EnableOnAttach = true for auto-visibility on attach

### ✅ COMPLETED: Inventory & Weapon Spawning
- [x] Created MMOWeaponController.cs with event-driven architecture
- [x] Wired InventoryUIController → GameEvent → MMOWeaponController
- [x] OnInventoryItemEquipped and OnInventoryItemUnequipped game events created
- [x] Weapon spawning with SpawnWithOwnership(OwnerClientId) working
- [x] Weapon attaching to Right_Hand_Attach bone verified
- [x] ComponentController auto-shows mesh on attach
- [x] Updated Resources.Load path to Weapon_SwordDefault

---

## NEXT PHASE: ANIMATIONS & ATTACK SYSTEM

### Phase Goals
1. **Sword Grip Animation**: Character's hand grips sword naturally (not floating)
2. **Attack Mechanic**: Left-click to swing sword with attack animation
3. **Network Sync**: Attack animations and state synchronized across players

### Step 1: Create MMO Animator Controller
**Reference**: Shooter uses `ShooterAnimator.controller`

- [ ] Create `Assets/MMO/Animators/MMOAnimator.controller` (copy/adapt from Shooter pattern)
- [ ] Add animation layers:
  - [ ] Base Layer: Idle, Walk, Run (unarmed state)
  - [ ] Weapon Layer: Sword idle/walk/run (overlay when weapon equipped)
  - [ ] Attack Layer: Sword swing animations (additive)
- [ ] Add parameters:
  - [ ] `weaponTypeID` (int) - 0=unarmed, 1=sword, 2+=future weapons
  - [ ] `IsAttacking` (bool) - triggers attack state
  - [ ] `AttackType` (int) - 0=light, 1=heavy, 2=special (future)
  - [ ] `IsEquipped` (bool) - weapon equipped state
- [ ] Assign to MMOPlayer prefab's Animator component
- [ ] Test: Controller loads without errors, parameters visible in Animator window

### Step 2: Setup Animation Clips for Sword
**Use Danvil Rusty Sword rig or create basic animations**

- [ ] Find/create sword idle animation clip
- [ ] Find/create sword walk animation clip
- [ ] Create sword attack/swing animation clip (or use placeholder)
- [ ] Import clips into project
- [ ] Create animation directory structure: `Assets/MMO/Animations/Sword/`

### Step 3: Build Animator State Machine (Sword Grip Pose)
**Goal**: When sword equipped, character's hand is on grip properly

- [ ] In Weapon Layer, create states:
  - [ ] Sword_Idle (references sword idle animation clip)
  - [ ] Sword_Walk (references sword walk animation clip)
  - [ ] Sword_Run (references sword run animation clip)
- [ ] Create blend tree for locomotion (Sword_Idle → Sword_Walk → Sword_Run based on speed)
- [ ] Add transitions:
  - [ ] (Any) → Sword_Idle when weaponTypeID==1 and !IsAttacking
  - [ ] Base Layer and Weapon Layer stay synced
- [ ] Set Weapon Layer weight to 1.0 when weapon equipped, 0.0 when not
  - [ ] Update MMOWeaponController.EquipWeapon() to set `animator.SetLayerWeight(1, 1.0f)`
  - [ ] Update MMOWeaponController.UnequipWeapon() to set `animator.SetLayerWeight(1, 0.0f)`
- [ ] Test in scene: Equip sword → character automatically plays sword idle pose with hand on grip

### Step 4: Setup Attack Animation Layer & State Machine
**Goal**: Left-click triggers attack animation**

- [ ] Create Attack Layer (additive blending)
- [ ] Create Sword_Attack state:
  - [ ] References sword swing animation clip
  - [ ] Loop = false (one-shot attack)
  - [ ] Transitions back to Idle when animation completes
- [ ] Add transition: (Any) → Sword_Attack when IsAttacking==true
- [ ] Add parameter reset logic: Set IsAttacking=false after attack animation finishes (use animation event)
- [ ] Set Attack Layer weight to 1.0 (always active)
- [ ] Test: No sword equipped → no attack available; Sword equipped → animations play correctly

### Step 5: Create Attack Input System (Left-Click)
**Pattern**: Follow Shooter's input event system

- [ ] Check if Shooter has input handling in MMOAddon equivalent (player input component)
- [ ] Create MMOAttackController.cs OR add to MMOWeaponController:
  - [ ] Listen for left-click input (Input.GetMouseButtonDown(0))
  - [ ] Only trigger attack if:
    - [ ] Player is owner (IsOwner check)
    - [ ] Weapon is equipped
    - [ ] Not already attacking
    - [ ] Not during animation transition
  - [ ] Call Attack() method on current weapon
  - [ ] Set IsAttacking=true parameter
- [ ] Create SwordAttackEvent (GameEvent) for decoupled attack communication
- [ ] Verify input works in single-player: Click → attack animation plays

### Step 6: Network Attack Synchronization
**Goal**: All players see attack animation when someone attacks**

- [ ] Add to MMOWeaponController:
  - [ ] `[ServerRpc]` method: OnAttackPressed_ServerRpc()
  - [ ] `[ClientRpc]` method: PlayAttackAnimation_ClientRpc()
- [ ] Attack flow:
  - [ ] Player presses left-click → Local check (can attack?)
  - [ ] If valid → call ServerRpc on authority
  - [ ] Server validates attack → calls ClientRpc to all clients
  - [ ] All clients play attack animation simultaneously
- [ ] Alternative (simpler): Use NetworkAnimator component
  - [ ] Unity's built-in NetworkAnimator syncs Animator parameters automatically
  - [ ] Just set `animator.SetBool("IsAttacking", true)` locally
  - [ ] NetworkAnimator handles RPC calls behind scenes
  - [ ] Reference: Check if Shooter uses NetworkAnimator or manual RPCs
- [ ] Test: Equip sword, left-click → all players see attack animation

### Step 7: Animation Rigging for Hand Grip (Advanced)
**Goal**: Hand automatically grips sword handle naturally (not floating)**

**Two approaches:**
- **A) Baked Animation** (simpler): Hand positions baked into animation clips
  - Pros: Works immediately, no setup
  - Cons: Same grip for all sword sizes, less realistic
  
- **B) IK Rigging** (advanced): Procedural hand positioning to grip
  - [ ] Add Rig Builder component to player
  - [ ] Create grip point on sword prefab (empty child at handle)
  - [ ] Add Two-Bone IK Constraint (arm IK)
  - [ ] Target = grip point on sword
  - [ ] Enable when weaponTypeID==1, disable when ==0
  - [ ] Pros: Works with any weapon size/shape, most realistic
  - [ ] Cons: More setup, needs testing on multiplayer

- [ ] Recommend B if time permits (Shooter uses animation rigging)
- [ ] Fallback to A if needed

### Step 8: Attack Feedback
**Goal**: Visual/audio feedback when attack is triggered**

- [ ] Add weapon hit detection (raycast or collider-based)
- [ ] Create attack hitbox with OnTriggerEnter for enemies (future)
- [ ] Add weapon swing VFX (particle trail or visual feedback)
- [ ] Add sound effect on attack press
- [ ] Add damage calculation (future phase)

### Step 9: Multiplayer Attack Testing
- [ ] Host + Client setup
- [ ] Host equips sword, left-click → client sees attack animation
- [ ] Client equips sword, left-click → host sees attack animation
- [ ] Verify animation timing is synced across network
- [ ] Test rapid clicking (attack spam prevention)
- [ ] Verify attack doesn't happen during other animations

### Step 10: Polish & Edge Cases
- [ ] Add attack cooldown (can't spam-click, must wait for animation)
- [ ] Add animation event to reset IsAttacking parameter
- [ ] Prevent attack during equip/unequip animation
- [ ] Handle weapon swap during attack (interrupt and cancel)
- [ ] Test movement during attack (can move while swinging)
- [ ] Audio: Footstep sounds, sword whoosh sound on attack

---

## 6. KEY DESIGN QUESTIONS

### Question 1: Weapon Visibility
**Do you want one weapon at a time** (like the sword always in hand when equipped)?

Options:
- A) Only one weapon visible at a time (equipped = in hand, unequipped = disappears)
- B) Multiple weapons visible (equipped in hand, others holstered on body)
- C) Single weapon that toggles between hand and holster position

### Question 2: Unequipped State
**Should unequipped state** show weapon holstered on back/hip, or disappear entirely?

Options:
- A) Weapon disappears completely when unequipped
- B) Weapon moves to holster attachment point (IdleWeaponAttachNode)
- C) Weapon remains in hand but plays different animation

### Question 3: Animation Scope
**Animation scope**: Simple "hold sword" or full attack animations too?

Phase 1 (Minimum):
- Idle with weapon
- Walk/run with weapon

Phase 2 (Future):
- Attack animations
- Block/parry animations
- Ability animations (special attacks)

### Question 4: Multiplayer Priority
**Should other players see your equipped weapon immediately?**

Options:
- A) Instant sync (NetworkVariable updates immediately)
- B) Animation-synced (weapon appears after equip animation plays)
- C) Delayed (weapon appears after slight delay for smoothness)

---

## 7. REFERENCE FILES

### Code References
```
Assets/Shooter/Scripts/Runtime/Components/WeaponController.cs
Assets/Shooter/Scripts/Runtime/Components/ModularWeapon.cs
Assets/Shooter/Scripts/Runtime/Framework/Weapons/WeaponData.cs
Assets/Shooter/Scripts/Runtime/Framework/ShooterNetworkDataStructures.cs
Assets/Shooter/Scripts/Runtime/Components/AimController.cs
```

### Prefab References
```
Assets/Shooter/Prefabs/[BB] ShooterPlayer.prefab
Assets/Shooter/Prefabs/Weapons/Weapon_HandGun.prefab
Assets/Shooter/Prefabs/Weapons/Weapon_Shotgun.prefab
Assets/Shooter/Prefabs/Weapons/Weapon_AssaultRifle.prefab
```

### Asset References
```
Assets/Danvil/Rusty Sword (sword mesh to use)
Assets/Core/Settings/PanelSettings.asset (UI rendering settings)
Assets/MMO/UI/MMOPlayerHUD.uxml (current UI structure)
```

### Documentation
- Unity Netcode for GameObjects - AttachableBehaviour
- Unity Animation Rigging Package
- Unity Animator Controllers & Blend Trees
- Blocks Framework Architecture (inspect Shooter/Platformer examples)

---

## 8. CURRENT IMPLEMENTATION STATUS

### ✅ WEAPONS & EQUIPPING (PHASE 1 COMPLETE)
- Inventory UI with 4x4 grid (16 slots)
- Generic equip/unequip system (visual border feedback)
- Mutual exclusivity (only one item equipped at a time)
- Two sword icons in slots 0 and 5 for testing
- Network-ready player spawning system
- Sword prefab with ComponentController (auto-show on attach)
- Armature_MMO with Right_Hand_Attach node
- MMOWeaponController with event-driven spawning
- OnInventoryItemEquipped/OnInventoryItemUnequipped GameEvents
- Weapon networking with SpawnWithOwnership() and AttachableBehaviour
- **Sword visually appears in hand when equipped** ✨

### 🔄 IN PROGRESS (PHASE 2: ANIMATIONS & ATTACKS)
- Animator Controller setup
- Sword grip pose animation
- Attack input system
- Attack animation and network sync

### ⏳ NOT STARTED
- Advanced features (Animation Rigging IK, multiple weapons, special attacks)

---

## 9. NEXT SESSION PLAN

**Current Status**: Sword equipped and visible in hand! 🎉

**Starting Point**: Phase 2 - Animations & Attacks (Step 1 above)

**Goal**: Get character to hold sword with proper grip pose + left-click to swing

**Estimated Time**: 2-3 hours (Steps 1-6 for basic attack working)

---

## Notes & Considerations

### Performance
- Consider object pooling for weapons if players frequently swap
- AttachableBehaviour is network-efficient (Netcode handles sync)
- Animation Rigging has CPU cost - use sparingly on many characters

### Multiplayer
- All weapon equip logic should be owner-authoritative
- NetworkVariables ensure state consistency
- AttachableBehaviour handles transform sync automatically

### Extensibility
- System should support multiple weapon types (swords, bows, shields, etc.)
- WeaponData ScriptableObjects make adding new weapons designer-friendly
- Inventory slot → weapon type mapping should be data-driven

### Testing Strategy
1. Test single-player first
2. Test multiplayer locally (host + client on same machine)
3. Test over network (separate machines)
4. Test edge cases (equip during movement, rapid swapping, respawn with equipped weapon)

---

## Git Commits Plan

After each major milestone:
- Step 1-2 complete: "Add sword weapon prefab and attachment points"
- Step 3-5 complete: "Implement weapon spawning and inventory integration"
- Step 6 complete: "Add basic weapon animations"
- Step 7 complete: "Add animation rigging for realistic grip"
- Step 8 complete: "Verify multiplayer weapon synchronization"

Each commit should be tested and verified working before pushing.
