# Ricostruzione della basilica

[Indice](../README.md) · [Creazione dei pezzi](construction-crafting.md)

Il cantiere contiene **82 slot in dieci fasi**, sotto `Basilica - Cantiere`. Gli otto elementi iniziali restano in `Foundations` e `Pavement`; i 74 nuovi placeholder sono sotto `Architettura`, suddivisi per fase. I suffissi `_SX` e `_DX` indicano le metà specchiate del modello originale.

| Fase nel gioco | Elementi | Slot | Materiali per pezzo |
| --- | --- | --- | --- |
| 1 | Fondamenta | 4 | Pietra 4, calce 2 |
| 2 | Pavimento | 4 | Pietra 6, calce 2 |
| 3 | Colonnati e colonne | 8 | Pietra 4, calce 2 |
| 4 | Capitelli | 2 | Pietra 2, calce 2 |
| 5 | Pareti e ingressi | 18 | Pietra 6, calce 2 |
| 6 | Architravi | 16 | Pietra 4, calce 2 |
| 7 | Piano superiore | 6 | Pietra 4, calce 2 |
| 8 | Tetti inferiori | 10 | Legno 4, ferro 2 |
| 9 | Tetti superiori | 10 | Legno 6, ferro 2 |
| 10 | Scale e gradini | 4 | Pietra 2, calce 2 |

`Build Phase` nell'Inspector parte da zero. Ogni fase ha una ricetta e una riga già salvata nel banco. Il gioco sblocca soltanto gli slot della fase corrente; il sacco generico assume la mesh del pezzo alla consegna.

Il banco organizza le ricette nelle categorie Fondamenta, Pavimenti, Colonne, Muri, Supporti tetti, Tetti e Scale. Le schede mostrano silhouette nere derivate dalle mesh; la [guida alle interfacce](interfacce-crafting-acquisto.md#4-aggiungere-una-ricetta) spiega come modificarle.

Porta e rilascia il pezzo nel volume del placeholder. Il **BoxCollider segue il centro e le dimensioni della mesh**, anche quando la geometria è decentrata rispetto al pivot o si trova in quota. Per una mesh figlia vengono considerate anche rotazione e scala. L'adattamento avviene alla validazione nell'Inspector, all'avvio e mentre lo slot è selezionato nella Scene View; il menu contestuale **Fit collider to mesh** permette di richiamarlo esplicitamente. I vecchi indicatori `Punto di posa` a terra sono disattivati. Una consegna riempie un solo slot; dopo la posa, il trigger si disattiva e il pezzo entra nella gerarchia del cantiere.

Le 200 monete iniziali coprono la prova di fondamenta e pavimento. Per finanziare le altre fasi, installa le [macchine di estrazione](estrazione.md). Credito iniziale e prezzi non sono stati cambiati.

## Asset e modifica della scena

`ColonnatoInfSX1.fbx` contiene anche pareti, architravi, piano superiore, tetti e gradini. `MuroIngresso.fbx` fornisce arco e segmenti dell'ingresso; `Column.fbx` e `Capitol.fbx` forniscono colonna e capitello. Il modello già unito e i campioni esterni `ColonnaQuadrata` e `Rignhiera` sono esclusi. Il capitello passa da circa 3,7 milioni di vertici a circa 20.700 per placeholder, conservandone sagoma e decorazione principale mediante raggruppamento spaziale dei vertici.

Le mesh leggibili a runtime e le nuove ricette sono in `Assets/Data/Construction/Architecture`. La trasformazione delle parti deriva dal riferimento `BASILICA FIN 2026`, condiviso con il completamento. Per spostare il sito, muovi o ruota `Basilica - Cantiere` mantenendone la scala `(1, 1, 1)`.

Quando tutti gli 82 slot sono pieni, `BasilicaCompletionSpawner` crea una sola basilica e nasconde `Construction Root`, assegnato a `Basilica - Cantiere`. Anche i pezzi posati vengono nascosti, evitando sovrapposizioni con l'edificio finale.

**Tools > Vitruvio > Prepare basilica architecture placeholders**, implementato in [BasilicaArchitectureEditorSetup.cs](../Assets/Editor/BasilicaArchitectureEditorSetup.cs), prepara esplicitamente la scena aperta e salvata. Ripetere il comando conserva slot e ricette esistenti senza duplicarli; non viene eseguito durante il gioco.

Verifica del 7 novembre 2026: **61 test EditMode superati** e prova guidata in Play Mode di tutte le dieci ricette e delle **82 consegne mediante trigger fisici ed `EndDrag`**. Verificati progressione, consumo singolo del pezzo, una sola basilica finale e scomparsa del cantiere. Il credito aggiunto per la prova era temporaneo nello stato Play; il credito salvato nella scena resta invariato.

Aggiornamento dei collider: **64 test EditMode superati**, inclusi mesh decentrate, mesh figlie ruotate/scalate e validazione nell'Editor. Verificata in Play Mode anche una posa fisica nel collider di un tetto, alla quota effettiva della sua mesh.
