# Modifiche passo per passo e problemi frequenti

[Indice generale](../README.md) · [Guida da zero](guida-codice.md) · [Mappa degli script](mappa-script.md)

## Prima di modificare

1. Esci da **Play Mode**. Le modifiche ai componenti di scena fatte durante la partita generalmente si perdono uscendo da Play; gli asset condivisi possono invece conservare modifiche.
2. Apri la scena giusta: `Assets/Scenes/GameScene.unity` per il gioco, `Assets/Scenes/MainMenu.unity` per il menu.
3. Seleziona l'oggetto in **Hierarchy** oppure l'asset nella finestra **Project**, secondo le istruzioni qui sotto.
4. Cambia un parametro alla volta, salva e controlla il risultato atteso.

I nomi come `startingCoins` sono quelli del codice: nell'Inspector il campo può apparire come **Starting Coins**. Se uno script mostra un numero diverso dal componente, conta il valore salvato nel componente.

**Per le interfacce di acquisto e crafting:** il layout è salvato sotto `Canvas > Construction UI > Screens` e si modifica nell'Editor, fuori da Play Mode. Il codice aggiorna soltanto dati, azioni e visibilità. Vedi [modifica delle UI nel Canvas](interfacce-crafting-acquisto.md) per duplicare righe e personalizzare i pannelli.

## Trova subito il punto da cambiare

| Voglio cambiare… | Punto da aprire |
| --- | --- |
| Monete iniziali | `GameManager > ConstructionSystem > Starting Coins` |
| Prezzo di pietra o calce | `Assets/Data/Construction/Pietra.asset` / `Calce.asset` |
| Costi e difficoltà dei pezzi | `Assets/Data/Construction/Fondamenta.asset` / `Pavimento.asset` |
| Forma, posizione o fase di uno slot | Istanza del prefab `PlaceHolder` nella scena |
| Testi, ritratti e soglie dei dialoghi | Asset milestone e componente `ArchitectDialoguePanel` |
| Testi o immagine del diario | Campi della milestone; layout del pannello diario nella scena |
| Distanza per afferrare o trasportare | `PlayerPieceInteractor` nella scena |
| Velocità del personaggio | `StarterAssets.FirstPersonController` sul giocatore |
| Reattività e dimensione del pezzo in mano | `DraggableObject` sul prefab del pezzo |
| Colori e layout delle interfacce | GameObject sotto `Canvas > Construction UI > Screens`, componenti RectTransform / Image / TextMeshPro / Button |
| Quantità dei pulsanti di acquisto | Componente `ConstructionMaterialRow` sulla riga, elenco `Purchases > Quantity` |
| Livelli del metronomo, motivetto e accenti delle note | `GameManager > RhythmAudio > Metronome Volume / Melody Volume / Note Volume` |
| Destinazione dei pezzi creati | `GameManager > ConstructionSystem > Storage Area`, collegato al componente `ConstructionStorageArea` del deposito |
| Inquadratura del pezzo completato | `GameManager > CraftingResultView > View Offset / Field Of View / Horizontal Framing` |
| Musica, effetti e volumi | `AudioManager`, AudioMixer e slider collegati al controller impostazioni |
| Monumento mostrato alla fine | `GameManager > BasilicaCompletionSpawner` |

## 1. Cambiare credito, prezzi e difficoltà

### Più monete iniziali

Seleziona `GameManager`, trova `ConstructionSystem` e modifica **Starting Coins**, per esempio da `200` a `300`. Salva la scena e avvia una nuova prova: il mercante deve mostrare il nuovo credito.

Il credito non si ricarica da solo e non viene ricostruito mentre una partita è già in corso. Cambiare il valore nell'Inspector durante il gioco non reinizializza l'inventario già creato.

### Un prezzo diverso

Apri `Pietra.asset` o `Calce.asset` e cambia **Unit Price**, mantenendolo maggiore di zero. Avvia la partita e apri il mercante: verifica il prezzo unitario e quello dei pulsanti `+1`, `+5`, `+10`.

### Un minigioco più facile

Apri la ricetta `Fondamenta.asset` o `Pavimento.asset`:

