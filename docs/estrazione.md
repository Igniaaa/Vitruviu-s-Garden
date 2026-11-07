# Punti di estrazione e macchine di Vitruvio

[Indice](../README.md)

## Come provarli

1. Apri `Assets/Scenes/GameScene.unity` e premi Play.
2. Vai al mercante **MATERIALI**, premi **E** e compra **4 Legno** e **2 Ferro**. Costano 16 monete in totale.
3. Chiudi il mercante e premi **M** per aprire la mappa di estrazione.
4. Clicca uno dei tre punti e premi **INSTALLA MACCHINA**.
5. I materiali vengono consumati; il punto mostra **Macchina attiva**. Ogni 10 secondi arrivano automaticamente **5 monete** nello stesso portafoglio usato per gli acquisti.
6. Chiudi con **M** o **CHIUDI**. La macchina continua a produrre durante il gioco. Con due macchine ricevi 10 monete ogni 10 secondi, con tre 15; ciascuna ha il proprio timer.

Senza materiali il pulsante è disabilitato. Un punto può ospitare una sola macchina. La pausa ferma la produzione e nasconde temporaneamente la mappa; riprendendo ritrovi il punto selezionato. Movimento, sguardo e interazione con i pezzi sono bloccati mentre la mappa è aperta. Chiudi la mappa prima di usare il mercante o il banco.

Questa prima versione mantiene macchine, timer, materiali e monete **solo per la partita corrente**: uscire da Play o ricaricare la scena azzera lo stato. Non c'è produzione offline. La macchina è rappresentata dallo stato del punto sulla mappa; non viene ancora collocato un modello 3D nel mondo.

## Dove modificare l'interfaccia

Nella Hierarchy trovi **Canvas > Extraction Map**. È una UI salvata nella scena, non creata durante la partita.

```text
Canvas
  Extraction Map                        ExtractionMapSystem + Canvas
    Apri mappa
    Schermata mappa                     inizialmente inattiva
      Titolo / Sottotitolo / Risorse
      Chiudi mappa
      Territorio
        Fiume / Sentiero / Legenda
        Punto 1 / Punto 2 / Punto 3
      Dettagli punto
        Costo e produzione / Installa macchina / Messaggio
```

Per vederla senza Play, attiva temporaneamente **Schermata mappa** nell'Inspector. Puoi cambiare posizioni, colori, dimensioni e testi statici. I testi di risorse, punti, dettagli e messaggi vengono aggiornati dal codice in partita. Sul componente `ExtractionMapSystem`, l'elenco **Points** collega nome, pulsante e testo di ogni punto; per aggiungerne uno duplica un pulsante nel territorio e aggiungi i suoi riferimenti all'elenco.

## Dove modificare costi e produzione

Seleziona `Assets/Data/Construction/MacchinaVitruvio.asset`:

| Campo | Valore iniziale | Significato |
| --- | --- | --- |
| Display Name | Macchina estrattrice di Vitruvio | Nome mostrato nei dettagli |
| Costs | 4 Legno, 2 Ferro | Materiali consumati all'installazione |
| Coins Per Cycle | 5 | Monete accreditate a ogni ciclo |
| Cycle Seconds | 10 | Durata del ciclo in secondi di gioco, minimo 1 |

Tutti i punti usano questa definizione. Ogni materiale deve comparire una sola volta, con quantità positiva. Per cambiare il prezzo d'acquisto seleziona `Legno.asset` (2 monete) o `Ferro.asset` (4 monete) nella stessa cartella. Le relative righe modificabili sono nel pannello **Acquisto** della Construction UI.

## Responsabilità del codice

- `ExtractionMapSystem`: collega i pulsanti esistenti, controlla la mappa, installa e aggiorna i timer. Usa `Time.deltaTime`, quindi non produce con il tempo di gioco fermo.
- `ExtractionMachineDefinition`: dati modificabili di una macchina, con validazione dei costi e della produzione.
- `MaterialInventory.TrySpend`: controlla tutti i materiali prima di consumarne uno; impedisce spese parziali e costi duplicati.
- `MaterialInventory.TryCredit`: aggiunge le entrate senza permettere overflow del portafoglio.
- `ConstructionSystem.BlocksInput`: include la mappa tra le interfacce che bloccano il giocatore.
- `ExtractionMapEditorSetup`: comando **Tools > Vitruvio > Prepare editable extraction map**, usato solo nell'Editor per preparare e salvare UI, dati e righe del mercante. Ripetendolo conserva la mappa e gli asset esistenti. Non viene eseguito in una build.

## Verifiche

I test `ExtractionEconomyTests` controllano spesa atomica, costi duplicati, pagamento e limiti delle monete. Eseguibili da Test Runner insieme agli altri test dell'assembly `Basilica.Crafting.Tests`. La prova via MCP in Play Mode copre acquisto dal mercante, due installazioni, blocco del terzo punto senza materiali, accredito periodico, pausa, chiusura e click sui punti.
