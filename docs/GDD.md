# Game Design Document — Whispers of the Green Mist

> A mystical race against time, where every decision shapes the fate of your village.

**Genre:** Action / Adventure
**Players:** Single player
**Format:** 3D, third person
**Platform:** PC (console adaptation planned)
**Language:** C#
**Estimated length:** ~1 hour 30 minutes

This is the English version of the design document. The Spanish original is in [GDD.es.md](GDD.es.md), and the source file it was written in is [GDD.es.docx](GDD.es.docx).

---

## 1. Synopsis

In a village crippled by drought, Onji — a frog messenger — sets out to carry a Daruma amulet to the shrine at the mountain summit. An old legend says that completing the ritual before dawn will call the rain and save her people. The clock starts the moment the Daruma's left eye is painted.

---

## 2. Core elements

- Exploration of villages, bamboo forests and hidden shrines
- Item deliveries and small messenger errands in the opening act, which double as the tutorial
- Encounters with mystical predators — mist foxes, shadow frogs, forest beasts — that act as both enemies and benevolent spirits
- Gradual learning: mechanics unlock or are discovered naturally as the story advances, never dumped on the player at once
- Hints hidden inside objects: delivering certain items to NPCs earns small visual objects (drawings, notes, symbols) that serve as direct references for solving puzzles
- Stealth and observation in forest zones where direct combat may not be the best answer
- Survival and endurance: the summit must be reached before dawn, with limited resources

---

## 3. Gameplay

The player controls Onji, an anthropomorphic Japanese tree frog who begins the story as a village messenger. The game runs in three acts.

### A. Tutorial — The Village

Onji receives simple messenger errands: delivering letters, packages and talismans to the village NPCs. Some errands require gathering resources, such as cutting lengths of bamboo, which introduces light combat gently.

The player learns movement, interaction, item use and the basics of combat. Items obtained in this phase — bamboo, talismans, herbs — are used later as tools or resources.

### B. Exploration and journey

At night, Onji enters the haunted forest and begins the climb. Alternative routes, environmental riddles and strategic use of gathered items are all encouraged.

**Strategic combat and stealth.** More complex enemies introduce attack patterns and force the player to choose between fighting, dodging, or moving through in stealth. The combat system is quick and accessible, built around strategy and reading the environment — a simplified take on the *Assassin's Creed* feel.

**Resource and time management.** The player must reach the shrine before dawn. Narrative tension is held by limited resources and by strategic choices about which route to take and when to risk a fight.

### C. Climax — The Shrine

The journey ends in a confrontation with the Guardian of the Shrine. On defeating him and completing the ritual in time, Onji discovers her true destiny: to become the new guardian and protector of the natural balance.

---

## 4. Levels

| Level | Content |
|---|---|
| **The Village** (tutorial) | Item deliveries, introduction of basic mechanics, first combat against minor creatures and environment objects |
| **Haunted Bamboo Forest** | Non-linear exploration, environmental riddles, mini-boss |
| **Rope Bridge** | Balance challenge under extreme wind, mini-boss |
| **Summit Shrine** | Final riddles and the Guardian fight, Souls-lite boss encounter |

---

## 5. Puzzles

### Aligned runes (Act B)

Three or four wooden totems marked with symbols. The player must strike or rotate the correct ones so they match a sequence seen earlier — the hint arrives as an object from an NPC, such as a small map, a photograph or a drawing. Solving it opens the path leading to the bridge.

### Guardian statues (Act B, after the bridge)

Roughly four stone statues — komainu or frogs. The player must rotate or position them so they point toward the shrine, or align with a pattern in the environment: constellations, the direction of the moon, symbols on the ground. This is a door puzzle: solving it opens the Torii gate to the shrine and the final boss.

### The shrine ritual (Act C)

Onji places the Daruma on the altar and begins the ritual by activating symbols in the correct order, tracing the kanji for rain (雨).

The ritual runs in parallel with combat: the longer it takes, the more enemies arrive to interrupt it. These are mist creatures — individually weak but numerous, in the spirit of the spectral hyena adds in the Anubis fight from *Assassin's Creed Origins*. Just before completion, the Guardian of the Shrine breaks in. The player must defeat the Guardian to finish the symbol sequence and complete the ritual that calls the rain.

---

## 6. Player definition

Onji is an anthropomorphic frog messenger who becomes a spiritual hero over the course of the journey. Her abilities combine agile movement, use of ritual objects and light combat against mystical creatures.

### Properties

| Property | Behaviour |
|---|---|
| **Health** | Health bar. When it drops too low, movement speed and dodge capability degrade. At zero, the player dies |
| **Stamina** | Governs dodging and quick actions. Drains with each dodge, recovers slowly while resting |
| **Weapon** | A single main weapon — an improvised bamboo staff / naginata — used both for combat and for interacting with certain environment elements |
| **Time remaining** | A global countdown to dawn. As dawn approaches, narrative pressure and urgency rise |

