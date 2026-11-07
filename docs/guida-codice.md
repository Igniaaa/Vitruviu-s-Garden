# Guida al codice, partendo da zero

[Indice generale](../README.md) · [Mappa degli script](mappa-script.md) · [Modifiche e problemi](modifiche-e-problemi.md)

Riferimento: sorgenti e dati locali del **7 novembre 2026**, aggiornati fino alla visuale dedicata del pezzo e al deposito nel recinto. I risultati dei collaudi già eseguiti sono riportati nella [guida al crafting](construction-crafting.md). Per trovare e modificare i pannelli a schermo, leggi [da dove arrivano le UI](interfacce-crafting-acquisto.md).

## 1. Le parole di Unity che servono davvero

| Parola | Significato pratico | Esempio nel progetto |
| --- | --- | --- |
| Scena | Un ambiente con oggetti e collegamenti salvati | `GameScene.unity` |
| GameObject | Un oggetto nella scena, anche invisibile | `GameManager`, un banco, il giocatore |
| Componente | Una funzione aggiunta a un GameObject | `Collider`, `ConstructionSystem` |
| Script | Un file C# che definisce un comportamento o un dato | `DraggableObject.cs` |
| Inspector | Il pannello dove imposti i componenti dell'oggetto selezionato | Campo `Starting Coins` del sistema di costruzione |
| Campo serializzato | Un valore salvato da Unity, modificabile nell'Inspector | `startingCoins`, `pieceId` |
| Prefab | Un modello riutilizzabile da cui creare oggetti | Il sacco trasportabile |
| Istanza | Una copia concreta di un prefab nella scena | Il pezzo appena prodotto sul banco |
| ScriptableObject | Un asset che contiene dati condivisi | Ricetta `Fondamenta`, materiale `Pietra` |
| Collider | Il volume che rileva urti o sovrapposizioni | Il volume di uno slot |
| Trigger | Un collider che segnala il passaggio senza fermare gli oggetti | Uno slot ancora vuoto |
| Rigidbody | Il componente che partecipa alla fisica | Un pezzo libero che cade a terra |
| Kinematic | Un Rigidbody mosso dal codice, senza essere spinto dalla simulazione | Un pezzo già fissato |
| Evento | Una notifica a chi si è iscritto ad ascoltarla | «Questo slot è stato riempito» |
| UI / Canvas | Gli elementi dell'interfaccia e il loro contenitore | Menu, testi, diario |
| Singleton / `Instance` | Un riferimento statico all'istanza usata dagli altri script | `ConstructionSystem.Instance` |

**Esempio di collegamento nell'Inspector:** seleziona `GameManager` nella **Hierarchy**, trova `ConstructionSystem` e trascina un asset ricetta nel suo elenco `Recipes`. Lo script ora conosce quella ricetta. Avere il file nella cartella del progetto, da solo, non basta.

### Quando viene eseguito uno script?

Unity chiama alcuni metodi automaticamente:

| Metodo | Quando | Uso qui |
| --- | --- | --- |
| `Awake()` | All'inizializzazione dell'oggetto | Leggere componenti, creare inventario e collegare la UI salvata |
| `OnEnable()` / `OnDisable()` | Quando il componente viene attivato/disattivato | Abilitare input, iscriversi a eventi, interrompere lavorazioni |
| `Start()` | Prima del primo aggiornamento del componente attivo | Cercare gli slot presenti nella scena |
| `Update()` | Ogni fotogramma | Leggere E, aggiornare menu e sequenza ritmica |
| `LateUpdate()` | Dopo gli aggiornamenti normali | Aggiornare la visuale del giocatore |
| `FixedUpdate()` | A intervalli della simulazione fisica | Muovere il pezzo verso il punto davanti alla camera |
| `OnTriggerEnter/Exit()` | Quando un collider entra/esce da un trigger | Candidare un pezzo per lo slot |

Non affidarti all'ordine casuale tra due `Start()` di componenti diversi. Per esempio, `BasilicaCompletionSpawner` ascolta l'evento finale e controlla anche se il tracker risulta già completo.

## 2. Mappa delle cartelle

