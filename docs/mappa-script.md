# Mappa degli script: chi fa cosa

[Indice generale](../README.md) · [Guida da zero](guida-codice.md) · [Modifiche e problemi](modifiche-e-problemi.md)

Questa mappa copre i **31 script in `Assets/Scripts`**, i **2 script del controller in prima persona**, i **3 strumenti in `Assets/Editor`** e i **4 file di test del crafting**. I nomi di campi e metodi sono quelli del codice; Unity può visualizzarli nell'Inspector con spazi e maiuscole.

## 1. Giocatore e pezzi

### [PlayerInputHandler.cs](../Assets/Scripts/PlayerInputHandler.cs)

**Compito:** leggere la mappa di input `Player` e offrire agli altri script valori e comandi già pronti. È su `GameManager` nella scena attuale.

- Configurazione: `playerControls`, `actionMapName` e nomi delle azioni.
- Uscite principali: `MovementInput`, `LookInput`, `SprintTriggered`, `JumpPressed()`, `OpenJournalPressed()`, `NextJournalPagePressed()`, `PreviousJournalPagePressed()`, `PausePressed()`.
- `Awake()` clona l'asset degli input; `OnEnable()` ne riabilita la mappa al frame successivo.
- `ResetUIInputModule()` riavvia il modulo input della UI; `ForceReEnable()` disabilita e riabilita la mappa.
- Dipende da un asset input assegnato e dai nomi esatti delle azioni. L'azione precedente pagina si chiama davvero **`PreviouseJournalPage`**, con questa grafia.

### [FirstPersonController.cs](../Assets/StarterAssets/FirstPersonController/Scripts/FirstPersonController.cs)

**Compito:** movimento in prima persona, salto, gravità e visuale. Classe `StarterAssets.FirstPersonController`, adattata per usare `PlayerInputHandler` e gli stati dei pannelli.

- Richiede `CharacterController` e, con il nuovo Input System abilitato, `PlayerInput`.
- Campi utili: `MoveSpeed`, `SprintSpeed`, `RotationSpeed`, `JumpHeight`, `Gravity`, `GroundLayers`, `CinemachineCameraTarget`.
- `Update()` gestisce fisica del personaggio e movimento; `LateUpdate()` aggiorna la rotazione della visuale.
- Diario, pausa e costruzione bloccano entrambi. Il tasto destro con pezzo in mano sospende la visuale per consentire la rotazione del pezzo. Il dialogo non è incluso in `IsInputBlocked()`.

### [BasicRigidBodyPush.cs](../Assets/StarterAssets/FirstPersonController/Scripts/BasicRigidBodyPush.cs)

**Compito:** far spingere al personaggio gli oggetti fisici urtati dal `CharacterController`.

`canPush` abilita la funzione, `pushLayers` limita gli oggetti interessati e `strength` regola l'impulso. Ignora Rigidbody kinematici e urti diretti troppo verso il basso. Il metodo di ingresso è `OnControllerColliderHit()`.

### [PlayerPieceInteractor.cs](../Assets/Scripts/PlayerPieceInteractor.cs)

**Compito:** riconoscere cosa stai guardando, mostrare il suggerimento, prendere/rilasciare pezzi e aprire postazioni.

- Campi: `playerCamera`, `interactRange`, `holdDistance`, `interactKey`, `aimRadius`, `hidePromptDelay`, `heldPieceRotationSensitivity`, oggetti UI dei suggerimenti.
- Usa la camera assegnata o, come ripiego, `Camera.main`.
- Lo SphereCast ignora i trigger. Cerca `ConstructionStation` anche nei genitori del collider, ma cerca `DraggableObject` **sullo stesso GameObject del collider colpito**.
- Chiama `BeginDrag()`, `UpdateDragTarget()`, `RotateWhileHeld()` ed `EndDrag()` sul pezzo; chiama `ConstructionSystem.Open()` sulla postazione.
- `IsHoldingPiece` è letto dal banco; `IsRotatingHeldPiece` dal controller della visuale.
- `ConstructionSystem.InteractionConsumedFrame` evita che la stessa pressione di E riapra un menu appena chiuso nello stesso frame.

