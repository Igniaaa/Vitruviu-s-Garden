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
                Compra 1 / Compra 5 / Compra 10
              Calce                   ConstructionMaterialRow
              Legno                   ConstructionMaterialRow
              Ferro                   ConstructionMaterialRow
          CHIUDI [E]
      Ricette
        Panel
          Viewport
            Content
              Fondamenta              ConstructionRecipeRow
              Pavimento               ConstructionRecipeRow
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
| Distanza tra righe di acquisto o ricetta | `Viewport > Content`, componente `VerticalLayoutGroup` |
| Altezza di una riga | Il `RectTransform` della riga; il gruppo dispone le righe verticalmente |
| Area scorrevole | `Viewport`, con `ScrollRect` e `RectMask2D` |
| Corsia, linea e forma delle note | Gli oggetti salvati sotto `Ritmo > Panel > Viewport > Content > Corsia` |
| Distanza tra indicatori dei colpi | `Giudizi`, componente `GridLayoutGroup` |
| Velocità delle note e colori dei giudizi | `ConstructionUI` sul GameObject principale |
| Inquadratura del pezzo reale | `GameManager > CraftingResultView` |

Il Canvas principale mantiene la risoluzione di riferimento 1920 × 1080. Il Canvas annidato di Construction UI ha ordine 30; gli altri menu della scena restano separati.

### Testi fissi e dati dinamici

Puoi modificare direttamente titoli fissi, istruzioni ed etichette statiche. I campi che rappresentano dati di gioco vengono aggiornati dal controller: credito, scorte, prezzi, costi, disponibilità, punteggio e messaggi del risultato. Questi testi riflettono inventario, asset e stato della partita.

Le coordinate X delle note cambiano mentre scorrono e la larghezza della finestra del colpo segue la tolleranza della ricetta. Il resto del layout e dello stile rimane quello salvato nel Canvas.

## 3. Aggiungere un materiale o cambiare i tagli di acquisto

Per cambiare i tagli `+1`, `+5`, `+10`:

1. Seleziona la riga `Pietra` o `Calce` nella schermata Acquisto.
2. Nel componente **ConstructionMaterialRow**, apri l'elenco **Purchases**.
3. Modifica `Quantity` della voce desiderata. `Button` e `Label` devono puntare al pulsante e alla sua etichetta.
4. Salva e prova: testo, prezzo e acquisto useranno quella quantità.

Per aggiungere un materiale:

1. Crea un asset `MaterialDefinition` e aggiungilo a `GameManager > ConstructionSystem > Materials`.
2. Duplica una riga di Acquisto con Ctrl+D e rinominala.
3. Assegna il nuovo asset al campo **Material** del suo componente `ConstructionMaterialRow`.
4. Controlla che i riferimenti a testi e pulsanti siano quelli della riga duplicata.

Il gruppo di layout dispone la nuova riga. Il controller trova tutte le righe già presenti nella schermata e collega gli acquisti senza crearne altre. Un materiale aggiunto al solo elenco del sistema non genera automaticamente una nuova riga.

## 4. Aggiungere una ricetta

1. Crea/configura l'asset `CraftingRecipe` e aggiungilo a `ConstructionSystem > Recipes`.
2. In `Ricette > Panel > Viewport > Content`, duplica una riga esistente.
3. Assegna il nuovo asset al campo **Recipe** del componente `ConstructionRecipeRow`.
4. Modifica l'aspetto della riga e controlla i suoi riferimenti.

Il componente aggiorna nome, costi, stato e pulsante CREA. I campi **Available Color** e **Blocked Color** regolano i colori della disponibilità. La logica continua a verificare fase, materiali, uscita libera e numero di pezzi necessari.

## 5. Note ritmiche, risultato e visuale

La scena contiene già **64 note e 64 indicatori**, sufficienti per tutte le lunghezze di ricetta valide. Il controller attiva soltanto gli elementi necessari. Non serve duplicare oggetti durante il gioco; conserva i riferimenti degli elenchi `Notes` e `Tally` sul componente `ConstructionUI`.

**Completato** ha fondo trasparente e controlli sul bordo destro. La visuale ravvicinata usa la camera del giocatore tramite `CraftingResultView`, senza mostrare una foto. Chiudi lascia il pezzo sul tavolo; Sposta nel recinto trasferisce la stessa istanza e torna alle ricette. Entrambe le azioni ripristinano la visuale precedente. Fallimento e interruzione usano la schermata **Fallimento**.

## 6. Cosa fa ancora il codice

| Script | Responsabilità |
| --- | --- |
| [ConstructionSystem](../Assets/Scripts/Construction/ConstructionSystem.cs) | Riferimento serializzato `View` alla UI della scena; coordina acquisti, lavorazione, esiti, deposito e visuale |
| [ConstructionUI](../Assets/Scripts/Construction/ConstructionUI.cs) | Seleziona le schermate salvate, aggiorna testi, note, pulsanti e stato di pausa |
| [ConstructionMaterialRow](../Assets/Scripts/Construction/ConstructionMaterialRow.cs) | Collega una riga materiale e i suoi pulsanti all'acquisto |
| [ConstructionRecipeRow](../Assets/Scripts/Construction/ConstructionRecipeRow.cs) | Collega una riga ricetta al crafting e ne aggiorna disponibilità e dati |
| [CraftingResultView](../Assets/Scripts/Construction/CraftingResultView.cs) | Inquadra il pezzo reale e ripristina la camera |

I callback dei pulsanti vengono collegati ai componenti esistenti all'inizializzazione. Puoi modificare i componenti grafici e aggiungere tuoi eventi Inspector; il collegamento del controller non cancella gli eventi persistenti. Non eliminare un elemento referenziato senza riassegnare il relativo campo.

## 7. Preparazione e riferimenti mancanti

La scena è già pronta. Il comando **Tools > Vitruvio > Prepare editable construction UI** è uno strumento **solo Editor**, implementato in [ConstructionUIEditorSetup.cs](../Assets/Editor/ConstructionUIEditorSetup.cs): prepara i GameObject quando mancano e collega il sistema. Se la UI esiste già, conserva il layout e le modifiche manuali. Non viene eseguito all'avvio della partita.

Anche **Configure construction crafting** collega questa UI durante la configurazione delle postazioni. I tavoli e i cartelli 3D rimangono oggetti distinti dai pannelli del Canvas.

Se non compare l'interfaccia, controlla `GameManager > ConstructionSystem > View`, i cinque riferimenti alle schermate e i campi richiesti sul componente `ConstructionUI`. Il sistema segnala una configurazione incompleta anziché costruire un'interfaccia sostitutiva a runtime.