| Campo | Significato | Vincolo della validazione |
| --- | --- | --- |
| `noteCount` | Numero di note | Da 2 a 64 |
| `minimumHits` | Colpi corretti necessari | Da 1 a `noteCount` |
| `bpm` | Velocità del ritmo | Da 40 a 180 |
| `hitWindow` | Tolleranza prima e dopo il tempo esatto, in secondi | Almeno `0.02`, al massimo `15 / bpm` |
| `costs` | Materiali da prenotare | Elenco non vuoto, quantità positive e pari, materiale senza duplicati e con prezzo valido |

Per esempio, a 100 BPM la finestra massima valida è `0.15`: anche se lo slider consente valori superiori, `IsValid()` rifiuta la ricetta. Per una prima semplificazione puoi ridurre `minimumHits` da 6 a 5, lasciando 8 note.

**Controllo:** il banco deve proporre `CREA 5/8`; cinque colpi corretti devono dare una riuscita normale, otto una riuscita perfetta.

## 2. Aggiungere uno slot a una fase esistente

1. Nella scena, seleziona uno slot della fase desiderata e duplicalo con **Ctrl+D**.
2. Sposta la copia e adatta forma e scala. Tieni semplici le trasformazioni del genitore: il pezzo fissato adotta la scala locale dello slot e non viene automaticamente reso suo figlio.
3. Controlla `PlaceholderSlot > Piece Id` e `Build Phase`. Per gli slot attuali sono `C1` e fase `0` oppure `1`.
4. Mantieni attivo il GameObject e il componente prima di Play. Il tracker gestisce la visibilità delle fasi future tramite renderer e collider.
5. Salva e avvia una nuova partita.

**Controllo:** con uno slot aggiuntivo nelle fondamenta, servono cinque pose per passare al pavimento. Il banco deve permettere un pezzo in più, contando anche quelli già prodotti ma non posati.

Il dialogo non si adatta da solo: se rappresenta «fondamenta completate», aggiorna anche la sua soglia. Rivaluta credito e materiali necessari.

## 3. Aggiungere un nuovo tipo di pezzo o una fase

Questa procedura configura contenuti usando il sistema esistente.

1. Duplica un prefab di pezzo funzionante in `Assets/Prefabs/INTERACTABLE`. Mantieni `DraggableObject` e il collider sullo **stesso GameObject**; per il comportamento fisico attuale conserva anche il Rigidbody.
2. Dai al pezzo un `pieceId` esplicito, per esempio `Colonna`. È il codice di compatibilità, non il nome visibile dell'oggetto.
3. Prepara uno o più slot con lo stesso `pieceId`. Assegna la mesh definitiva da usare alla posa, renderer e collider; il componente richiede anche un Rigidbody.
4. Imposta la fase degli slot: per una fase dopo il pavimento attuale usa `2`.
5. Crea un asset da **Create > Vitruvio Garden > Construction > Recipe**. Assegna nome, prefab, `buildPhase = 2`, costi validi e parametri ritmici.
6. Aggiungi l'asset a `GameManager > ConstructionSystem > Recipes`.
7. Se usi un nuovo materiale, crealo da **Create > Vitruvio Garden > Construction > Material**, assegna prezzo positivo e aggiungilo anche a `Materials` del sistema.
8. Salva prefab, asset e scena; prova dall'inizio.

Prepara anche le righe del Canvas: duplica una riga in `Screens > Ricette` e assegnale la nuova ricetta nel componente `ConstructionRecipeRow`. Per un nuovo materiale duplica una riga di `Screens > Acquisto` e assegnagli il relativo asset. Gli elenchi del sistema non generano più automaticamente gli elementi grafici.

**Controllo:** la ricetta deve risultare bloccata finché la fase precedente non è completa; poi deve attivarsi. Il pezzo deve agganciarsi soltanto agli slot con lo stesso ID. Il completamento finale deve attendere anche i nuovi slot.

Non servono nuove classi C# per aggiungere una normale ricetta. Le fasi nel codice partono da **0**, mentre il banco le mostra come `buildPhase + 1`.