```text
Assets/
  Scenes/                         MainMenu e GameScene
  Scripts/                        interazione, progressi, dialoghi, menu, audio
    Construction/                 banco, mercante, interfaccia e audio ritmico
      Core/                       inventario, ricette e regole del ritmo
  Data/
    Construction/                 prezzi, ricette e materiali grafici delle postazioni
    MonumentMilestoneSet_Vitruvio.asset
  Prefabs/
    INTERACTABLE/                 pezzi e slot riutilizzabili
    ENV/                          elementi ambientali e basilica finale
  StarterAssets/FirstPersonController/Scripts/
                                  movimento e spinta degli oggetti
  Editor/                         comandi utilizzabili nell'Editor
  Tests/Construction/             test della logica Core
Packages/                         dipendenze e pacchetti, incluso ProBuilder incorporato
ProjectSettings/                  versione Unity, scene di build e impostazioni
docs/                             questa documentazione e documenti precedenti
```

`Library`, `Temp`, `Logs`, i file `.csproj` e le soluzioni `.slnx` sono dati o file generati dagli strumenti. Per modificare il gioco parti da script, scene, prefab e asset.

Le cartelle DOTween, TextMesh Pro e i campioni dei pacchetti contengono anche codice di libreria o dimostrazione. La mappa documenta gli script del gioco, i due script del controller in prima persona e gli strumenti dedicati al crafting; non è un catalogo delle API di tutte le dipendenze.

## 3. Una partita, vista dall'interno

### A. Avvio e input

In `GameScene`, `GameManager` ospita fra gli altri `PlayerInputHandler`, `MonumentProgressTracker`, `ConstructionSystem`, `RhythmAudio` e `BasilicaCompletionSpawner`.

Le cinque schermate di acquisto e crafting sono salvate sotto `Canvas > Construction UI > Screens`. `ConstructionSystem` vi si collega tramite il campo `View`; il controller della UI aggiorna gli elementi esistenti senza crearli durante la partita. Colori, font e disposizione si modificano direttamente nella scena.

`PlayerInputHandler` clona l'asset delle azioni, cerca la mappa `Player` e legge movimento, visuale, salto, corsa, diario e pausa. `FirstPersonController` usa questi valori per muovere il personaggio.

Alcuni comandi sono letti direttamente dagli altri script: **E** per pezzi/postazioni e dialoghi, **tasto destro** per ruotare i pezzi, **Spazio** per il ritmo. Cambiare soltanto l'asset degli input non cambia automaticamente questi comandi.

### B. Compra i materiali

Guardi il mercante e premi E. `PlayerPieceInteractor` rileva il banco usando uno **SphereCast**, cioè una ricerca lungo la mira con un piccolo raggio di tolleranza. I collider trigger vengono ignorati, così gli slot non intercettano la mira.

`ConstructionSystem.Open()` controlla che non ci siano pausa, diario, dialogo, un altro menu di costruzione o un pezzo in mano. Poi libera il cursore e mostra il negozio.

Il pulsante di acquisto chiama `Buy()`, che passa a `MaterialInventory.TryBuy()`. Se il credito basta, l'inventario sottrae le monete e aggiunge il materiale. Un acquisto rifiutato lascia i valori intatti.

### C. Scegli una ricetta

Il banco usa lo stesso sistema del mercante, ma mostra le ricette. Prima di iniziare, `CanCraft()` verifica ricetta e prefab, fase corrente, quantità ancora necessarie, uscita libera e materiali disponibili.

I pezzi «ancora necessari» sono gli slot vuoti per quella fase e quel `pieceId`, meno i pezzi già prodotti e ancora da posare. `CraftedPiece` identifica questi ultimi. Il vecchio generatore automatico non produce questa etichetta.

`TryReserve()` mette da parte **l'intera quantità della ricetta**. Anche per ottenere un risultato perfetto devi avere tutti i materiali richiesti all'inizio.

### D. Completa il ritmo

`RhythmSequence` crea i tempi delle note e giudica i colpi. `RhythmAudio` genera un metronomo al BPM della ricetta, con accento ogni quattro battiti, un motivetto pizzicato e gli accenti acuti delle note bersaglio. Tutti partono durante «Preparati» nella stessa clip, usando l'orologio audio di Unity, chiamato **DSP**, per misurare il tempo. `ConstructionUI` disegna le note. I tre livelli sonori sono regolabili separatamente nel componente `RhythmAudio`.

