# Modificare le UI di acquisto e crafting nel Canvas

[Indice generale](../README.md) · [Guida al codice](guida-codice.md) · [Mappa degli script](mappa-script.md)

Le interfacce sono **GameObject salvati nella scena**, sotto **Canvas > Construction UI > Screens**. Puoi modificarle nell'Editor senza avviare Play. Il codice aggiorna dati, azioni e visibilità; non crea né ricostruisce pannelli, pulsanti o note durante la partita.

## 1. Dove trovarle

Apri `Assets/Scenes/GameScene.unity` e, nella Hierarchy, espandi:

```text
Canvas
  Construction UI                     Canvas annidato + ConstructionUI
    Screens
      Acquisto
        Panel
          Titolo / Riepilogo / Messaggio / Istruzioni
          Viewport
            Content
              Pietra                  ConstructionMaterialRow
                Quantita / Diminuisci / Aumenta / ACQUISTA
              Calce                   ConstructionMaterialRow
              Legno                   ConstructionMaterialRow
              Ferro                   ConstructionMaterialRow
          CHIUDI [E]
      Ricette
        Panel
          Categorie                   pulsanti del catalogo
          Categoria selezionata
          Viewport
            Content
              Fondamenta              ConstructionRecipeRow
              Pavimento               ConstructionRecipeRow
              ...                     dieci schede con silhouette
      Ritmo
        Panel
          Viewport
            Content
              Punteggio
              Corsia
                Finestra colpo / Linea colpo
                Nota 01 ... Nota 64
              Giudizi
                Giudizio 01 ... Giudizio 64
              ANNULLA
      Completato
        Panel
          Titolo / Riepilogo / Content / Istruzioni
          CHIUDI [E] / SPOSTA NEL RECINTO
      Fallimento
        Panel
          Viewport / Content / Dettagli esito
          TORNA AL BANCO / CHIUDI [E]
```

Di partenza è visibile **Acquisto** nell'Editor. Per lavorare su un'altra schermata, disattiva Acquisto e attiva quella desiderata usando la casella del GameObject nell'Inspector. Mantieni attivi `Canvas`, `Construction UI` e `Screens` per vedere l'anteprima. All'avvio del gioco il controller nasconde le schermate e mostra quella della postazione con cui interagisci.

Le schermate sono indipendenti: il risultato trasparente ha il proprio pannello, quindi cambiare la sua posizione non modifica negozio, ricette o minigioco.

## 2. Come cambiare l'aspetto

Lavora **fuori da Play Mode**, poi salva la scena con Ctrl+S.

| Voglio cambiare… | Cosa selezionare / modificare |
| --- | --- |
| Dimensioni e posizione di un pannello | Il suo `Panel`, componente `RectTransform` |
| Colore o trasparenza del fondo | Componente `Image` sulla schermata o su `Panel` |
| Font, dimensione, allineamento e colore | L'oggetto di testo, componente `TextMeshPro - Text (UI)` |
| Aspetto di un pulsante | I componenti `Image` e `Button`, più il testo figlio `Etichetta` |
| Dimensioni, colonne e distanza delle schede del negozio | `Acquisto > Panel > Viewport > Content`, componente `GridLayoutGroup` |
| Dimensioni e distanza delle schede delle ricette | `Ricette > Panel > Viewport > Content`, componente `GridLayoutGroup` |
| Categorie e aspetto della selezione | `Ricette > Panel > Categorie`, componenti `ConstructionCategoryButton` |
| Silhouette della ricetta | `Riquadro silhouette > Silhouette` sulla scheda; campo `Silhouette` di `ConstructionRecipeRow` |
| Area scorrevole | `Viewport`, con `ScrollRect` e `RectMask2D` |
| Corsia, linea e forma delle note | Gli oggetti salvati sotto `Ritmo > Panel > Viewport > Content > Corsia` |
| Distanza tra indicatori dei colpi | `Giudizi`, componente `GridLayoutGroup` |
| Velocità delle note e colori dei giudizi | `ConstructionUI` sul GameObject principale |
| Inquadratura del pezzo reale | `GameManager > CraftingResultView` |

