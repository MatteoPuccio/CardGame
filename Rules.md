# CardGame — Rules & Glossary

---

## Table of Contents

1. [Overview](#overview)
2. [Game Setup](#game-setup)
3. [Win & Loss Conditions](#win--loss-conditions)
4. [Turn Structure](#turn-structure)
5. [Zones](#zones)
6. [Card Types](#card-types)
7. [Playing Cards](#playing-cards)
8. [Combat (Attack Phase)](#combat-attack-phase)
9. [Keywords](#keywords)
10. [Troop Races & Race Traits](#troop-races--race-traits)
11. [Spell Schools & Immunities](#spell-schools--immunities)
12. [Effects](#effects)
13. [Rapid Effects & the Chain](#rapid-effects--the-chain)
14. [Glossary](#glossary)

---

## Overview

CardGame is a two-player, turn-based card game where each player builds a deck, deploys troops to a shared board, casts spells, and performs rituals to reduce the opponent's **Life Points** to zero.

---

## Game Setup

| Parameter | Value |
|---|---|
| **Starting Life Points** | 30 |
| **Starting Hand Size** | 3 cards (drawn from the Main Deck) |
| **Deploy Points per Turn** | 3 (replenished at the start of each turn) |
| **Board Zones (per player)** | 7 Play Area Zones |

1. Each player constructs a deck consisting of a **Main Deck** and an **Extra Deck**.
   - The **Main Deck** contains Troops, Spells, and Champions.
   - The **Extra Deck** contains only **Ritual** and **Avatar** cards.
2. Both decks are shuffled.
3. Each player draws their starting hand of 3 cards from the Main Deck.
4. Player 1 takes the first turn.

---

## Win & Loss Conditions

- A player is **defeated** when their Life Points reach **0**.
- When a player is defeated, the match ends (or restarts).
- *Note: There is no deck-out loss condition currently implemented.*

---

## Turn Structure

Each turn proceeds through **five phases** in order:

### 1. Draw Phase

- The active player's **Deploy Points** are replenished to their per-turn allowance (default: 3).
- The active player draws **1 card** from the top of their Main Deck into their Hand.

### 2. Ritual Phase

- The active player may **play Ritual cards** from their Extra Deck or Hand during this phase (Rituals can *only* be played in this phase).
- Active Rituals already on the Ritual Zone may be **advanced by one stage** (once per turn per Ritual).
- Ritual stage advancement is player-triggered (not automatic).

### 3. Play Phase

- The active player may play **Troops** and **Spells** from their Hand.
- Troops require spending **Deploy Points** equal to their Deploy Cost.
- Spells resolve their effects and are then sent to the **Cemetery**.

### 4. Attack Phase

- The active player may declare attacks with Troops on the board.
- **No attacks are allowed on the very first turn** of the game (Turn 1).
- Each Troop may attack **once per Attack Phase**.
- See [Combat](#combat-attack-phase) for full details.

### 5. End Phase

- Any active attack targeting is cancelled.
- The turn passes to the opponent, who starts a new turn from the Draw Phase.

---

## Zones

Each player has the following card zones:

| Zone | Description |
|---|---|
| **Deck** (Main Deck) | The face-down draw pile. Players draw from the top. Contains Troops, Spells, and Champions. |
| **Extra Deck** | A separate pile that holds only **Ritual** and **Avatar** cards. Not drawn from; cards are played directly from here. |
| **Hand** | Cards held by the player, available to be played. |
| **Play Area** | The battlefield. Consists of **7 zones** per player; each zone can hold at most **one card** (Troop or Champion). |
| **Ritual Zone** | Where active Ritual cards reside while their stages are being advanced. |
| **Cemetery** | The discard/graveyard pile. Destroyed troops, resolved spells, and completed rituals end up here. |

### Zone Movement Rules

- **Extra Deck Return Rule**: Ritual and Avatar cards that would be returned to the Main Deck or Hand by an effect are instead redirected to the Extra Deck (unless the move is a direct player interaction like clicking in the Cemetery UI).
- A Play Area Zone can only accept cards whose type requires a play zone (Troops and Champions). It must also be unoccupied.

---

## Card Types

### Troop

The primary combat unit. Troops have the following stats:

| Stat | Description |
|---|---|
| **Power** | The amount of damage this troop deals in combat. Minimum 0. |
| **Health** (HP) | The troop's hit points. When reduced to 0, the troop dies and is sent to the Cemetery. |
| **Max Health** | The upper cap for Health. Health cannot exceed Max Health. Minimum 1. |
| **Deploy Cost** | The number of Deploy Points required to play this Troop from hand. |
| **Race** | An optional race classification (see [Troop Races](#troop-races--race-traits)). |
| **Keywords** | Special abilities (see [Keywords](#keywords)). |

- Troops occupy a **Play Area Zone** when in play.
- Troops can attack enemy Troops or the enemy Player during the Attack Phase.
- When a Troop takes lethal damage (HP reaches 0), it **dies** and is moved to its owner's Cemetery.

### Spell

A one-shot card that resolves its effect and is immediately sent to the **Cemetery**.

- Spells may belong to a **Spell School** (see [Spell Schools](#spell-schools--immunities)).
- Spells can have targeted or untargeted effects.

### Ritual

A multi-stage card that lives in the **Extra Deck** and can only be played during the **Ritual Phase**.

- When played, a Ritual is moved to the owner's **Ritual Zone**.
- Each turn (during the Ritual Phase), the owning player may **advance the Ritual by one stage**.
- Each stage has an associated effect that resolves when advanced.
- A Ritual can only be advanced **once per turn**.
- When all stages are completed, the Ritual is **destroyed** and sent to the Cemetery.
- Some Ritual stages may have **Rapid Effects** that activate through the chain system rather than manual clicking.

### Avatar

A special card type that resides in the **Extra Deck**.

- Avatar rules are not yet fully implemented.
- Avatars can only be placed in the Extra Deck.

### Champion

A board-presence card type (similar to Troops in placement).

- Champions do not currently have extensive combat rules beyond occupying a Play Area Zone.
- *Note: Champion mechanics are still under development.*

---

## Playing Cards

### From Hand

1. The player selects a card from their Hand.
2. **Ownership check**: only the active player may play cards.
3. **Phase check**: Rituals can only be played during the Ritual Phase.
4. **Deploy Cost** (Troops only): the player must have enough Deploy Points. The cost is deducted upon successful placement.
5. **Triggered effects** ("When this is played" effects) are evaluated and resolved.
6. **After-play behavior**:
   - **Troops** → remain on the board in a Play Area Zone.
   - **Spells** → sent to the Cemetery after resolving.
   - **Rituals** → moved to the Ritual Zone (only from Ritual Phase).

### From Extra Deck

- Ritual and Avatar cards may be played from the Extra Deck (Rituals only during the Ritual Phase).

---

## Combat (Attack Phase)

### Overview

During the Attack Phase, the active player may declare attacks one at a time. Each declared attack is fully resolved before the next can be declared.

### Attack Rules

1. **Turn 1 restriction**: No attacks can be declared on the first turn of the game.
2. **Attacker requirements**:
   - Must be a Troop owned by the active player.
   - Must be in a Play Area Zone (on the board).
   - Must not have already attacked this phase.
3. **Defender selection**:
   - If the opponent has **no Troops in play**, the attack automatically targets the opponent Player directly.
   - If the opponent **has Troops**, the attacker must target an enemy Troop (unless the attacker has the **Bypass Troops** keyword).
   - If any defending Troop has **Taunt**, the attacker can *only* target Taunt Troops.

### Attack Resolution Steps

| Step | Description |
|---|---|
| **Start** | An attack start event is broadcast. |
| **Declare Attackers** | The player selects an attacker and a defender. A priority window opens for Rapid Effects. |
| **Damage Calculation** | Damage is calculated and applied (see below). Another priority window opens for Rapid Effects. |
| **End** | The individual attack concludes. The attacker may not attack again this phase. |

### Damage Calculation

- **Troop vs. Troop**: Both troops deal damage to each other simultaneously equal to their respective Power. The **First Strike** keyword can alter this (see [Keywords](#keywords)).
- **Troop vs. Player**: The attacking troop deals damage equal to its Power to the defending player's Life Points.
- When a player's Life Points reach 0, they are defeated.

### Priority Windows

After an attack is declared and after damage is applied, a **priority window** opens. During this window, both players may activate **Rapid Effects** in response.

---

## Keywords

Keywords are special abilities that modify how a Troop interacts with the game:

| Keyword | Description |
|---|---|
| **Taunt** | Enemies **must** attack this Troop if any Taunt Troops are present on the defender's side. When Taunt Troops exist, non-Taunt Troops and the Player cannot be targeted by attacks. |
| **First Strike** | This Troop deals its combat damage **before** a Troop without First Strike. If the First Strike Troop kills its opponent before the opponent strikes back, the opponent deals no damage. If both combatants have First Strike, combat proceeds normally (simultaneous). |
| **Lifesteal** | When this Troop deals damage (to Troops or Players), its owner's Life Points are **healed** by the same amount. |
| **Bypass Troops** | This Troop can attack the opposing **Player directly**, even if the opponent has Troops in play. Ignores the normal requirement to attack Troops first. |

Keywords can come from two sources:
- **Base Keywords**: defined directly on the card.
- **Race Keywords**: granted automatically by the Troop's Race (via the Race Definitions Database).

---

## Troop Races & Race Traits

Troops may optionally belong to a **Race**. Each race can grant passive traits and bonus keywords.

### Available Races

| Race | Description |
|---|---|
| **Drake** | Dragon-like creatures. |
| **Humanoid** | Human or human-like beings. |
| **Undead** | Reanimated or deathless entities. |
| **Beast** | Natural animals and wild creatures. |
| **Elemental** | Beings made of pure elemental energy. |
| **Demon** | Dark and infernal creatures. |
| **Machine** | Mechanical constructs. |
| **Plant** | Flora-based organisms. |
| **Insect** | Arthropods and similar creatures. |
| **Fae** | Magical woodland folk. |
| **Mythical** | Legendary and fantastical beings. |

### Race Traits

Races can grant **spell immunity traits**, making Troops of that race unaffected by spells of a particular school.

| Trait | Effect |
|---|---|
| **Immune to Earth Spells** | Unaffected by Earth-school spells. |
| **Immune to Air Spells** | Unaffected by Air-school spells. |
| **Immune to Fire Spells** | Unaffected by Fire-school spells. |
| **Immune to Water Spells** | Unaffected by Water-school spells. |
| **Immune to Chaos Spells** | Unaffected by Chaos-school spells. |
| **Immune to Order Spells** | Unaffected by Order-school spells. |

Races can also grant **bonus keywords** (e.g., a race might automatically give all its Troops the Taunt keyword).

A Troop's race can be changed at runtime by effects (e.g., `ChangeRaceEffect`), which also updates its race-granted keywords.

---

## Spell Schools & Immunities

Spells may belong to one of the following **Spell Schools**:

| School |
|---|
| **Earth** |
| **Air** |
| **Fire** |
| **Water** |
| **Chaos** |
| **Order** |

When a spell with a school targets Troops, the game checks each target's **Race Traits**. If a Troop's race grants immunity to that spell school, the Troop is **removed from the target list** and is unaffected by the spell.

---

## Effects

Effects are the core mechanic for card abilities. They can be triggered by playing cards, activated as rapid responses, or resolved through ritual stages.

### Available Effect Types

| Effect | Description |
|---|---|
| **Draw** | The card's owner (or targeted player) draws cards from their Deck. |
| **Damage Troop** | Deals a specified amount of damage to targeted Troop(s). |
| **Destroy** | Destroys a target card (Troops are destroyed via the damage pipeline, dealing lethal damage). |
| **Buff Troop** | Modifies a Troop's Power and/or Max Health by a given delta. Can optionally heal the troop by the added max health. |
| **Change Race** | Changes a Troop's race to a new race, updating race-granted keywords. |
| **Modify Deploy Points** | Adds or removes Deploy Points for the card's owner. |
| **Modify Life** | Directly adds or removes Life Points for the card's owner. |
| **Tutor (Select & Act)** | Lets the player search a zone (Deck, Cemetery, Hand, or Ritual Zone) for cards matching certain filters, then performs an action on the selected cards. |

### Effect Triggers

| Trigger | Description |
|---|---|
| **When This Is Played** | Fires when the card is played from hand or extra deck. |
| **When This Is Sent to Cemetery** | Fires when the card is moved to the Cemetery zone. |

### Optional Effects

Some triggered effects are marked as **optional**, meaning the owning player may choose whether or not to activate them.

---

## Rapid Effects & the Chain

**Rapid Effects** are special response effects that can be activated during **priority windows** (e.g., after an attack declaration or after damage is dealt).

### How They Work

1. A game event opens a **chain window** (e.g., an attack is declared).
2. Both players are prompted: they may choose to activate an available Rapid Effect or pass.
3. Rapid Effects can be chained — after one resolves, another priority window opens.
4. Chain resolution follows a stack-like pattern.

### Activation Conditions

Rapid Effects can have conditions that must be met:
- **Always**: can activate at any time a window is open.
- **Must Be In Play**: the card must be on the board.
- **Phase Condition**: must be in a specific turn phase.
- **Opponent Did Something**: triggers in response to an opponent's action.
- **Ritual Stage Equals**: only activates when the owning Ritual is at a specific stage.

### Activation Frequency

| Frequency | Description |
|---|---|
| **Whenever** | Can activate every time a window opens. |
| **Once Per Turn** | Can only activate once per turn. |

---

## Glossary

| Term | Definition |
|---|---|
| **Active Player** | The player whose turn it currently is. |
| **Attack Declaration** | The pairing of an attacking Troop with a defending target (Troop or Player). |
| **Avatar** | A special card type stored in the Extra Deck. (Rules not yet finalized.) |
| **Board / Play Area** | The area where Troops and Champions are deployed, consisting of 7 zones per player. |
| **Buff** | A positive modification to a Troop's Power and/or Max Health. |
| **Bypass Troops** | A keyword allowing direct attacks on the enemy Player even when enemy Troops are present. |
| **Cemetery** | The discard pile / graveyard where destroyed, resolved, or completed cards go. |
| **Chain** | A sequence of Rapid Effects activated in response to game events, resolving in stack order. |
| **Champion** | A card type that occupies a Play Area Zone. (Under development.) |
| **Damage** | Reduction of a Troop's Health or a Player's Life Points. |
| **Debuff** | A negative modification to a Troop's Power and/or Max Health. |
| **Deck (Main Deck)** | The face-down pile from which the player draws cards. Contains Troops, Spells, and Champions. |
| **Deploy Cost** | The number of Deploy Points a Troop requires to be played from hand to the board. |
| **Deploy Points (DP)** | A per-turn resource spent to play Troops. Replenished at the start of each turn (default: 3). |
| **Destroy** | Remove a card from the board and send it to the Cemetery. |
| **Draw** | Move the top card of the Deck into the player's Hand. |
| **Effect** | Any game action produced by a card (damage, draw, buff, etc.). |
| **Extra Deck** | A secondary deck containing only Ritual and Avatar cards. |
| **First Strike** | A keyword; the Troop deals combat damage before a Troop without First Strike. |
| **Hand** | The set of cards a player holds and can play. |
| **Health (HP)** | A Troop's current hit points. When it reaches 0, the Troop dies. |
| **Immunity** | A race trait that makes a Troop unaffected by spells of a specific school. |
| **Keyword** | A named ability on a Troop that modifies game rules (Taunt, First Strike, Lifesteal, Bypass Troops). |
| **Life Points (LP)** | A player's health total. Starts at 30; reaching 0 means defeat. |
| **Lifesteal** | A keyword; when this Troop deals damage, its owner heals for the same amount. |
| **Max Health** | The upper limit for a Troop's Health. Minimum 1. |
| **Phase** | One of the five sequential steps in a turn: Draw, Ritual, Play, Attack, End. |
| **Play Area Zone** | One of 7 slots per player on the board where a Troop or Champion can be placed. Each can hold one card. |
| **Power** | A Troop's attack stat, determining how much combat damage it deals. |
| **Priority Window** | A pause in game flow where both players may activate Rapid Effects. |
| **Race** | An optional classification for Troops (Drake, Humanoid, Undead, etc.) that can grant traits and keywords. |
| **Race Trait** | A passive ability granted by a Troop's race (e.g., spell school immunity). |
| **Rapid Effect** | A fast-response effect that can be activated during priority windows. |
| **Resolve** | Execute an effect's game logic. |
| **Ritual** | A multi-stage card played from the Extra Deck during the Ritual Phase, advancing one stage per turn. |
| **Ritual Zone** | The zone where active Ritual cards reside. |
| **Spell** | A one-use card that resolves its effect and is sent to the Cemetery. |
| **Spell School** | A classification for Spells (Earth, Air, Fire, Water, Chaos, Order) that interacts with race immunities. |
| **Stage (Ritual)** | One step in a Ritual's progression. Each stage has an effect. |
| **Tags** | Optional labels on cards (e.g., "Dragon", "Necromancy") used for filtering and archetypes. |
| **Taunt** | A keyword; enemies must attack this Troop if any Taunt Troops are present. |
| **Triggered Effect** | An effect that activates automatically when a specific game event occurs (e.g., "When this is played"). |
| **Troop** | The primary combat card type with Power, Health, and Deploy Cost. |
| **Turn** | A full cycle of phases (Draw → Ritual → Play → Attack → End) for one player. |
| **Tutor** | An effect that lets the player search a zone for specific cards. |
| **Zone** | A designated area where cards can exist (Deck, Hand, Play Area, Cemetery, Ritual Zone, Extra Deck). |