Premendo Spazio, il sistema valuta la prossima nota non ancora giudicata. Un colpo troppo anticipato la consuma come errore. Una nota lasciata passare viene segnata come mancata. Premere ripetutamente non migliora il punteggio.

Con le ricette attuali, ogni tentativo ha **8 note**, richiede **6 colpi corretti** e usa **100 BPM** con una finestra di **±0,15 secondi**.

| Risultato | Pezzi creati | Materiali consumati alla fine |
| --- | --- | --- |
| 8 note corrette | 1 | Metà ricetta |
| 6 o 7 note corrette | 1 | Ricetta intera |
| Da 0 a 5 corrette oppure pulsante Annulla | 0 | Metà ricetta |
| Interruzione tecnica gestita dal sistema | 0 | Nessuno: rimborso completo |

Il rimborso restituisce **materiali**, non monete. `CraftingReservation.Settle()` chiude il conto una sola volta e impedisce un doppio rimborso.

Esc sospende il minigioco e il suo audio. Anche la perdita del focus dell'applicazione sospende la lavorazione; questo controllo non equivale a una pausa globale di tutto il mondo di gioco.

### E. Prendi e posa il pezzo

La riuscita crea il prefab sull'`outputPoint` del banco e mostra un'interfaccia trasparente sul bordo dello schermo: guardi il pezzo reale nella scena, senza foto o anteprima. `CraftingResultView` porta temporaneamente la camera del giocatore su un'inquadratura ravvicinata del tavolo. Puoi chiudere il menu e premere E guardando il pezzo, oppure scegliere **Sposta nel recinto**: `ConstructionStorageArea` trasferisce la stessa istanza in un punto libero del deposito, senza cambiare costi o conteggi, e torna alle ricette. Entrambe le azioni ripristinano la visuale precedente. Se il deposito è pieno, il pezzo rimane sul banco e resta attiva la visuale dedicata.

`PlayerPieceInteractor` chiama `DraggableObject.BeginDrag()`. Il pezzo segue un punto davanti alla camera, si rimpicciolisce e può essere ruotato tenendo il tasto destro del mouse. Con un Rigidbody, l'inseguimento avviene tramite velocità in `FixedUpdate()`.

Quando il pezzo entra in uno slot, `PlaceholderSlot` confronta il suo `pieceId` con quello atteso. Se combaciano, si iscrive all'evento `OnReleased`. **Entrare nello slot non basta: devi rilasciare il pezzo con E mentre è dentro.**

Al rilascio lo slot chiama `LockAt()`: posizione e rotazione diventano quelle dello slot, la scala converge verso quella dello slot e, se disponibile, la mesh viene sostituita con quella dello slot. Un sacco generico può quindi diventare una parte delle fondamenta. La rotazione manuale non è una condizione di validità della posa.

```text
E sul pezzo → BeginDrag → inseguimento davanti alla camera
                                  ↓
             ingresso nello slot con lo stesso pieceId
                                  ↓
E per rilasciare → OnReleased → Fill → LockAt → OnSlotFilled
```

### F. Avanza di fase e mostra il dialogo

`MonumentProgressTracker` ascolta gli slot individuati nel suo `Start()`. Ogni posa incrementa il totale e controlla la prossima milestone, cioè il prossimo traguardo di dialogo nell'asset.

Il tracker passa alla fase successiva quando **tutti** gli slot della fase corrente sono pieni; salta le fasi senza slot. Gli slot futuri hanno renderer e collider disattivati. Gli slot già completati restano parte della costruzione.

Le fasi e le milestone hanno scopi diversi:

| Dato | Decide | Esempio |
| --- | --- | --- |
| `buildPhase` di slot e ricetta | Quale parte puoi costruire adesso | Fase 0: fondamenta |
| `piecesRequired` di una milestone | Quando parte un dialogo sul totale posato | Dialogo dopo 4 pezzi complessivi |

`ArchitectDialoguePanel` mostra le battute una alla volta. `DialogueLog` conserva la voce e `DialogueJournalPanel` la rende rileggibile con Tab, E e Q.