## 4. Modificare i dialoghi e il diario

1. Apri `Assets/Data/MonumentMilestoneSet_Vitruvio.asset`.
2. Nell'array **Milestones**, seleziona una voce.
3. Imposta `label` per riconoscerla, `piecesRequired` come numero **totale** di pezzi posati e `dialogue` come elenco ordinato di battute con `speaker` e `text`.
4. Per il diario compila `constructionNote`, `deArchitecturaExcerpt` e, se serve, `deArchitecturaPageImage` con uno Sprite.
5. Mantieni le soglie in ordine crescente e coerenti con il numero di slot.
6. Controlla che `GameManager > MonumentProgressTracker > Milestone Set` punti all'asset desiderato e `Dialogue Panel` al pannello corretto.

Per il livello attuale, due soglie **4** e **8** corrispondono a fine fondamenta e fine pavimento. L'asset salvato ha invece **4** e **1** nelle prime due posizioni: la seconda parte al quinto pezzo perché il tracker rispetta l'ordine e valuta una voce per posa.

**Controllo:** completa il numero previsto di pose, avanza le battute con E e apri il diario con Tab. La voce deve mostrare dialogo, nota ed eventuale immagine; E e Q devono cambiare pagina senza afferrare oggetti.

`label` è usata anche come titolo nel diario. I commenti più vecchi che la descrivono come etichetta solo per Inspector non rappresentano più l'intero utilizzo.

## 5. Cambiare i comandi

Per movimento, visuale, salto, corsa, diario e pausa apri `Assets/StarterAssets/InputSystem/StarterAssets.inputactions`, seleziona la mappa **Player**, modifica i binding e salva l'asset.

Se rinomini un'azione devi aggiornare anche il nome configurato in `PlayerInputHandler`. In particolare `PreviouseJournalPage` va mantenuto uguale da entrambe le parti.

| Comando separato dall'asset Player | Dove cambiarlo |
| --- | --- |
| Afferra/rilascia e apri postazione | `PlayerPieceInteractor.interactKey`, configurabile in Inspector |
| Avanzamento dialogo | `ArchitectDialoguePanel.advanceKey`, configurabile in Inspector |
| Rotazione pezzo con tasto destro | Controllo di `Mouse.current.rightButton` in `PlayerPieceInteractor` |
| Colpo ritmico con Spazio | Binding `"<Keyboard>/space"` creato in `ConstructionSystem.Awake()` |
| Chiusura banco con E | Lettura di `Keyboard.current.eKey` in `ConstructionSystem.Update()` |

Aggiorna anche messaggi, prompt e istruzioni della UI. Per esempio, cambiare `interactKey` da E a F non cambia il testo `Premi E`, i prompt di `ConstructionStation` o la chiusura del banco.

Sono presenti binding gamepad per movimento, visuale, salto e corsa. Le altre interazioni citate leggono direttamente tastiera/mouse: questo non costituisce un supporto completo al gamepad.

## 6. Collegare menu, audio e basilica finale

### Menu e scene nella build

Apri **File > Build Profiles**, controlla la **Scene List** del profilo attivo e aggiungi entrambe le scene del gioco, abilitate: prima `MainMenu`, poi `GameScene`. Se un profilo usa un elenco personalizzato, verifica quello effettivamente usato nella build.

In `MainMenuController`, il pulsante di inizio chiama `StartGame()` per mostrare la prepartita; il pulsante di conferma deve chiamare `ConfirmStartGame()` per caricare il gioco. Il ritorno dalla pausa usa `QuitToMainMenu()`. I nomi configurati devono coincidere con quelli delle scene.

**Controllo:** menu → prepartita → gioco → pausa → ritorno al menu, senza errori di caricamento nella Console.

### Slider e suoni

Assegna gli eventi **On Value Changed (float)** degli slider ai metodi `Set…Volume()` di `SettingsPanelController`. Configura gli slider tra 0 e 1. Per riallinearli a ogni apertura, il controller deve ricevere `OnEnable()` quando il pannello viene riaperto; se sta su un manager sempre attivo, questo aggiornamento non avviene automaticamente.