Il Canvas principale mantiene la risoluzione di riferimento 1920 × 1080. Il Canvas annidato di Construction UI ha ordine 30; gli altri menu della scena restano separati.

### Testi fissi e dati dinamici

Puoi modificare direttamente titoli fissi, istruzioni ed etichette statiche. I campi che rappresentano dati di gioco vengono aggiornati dal controller: credito, scorte, prezzi, costi, disponibilità, punteggio e messaggi del risultato. Questi testi riflettono inventario, asset e stato della partita.

Le coordinate X delle note cambiano mentre scorrono e la larghezza della finestra del colpo segue la tolleranza della ricetta. Il resto del layout e dello stile rimane quello salvato nel Canvas.

## 3. Quantità di acquisto e nuovi materiali

Il negozio presenta i materiali in una griglia a due colonne. Ogni scheda contiene un campo numerico **Quantita**, i pulsanti **Diminuisci** (−) e **Aumenta** (+) e un pulsante **ACQUISTA** con il prezzo totale aggiornato. Puoi scrivere direttamente qualsiasi quantità intera positiva oppure modificarla di una unità alla volta. Il valore resta selezionato dopo un acquisto e alla riapertura del menu durante la partita.

Il pulsante di acquisto si disabilita se il campo è vuoto, il numero non è valido o il credito non basta. Il prezzo unitario e le scorte sono visibili su ogni scheda; il credito e tutte le disponibilità vengono aggiornati dopo ogni acquisto.

Nel componente **ConstructionMaterialRow**, i campi **Quantity Input**, **Decrease Button**, **Increase Button**, **Purchase Button** e **Purchase Label** devono riferirsi ai controlli della stessa scheda. Il valore iniziale si modifica nel campo **Text** del `TMP_InputField` Quantita (normalmente `1`).

Per aggiungere un materiale:

1. Crea un asset `MaterialDefinition` e aggiungilo a `GameManager > ConstructionSystem > Materials`.
2. Duplica una riga di Acquisto con Ctrl+D e rinominala.
3. Assegna il nuovo asset al campo **Material** del suo componente `ConstructionMaterialRow`.
4. Controlla che i riferimenti a testi e pulsanti siano quelli della riga duplicata.

La griglia dispone la nuova scheda e l'area scorrevole si adatta alle righe aggiuntive. Il controller trova tutte le schede già presenti nella schermata e collega gli acquisti senza crearne altre. Un materiale aggiunto al solo elenco del sistema non genera automaticamente una nuova scheda.

## 4. Aggiungere una ricetta

Il banco mostra sette categorie: **Fondamenta, Pavimenti, Colonne, Muri, Supporti tetti, Tetti e Scale**. Le dieci ricette sono distribuite in queste categorie; Pavimenti comprende anche il piano superiore, Colonne comprende i capitelli e Tetti comprende entrambe le coperture. La categoria della fase corrente si apre alla prima visita e quando la fase cambia. Una scelta manuale resta selezionata tornando al banco; cambiare categoria riporta la griglia all'inizio. I pezzi futuri sono consultabili e conservano i normali vincoli sul pulsante CREA.

Ogni scheda presenta una **silhouette nera di una mesh rappresentativa della ricetta**, con margini trasparenti su un riquadro chiaro. Gli sprite sono in `Assets/UI/Construction/Silhouettes`; derivano dai placeholder della basilica, compresi scala e orientamento. Il comando **Tools > Vitruvio > Prepare construction categories and silhouettes** genera gli sprite e prepara i controlli salvati nel Canvas. Ripetere il comando riutilizza gli oggetti esistenti. Il runtime filtra le schede e aggiorna dati e selezione senza ricostruirle.

1. Crea/configura l'asset `CraftingRecipe` e aggiungilo a `ConstructionSystem > Recipes`.
2. In `Ricette > Panel > Viewport > Content`, duplica una riga esistente.
3. Assegna il nuovo asset al campo **Recipe** del componente `ConstructionRecipeRow`.
4. Modifica l'aspetto della riga e controlla i suoi riferimenti.

Assegna anche **Category** e **Silhouette**, oppure richiama lo strumento del catalogo dopo aver predisposto il placeholder della nuova fase.

