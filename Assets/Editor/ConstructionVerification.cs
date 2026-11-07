using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Basilica.Crafting;
using UnityEditor;
using UnityEngine;

// Explicit local verification runner, also usable when the remote MCP Test Runner reloads its connection.
[InitializeOnLoad]
public static class ConstructionVerification
{
    private const string Running = "Vitruvio.CraftingVerification";
    private const string Request = ".ai-game-dev/run-crafting-verification";
    private static readonly string Output = Path.GetFullPath(".ai-game-dev/verification");
    private static readonly Queue<Action> Steps = new Queue<Action>();
    private static double nextStep;
    private static ConstructionSystem system;
    private static ConstructionStation merchant, bench;
    private static CraftingRecipe foundation, pavement;
    private static MaterialDefinition stone, lime;
    private static double pausedTime;

    static ConstructionVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
                EditorApplication.delayCall += PreparePlayChecks;
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Running, false))
            {
                SessionState.SetBool(Running, false);
                File.AppendAllText(Path.Combine(Output, "play.txt"), "Returned to Edit Mode.\n");
            }
        };
        EditorApplication.delayCall += () => { if (File.Exists(Request)) { File.Delete(Request); Run(); } };
    }

    [MenuItem("Tools/Vitruvio/Verify construction crafting")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "setup.txt"), ConstructionSceneSetup.Configure());
        RunCoreTests();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        File.WriteAllText(Path.Combine(Output, "play.txt"), "Play Mode integration checks\n");
        SessionState.SetBool(Running, true);
        var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(viewType).Show();
        EditorWindow.GetWindow(viewType).Focus();
        EditorApplication.isPlaying = true;
    }

    public static string RunCoreTests()
    {
        Directory.CreateDirectory(Output);
        var fixture = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Basilica.Crafting.Tests.CraftingCoreTests"))
            .FirstOrDefault(t => t != null);
        if (fixture == null) throw new InvalidOperationException("Crafting test assembly not loaded.");
        var results = new List<string>();
        foreach (var method in fixture.GetMethods())
        {
            var attributes = method.GetCustomAttributes(false);
            var cases = attributes.Where(a => a.GetType().Name == "TestCaseAttribute").ToArray();
            if (cases.Length == 0 && !attributes.Any(a => a.GetType().Name == "TestAttribute")) continue;
            var inputs = cases.Length == 0 ? new object[][] { null } :
                cases.Select(a => (object[])a.GetType().GetProperty("Arguments").GetValue(a)).ToArray();
            foreach (var args in inputs)
            {
                var instance = Activator.CreateInstance(fixture);
                try
                {
                    fixture.GetMethod("Setup").Invoke(instance, null);
                    method.Invoke(instance, args);
                    results.Add("PASS " + method.Name);
                }
                catch (Exception exception) { results.Add("FAIL " + method.Name + ": " + (exception.InnerException ?? exception).Message); }
                finally { fixture.GetMethod("Cleanup").Invoke(instance, null); }
            }
        }
        string report = string.Join("\n", results);
        File.WriteAllText(Path.Combine(Output, "core.txt"), report);
        return report;
    }

    private static void PreparePlayChecks()
    {
        nextStep = EditorApplication.timeSinceStartup + 1;
        Steps.Clear();
        Steps.Enqueue(() =>
        {
            Application.runInBackground = true;
            system = ConstructionSystem.Instance;
            // Editor-driven checks run while Codex has OS focus; simulate the Game View regaining it.
            typeof(ConstructionSystem).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(system, new object[] { true });
            merchant = UnityEngine.Object.FindObjectsByType<ConstructionStation>(FindObjectsSortMode.None).First(s => s.kind == ConstructionStationKind.Merchant);
            bench = UnityEngine.Object.FindObjectsByType<ConstructionStation>(FindObjectsSortMode.None).First(s => s.kind == ConstructionStationKind.Workbench);
            foundation = system.Recipes.First(r => r.buildPhase == 0);
            pavement = system.Recipes.First(r => r.buildPhase == 1);
            stone = system.Materials.First(m => m.displayName == "Pietra");
            lime = system.Materials.First(m => m.displayName == "Calce");
            Check("NoAutomaticPiecesAtStart", UnityEngine.Object.FindObjectsByType<DraggableObject>(FindObjectsSortMode.None).Length == 0);
            Check("OpenMerchant", system.Open(merchant));
            Check("PurchaseMaterials", system.Buy(stone, 40) && system.Buy(lime, 16) && system.Inventory.Coins == 48);
            Capture("shop.png");
        });
        Steps.Enqueue(() =>
        {
            system.Close();
            bool reopened = system.Open(merchant);
            Check("CloseConsumesEForTheWholeFrame", !reopened);
            if (reopened) system.Close();
        });
        Steps.Enqueue(() =>
        {
            Check("OpenWorkbench", system.Open(bench));
            Check("FutureRecipeLocked", !system.CanCraft(pavement, out _));
            Check("BeginCraftReservesFullCost", system.BeginCraft(foundation) && system.Inventory.Count(stone) == 36 && system.Inventory.Count(lime) == 14);
            PauseMenuController.Instance.Pause();
            // Give the current implementation an Update to observe Pause before measuring.
        });
        Steps.Enqueue(() => { pausedTime = system.GetComponent<RhythmAudio>().Elapsed; });
        Steps.Enqueue(() =>
        {
            Check("PauseFreezesClock", Math.Abs(system.GetComponent<RhythmAudio>().Elapsed - pausedTime) < .04);
            system.SubmitBeat(system.Sequence.NoteTimes[0]);
            Check("PauseRejectsBeat", system.Sequence.Hits == 0);
            // Simulate a five-second pause even on machines whose Editor DSP clock is suspended in background.
            var audio = system.GetComponent<RhythmAudio>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var start = typeof(RhythmAudio).GetField("start", flags);
            var pausedAt = typeof(RhythmAudio).GetField("pausedAt", flags);
            start.SetValue(audio, (double)start.GetValue(audio) - 5);
            pausedAt.SetValue(audio, (double)pausedAt.GetValue(audio) - 5);
            PauseMenuController.Instance.Resume();
            double now = system.GetComponent<RhythmAudio>().ElapsedAt(AudioSettings.dspTime);
            Check("ResumeImmediatelyResynchronizesAudioClock", Math.Abs(now - pausedTime) < .08);
            Check("ResumeKeepsModalCursor", Cursor.lockState == CursorLockMode.None && Cursor.visible);
            Capture("rhythm.png");
        });
        Steps.Enqueue(() =>
        {
            system.CancelCraft();
            Check("FailureReturnsHalfAndCreatesNothing", !system.IsCrafting && system.Inventory.Count(stone) == 38 && system.Inventory.Count(lime) == 15 &&
                UnityEngine.Object.FindObjectsByType<CraftedPiece>(FindObjectsSortMode.None).Length == 0);
        });
        Steps.Enqueue(() =>
        {
            system.ShowMenu();
            Produce(foundation, true);
            Check("PerfectConsumesHalf", system.Inventory.Count(stone) == 36 && system.Inventory.Count(lime) == 14);
            Check("OccupiedOutputBlocksNextCraft", !system.CanCraft(foundation, out var reason) && reason.Contains("Ritira"));
            Capture("perfect.png");
            PlaceOutput(0);
        });
        for (int i = 0; i < 3; i++) Steps.Enqueue(() =>
        {
            Check("ReopenWorkbench", system.Open(bench));
            Produce(foundation, false);
            Check("NormalConsumesFullAndCreatesOne", system.LastOutcome == CraftOutcome.NormalSuccess);
            PlaceOutput(0);
        });
        Steps.Enqueue(() =>
        {
            Check("FoundationsUnlockAllPavement", system.CurrentPhase == 1 && UnityEngine.Object.FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None)
                .Where(s => s.BuildPhase == 1).All(s => s.IsUnlocked && s.GetComponent<Collider>().enabled && s.GetComponentsInChildren<Renderer>().All(r => r.enabled)));
            if (ArchitectDialoguePanel.Instance.IsDialogueActive)
                typeof(ArchitectDialoguePanel).GetMethod("EndDialogue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ArchitectDialoguePanel.Instance, null);
        });
        Steps.Enqueue(() =>
        {
            Check("OpenNextPhaseWorkbench", system.Open(bench));
            Check("NextPhaseRecipeAvailable", system.CanCraft(pavement, out _));
            Check("FoundationProductionCapped", !system.CanCraft(foundation, out _) && system.Remaining(foundation) == 0);
            Check("BalanceAfterFoundationTrials", system.Inventory.Count(stone) == 24 && system.Inventory.Count(lime) == 8);
            Capture("workbench.png");
        });
        Steps.Enqueue(() =>
        {
            Check("BeginNextPhaseCraft", system.BeginCraft(pavement));
            bench.gameObject.SetActive(false);
        });
        Steps.Enqueue(() =>
        {
            Check("TechnicalInterruptionReturnsAll", !system.IsCrafting && system.LastOutcome == CraftOutcome.TechnicalAbort &&
                system.Inventory.Count(stone) == 24 && system.Inventory.Count(lime) == 8);
            bench.gameObject.SetActive(true);
            system.Close();
            Check("ClosingRestoresMovementAndCursor", !ConstructionSystem.BlocksInput && Cursor.lockState == CursorLockMode.Locked);
        });
        for (int i = 0; i < 4; i++) Steps.Enqueue(() =>
        {
            if (ArchitectDialoguePanel.Instance.IsDialogueActive)
                typeof(ArchitectDialoguePanel).GetMethod("EndDialogue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ArchitectDialoguePanel.Instance, null);
            Check("OpenPavementWorkbench", system.Open(bench));
            Produce(pavement, false);
            PlaceOutput(1);
        });
        Steps.Enqueue(() =>
        {
            var tracker = UnityEngine.Object.FindFirstObjectByType<MonumentProgressTracker>();
            Check("AllEightPiecesCompleteBasilica", tracker.IsReconstructionComplete && system.Remaining(foundation) == 0 && system.Remaining(pavement) == 0);
            Check("FullCycleMaterialBalance", system.Inventory.Count(stone) == 0 && system.Inventory.Count(lime) == 0 && system.Inventory.Coins == 48);
            var completion = UnityEngine.Object.FindFirstObjectByType<BasilicaCompletionSpawner>();
            Check("CompletedBasilicaSpawned", completion != null && (bool)typeof(BasilicaCompletionSpawner)
                .GetField("hasSpawned", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(completion));
            if (ArchitectDialoguePanel.Instance.IsDialogueActive)
                typeof(ArchitectDialoguePanel).GetMethod("EndDialogue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ArchitectDialoguePanel.Instance, null);
        });
        Steps.Enqueue(() =>
        {
            Check("OpenMerchantBeforeRemoval", system.Open(merchant));
            UnityEngine.Object.Destroy(merchant.gameObject);
        });
        Steps.Enqueue(() =>
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<ConstructionUI>(FindObjectsInactive.Include);
            var overlay = ui.transform.Find("Screens");
            Check("RemovedStationReleasesModalAndHidesUI", !ConstructionSystem.BlocksInput && !overlay.gameObject.activeSelf);
            Application.runInBackground = false;
        });
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup < nextStep) return;
        nextStep = EditorApplication.timeSinceStartup + .4;
        try
        {
            if (Steps.Count == 0)
            { EditorApplication.update -= Tick; EditorApplication.isPlaying = false; return; }
            Steps.Dequeue()();
        }
        catch (Exception exception)
        {
            File.AppendAllText(Path.Combine(Output, "play.txt"), "ERROR " + exception + "\n");
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
        }
    }

    private static void Produce(CraftingRecipe recipe, bool perfect)
    {
        if (!system.BeginCraft(recipe)) throw new InvalidOperationException("Cannot craft: " + system.Message);
        var times = (double[])system.Sequence.NoteTimes.Clone();
        for (int i = 0; i < times.Length; i++) system.SubmitBeat(times[i] - (!perfect && i >= recipe.minimumHits ? .2 : 0));
        if (system.IsCrafting) throw new InvalidOperationException("Craft did not settle.");
    }

    private static void PlaceOutput(int phase)
    {
        var output = UnityEngine.Object.FindObjectsByType<CraftedPiece>(FindObjectsSortMode.None).Single(p => !p.Piece.IsLocked);
        var slot = UnityEngine.Object.FindObjectsByType<PlaceholderSlot>(FindObjectsSortMode.None).First(s => s.BuildPhase == phase && !s.IsFilled);
        system.Close();
        typeof(PlaceholderSlot).GetMethod("Fill", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(slot, new object[] { output.Piece });
        Check("PieceSnapsToPlaceholder", slot.IsFilled && output.Piece.IsLocked);
    }

    private static void Check(string name, bool passed)
    { File.AppendAllText(Path.Combine(Output, "play.txt"), (passed ? "PASS " : "FAIL ") + name + "\n"); }

    private static void Capture(string file) => ScreenCapture.CaptureScreenshot(Path.Combine(Output, file));
}
