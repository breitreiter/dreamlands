# Title
Health = 4
- A serious problem
Health = 3
- Another grim night
Health = 2
- You are dying
Health = 1
- Things are dire

# Subtitle
- You have a serious condition. You will lose health every night until the condition is treated.

# Situation table
## current health row
### Health in the evening
### 4 (or whatever the value is)
## condition row
### description
- [icon] Injured x 3
- "Consumed 1 bandage, removed 1 stack (now x2)" OR "You have no bandages"
- [icon] Poisoned x 2
- "Consumed 1 anti-venom, removed 1 stack (now healed)" OR "You have no anti-venom"
- You have untreated conditions (only if true)
### -1 OR 0
## final health row
### Health by morning
### 3 (or whatever)

# Minor events — collapsed, dimmed, secondary
- Food consumed, foraging results, spirit changes, rest recovery. Shown in text-dim below a divider. This is the "yes yes, you ate bread" section.

# Continue button (same as current)
