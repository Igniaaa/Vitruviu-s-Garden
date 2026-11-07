# Acquisto e creazione dei pezzi

Per la documentazione completa del progetto: [indice generale](../README.md), [guida da zero](guida-codice.md), [mappa degli script](mappa-script.md) e [modifiche e problemi](modifiche-e-problemi.md).

**Cerchi i pannelli in Unity?** Acquisto, ricette, ritmo e risultato sono GameObject salvati sotto **Canvas > Construction UI > Screens**, modificabili fuori da Play Mode. `ConstructionUI` aggiorna dati e visibilità senza crearli durante la partita. La [guida alle interfacce](interfacce-crafting-acquisto.md) spiega come cambiare layout, pulsanti e righe.

Vicino al punto iniziale di GameScene si trovano il banco da lavoro e il mercante MATERIALI. Guarda il banco e premi **E** per interagire; **E** chiude il menu. Non puoi aprire il banco mentre stai portando un pezzo.

Acquista pietra e calce dal mercante, poi seleziona una ricetta al banco. La ricetta richiede tutti i materiali prima di iniziare. Premi **Spazio** quando ogni nota attraversa la linea dorata; premi **Esc** per mettere in pausa senza perdere note. Le pressioni anticipate fanno perdere la nota successiva, quindi premere ripetutamente non aiuta.

All'avvio della lavorazione parte un **metronomo al BPM della ricetta**, con un accento ogni quattro battiti, insieme a un breve **motivetto originale** dal timbro pizzicato. I suoni più acuti indicano le note da colpire. L'accompagnamento si sente già durante «Preparati» e segue pausa, ripresa e fine del tentativo.

In `GameManager > RhythmAudio` puoi regolare separatamente `Metronome Volume`, `Melody Volume` e `Note Volume`, oltre al volume generale. Imposta `Melody Volume` a `0` per disattivare il motivetto mantenendo il metronomo e gli accenti delle note.

| Esito | Produzione | Consumo |
| --- | --- | --- |
| 6 o 7 note corrette su 8 | Un pezzo | Tutta la ricetta |
| 8 note corrette | Un pezzo | Metà ricetta |
| Meno di 6, o annullamento | Nessun pezzo | Metà ricetta |
| Interruzione tecnica | Nessun pezzo | Rimborso completo |

Alla riuscita, l'interfaccia diventa trasparente e si dispone sul bordo dello schermo: il pezzo resta visibile direttamente nella scena sul tavolo, senza foto o anteprima. **Chiudi [E]** lascia il pezzo sul banco per ritirarlo a mano; **Sposta nel recinto** trasferisce la stessa istanza in un punto libero dell'area di stoccaggio e torna alle ricette. Se il recinto non ha spazio, il pezzo resta sul tavolo e appare un messaggio. I pezzi depositati rimangono afferrabili con E.

Alla riuscita si attiva anche una visuale ravvicinata del pezzo: `CraftingResultView` riposiziona temporaneamente la camera del giocatore rispetto all'uscita del banco, lasciando il pezzo sulla sinistra e i comandi sulla destra. Chiudi o invio al recinto ripristinano posizione locale, rotazione, campo visivo e controllo Cinemachine precedenti. La pausa conserva l'inquadratura; un deposito pieno mantiene la visuale del risultato. Il componente su `GameManager` espone offset, campo visivo e spostamento orizzontale per regolare l'inquadratura.

Porta il pezzo al placeholder. Completa le quattro fondamenta per sbloccare i quattro elementi del pavimento. Il banco considera anche i pezzi già creati e ancora da posare, inclusi quelli nel recinto, per evitare di produrne più del necessario.

Il riferimento `Storage Area` di `ConstructionSystem` punta a `ConstructionStorageArea` sul `PieceRespawnPoint` esistente all'interno di `Fences`. Il suo `Volume` descrive l'ingombro del deposito: il sistema cerca terreno solido e spazio libero, azzera le velocità e aggiorna subito la fisica dopo il trasferimento. La schermata completata non crea camere o RenderTexture; negozio, ricette, minigioco e risultati non riusciti mantengono il loro layout normale.

Materiali e ricette sono in `Assets/Data/Construction`. Prezzi, costi, prefab, fase, BPM, tolleranza, numero di note e soglia sono modificabili nei relativi asset. Le quantità devono essere positive e pari. `ConstructionSystem` sul GameManager contiene credito iniziale (200), materiali e ricette; `ConstructionStation` contiene tipo di postazione, punto e ingombro dell'uscita.

Il credito non si ricarica e la partita non salva l'inventario fra avvii. Le otto riuscite normali costano 152 monete; fallimenti ripetuti possono esaurire le risorse. Modifica il credito iniziale per il bilanciamento.

`Tools > Vitruvio > Configure construction crafting` configura esplicitamente la scena aperta; non modifica asset e postazioni già esistenti. Il vecchio `PiecePhaseSpawner` resta disponibile ma disattivato in GameScene.

## Verifiche

`Tools > Vitruvio > Verify construction crafting` esegue i 21 casi NUnit della logica e una simulazione di 45 controlli in Play Mode, poi torna in Edit Mode. La simulazione include tutti gli otto pezzi e la comparsa della basilica finale; durante la verifica acquisti, pressioni ritmiche e posa sono guidati dal codice. I risultati e le schermate vengono salvati in `.ai-game-dev/verification`. Salva prima eventuali modifiche alla scena.

Verifica precedente: 6 novembre 2026, 21/21 casi della logica e 45/45 controlli in Play Mode superati. Corretti anche riapertura involontaria con E e disallineamento del primo input dopo la pausa.

Verifica del 7 novembre 2026 per audio e deposito: **33/33 test EditMode** e **36/36 controlli guidati Play** superati. Controllate anche le immagini dell'anteprima del pezzo reale sul tavolo e della risposta a recinto pieno. Il report Play e le immagini sono in `.ai-game-dev/verification/delivery`; il controllo include chiusura, trasferimento senza duplicazione, conteggio dei pezzi nel recinto, pausa/ripresa e rimozione della camera temporanea. La scena viene lasciata fuori da Play Mode.

Revisione successiva del risultato trasparente, sempre il 7 novembre 2026: **33/33 test EditMode** e **38/38 controlli Play** superati. Rimossa l'anteprima: verificati sfondo trasparente, assenza di immagini e camere aggiuntive, pausa/ripresa, pulsanti e ritorno al layout normale delle ricette. Report e schermate di verifica in `.ai-game-dev/verification/delivery-transparent`.

Visuale dedicata del pezzo, 7 novembre 2026: **39/39 test EditMode** e **46/46 controlli Play** superati. Verificati inquadratura, riutilizzo della camera del giocatore, ripristino dopo chiusura e deposito, pausa e rifiuto del deposito pieno. Report e schermata ispezionata in `.ai-game-dev/verification/result-view`.

UI salvata nel Canvas, 7 novembre 2026: **44/44 test EditMode** e **58/58 controlli Play** superati. Le cinque schermate e i pool di note sono oggetti persistenti, modificabili nell'Editor; la gerarchia di 229 elementi rimane invariata durante la prova. Verificati click/raycast sui pulsanti, acquisto, CREA, annullamento, ritorno alle ricette, deposito, pausa e visuale del pezzo. Report e immagini in `.ai-game-dev/verification/authored-ui`.
