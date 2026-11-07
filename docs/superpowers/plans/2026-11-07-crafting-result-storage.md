# Risultato del crafting e deposito nel recinto

**Richiesta:** riconnettere MCP, mostrare il pezzo reale sul tavolo alla riuscita e offrire chiusura oppure invio al recinto esistente.

**Scelte di implementazione:** mantenere la UI uGUI già generata dal codice. Un RawImage mostra una camera temporanea del banco e del pezzo reale. Chiudi conserva il pezzo sul tavolo; l'invio trasferisce la stessa istanza e riapre le ricette. Un componente di deposito usa il volume già presente su PieceRespawnPoint dentro Fences, cercando un punto libero sopra una superficie solida. Nessuna modifica a costi, ritmo, progressione o compatibilità degli slot.

## Passi e verifiche

- [x] Riconnettere Unity via pannello MCP e verificare una chiamata live.
- [x] Aggiungere test che falliscano prima dell'implementazione: trasferimento fisico, velocità azzerate, posizioni distinte, rifiuto deposito pieno/non configurato, rifiuto pezzo bloccato o in mano.
- [x] Implementare ConstructionStorageArea e integrare riferimento e pezzo appena creato in ConstructionSystem. Il conteggio CraftedPiece deve restare invariato; invii ripetuti dallo stesso risultato devono essere impossibili.
- [x] Aggiungere l'anteprima live e le due azioni al risultato riuscito. Fallimento e interruzione mantengono la schermata informativa. Camera e RenderTexture devono essere liberate a chiusura/cambio schermata/disattivazione e sospese in pausa.
- [x] Collegare il deposito alla scena tramite MCP, salvare e aggiornare le guide.
- [x] Eseguire tutta la suite EditMode e una prova guidata Play: riuscita, preview, chiusura, secondo pezzo inviato al recinto, banco libero, nessuna duplicazione, invio rifiutato se pieno e fallimento senza anteprima.
- [x] Controllare immagini della schermata finale e fare una revisione del codice.

**Casi di attenzione:** il deposito non deve sovrapporre i pezzi o posizionarli nei pali; il trasferimento deve aggiornare subito le query fisiche del banco; pausa e chiusura non devono lasciare una camera aggiuntiva attiva. Le scene e gli asset già modificati dall'utente non vanno ripristinati.

## Esito verificato

Il 7 novembre 2026: sei test deposito falliti prima dell'implementazione per assenza del componente; dopo l'implementazione, intera suite EditMode **33/33** superata. Simulazione guidata in GameScene **36/36** controlli superati, report `.ai-game-dev/verification/delivery/play.txt`. Controllate visivamente la schermata riuscita e quella di recinto pieno; nessun errore/exception in Console. Revisione indipendente senza rilievi. Editor lasciato fuori da Play Mode con deposito e riferimento salvati in scena e MCP connesso.

## Revisione richiesta: risultato trasparente

La successiva richiesta dell'utente sostituisce l'anteprima con la vista diretta della scena: schermata riuscita trasparente e raccolta sul bordo, senza RawImage, camera o RenderTexture. Conservati Chiudi e Sposta nel recinto; ogni altra schermata ripristina il layout precedente. Verifica finale: **33/33 EditMode**, **38/38 controlli Play**, screenshot del risultato ispezionato e report `.ai-game-dev/verification/delivery-transparent/play.txt`.