Quando tutte le fasi degli slot rilevati sono complete, il tracker invia `OnReconstructionCompleted`. `BasilicaCompletionSpawner` crea una sola istanza della basilica sul punto assegnato. Non elimina automaticamente i pezzi già posati.

## 4. Cosa blocca cosa

| Situazione | Movimento e visuale | Afferrare / aprire postazioni | Note del minigioco | Esc apre la pausa? |
| --- | --- | --- | --- | --- |
| Gioco libero | Consentiti | Consentiti | Nessuna lavorazione | Sì |
| Pezzo in mano, tasto destro premuto | Movimento sì, visuale sospesa per ruotare il pezzo | Puoi rilasciare; banco non apribile | Nessuna lavorazione | Sì |
| Dialogo aperto | Restano consentiti dal controller attuale | Bloccati | Nessuna lavorazione | No |
| Diario aperto | Bloccati | Bloccati | Nessuna lavorazione | No: chiudi con Tab |
| Mercante o banco aperto | Bloccati | Bloccati | Attive solo durante una lavorazione | Sì |
| Pausa aperta | Bloccati | Bloccati | Sospese | Esc riprende |

`Time.timeScale = 0` ferma la simulazione in pausa. Gli script controllano anche gli stati dei pannelli perché alcuni input, l'orologio audio e le animazioni UI possono usare tempi indipendenti.

## Configurazione attuale e limiti

Questi dati vengono dai file salvati, non da una nuova verifica in Play Mode:

| Elemento | Stato presente |
| --- | --- |
| Versione Unity | `6000.3.18f1` |
| Scene del gioco | `MainMenu`, `GameScene` |
| Elenco scene di build salvato | Solo `GameScene` |
| Credito iniziale in scena | 200 monete |
| Pietra / calce | 3 / 2 monete a unità |
| Ricetta Fondamenta, fase 0 | 4 pietre + 2 calce, per ciascuno dei 4 slot |
| Ricetta Pavimento, fase 1 | 6 pietre + 2 calce, per ciascuno dei 4 slot |
| Identificativo condiviso dei pezzi/slot attuali | `C1`; la fase distingue le due ricette |
| Raggio massimo di interazione in `GameScene` | 5 metri, mentre il valore iniziale nel sorgente è 3 |
| Distanza del pezzo tenuto in `GameScene` | 2 metri, mentre il valore iniziale nel sorgente è 1,5 |
| `PiecePhaseSpawner` | Presente ma disabilitato; la produzione avviene al banco |

Nel file di scena esiste anche un'istanza `PlaceHolder` disattivata: non rientra negli otto slot attivi rilevati dal tracker.

**Valori in Inspector e valori nel codice:** il `= 200` di un campo definisce un valore iniziale. Se una scena o un prefab ha già un valore salvato, è quello a essere usato. Per questo modificare il numero nel sorgente può non cambiare la partita.

**Soglie dei dialoghi:** l'asset attuale contiene `4, 1, 26, 34, 42, 48, 56, 64`. Il tracker rispetta l'ordine dell'elenco e controlla una sola milestone per posa: il primo dialogo scatta al quarto pezzo e il secondo, con soglia `1`, al quinto. Le soglie da `26` in poi superano gli otto slot attuali. Per ottenere un dialogo a fine fondamenta e uno a fine pavimento, imposta rispettivamente `4` e `8`.

**Memoria e salvataggi:** inventario e monete vengono ricreati nell'`Awake()` del sistema; il progresso è runtime e non ha un caricamento da disco. `DialogueLog` è una lista statica in memoria, senza metodo di svuotamento: può conservare voci fra cambi scena nella stessa esecuzione. Non è un salvataggio permanente. In Editor la sua durata dipende anche dalle impostazioni di Domain Reload. `AudioManager` sopravvive ai cambi scena; non contiene un salvataggio esplicito dei volumi su disco.

**Slot rilevati una volta:** il tracker trova gli slot attivi al proprio `Start()`; slot creati o attivati soltanto più tardi non si registrano automaticamente. Per nuove fasi, prepara gli oggetti attivi nella scena prima dell'avvio e lascia che sia il tracker a nasconderne renderer e collider.

Per intervenire su questi punti, passa a [Modifiche passo per passo](modifiche-e-problemi.md).