### [DraggableObject.cs](../Assets/Scripts/DraggableObject.cs)

**Compito:** gestire il pezzo libero, in mano e fissato. Richiede un `Collider`; usa un Rigidbody se presente, con un ripiego sul Transform quando manca.

| Metodo / stato | Effetto |
| --- | --- |
| `BeginDrag()` | Inizia il trasporto, sospende gravità e rotazione fisica, ignora urti con giocatore e pezzi già fissati |
| `UpdateDragTarget(position)` | Imposta il punto da inseguire |
| `RotateWhileHeld(view, delta, sensitivity)` | Applica la rotazione manuale |
| `EndDrag()` | Ripristina il comportamento libero ed emette `OnReleased` |
| `LockAt(position, rotation, scale, mesh)` | Blocca il pezzo e applica la forma dello slot se presente |
| `ResetToSpawn(position, rotation)` | Riporta un pezzo non fissato al punto di recupero e azzera le velocità |
| `IsDragging`, `IsLocked`, `PieceId` | Stati pubblici letti dagli altri sistemi |

`dragSmoothing` regola l'inseguimento, `maxHoldSpeed` limita la velocità e `heldScale` la scala in mano. Se il pezzo resta lontano dal bersaglio oltre `stuckDistanceThreshold` per `stuckTimeBeforeIgnoringCollision`, il collider viene temporaneamente disabilitato per superare l'ostacolo. Il ritorno vicino al bersaglio o il normale rilascio ripristinano quel collider.

### [PlaceholderSlot.cs](../Assets/Scripts/PlaceholderSlot.cs)

**Compito:** mostrare dove posare un pezzo, riconoscere i candidati compatibili e fissare quello rilasciato nel suo volume.

- Richiede `Collider` e `Rigidbody`; in `Awake()` imposta trigger, Rigidbody kinematic e assenza di gravità.
- Configurazione: `pieceId`, `buildPhase`, colori del volume e materiale/colore dei bordi.
- `OnTriggerEnter()` confronta il `PieceId` e ascolta `OnReleased` per i pezzi corretti; `OnTriggerExit()` smette di ascoltarli.
- `Fill()` chiama `DraggableObject.LockAt()`, nasconde le guide, rende solido il collider dello slot e invia `OnSlotFilled`.
- `SetUnlocked()` abilita/disabilita renderer e collider di uno slot non ancora pieno.
- Espone `IsFilled`, `IsUnlocked`, `PieceId`, `BuildPhase`, `SlotMesh`.

Il materiale della guida deve supportare la trasparenza. `wrongColor` determina il colore del pezzo errato: il valore iniziale nel codice è verde, anche se un commento parla di rosso. La mesh finale è il primo `MeshFilter` trovato nei figli prima di generare i bordi.

### [PieceRespawnVolume.cs](../Assets/Scripts/PieceRespawnVolume.cs)

**Compito:** recuperare i pezzi che cadono sotto il livello. Richiede `Collider` e `Rigidbody`, configurati come trigger e kinematic in `Awake()`.

Assegna `spawnPoint`. Quando entra un collider con `DraggableObject` sullo stesso GameObject, chiama `ResetToSpawn()`. Non recupera pezzi già fissati e non serve a riposizionare il giocatore.

## 2. Costruzione, acquisti e ritmo

I quattro script Core usano il namespace **`Basilica.Crafting`** e l'assembly [Basilica.Crafting.Core.asmdef](../Assets/Scripts/Construction/Core/Basilica.Crafting.Core.asmdef). Inventario e sequenza ritmica sono classi C# normali; materiali e ricette sono asset Unity.