Nel manager audio assegna le sorgenti e il mixer con i quattro parametri esposti previsti. Le voci di musica si chiamano come le scene per l'avvio automatico. L'animazione hover cerca `buttonpop` in `uiList`; il suono dello spawner, se configurato, viene cercato in `sfxList`.

**Controllo:** cambia un volume, cambia scena e riapri le impostazioni. Controlla sia il suono sia la posizione degli slider.

### Basilica finale

In `GameManager > BasilicaCompletionSpawner` assegna tracker, prefab desiderato e Transform del punto di comparsa. Il prefab attuale è `Assets/Prefabs/ENV/Basilica_Ricostruita.prefab`.

**Controllo:** completa tutti gli slot e verifica una sola comparsa nel punto desiderato. Il sistema non elimina le fondamenta o il pavimento posati: controlla eventuali sovrapposizioni visive con il prefab finale.

## Problemi frequenti

| Sintomo | Cosa controllare, in ordine |
| --- | --- |
| Non mi muovo o non guardo intorno | Dai focus alla Game View; chiudi diario/banco/pausa; verifica `PlayerInputHandler`, asset assegnato, mappa Player e controller |
| `Action Map NOT FOUND` o errore sugli input | Nome della mappa e nomi delle azioni devono corrispondere all'asset, incluso `PreviouseJournalPage` |
| E non afferra un pezzo | Camera reale assegnata o tag MainCamera; distanza; collider attivo e non trigger; `DraggableObject` sul collider colpito; pezzo non già bloccato |
| E non apre il banco | Rilascia il pezzo in mano; chiudi altri pannelli; verifica collider non trigger e `ConstructionStation` sull'oggetto o un suo genitore |
| Non posso creare altri pezzi | Leggi il motivo sulla ricetta: materiali, fase, pezzi già prodotti o uscita occupata; cerca anche pezzi non posati lontani dal banco |
| Il pulsante di deposito è disabilitato | Verifica il riferimento `Storage Area`, componente attivo e pezzo appena creato ancora disponibile; un fallimento di crafting non produce un pezzo |
| Il recinto risulta pieno | Libera spazio nel volume assegnato; il sistema evita di sovrapporre oggetti e lascia il pezzo sul tavolo quando non trova posto |
| Ho tutti i materiali per metà ricetta ma non posso iniziare | Devi possedere l'intero costo prima della prenotazione; il rimborso arriva alla fine |
| Ricetta non valida | Nome/prefab/fase; quantità positive e pari; niente ingredienti duplicati; prezzi positivi; note/soglia/BPM/finestra validi |
| Lo slot non aggancia | Stesso `pieceId`; fase sbloccata; entra nel volume e rilascia; collider e `DraggableObject` sullo stesso oggetto; slot presente quando il tracker si avvia |
| La guida dello slot è opaca | Materiale con trasparenza abilitata; cambiare solo l'alpha non basta |
| Il pezzo errato non diventa rosso | Imposta esplicitamente `wrongColor`: il valore iniziale nel codice è verde |
| Il pezzo si incastra | Aspetta il recupero collisione; controlla Rigidbody, `maxHoldSpeed`, distanza di trasporto e collider; evita di cambiare tutto insieme |
| Il pezzo cade senza tornare | Volume di recupero abbastanza grande e sotto il livello, `spawnPoint` assegnato, trigger raggiunto dal collider del pezzo |
| Il diario non si apre all'inizio | È previsto: serve almeno una voce aggiunta da una milestone |
| Il dialogo arriva troppo presto o non arriva | Soglie totali ordinate, asset corretto, `dialoguePanel` assegnato; le soglie oltre gli slot disponibili non saranno raggiunte |
| Vedo vecchie voci nel diario dopo un cambio scena | `DialogueLog` è statico e non viene svuotato automaticamente; non indica un salvataggio su disco |
| Esc non apre la pausa | Chiudi prima dialogo con E o diario con Tab |
| Il menu di costruzione resta nascosto dopo aver cambiato finestra | Ridai focus al gioco e verifica anche lo stato della pausa |
| `MainMenu` non si carica tornando dalla pausa | Aggiungi la scena alla Scene List del profilo di build e controlla il nome |
| Manca il suono o lo slider non funziona | Controlla `AudioManager.Instance`, sorgenti, nomi nelle liste, gruppi mixer e parametri esposti; usa gli eventi del controller impostazioni |
| La basilica finale non compare | Tutti gli slot rilevati devono essere pieni; controlla tracker, prefab, punto assegnato ed errori in Console |
| Pezzi gratuiti all'avvio insieme al banco | Verifica che `PiecePhaseSpawner` sia disabilitato |

