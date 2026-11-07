# Vitruviu's Garden — da dove cominciare

Un progetto Unity in cui compri materiali, costruisci pezzi con un minigioco ritmico e li porti negli spazi indicati per ricostruire un monumento. I dialoghi tra Vitruvio e l'Imperatore accompagnano i progressi e finiscono nel diario.

Questa documentazione è in italiano e parte dalle basi. Descrive il codice e i dati presenti nel repository al **7 novembre 2026**.

## Scegli cosa ti serve

| Voglio… | Apri |
| --- | --- |
| Capire il progetto anche se conosco poco Unity | [Guida da zero](docs/guida-codice.md) |
| Sapere a cosa serve uno script e chi lo chiama | [Mappa degli script](docs/mappa-script.md) |
| Capire da dove arrivano e come modificare le UI di acquisto e crafting | [Origine delle interfacce](docs/interfacce-crafting-acquisto.md) |
| Cambiare prezzi, pezzi, dialoghi, comandi o interfaccia | [Modifiche passo per passo](docs/modifiche-e-problemi.md) |
| Risolvere un problema o verificare una modifica | [Problemi e verifiche](docs/modifiche-e-problemi.md#problemi-frequenti) |
| Leggere la guida breve del sistema di creazione | [Acquisto e creazione dei pezzi](docs/construction-crafting.md) |
| Usare o modificare la mappa, i materiali e le macchine che producono monete | [Punti di estrazione](docs/estrazione.md) |

Se è la prima volta che apri il progetto, segui i primi tre documenti nell'ordine della tabella.

## Avvio rapido

1. Apri **Unity Hub** e aggiungi questa cartella come progetto: è quella che contiene `Assets`, `Packages` e `ProjectSettings`.
2. Usa **Unity 6000.3.18f1**, la versione indicata in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt).
3. Attendi importazione, risoluzione dei pacchetti e compilazione. Per scaricare le dipendenze mancanti serve una connessione.
4. Apri [GameScene](Assets/Scenes/GameScene.unity) dalla finestra **Project**.
5. Premi **Play**, poi clicca nella scheda **Game** per dare il controllo al gioco.
6. Cerca il mercante **MATERIALI**, premi **E**, compra materiali e usa il **BANCO DA LAVORO**.

Per la prima prova puoi comprare **40 pietre e 16 unità di calce**: costano **152 delle 200 monete iniziali** e coprono gli otto pezzi con riuscite normali. Gli errori consumano materiali: questa scorta non copre tentativi falliti aggiuntivi.

## Comandi essenziali

| Comando | Azione |
| --- | --- |
| W A S D / frecce | Movimento |
| Mouse | Visuale |
| Shift sinistro | Corsa |
| Spazio | Salto; al banco, colpo ritmico |
| E | Afferra/rilascia, interagisci con una postazione, avanza un dialogo; nel diario, pagina successiva |
| Tasto destro del mouse tenuto + movimento del mouse | Ruota il pezzo in mano |
| Tab | Apri/chiudi il diario dopo la prima voce registrata |
| Q | Pagina precedente del diario |
| Esc | Pausa, anche durante il minigioco; durante dialogo o diario la pausa non si apre |

## Due dettagli della configurazione attuale

- Esistono `MainMenu` e `GameScene`, ma [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset) contiene soltanto `GameScene`. Per usare il percorso completo menu → partita → menu in una build, aggiungi entrambe le scene al profilo di build, con `MainMenu` per prima.
- Le fasi giocabili sono **quattro fondamenta** e **quattro elementi del pavimento**. L'asset dei dialoghi contiene anche traguardi per una costruzione più lunga e una seconda soglia impostata a `1` dopo una prima soglia a `4`: vedi [configurazione e limiti](docs/guida-codice.md#configurazione-attuale-e-limiti).

## Dove si trova il lavoro del progetto

Il codice del gioco è in [Assets/Scripts](Assets/Scripts). Il controller in prima persona usato dal progetto è anche in [Assets/StarterAssets/FirstPersonController/Scripts](Assets/StarterAssets/FirstPersonController/Scripts).

Prezzi, ricette e dialoghi stanno in [Assets/Data](Assets/Data). Gli strumenti di configurazione e verifica sono in [Assets/Editor](Assets/Editor), i test della logica di creazione in [Assets/Tests/Construction](Assets/Tests/Construction).

Le interfacce di acquisto e crafting sono salvate in **Canvas > Construction UI > Screens** e modificabili nell'Editor. La [guida alle UI](docs/interfacce-crafting-acquisto.md) spiega come cambiare layout, pulsanti e righe. Le verifiche di acquisti, crafting, deposito e visuale sono riportate nella [guida al crafting](docs/construction-crafting.md).