| File | Responsabilità | Interfaccia da conoscere |
| --- | --- | --- |
| [MaterialDefinition.cs](../Assets/Scripts/Construction/Core/MaterialDefinition.cs) | Definisce un materiale acquistabile | `displayName`, `unitPrice`; asset creato da `Vitruvio Garden/Construction/Material` |
| [CraftingRecipe.cs](../Assets/Scripts/Construction/Core/CraftingRecipe.cs) | Definisce cosa produrre, fase, costo e difficoltà | `MaterialCost`, `costs`, `piecePrefab`, `buildPhase`, `noteCount`, `minimumHits`, `bpm`, `hitWindow`, `IsValid(out error)` |
| [MaterialInventory.cs](../Assets/Scripts/Construction/Core/MaterialInventory.cs) | Tiene monete, scorte e prenotazioni; applica consumo e rimborsi | `Count()`, `TryBuy()`, `TryReserve()`; `CraftingReservation.Settle()`; enum `CraftOutcome` |
| [RhythmSequence.cs](../Assets/Scripts/Construction/Core/RhythmSequence.cs) | Genera il pattern e giudica i colpi senza dipendere dalla UI | `GeneratePattern()`, `Press()`, `Advance()`, `NoteTimes`, `Judgments`, `Hits`, `Errors`, `IsComplete`, `Outcome` |

`Judgments` usa `0` per una nota da giudicare, `1` per un colpo corretto e `-1` per un errore. La prima nota generata è a 2,4 secondi dall'inizio della sequenza; gli intervalli successivi sono multipli casuali di mezzo battito. Il pattern appena usato viene evitato quando coincide esattamente con il successivo.

### [ConstructionSystem.cs](../Assets/Scripts/Construction/ConstructionSystem.cs)

**Compito:** coordinare mercante, banco, inventario, minigioco e uscita del pezzo. È il punto da leggere quando il problema attraversa più sottosistemi.

- Configurazione: `startingCoins`, `materials`, `recipes`, `progressTracker`, `view`, `storageArea`. Richiede `RhythmAudio`.
- `Awake()` crea l'inventario e collega la UI già salvata nel Canvas tramite `view.Initialize(this)`; collega una propria azione Spazio. Se mancano UI o riferimenti richiesti, segnala l'errore e si disabilita senza generare pannelli sostitutivi.
- `Open()` / `Close()` gestiscono menu, cursore e consumo dell'interazione E. Durante una lavorazione, una chiamata a `Close()` annulla il tentativo senza chiudere subito il menu; E viene letto per chiudere soltanto quando non si sta lavorando.
- `Buy()` acquista dal mercante. `Remaining()` conta il fabbisogno residuo. `CanCraft()` restituisce anche un motivo di rifiuto leggibile.
- `BeginCraft()` prenota il costo, genera il pattern e avvia l'audio. `SubmitBeat()` giudica una pressione. `CancelCraft()` conclude come fallimento.
- `FinishCraft()` è interno: crea il prefab per gli esiti riusciti, aggiunge `CraftedPiece`, liquida la prenotazione e mostra il risultato. Se la produzione fallisce tecnicamente, restituisce tutto.
- `RefreshPauseState()` sincronizza sospensione audio e UI con la pausa. La perdita del focus viene trattata separatamente.
- `OnDisable()` restituisce una prenotazione pendente come `TechnicalAbort`; `OnDestroy()` rilascia l'azione input.
- Stati pubblici: `IsOpen`, `IsCrafting`, `Sequence`, `Inventory`, `CurrentPhase`, `Message`, `LastOutcome`. Stato globale di blocco: `BlocksInput`.
- `CreatedPiece` identifica il pezzo dell'ultimo risultato ancora disponibile. `StoreCreatedPiece()` trasferisce quella stessa istanza nel deposito, libera il tavolo e torna al menu; il fallimento del deposito mantiene il risultato e il pezzo sul tavolo. Chiusura e trasferimento eliminano il riferimento della schermata, senza eliminare il pezzo dalla scena.

### [ConstructionStorageArea.cs](../Assets/Scripts/Construction/ConstructionStorageArea.cs)

**Compito:** trovare un posto libero nel recinto e trasferirvi il pezzo reale. `volume` è il BoxCollider che definisce l'area; `spacing` tiene separati i pezzi. Nella scena usa il volume già presente su `PieceRespawnPoint` dentro il recinto `Fences`.

