# Player Animation Setup Guide

## Step 1: Create Animator Controller
1. Right-click in Project window → Create → Animator Controller
2. Name it "PlayerAnimator"

## Step 2: Add Parameters
In the Animator Controller Inspector, Parameters tab:
- **State** (Int) - Controls which animation state to play
- **IsGrounded** (Bool) - Whether player is on ground
- **IsGrabbing** (Bool) - Whether player is grabbing
- **VelocityX** (Float) - Horizontal velocity
- **VelocityY** (Float) - Vertical velocity

## Step 3: Create States
In the Animator window, right-click → Create State → Empty:
1. **Idle** - Default state
2. **WindUp** - When player is dragging to launch (NEW!)
3. **Jump** - When jumping up
4. **JumpForward** - When jumping with horizontal movement
5. **Fall** - When falling down
6. **Land** - When landing on ground
7. **Spit** - Attack animation
8. **SplatWall** - When grabbing walls
9. **Injured** - When taking damage
10. **Recover** - Recovery animation
11. **Death** - Death animation
12. **Spike** - Another attack

## Step 4: Set up Transitions (CRITICAL - This is likely your issue!)

**Your Animator Controller has the parameters but is missing the conditional transitions!**

### Required Transitions Setup:

1. **Right-click in the Animator graph area** → Create State → Empty
2. **Create these states** (if not already present):
   - **Idle** (you have this)
   - **Jump** 
   - **JumpForward**
   - **Fall**
   - **Land**
   - **Spit**
   - **SplatWall**
   - **Injured**
   - **Recover**
   - **Death**
   - **Spike**

3. **Create Transitions from Any State:**
   - **Right-click on "Any State"** → Make Transition → Select "Idle"
   - **Click on the transition arrow** (should become highlighted)
   - **In Inspector**: Set Condition to "State equals 0"
   - **Repeat for all other states**:
     - Any State → WindUp: Condition = "State equals 1"
     - Any State → Jump: Condition = "State equals 2"
     - Any State → JumpForward: Condition = "State equals 3"
     - Any State → Fall: Condition = "State equals 4"
     - Any State → Land: Condition = "State equals 5"
     - Any State → Spit: Condition = "State equals 6"
     - Any State → SplatWall: Condition = "State equals 7"
     - Any State → Injured: Condition = "State equals 8"
     - Any State → Recover: Condition = "State equals 9"
     - Any State → Death: Condition = "State equals 10"
     - Any State → Spike: Condition = "State equals 11"

4. **Transition Settings:**
   - **Uncheck "Has Exit Time"** for immediate transitions
   - **Set Transition Duration** to 0.1 seconds
   - **Make sure "Fixed Duration"** is unchecked

### Quick Fix Steps:

1. **Open your Animator Controller** (the one shown in your screenshot)
2. **Entry Point Setup** (if not already done):
   - **Entry should connect to "Idle"** (this is your default state)
   - **Right-click on "Entry"** → Make Transition → Select "Idle"
   - **This transition should have NO conditions** (it's automatic)
3. **Add Conditional Transitions from Any State:**
   - **Right-click on "Any State"** → Make Transition → Select "Idle"
   - **Click the transition arrow** → In Inspector → Add Condition → State equals 0
   - **Uncheck "Has Exit Time"** for immediate transitions
4. **Repeat for Jump state**: Any State → Jump, Condition = State equals 1
5. **Continue for all states** (Fall = 3, Landing = 4, etc.)
6. **Test**: Run the game and check if animations change when state changes

### Common Issues:
- **"State equals 0"** not "State equals State.0"
- **Uncheck "Has Exit Time"** for immediate transitions
- **Make sure transitions point FROM "Any State"** not from individual states

## Step 5: Assign to Player
1. Select Player GameObject
2. In Animator component, assign the PlayerAnimator controller
3. The PlayerStateMachine will automatically update parameters

## Step 6: Assign Animation Clips
For each state, assign the corresponding animation clip:
1. **Idle** → `slime_idle.anim` (or your idle animation)
2. **WindUp** → `slime_jump_up.anim` (first half of jump animation - wind up part)
3. **Jump** → `slime_jump_up.anim` (second half of jump animation - launch part)
4. **JumpForward** → `slime_jump_forward.anim` (if you have one)
5. **Fall** → `slime_fall.anim`
6. **Land** → `slime_land_down.anim`
7. **Spit** → `slime_spit.anim`
8. **SplatWall** → `slime_splat_wall.anim`
9. **Injured** → `slime_injured.anim`
10. **Recover** → `slime_recover.anim`
11. **Death** → `slime_death.anim`
12. **Spike** → `slime_spike.anim`

## State Enum Values:
- Idle = 0
- WindUp = 1
- Jumping = 2
- JumpingForward = 3
- Falling = 4
- Landing = 5
- Spitting = 6
- SplatWall = 7
- Injured = 8
- Recovering = 9
- Dead = 10
- Spiking = 11

## Troubleshooting Animation Issues:

### 1. Idle Animation Keeps Resetting:
- **Cause**: Ground check is too sensitive or state transitions are happening too frequently
- **Fix**: I've already improved the ground check to be more stable and added state change cooldowns
- **Check**: Look at the Console for debug logs about state changes

### 2. Animations Not Playing or Restarting After 2 Frames:
- **Check 1**: Verify Animator Controller is assigned to the Player GameObject
- **Check 2**: Verify animation clips are assigned to each state in the Animator Controller
- **Check 3**: Verify parameter names match exactly (State, IsGrounded, IsGrabbing, VelocityX, VelocityY)
- **Check 4**: Look at the Console for debug logs about animator parameters
- **Check 5**: **NEW** - Check if transitions in Animator Controller are set to "Has Exit Time" for proper timing
- **Check 6**: **NEW** - Verify transition conditions are set correctly (State equals X, not State equals State.X)

### 3. State Transitions Not Working:
- **Check**: Open the Animator window (Window → Animation → Animator) and watch the state changes in real-time
- **Verify**: The State parameter should change when you move the player

### 4. **NEW: Animation Restarting Issue - Common Causes:**

#### A. Animator Controller Setup Issues:
1. **Exit Time Not Set**: In the Animator Controller, select transitions and check "Has Exit Time"
2. **Transition Duration**: Set transition duration to 0.1-0.2 seconds for smooth blending
3. **Condition Logic**: Make sure transitions use "State equals 0" not "State equals State"

#### B. State Machine Issues:
1. **Constant State Changes**: Added cooldown to prevent rapid state transitions
2. **Ground Check**: Improved to be more stable
3. **Debug Logging**: Check Console for state change frequency

#### C. Animation Clip Issues:
1. **Loop Settings**: Non-looping animations (Jump, Fall, Land) should have `m_LoopTime: 0`
2. **Looping animations** (Idle) should have `m_LoopTime: 1`

### 5. **Quick Fix Steps:**
1. **In Animator Controller**: Select all transitions → Check "Has Exit Time" → Set transition duration to 0.1
2. **Check Parameters**: Verify State parameter is an Integer, not Float
3. **Test**: Run game and check Console for debug logs about state changes
4. **Verify**: Animations should now play completely before transitioning

## Tips:
- Use "Any State" transitions for immediate state changes
- Set transition duration to 0.1-0.2 seconds for smooth blending
- You can add more complex conditions using VelocityX/VelocityY
- Test by running the game and watching the Animator window
- Enable debug logging in the Console to see state changes and animator parameters

### 6. **NEW: Wrong Animation Playing Issue:**

#### **Problem**: States are changing correctly but wrong animations are playing

#### **A. Animation Clip Assignment Issues:**
1. **Check Animation Clip Assignment**:
   - Select each state in your Animator Controller
   - In Inspector → Motion field → Verify correct animation clip is assigned
   - **Idle state** should have `slime_idle.anim`
   - **Jump state** should have `slime_jump_up.anim`
   - **Fall state** should have `slime_fall.anim`
   - **Land state** should have `slime_land_down.anim`

2. **Check Animation Clip Names**:
   - Make sure animation clip names match your state names
   - State name: "Jump" → Animation clip name: "slime_jump_up"
   - State name: "Fall" → Animation clip name: "slime_fall"

#### **B. Transition Priority Issues:**
1. **Check Transition Order**:
   - Transitions are evaluated in order (top to bottom)
   - **Higher priority transitions** (at the top) will trigger first
   - **Move important transitions higher** in the list

2. **Check Transition Conditions**:
   - Verify conditions are set correctly: "State equals 1" not "State equals 0"
   - Make sure conditions use the correct parameter name: "State"

#### **C. State Name Mismatch:**
1. **Check State Names**:
   - State names in Animator Controller must match exactly:
   - "Idle" (not "idle" or "IDLE")
   - "Jump" (not "jump" or "JUMP")
   - "Fall" (not "fall" or "FALL")

2. **Check for Typos**:
   - "JumpForward" (not "Jump_Forward" or "JumpForward")
   - "SplatWall" (not "Splat_Wall" or "Splatwall")

#### **D. Debug Steps:**
1. **Run the game** and check Console for debug logs
2. **Look for these specific logs**:
   - `"Setting animator state: Jump (value: 1)"`
   - `"Animator Current State: Jump, IsName('Jump'): True"`
3. **Open Animator window** (Window → Animation → Animator)
4. **Watch the state changes** in real-time
5. **Check if the orange bar** moves between states correctly

#### **E. Quick Fix Checklist:**
- [ ] **Animation clips assigned** to each state
- [ ] **State names match** exactly (case-sensitive)
- [ ] **Transition conditions** use "State equals X"
- [ ] **Transition priority** (important transitions at top)
- [ ] **No conflicting transitions** from same source
- [ ] **Animator Controller assigned** to Player GameObject

## NEW: WindUp Animation System

### **What This Does:**
- **WindUp state** plays when player starts dragging (mouse down)
- **Jump state** plays when player releases drag (mouse up)
- **Animation timing** is now more responsive and natural

### **How It Works:**
1. **Player clicks and drags** → WindUp state triggers immediately
2. **WindUp animation plays** → Shows the "wind up" part of the jump sprite sheet
3. **Player releases drag** → Transitions to Jump state
4. **Jump animation continues** → Shows the "launch" part of the jump sprite sheet

### **Setup Instructions:**

#### **1. Animator Controller Setup:**
1. **Add new state**: Create "WindUp" state in Animator Controller
2. **Assign animation**: WindUp state uses `slime_jump_up.anim` (same as Jump)
3. **Add transition**: Any State → WindUp (State equals 1)
4. **Configure transition**: 
   - ✅ Uncheck "Has Exit Time"
   - ✅ Set duration to 0.1 seconds
   - ✅ Add condition: "State equals 1"

#### **2. Animation Clip Setup:**
- **WindUp state**: `slime_jump_up.anim` (plays from start to middle)
- **Jump state**: `slime_jump_up.anim` (plays from middle to end)
- **Alternative**: Create separate "wind_up.anim" and "jump_up.anim" clips

#### **3. Animation Timing:**
- **WindUp duration**: Should match the "wind up" portion of your sprite sheet
- **Transition timing**: WindUp → Jump happens when drag ends
- **Smooth blending**: 0.1 second transition duration

### **Benefits:**
- ✅ **Immediate feedback** when player starts dragging
- ✅ **Natural animation flow** from wind up to jump
- ✅ **Better timing** - animation starts before launch
- ✅ **More responsive feel** overall

### **Troubleshooting:**
- **WindUp not playing**: Check State equals 1 condition
- **Animation timing off**: Adjust transition duration or animation clip settings
- **Jump animation repeats**: Make sure "Has Exit Time" is unchecked
