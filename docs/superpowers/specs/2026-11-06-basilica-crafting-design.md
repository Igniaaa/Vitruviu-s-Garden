# Acquisto materiali e creazione ritmica della basilica

Stato: approvato dall'utente il 6 novembre 2026 e implementato. I valori di bilanciamento restano modificabili nell'Inspector.

## Obiettivo e requisiti confermati

Sostituire la comparsa automatica dei pezzi con un ciclo giocabile: acquistare materiali presso un punto o personaggio, portarli nell'inventario, creare un pezzo presso un banco da lavoro tramite QTE possibilmente ritmico e con pattern variabile, quindi trasportarlo ai placeholder della basilica.

| Esito | Pezzo prodotto | Materiali consumati |
| --- | --- | --- |
| Riuscita con abbastanza successi | Uno | 100% della ricetta |
| Riuscita perfetta, tutti i QTE corretti | Uno | 50% della ricetta |
| Fallimento | Nessuno | 50% della ricetta, restituzione dell'altra metà |

## Situazione attuale e vincoli

- Unity 6000.3.18f1, Input System, uGUI, TextMeshPro e URP sono già presenti.
- `PiecePhaseSpawner` genera otto istanze all'avvio: quattro per le fondamenta e quattro per il pavimento. Tutte usano un prefab trascinabile con `pieceId = C1`.
- `MonumentProgressTracker` sblocca i placeholder per fase; `PlaceholderSlot` e `DraggableObject` gestiscono aggancio e trasformazione del pezzo nella mesh definitiva.
- Non esistono inventario, valuta, negozio o creazione. Pausa, quaderno, dialoghi e movimento hanno già controlli di esclusione da estendere.
- Conservare le modifiche dell'utente presenti nel progetto e la correzione del placeholder non configurato, attualmente inattivo.
- Implementare in C# con le dipendenze esistenti, senza servizi esterni o pacchetti aggiuntivi.

## Approccio proposto

Introdurre componenti dedicati per inventario, vendita e creazione, con materiali e ricette come ScriptableObject. Riutilizzare interazione, prefab trascinabili e progressione esistenti. Disattivare lo spawn automatico nella scena; la nuova creazione possiede il momento di produzione.

Questo è il percorso adatto al progetto: lasciare lo spawn automatico attivo aggirerebbe acquisti e QTE, mentre riscrivere aggancio e fasi aggiungerebbe rischio senza modificare il ciclo richiesto. Il vecchio componente può rimanere disponibile per altre scene.

## Flusso e interazione

1. Aggiungere in `GameScene` un punto vendita riconoscibile e un banco, collocati vicino al deposito iniziale senza bloccare passaggi o placeholder. Per la prima versione usare oggetti semplici e materiali coerenti con la scena, sostituibili con modelli successivamente.
2. Guardare il punto vendita e premere E apre il negozio: saldo, inventario, prezzi e acquisto per quantità. Nessun acquisto con saldo insufficiente; nessun numero negativo o overflow.
3. Guardare il banco e premere E apre l'elenco ricette. Mostrare materiali richiesti e posseduti, fase e pezzi ancora necessari.
4. Iniziare solo con tutti i materiali richiesti, una ricetta della fase corrente e nessun'altra lavorazione. Il costo pieno viene prelevato una sola volta prima della sequenza.
5. Al termine mostrare esito, successi e materiali effettivamente consumati o restituiti. In caso di riuscita generare un solo pezzo sopra un punto di uscita del banco.
6. Dopo la chiusura ripristinare controllo e cursore; trasporto, posa, dialoghi di avanzamento e comparsa finale della basilica continuano attraverso i sistemi attuali.

Il banco non avvia un nuovo tentativo finché il pezzo precedente occupa la zona di uscita. Le ricette della fase successiva si sbloccano quando sono stati riempiti i placeholder della fase corrente. La produzione riuscita non può superare i pezzi previsti: contare sia quelli già piazzati sia quelli prodotti e ancora disponibili. Un fallimento non incrementa tale conteggio.

## Materiali, ricette e valuta: proposta iniziale