`TryStore(piece, out reason)` rifiuta pezzi bloccati o in mano, cerca una superficie solida con raycast e verifica l'ingombro con una query fisica. Se non trova spazio non sposta nulla. Se riesce chiama `ResetToSpawn()` sulla stessa istanza e sincronizza i Transform con la fisica. `CraftedPiece` resta sul pezzo, quindi il deposito non aumenta il fabbisogno delle ricette.

### [ConstructionStation.cs](../Assets/Scripts/Construction/ConstructionStation.cs)

**Compito:** descrivere una postazione fisica. `ConstructionStationKind` distingue `Merchant` e `Workbench`; `Prompt` restituisce il testo di interazione.

`outputPoint` e `outputHalfExtents` definiscono l'uscita del banco. `IsOutputBlocked()` cerca pezzi non fissati nel volume con `Physics.OverlapBox()`, ignorando i trigger. **Half extents significa metà dimensione:** `(0.8, 0.8, 0.8)` produce un volume largo `1.6` su ciascun asse. Il gizmo arancione lo mostra selezionando la postazione.

### [ConstructionUI.cs](../Assets/Scripts/Construction/ConstructionUI.cs)

**Compito:** controllare schermate uGUI e TextMesh Pro già salvate in `Canvas/Construction UI/Screens`.

Per la spiegazione passo per passo di come viene creata, dove trovarla nella Hierarchy e come modificarla, vedi [origine delle UI di crafting e acquisto](interfacce-crafting-acquisto.md).

`Initialize()` collega dati e azioni ai componenti esistenti. `ShowShop()`, `ShowWorkbench()`, `ShowRhythm()` e `ShowResult()` selezionano una delle cinque schermate salvate; `UpdateRhythm()` muove il pool di note già presente; `SetSuspended()` nasconde temporaneamente il contenuto; `Hide()` lo chiude. Questi metodi non creano o distruggono elementi UI.

I pulsanti chiamano il sistema; le regole economiche sono in `MaterialInventory`, i giudizi in `RhythmSequence`. Colori, font, disposizione e testi statici si modificano nei GameObject dell'Editor. La UI usa la scala del Canvas principale, riferimento `1920 × 1080`, con Canvas annidato di ordine `30`.

Il risultato riuscito usa la schermata salvata `Completato`, con pannello trasparente e pulsanti di chiusura/deposito; le altre schermate hanno oggetti separati e mantengono ciascuna il proprio layout. Non crea immagini, camere aggiuntive o RenderTexture. `CurrentScreen` espone la schermata selezionata e `IsConfigured` verifica i riferimenti necessari.

### Righe modificabili di acquisto e ricetta

| File | Compito |
| --- | --- |
| [ConstructionMaterialRow.cs](../Assets/Scripts/Construction/ConstructionMaterialRow.cs) | Componente su ogni riga di Acquisto; asset `material`, testi e voci `purchases` con quantità/pulsante/etichetta. `Bind()` collega un solo callback per opzione; `Refresh()` aggiorna scorte, prezzi e disponibilità. |
| [ConstructionRecipeRow.cs](../Assets/Scripts/Construction/ConstructionRecipeRow.cs) | Componente su ogni riga di Ricette; asset `recipe`, testi, pulsante CREA e colori di disponibilità modificabili nell'Inspector. `Bind()` collega l'azione; `Refresh()` aggiorna i dati. |

Le righe si duplicano nell'Editor e si assegnano ai relativi asset. Il controller le trova nella schermata all'inizializzazione; aggiungere un asset al sistema non crea automaticamente una riga grafica.

### [CraftingResultView.cs](../Assets/Scripts/Construction/CraftingResultView.cs)

**Compito:** inquadrare il pezzo reale sul tavolo usando la camera del giocatore. `ConstructionSystem` attiva `Show(piece, output)` alla riuscita e chiama `Restore()` quando torna ai menu, chiude o viene disabilitato.

