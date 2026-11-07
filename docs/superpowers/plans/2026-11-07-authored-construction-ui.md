# UI di costruzione modificabile nel Canvas

**Richiesta:** sostituire la generazione runtime dell'interfaccia con GameObject salvati nel Canvas e modificabili dall'utente.

**Architettura:** `Canvas/Construction UI` contiene cinque schermate indipendenti: Acquisto, Ricette, Ritmo, Completato, Fallimento. Tutti gli elementi, le righe e il pool delle 64 note sono oggetti salvati nella scena. `ConstructionUI` si limita a selezionare schermate, aggiornare dati e stato e collegare azioni; non crea/distrugge elementi o impone dimensioni e colori di layout durante il gioco. Un comando Editor prepara una volta gli oggetti e i riferimenti. Il sistema riceve il riferimento serializzato alla UI esistente.

**Conservare:** acquisti, ricette, ritmo, pausa, esiti e rimborsi, deposito nel recinto, risultato trasparente e visuale dedicata. Il Canvas esistente e gli altri menu non vanno ricreati o sostituiti.

- [x] Test inizialmente falliti: oggetti salvati, riferimenti assegnati, nessuna crescita della gerarchia a runtime, conservazione delle modifiche di layout, capienza note, acquisto senza listener duplicati.
- [x] Separare controller e righe dati, collegare `ConstructionSystem` alla UI salvata.
- [x] Creare in Edit Mode le cinque schermate nel Canvas, collegarle e salvare GameScene; preparazione idempotente per conservare future modifiche manuali.
- [x] Superare l'intera suite EditMode e verificare in Play acquisti tramite bottoni, ritmo, risultato, deposito, pausa, visuale e assenza di elementi UI nuovi.
- [x] Ispezionare schermate e gerarchia, aggiornare documentazione e fare una revisione mirata.

**Decisioni pratiche:** aggiungere materiali/ricette significa duplicare una riga salvata e assegnarle l'asset nel suo componente; i pulsanti di acquisto espongono quantità modificabili. Le note si muovono a runtime entro una corsia modificabile, senza generare oggetti. Colori dei giudizi e velocità di scorrimento rimangono campi Inspector. Il risultato riuscito ha un proprio pannello trasparente, così non occorre riscrivere il layout quando si cambia schermata.

**Esito:** 44/44 test EditMode e 58/58 controlli Play superati. Gerarchia salvata di 229 oggetti, invariata tra schermate e dopo una seconda preparazione Editor. Testati acquisto, CREA, Chiudi, Annulla, Torna al banco e Sposta nel recinto; verificati raycast dei pulsanti dopo il primo frame renderizzato. Ispezionate immagini di acquisto, ricette e ritmo. Revisione indipendente senza rilievi. Report `.ai-game-dev/verification/authored-ui/play.txt`.