Per trovare velocemente un componente, seleziona gli oggetti pertinenti nella Hierarchy o cerca il nome dello script nella finestra Project. La **Console** mostra errori di compilazione, riferimenti mancanti e messaggi come «Banco non disponibile».

## Verificare una modifica

### Prova manuale minima

1. Avvia `GameScene` e acquista materiali: controlla credito e quantità.
2. Apri il banco: la ricetta del pavimento deve essere bloccata all'inizio.
3. Completa una lavorazione, ritira il pezzo e posalo nello slot corretto.
4. Completa le quattro fondamenta: devono sbloccarsi i quattro slot del pavimento.
5. Controlla dialogo e diario. Completa il pavimento e verifica la comparsa della basilica.
6. Se hai toccato input, audio o UI, prova anche pausa durante il ritmo, ripresa, cambio focus e riapertura dei pannelli.

### Test della logica senza configurare la scena

Apri il **Test Runner** dall'Editor, seleziona **EditMode** ed esegui l'assembly `Basilica.Crafting.Tests`. Include **21 casi** in `CraftingCoreTests`, **6 casi audio** in `RhythmAudioTests`, **6 casi deposito** in `ConstructionStorageTests`, **6 casi visuale** in `CraftingResultViewTests` e **5 casi UI salvata** in `ConstructionAuthoredUITests`, contando le combinazioni di `[TestCase]`.

Controllano acquisti, rifiuti senza consumo parziale, prenotazioni, rimborsi, costi copiati prima di modifiche agli asset, finestre dei colpi, note perse e pattern. I casi audio analizzano i campioni della clip per verificare la melodia tra i tic, l'accento periodico, la disattivazione separata del motivetto e l'assenza di saturazione a 40, 100 e 180 BPM. Non verificano da soli tutti i collegamenti della scena o l'esperienza del giocatore. La verifica integrata del menu Tools continua a eseguire i 21 casi Core, oltre alla propria simulazione Play.

### Verifica integrata del progetto

1. Esci da Play Mode, apri GameScene e salva le modifiche che intendi conservare.
2. Esegui **Tools > Vitruvio > Verify construction crafting**.
3. Il comando richiama **Configure construction crafting**, salva la scena, esegue i casi Core ed entra in Play Mode per una simulazione guidata; alla conclusione torna in Edit Mode.
4. Leggi `.ai-game-dev/verification/core.txt` e `play.txt`, cercando eventuali righe `FAIL`; consulta anche `setup.txt` e le immagini prodotte.

**Effetti da conoscere:** questa verifica può creare configurazione mancante e disabilita `PiecePhaseSpawner`; non è un comando di sola lettura. I report precedenti vengono sovrascritti. La simulazione include acquisti, lavorazioni, pose, sblocco della fase e basilica finale, con input e pose guidati dal codice.

La guida precedente [construction-crafting.md](construction-crafting.md) riporta una verifica del **6 novembre 2026** con 21 casi Core e 45 controlli Play superati. È uno storico; per sapere se la versione modificata funziona devi eseguire una nuova verifica e leggere i suoi risultati.

## Mantenere la documentazione utile

Quando cambi una responsabilità o un comando, aggiorna la [mappa degli script](mappa-script.md). Quando cambi costi, fasi, input o scene, aggiorna anche [guida](guida-codice.md) e [README](../README.md). I documenti in `docs/superpowers` descrivono progetti e piani precedenti: per il comportamento effettivo confronta sempre i sorgenti e i dati attuali.