`Show()` conserva posizione e rotazione locali, campo visivo e stato originale del `CinemachineBrain`, poi sospende il Brain e orienta la camera verso il pezzo. `LateUpdate()` mantiene l'inquadratura mentre il pezzo si assesta sul tavolo. Chiamate ripetute non sovrascrivono la posizione di ritorno; `Restore()` e `OnDisable()` restituiscono il controllo precedente. Non sposta il giocatore e non crea altre camere. `viewOffset`, `fieldOfView` e `horizontalFraming` regolano posizione, zoom e composizione. Il parametro opzionale `source` permette di usare una camera esplicita; in gioco il valore predefinito è `Camera.main`.

### [RhythmAudio.cs](../Assets/Scripts/Construction/RhythmAudio.cs)

**Compito:** sintetizzare una clip con metronomo, motivetto originale e accenti delle note e fornire un tempo affidabile. Richiede `AudioSource`.

`Play()` genera la clip mono a 44.100 Hz e la programma con `PlayScheduled()`. Il metronomo marca ogni quarto battito; la melodia ripete una frase pentatonica di quattro battute dal timbro pizzicato. La miscela mantiene margine per evitare saturazione quando i suoni coincidono. `Elapsed` / `ElapsedAt()` misurano il tempo DSP al netto delle pause. `SetPaused()` sospende/riprende la sorgente; `Stop()` ferma e distrugge la clip generata. Configurazione: gruppo mixer `output`, volume generale `volume` e livelli `metronomeVolume`, `melodyVolume`, `noteVolume`. `melodyVolume = 0` disattiva il motivetto.

### [CraftedPiece.cs](../Assets/Scripts/Construction/CraftedPiece.cs)

**Compito:** ricordare che un oggetto è stato prodotto al banco e con quale ricetta. `Initialize(recipe, piece)` valorizza `Recipe` e `Piece`. Il sistema aggiunge il componente alla produzione: serve a contare i pezzi disponibili e limitare le copie.

## 3. Progressi e completamento

### [MonumentProgressTracker.cs](../Assets/Scripts/MonumentProgressTracker.cs)

**Compito:** contare le pose, gestire le fasi e attivare le milestone. Configurazione: `dialoguePanel`, `milestoneSet`.

In `Start()` raggruppa gli slot attivi per `BuildPhase` e si iscrive a `OnSlotFilled`. `HandleSlotFilled()` incrementa il totale, controlla la prossima milestone e avanza la fase se completa. `AdvancePhaseIfComplete()` salta anche le fasi vuote; `UpdatePhaseLocks()` lascia sbloccati gli slot con fase minore o uguale a quella corrente.

Espone `CurrentPhase`, `IsReconstructionComplete` e l'evento `OnReconstructionCompleted`. Se non ha trovato alcuno slot non dichiara la ricostruzione completa. Non esegue un salvataggio dei progressi.

### [MonumentMilestoneSet.cs](../Assets/Scripts/MonumentMilestoneSet.cs)

**Compito:** contenere l'array ordinato `milestones` in uno ScriptableObject. Menu di creazione: `Vitruvio Garden/Monument Milestone Set`. L'asset attuale è [MonumentMilestoneSet_Vitruvio.asset](../Assets/Data/MonumentMilestoneSet_Vitruvio.asset).

### [BasilicaCompletionSpawner.cs](../Assets/Scripts/BasilicaCompletionSpawner.cs)

**Compito:** creare la basilica finale una volta sola. Assegna `progressTracker`, `basilicaPrefab` e `spawnPoint`.

Ascolta `OnReconstructionCompleted`, controlla anche lo stato già completato in `Start()` e chiama `SpawnBasilica()`. Se manca il tracker o prefab/punto di uscita registra un errore e si disabilita. Rimuove l'iscrizione quando disabilitato.

### [PiecePhaseSpawner.cs](../Assets/Scripts/PiecePhaseSpawner.cs)

**Compito:** generare direttamente un elenco di pezzi come alternativa al crafting. **È disabilitato nella GameScene attuale.**

`spawnAtStart = true` genera tutte le voci all'avvio. Se falso, aspetta che siano pieni tutti gli slot fino a `spawnAfterBuildPhase` inclusa, poi genera comunque tutte le voci. Il `buildPhase` di ogni voce è passato all'evento `OnPieceSpawned`; non è un calendario di generazione individuale.