Il componente aggiorna nome, costi, stato e pulsante CREA. I campi **Available Color** e **Blocked Color** regolano i colori della disponibilità. La logica continua a verificare fase, materiali, uscita libera e numero di pezzi necessari.

## 5. Note ritmiche, risultato e visuale

Il pannello **Ritmo** misura 900 × 620; la sua corsia misura 640 × 100, con la linea del colpo a X 128 e **Note Speed = 340**. L'anticipo visivo è circa 1,5 secondi. Il `CanvasGroup` salvato sulla schermata è collegato a **Rhythm Group** di `ConstructionUI` e permette la dissolvenza finale. **Tools > Vitruvio > Prepare compact rhythm panel** applica esplicitamente questo layout alla scena; il runtime aggiorna note e opacità senza ricostruire la UI.

La schermata conserva l'ultimo giudizio per 0,45 secondi e sfuma nei successivi 0,35 secondi, insieme all'audio. **Rhythm Result Hold** e **Rhythm Fade Duration**, su `GameManager > ConstructionSystem`, regolano i due tempi. All'apertura successiva opacità e pulsante Annulla vengono ripristinati. I 16 motivi e le loro combinazioni sono generati da `RhythmSequence` sulla griglia ritmica della ricetta.

La scena contiene già **64 note e 64 indicatori**, sufficienti per tutte le lunghezze di ricetta valide. Il controller attiva soltanto gli elementi necessari. Non serve duplicare oggetti durante il gioco; conserva i riferimenti degli elenchi `Notes` e `Tally` sul componente `ConstructionUI`.

**Completato** ha fondo trasparente e controlli sul bordo destro. La visuale ravvicinata usa la camera del giocatore tramite `CraftingResultView`, senza mostrare una foto. Chiudi lascia il pezzo sul tavolo; Sposta nel recinto trasferisce la stessa istanza e torna alle ricette. Entrambe le azioni ripristinano la visuale precedente. Fallimento e interruzione usano la schermata **Fallimento**.

## 6. Cosa fa ancora il codice

| Script | Responsabilità |
| --- | --- |
| [ConstructionSystem](../Assets/Scripts/Construction/ConstructionSystem.cs) | Riferimento serializzato `View` alla UI della scena; coordina acquisti, lavorazione, esiti, deposito e visuale |
| [ConstructionUI](../Assets/Scripts/Construction/ConstructionUI.cs) | Seleziona le schermate salvate, aggiorna testi, note, pulsanti e stato di pausa |
| [ConstructionMaterialRow](../Assets/Scripts/Construction/ConstructionMaterialRow.cs) | Gestisce quantità selezionata, prezzo totale e disponibilità dell'acquisto di un materiale |
| [ConstructionRecipeRow](../Assets/Scripts/Construction/ConstructionRecipeRow.cs) | Collega una riga ricetta al crafting e ne aggiorna disponibilità e dati |
| [CraftingResultView](../Assets/Scripts/Construction/CraftingResultView.cs) | Inquadra il pezzo reale e ripristina la camera |

I callback dei pulsanti vengono collegati ai componenti esistenti all'inizializzazione. Puoi modificare i componenti grafici e aggiungere tuoi eventi Inspector; il collegamento del controller non cancella gli eventi persistenti. Non eliminare un elemento referenziato senza riassegnare il relativo campo.

## 7. Preparazione e riferimenti mancanti

La scena è già pronta. Il comando **Tools > Vitruvio > Prepare editable construction UI** è uno strumento **solo Editor**, implementato in [ConstructionUIEditorSetup.cs](../Assets/Editor/ConstructionUIEditorSetup.cs): prepara i GameObject quando mancano e collega il sistema. Se la UI esiste già, conserva il layout e le modifiche manuali. Non viene eseguito all'avvio della partita.

Anche **Configure construction crafting** collega questa UI durante la configurazione delle postazioni. I tavoli e i cartelli 3D rimangono oggetti distinti dai pannelli del Canvas.

Se non compare l'interfaccia, controlla `GameManager > ConstructionSystem > View`, i cinque riferimenti alle schermate e i campi richiesti sul componente `ConstructionUI`. Il sistema segnala una configurazione incompleta anziché costruire un'interfaccia sostitutiva a runtime.