Inventario astratto, senza oggetti fisici per ogni risorsa. Valori interi, visibili e modificabili nell'Inspector.

- Credito iniziale: 200 monete per la partita; nessun sistema di guadagno o salvataggio persistente aggiunto in questa versione.
- Pietra: 3 monete per unità. Calce: 2 monete per unità.
- Un pezzo delle fondamenta: 4 pietra e 2 calce, fase 0.
- Un pezzo del pavimento: 6 pietra e 2 calce, fase 1.
- Otto riuscite normali costano complessivamente 152 monete. Errori ripetuti possono esaurire il credito; saldo iniziale e prezzi sono parametri di bilanciamento, non una nuova progressione economica.
- Ogni quantità di ricetta deve essere positiva e pari: così il 50% è sempre esatto. Le ricette non valide non possono avviare acquisti o creazioni collegate e riportano un messaggio chiaro di configurazione.
- Nuovi tipi di materiali, ricette, prefab e fasi devono poter essere aggiunti senza riscrivere la logica.

## QTE ritmico: proposta iniziale

- Un solo tasto, Spazio, distinto da E usato per interagire; otto note per tentativo, riuscita da sei note corrette su otto.
- Otto note corrette e nessuna pressione extra producono il risultato perfetto. Una pressione errata rende impossibile il perfetto e conta come errore; lo spam non deve trasformarsi in una strategia vincente.
- Tempo base 100 BPM; pattern nuovo a ogni tentativo, generato su una griglia musicale con distanze di mezzo, uno o un battito e mezzo. Evitare pattern identici consecutivi.
- Breve conto alla rovescia, segnali visivi anticipati, metronomo udibile e indicatore della zona di pressione. Tolleranza iniziale di 150 ms per lato, configurabile. Nessun obbligo di reagire a una nota senza preavviso.
- Giudicare una sola pressione per nota. Pressioni fuori finestra e note mancate contano come errori; il risultato deve dipendere dai tempi reali, non dal framerate.
- Segnali audio generabili localmente, senza dipendenze da file musicali esterni. La sequenza ritmica ha una propria base temporale sincronizzata ai segnali audio.

## Transazioni, interruzioni e input

- Prenotare il costo completo quando parte il tentativo. Normale: nessun rimborso. Perfetto o fallimento: rimborso esatto del 50% per ogni materiale.
- Chiudere il menu prima dell'avvio non costa niente. Un annullamento esplicito dopo l'avvio equivale a fallimento e restituisce metà. Un problema tecnico che impedisce di produrre un pezzo o rimuove la sessione restituisce tutto; non applicare due volte la transazione.
- Durante negozio e creazione bloccare movimento, visuale, presa/rilascio e apertura del quaderno. Non aprire queste schermate mentre si tiene un pezzo o è attivo un dialogo.
- La pausa ferma QTE e segnali audio senza assegnare errori; la ripresa riallinea i tempi. Il tasto Esc apre la pausa e non annulla contemporaneamente la lavorazione.
- Non usare il tasto che apre una schermata per attivare anche il suo primo comando nello stesso frame.
- Spiegare chiaramente materiali mancanti, saldo insufficiente, uscita occupata, fase bloccata e ricetta non valida.

## Verifica prevista

- Test della transazione per successo normale, perfetto, fallimento, annullamento e interruzione tecnica; nessun rimborso o pezzo duplicato.
- Test dei limiti temporali QTE, note mancate, pressioni extra, pattern variabile e sospensione/ripresa.
- Test di acquisti, inventario insufficiente e quantità di ricetta non valide.
- Verifica in Play Mode del ciclo negozio → banco → QTE → pezzo trascinabile → placeholder; niente pezzi automatici all'avvio.
- Completare le quattro fondamenta e verificare sblocco del pavimento, disponibilità della nuova ricetta e assenza di interferenze con pausa, quaderno e dialoghi.

## Decisioni lasciate all'implementazione

Nomi e ripartizione dei componenti, disposizione precisa delle schermate, posizione finale degli oggetti, colori, dettagli dei segnali e struttura dei test. Usare i sistemi UI e gli stili già presenti; privilegiare chiarezza degli esiti e facilità di configurazione.