Ogni voce richiede `piecePrefab` e `spawnPoint`. Un collider convesso sul punto permette di distribuire le posizioni nel suo volume; senza collider si usa il pivot. `spawnSfxName` è facoltativo. Non riattivarlo nel flusso crafting senza considerare i pezzi gratuiti e il conteggio basato su `CraftedPiece`.

## 4. Dialoghi e diario

| File | Compito e collegamenti |
| --- | --- |
| [DialogueLine.cs](../Assets/Scripts/DialogueLine.cs) | Definisce `DialogueLine` (parlante e testo), enum `Speaker`, `Milestone` (soglia, dialogo, nota, estratto e immagine) e `DialogueLogEntry` (voce da rileggere) |
| [DialogueLog.cs](../Assets/Scripts/DialogueLog.cs) | Lista statica in memoria; `AddEntry(milestone)` copia i dati della milestone e `Entries` li espone in sola lettura; non ha persistenza o reset esplicito |
| [ArchitectDialoguePanel.cs](../Assets/Scripts/ArchitectDialoguePanel.cs) | Riceve `PlayDialogue(lines)` dal tracker, accoda battute e le avanza con E; mostra nome, testo e ritratti con transizioni DOTween; espone `IsDialogueActive` |
| [DialogueJournalPanel.cs](../Assets/Scripts/DialogueJournalPanel.cs) | Legge `DialogueLog.Entries`; apre sull'ultima voce, mostra dialogo a sinistra, estratto/immagine/nota a destra; usa Tab, E, Q tramite l'input handler; espone `IsJournalActive` |

Nel pannello dialogo assegna contenitore, testi e immagini dei ritratti. Nel diario assegna contenitore, testi delle pagine, immagine, nota, indicatore e suggerimento. Senza voci registrate il diario non si apre. Una nuova chiamata a `PlayDialogue()` sostituisce la coda precedente: non c'è una coda globale di più dialoghi.

## 5. Menu, impostazioni e audio

| File | Compito | Collegamenti / metodi utili |
| --- | --- | --- |
| [MainMenuController.cs](../Assets/Scripts/MainMenuController.cs) | Alterna pannello principale, prepartita, impostazioni e conferma uscita | `StartGame()` apre la prepartita; `ConfirmStartGame()` carica `gameSceneName`; `CancelPreGame()`, `OpenSettings()`, `CloseSettings()`, `RequestQuit()`, `CancelQuit()`, `ConfirmQuit()` vanno agli eventi dei bottoni |
| [PauseMenuController.cs](../Assets/Scripts/PauseMenuController.cs) | Pausa con `timeScale = 0`, pannelli e cursore; non apre pausa su diario/dialogo | `Pause()`, `Resume()`, `OpenSettings()`, `CloseSettings()`, `QuitToMainMenu()`; richiede riferimenti ai pannelli e scena di ritorno |
| [SettingsPanelController.cs](../Assets/Scripts/SettingsPanelController.cs) | Collega quattro slider al manager audio vivo | `OnEnable()` legge i valori; i metodi `SetMasterVolume()`, `SetMusicVolume()`, `SetSFXVolume()`, `SetUISFXVolume()` ricevono lo slider da 0 a 1 |
| [AudioManager.cs](../Assets/Scripts/AudioManager.cs) | Singleton persistente con musica, effetti e mixer; definisce anche la classe dati `Sound` | `PlayMusic()`, `PlaySFX()`, `PlayUISFX()`, `FadeMusicOut()`, metodi `Set/Get…Volume()`; liste `musicList`, `sfxList`, `uiList` |
| [ButtonHoverAnimation.cs](../Assets/Scripts/ButtonHoverAnimation.cs) | Ingrandisce il bottone al passaggio del mouse e ripristina la scala all'uscita/disattivazione | `hoverScale`, `duration`, `ease`; DOTween usa tempo non scalato, quindi funziona in pausa; suono UI facoltativo `buttonpop` |

`AudioManager` seleziona automaticamente una musica il cui nome coincide con la scena caricata. Gli effetti si cercano per nome nelle liste. I parametri mixer predefiniti sono `MasterVolume`, `MusicVolume`, `SFXVolume`, `UISFXVolume`.

