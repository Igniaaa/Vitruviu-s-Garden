# Basilica crafting implementation plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace automatic pieces with material purchases and a rhythmic workbench.
**Architecture:** A separately testable core owns recipes, inventory, reservations and beat judgments. Scene components connect it to existing dragging, phases, modal input and uGUI. Existing scene edits remain in the active Unity checkout.
**Tech Stack:** Unity 6000.3.18f1, C#, uGUI/TMP, Input System, Unity Test Framework.
**Spec:** ../specs/2026-11-06-basilica-crafting-design.md (approved by the user).

## Global Constraints

- Normal success consumes 100%; perfect success and failure consume 50%; only successes create one piece.
- Default 200 coins; stone 3, lime 2; foundations 4 stone/2 lime, pavement 6 stone/2 lime.
- Eight notes, six required hits, Space, 100 BPM, 150 ms tolerance; variable half-beat grid.
- No new packages; preserve current scene and scripts; work directly in the checkout bound to the open Editor.

## Review Focus

- Duplicate settlement and failed instantiation must never duplicate pieces or consume materials.
- Early presses, repeated presses and frame stalls must not beat the rhythm by spamming.
- Pause/resume must preserve note timing and screen/cursor ownership.
- Slots already filled plus unplaced produced pieces must cap production per phase.
- Invalid quantities, duplicate ingredients and arithmetic overflow must not partially mutate inventory.

### Task 1: Data, transactions and rhythm

**Files:** Create Assets/Scripts/Construction/Core/{Basilica.Crafting.Core.asmdef,MaterialDefinition.cs,CraftingRecipe.cs,MaterialInventory.cs,RhythmSequence.cs}; Assets/Tests/Construction/{Basilica.Crafting.Tests.asmdef,CraftingCoreTests.cs}.
**Interfaces:** MaterialInventory(int coins), Count(material), TryBuy(material, quantity, out error), TryReserve(recipe, out reservation, out error); reservation.Settle(CraftOutcome). RhythmSequence(noteTimes, window, minimumHits), Press(time), Advance(time), Hits, Errors, Outcome; GeneratePattern(count,bpm,Random,previous).
- [x] Write tests for all outcomes, refunds exactly once, atomic insufficient materials, invalid/duplicate ingredients, overflow, rhythm tolerance, misses, spam and pattern variation.
- [x] Run EditMode tests against API stubs and confirm behavioral failures.
- [x] Implement core and rerun tests; expected all pass.

### Task 2: Scene integration and UI

**Files:** Create Assets/Scripts/Construction/{ConstructionStation.cs,ConstructionSystem.cs,ConstructionUI.cs,RhythmAudio.cs}; modify PlayerPieceInteractor.cs, DialogueJournalPanel.cs, PauseMenuController.cs, FirstPersonController.cs; expose current phase in MonumentProgressTracker.cs.
**Interfaces:** ConstructionSystem.Instance/IsOpen, Open(station), Buy(material,quantity), BeginCraft(recipe), CancelCraft(), Close(); ConstructionStation holds kind and output point. Existing interactor performs one cast and dispatches either piece pickup or station interaction.
- [x] Reproduce absent scene system via live Unity assertion before integration.
- [x] Implement modal screens, phase restrictions and output occupancy; DSP-timed sounds, timed Space input, pause/resume, cancellation/technical rollback.
- [x] Validate no double dispatch from E, no opening while holding a piece, journal exclusion and pause cursor restoration.

### Task 3: Configure and verify the playable scene

**Files:** Create material/recipe assets and station objects in GameScene; create Assets/Editor/ConstructionSceneSetup.cs for repeatable explicit setup; document controls in docs/construction-crafting.md.
- [x] Preserve/save current scene edits before running tests; record backup outside Assets.
- [x] Add merchant and workbench near starting position, configure assets, disable PiecePhaseSpawner component and save scene.
- [x] Run full EditMode suite; simulate purchases and normal/perfect/failed craft transactions in Play Mode; assert phase 0 to 1 transition and output occupancy.
- [x] Inspect actual UI at game resolution and run a fresh review of the implementation; fix important findings with tests.
- [x] Return Editor to Edit Mode, save changes and report controls and verification.

## Execution notes

User approved the design and authorized implementation. Execute inline without another approval round. Do not commit unrelated existing changes or create a worktree detached from the running Editor. Retain the plan and verification evidence in the workspace.

## Verification record — 6 November 2026

- Task 1 complete: 21/21 NUnit cases pass, with setup/teardown, invoked by `ConstructionVerification.RunCoreTests` in the Editor. Initial unimplemented inventory behavior failed before implementation.
- Task 2 complete: scene integration initially absent; now merchant, recipes, workbench, audio and modal input are wired. Fresh code review identified E reopening and a resume-clock race. Both regression checks failed before the fixes and pass afterward.
- Task 3 complete: 45/45 Play Mode assertions pass, including full purchase/crafting/placement of eight pieces, exact final inventory balance, basilica completion spawning, disabled/destroyed station cleanup and return to Edit Mode. UI inspected at 1920 × 1080.
- The remote `tests-run` transport timed out and Unity stalled during a domain reload. The user restarted the Editor. Verification then ran locally through the explicit Editor runner without relying on the remote Test Runner callback. Evidence: `.ai-game-dev/verification/core.txt` and `play.txt`; screenshots in the same folder.
- No remaining review findings. Existing unrelated changes remain in the checkout. GameScene is saved; automatic spawning is disabled and ConstructionSystem enabled.