### Actions

Move, jump, crouch, dodge, attack, interact with the environment, use consumables.

### Items and pickups

| Item | Effect |
|---|---|
| **Healing herbs** | Restore part of health |
| **Protection talismans** | Reduce damage taken for a limited time |
| **Omamori** (power talisman) | Rare and strictly limited, roughly 2 maximum. Enables the special ability (R); consumed on use and costs half the stamina bar |
| **Edible insects** | Light healing plus a temporary endurance boost |
| **Gyoza** | Moderate healing plus a brief regeneration buff |
| **Daruma** (key item) | Must be carried and protected all the way to the shrine |

---

## 7. Controls

Designed for PC first. A console adaptation maps to a standard gamepad scheme.

| Action | Key | Notes |
|---|---|---|
| Move | WASD | Direction relative to the camera, which is controlled with the mouse |
| Sprint | Shift | Hold |
| Crouch | Ctrl | Stealth |
| Jump | Space | Hold for extra height |
| Interact | E | Hold for extended actions |
| Inventory | Tab | Time pauses |
| Quick item select | 1, 2, 3, 4 | Changes the active slot |
| Use equipped item | Q | Consumes the item |
| Light attack | Left click | No stamina cost |
| Heavy attack | Right click | Hold to charge, small stamina cost |
| Special ability | R | Requires an equipped talisman, which is consumed; costs half the stamina bar |
| Dodge | Alt | Rolls in the current movement direction, costs stamina |

---

## 8. User interface

- Health bar, bottom centre
- Stamina indicator, directly below the health bar
- A small clock showing time remaining until dawn, top centre
- Equipped item icon, bottom right
- Discreet contextual prompts that appear only near an interactive object or NPC

### Menus

Title screen with a calm Edo-period soundtrack over an Ukiyo-e styled background.

Options: sound, brightness, language, new game / continue, and difficulty (Easy, Normal, Hard).

---

## 9. Win and lose conditions

**Win.** Reach the shrine with the Daruma and complete the ritual before dawn.

**Lose.** Die in combat, or fail to reach the shrine in time.

### Endings

**Success.** Onji completes the ritual, calls the rain and saves the village. She also discovers that the ritual's true purpose was never only to bring rain, but to name a new guardian of the shrine. In defeating the old guardian she takes his place, securing the shrine and the balance of nature. Her sacrifice reaches past the personal: she stops being a messenger and becomes a protector spirit. She is a hero who does not return.

**Failure.** The amulet shatters, and the drought becomes an eternal curse.

---

## 10. Setting and theme

The game is set in a world drawn from traditional East Asian landscapes: haunted bamboo forests, rural villages, and shrines hidden in the mist. Its central theme is the spiritual and heroic journey, where personal sacrifice outweighs the physical adventure. Mysticism, nature and the urgency of a countdown are combined, and reinforced by Edo-period music and visual style.

The atmosphere is predominantly dark and melancholic, lit by the faint glow of lanterns that guide the player through the gloom — closer in feel to *Stray* or to Japanese streets at night.

Sound design leans on traditional Japanese instruments: the koto (13-string zither), the shakuhachi (bamboo flute) and the shamisen (three-string lute).

---

## 11. Design guidelines

**Atmosphere first.** Every element — art, music, lighting, narrative — must reinforce the mysticism, melancholy and urgency that define Onji's journey.

**Progressive, fair difficulty.** The game opens with simple, accessible mechanics, evolves through more complex enemies, and culminates in a demanding Souls-lite boss, without frustrating the casual player.

**Meaningful exploration.** Every environment must include optional routes, secrets or hints that reward curiosity. Exploration should never feel empty.

**Organic learning.** Mechanics are introduced in natural situations, avoiding intrusive tutorials.

**Time as a narrative resource.** The dawn deadline is not only a pressure mechanic but a narrative pillar that guides decisions and reinforces dramatic tension.

**Modular design.** Each level works as an independent piece while remaining connected within the story's progression, which allows them to be adjusted and tested separately.

**Resource economy.** Items and amulets must carry clear, limited value, so that using them is a strategic decision.

---

## 12. Audience and references

Aimed at players aged roughly 15 to 35 with intermediate gaming experience, looking for immersive narrative experiences with exploration, fantastical settings, and combat that is accessible but epic.

**References:** *God of War*, *Stray*, *Ghost of Tsushima*, *Assassin's Creed*.

### Why this is fun

It combines immersive exploration, environmental riddles and a progressive combat system that builds toward a demanding final encounter. The pressure of the dawn deadline holds tension constantly, while dialogue and hidden hints reward attention and curiosity. Each level offers a distinct challenge, avoiding repetition and keeping a varied, memorable experience despite the short running time.

The world carries a nostalgic, mystical air — an echo of older times — wrapping the player in a melancholic, almost dreamlike tone.
