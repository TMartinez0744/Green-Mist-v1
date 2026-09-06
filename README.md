# Whispers of the Green Mist

A third-person action-adventure about a night that decides a village. Drought has broken the land, and Onji — a frog messenger, the person you send when something has to get somewhere — is handed a Daruma amulet and a legend: carry it to the shrine at the summit, complete the ritual before dawn, and the rain returns. The clock starts when the Daruma's left eye is painted.

The climb is the game. Onji leaves at nightfall and moves up through haunted bamboo, across a rope bridge in high wind, to the shrine at the top — carrying a bamboo staff and whatever she gathered on the way. Mist creatures hold the path; some can be fought, some are better avoided, and every minute spent choosing is a minute closer to sunrise. Reaching the summit in time saves the village, and costs Onji the trip home.

Built in Unity by a single developer. Design document: [docs/GDD.md](docs/GDD.md) (English) · [docs/GDD.es.md](docs/GDD.es.md) (Spanish original).

---

## Current state

**Playable today**, in a single scene: a third-person controller with camera-relative movement, sprint, crouch and a jump with coyote time and input buffering; melee combat with three attacks whose hitboxes open on animation timing windows, backed by knockback, damage flash and floating damage numbers; enemy AI running a patrol / chase / attack state machine on a baked NavMesh; health and death for both sides with a game over and retry flow; and a full menu flow — title, pause, game over, volume, and separate menu, gameplay and boss music. The village environment is built out in the scene.

**Partially in place.** Crouch works as movement, but enemies do not react to it, so stealth is not yet a mechanic. The special attack plays without a resource cost, and the heavy attack has no charge-by-holding. Enemies share a single behaviour and use placeholder models.

**Designed, not yet built.** The dawn timer and the day-night lighting it drives. Stamina. Inventory and consumables — herbs, talismans, Omamori, the Daruma. NPC dialogue and the messenger quests that carry the tutorial. The three environmental puzzles. The bamboo forest, bridge and summit shrine as distinct levels. The Guardian boss fight. Saves, difficulty, brightness and language options.

---

## Design decisions

**The dawn timer is a narrative pillar, not a stopwatch.**
A countdown normally reads as an arcade fail state bolted onto a story. Here the clock starts diegetically, when the Daruma's eye is painted, and it drives the lighting — the sky, the fog and the lantern glow all shift as dawn approaches. The player should be able to read the time remaining by looking at the world rather than at the HUD, which puts the deadline inside the fiction and forces every other system to negotiate against it.

**The tutorial is a job, not a tutorial.**
Movement, interaction, item use and combat have to be taught without a wall of prompts. So the first act is a run of messenger errands: delivering a letter teaches navigation and interaction, an errand that asks for lengths of bamboo teaches the first attack. Each mechanic arrives attached to a reason to use it, and the items gathered during the errands are the same ones that matter later on the mountain.

**Hints are objects, not a button.**
Environmental puzzles fail in one of two directions — the player stalls, or a hint button solves them for free. Instead, solutions are handed out as physical objects by NPCs: a drawing, a note, a small map, earned by completing a delivery. The hint stays in the world and in the player's inventory, acquiring it is a detour that costs time against the clock, and the reward goes to the player who explored rather than the one who pressed a key.

**The economy stays scarce so the special attack stays a decision.**
An ability on a cooldown becomes a rotation, and the player stops thinking about it. The special attack instead requires an Omamori — rare, capped at roughly two for the whole run, consumed on use, and costing half the stamina bar. With two charges for an entire night, spending one is a judgement about what is still ahead. Herbs and talismans follow the same rule: every consumable is a bet on the rest of the climb.

**Detours cost time, and that is the point.**
Optional content in a timed game is usually dead on arrival, because the clock tells the player to skip it. So the two systems are aimed at each other deliberately: alternative routes let the player avoid a fight, stealth zones make combat the expensive choice, and both take longer than walking straight through. Neither rushing nor exploring everything is the right answer, which leaves the route as the player's own reading of the risk.

---

## Art and sound direction

Edo-period Japan by way of Ukiyo-e: flat colour, strong silhouettes, and a palette that stays dark. The world is lit mostly by lanterns — small warm pools in a lot of gloom, closer to *Stray* or a Japanese street at night than to a fantasy forest. Mist is a constant, and it thins as the sky turns.

The score uses traditional instrumentation — koto, shakuhachi, shamisen — kept sparse, with silence doing much of the work between cues. *God of War* is the reference for how combat should read at close range; *Stray* for how a small protagonist should sit inside a large, indifferent world.

---

## Screenshots

![The path to the torii gate](docs/images/screenshot.png)

---

## Running the project

**Unity 6000.0.56f1**, Universal Render Pipeline. Open the folder from Unity Hub — the first import takes a few minutes while `Library` is rebuilt — then open `Assets/Scenes/SampleScene.unity` and press Play. The title screen comes up first.

| Action | Key |
|---|---|
| Move | WASD, mouse to look |
| Sprint | Shift (hold) |
| Crouch | Ctrl (hold) |
| Jump | Space |
| Light attack | Left click, or Z |
| Heavy attack | Right click, or X |
| Special attack | R, or C |
| Draw / sheathe weapon | Q |
| Pause | Esc |
