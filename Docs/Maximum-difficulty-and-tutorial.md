# Maximum difficulty and tutorial

The phone menu opens on Welcome. Comenzar entrenamiento opens the existing
individual/circuit selector; Ver tutorial opens a four-page text guide (preparation,
hands, feet, running). Back from either returns to Welcome. Account returns to the
page it was opened from. The tutorial does not launch exercises or capture results.
No additional Unity scene, media asset, or build-list entry is required.

Individual and circuit configuration now select a maximum, indexed 0–4. Each newly
loaded exercise starts at Inicial. Adaptation is preserved between sets through
ResumeExercise, and a newly loaded exercise in a circuit starts again at Inicial.

- Hands: five consecutive hits increase the level, three misses decrease it.
  Increasing is clamped to the selected maximum.
- Feet: start at 3.5 seconds per target. Five hits shorten the time by 0.25 seconds;
  three misses lengthen it. The minimum permitted time for each selected maximum
  is 3.5 / 3 / 2.5 / 2 / 1.5 seconds. Tracking loss handling is unchanged.
- Running: start with an 8-second response window. Five completed turns increase
  the level, three timeouts decrease it. Levels use 8 / 7 / 6 / 5 / 4 seconds,
  bounded by the maximum. Sensor interruptions and set endings do not change the
  level. The independent random wait remains 3–7 seconds, 20% per integer value.

Validation: all Assets/Scripts C# consumers were checked for the renamed option;
no old DifficultyLevel accesses remain. Source-level checks cover the initial
level, upper/lower clamps, running wait, and menu routes. Unity compilation and
Android layout/runtime testing are still required on the project machine.

Device checks:
1. Open MenuTelefono: Welcome must appear; verify training/tutorial/back/account.
2. Read all tutorial tabs and next/previous controls, including phone safe areas.
3. Choose Inicial as maximum for each exercise: success streaks must not accelerate it.
4. Choose Intermedio: repeated success must stop at the intermediate cap; misses
   must reduce difficulty. Repeat with a circuit and across rests/sets.
5. Confirm running cue waits remain 3–7 seconds after an attempt (tracking recovery
   may delay the next cue) and the response window is a separate timer.