Gli slider devono chiamare `SettingsPanelController`, che risolve `AudioManager.Instance` a ogni chiamata. Una copia locale del manager può autodistruggersi quando esiste già quello persistente. Per aggiornare gli slider a ogni apertura, l'`OnEnable()` del controller deve essere effettivamente richiamato: collegalo al pannello che si attiva oppure richiama esplicitamente un aggiornamento quando lo apri.

## 6. Strumenti Editor e test

| File | Compito | Come si usa |
| --- | --- | --- |
| [ConstructionSceneSetup.cs](../Assets/Editor/ConstructionSceneSetup.cs) | Crea asset mancanti, sistema e postazioni; disabilita il vecchio spawner; salva la scena | `Tools > Vitruvio > Configure construction crafting`, con GameScene aperta e fuori da Play Mode |
| [ConstructionUIEditorSetup.cs](../Assets/Editor/ConstructionUIEditorSetup.cs) | Prepara una volta la gerarchia modificabile nel Canvas e collega il riferimento `View`; se già presente conserva layout e modifiche manuali | `Tools > Vitruvio > Prepare editable construction UI`, solo Editor |
| [ConstructionVerification.cs](../Assets/Editor/ConstructionVerification.cs) | Avvia configurazione, casi della logica e simulazione in Play Mode; produce report e immagini | `Tools > Vitruvio > Verify construction crafting`; risultati in `.ai-game-dev/verification` |
| [CraftingCoreTests.cs](../Assets/Tests/Construction/CraftingCoreTests.cs) | `CraftingCoreTests` controlla le regole del crafting; `RhythmAudioTests` verifica presenza della melodia, accenti del metronomo, volumi separati e assenza di saturazione | Assembly `Basilica.Crafting.Tests`, solo Editor; eseguibile anche dal Test Runner |
| [ConstructionStorageTests.cs](../Assets/Tests/Construction/ConstructionStorageTests.cs) | Verifica trasferimento della stessa istanza, azzeramento velocità, separazione tra pezzi e rifiuti senza spostamento | Stesso assembly di test; richiede la scena salvata per l'esecuzione via MCP |
| [CraftingResultViewTests.cs](../Assets/Tests/Construction/CraftingResultViewTests.cs) | Verifica inquadratura e ripristino della camera, dello zoom e del controllo Cinemachine, anche con chiamate ripetute o disattivazione | Stesso assembly; usa una camera di test isolata |
| [ConstructionAuthoredUITests.cs](../Assets/Tests/Construction/ConstructionAuthoredUITests.cs) | Verifica UI presente nella scena salvata, riferimenti, riuso della gerarchia, conservazione del layout, capienza note e acquisti senza callback duplicati | Stesso assembly; carica una copia Preview della scena |

La configurazione crea `ConstructionSystem` e i suoi riferimenti solo se manca quel componente; non riallinea automaticamente tutti i campi di un sistema già presente. Riutilizza gli asset già esistenti e crea le postazioni se manca la root `ConstructionStations`.

La verifica personalizzata chiama la configurazione e **salva la scena**, poi simula acquisti, colpi e pose con chiamate di codice. È un controllo funzionale guidato; la qualità della mira, della lettura dei testi e del feeling dei comandi richiede anche una prova manuale. Vedi [come verificare](modifiche-e-problemi.md#verificare-una-modifica).

## 7. Ordine di lettura consigliato per chi programma

1. `PlayerPieceInteractor` → `DraggableObject` → `PlaceholderSlot`: dal comando E alla posa.
2. `MonumentProgressTracker` → `MonumentMilestoneSet` → `ArchitectDialoguePanel`: dalla posa al progresso e al racconto.
3. `CraftingRecipe` → `MaterialInventory` → `RhythmSequence`: le regole di produzione.
4. `ConstructionSystem` → `ConstructionStation` → `ConstructionUI` → `RhythmAudio`: come le regole arrivano in gioco.
5. `DialogueLog` → `DialogueJournalPanel`, poi menu/audio e strumenti Editor secondo la modifica da fare.
