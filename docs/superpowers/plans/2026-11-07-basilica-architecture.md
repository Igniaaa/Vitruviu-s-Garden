# Placeholder architettonici della basilica

**Obiettivo:** estendere il cantiere esistente con le parti effettivamente contenute negli FBX, allineate al modello finale e collegate alla progressione.

**Approccio:** leggere i nodi strutturali di ColonnatoInfSX1 e MuroIngresso, la colonna di Column e il capitello di Capitol. Trasformare le mesh dalle coordinate degli asset al riferimento finale già salvato in scena; specchiare la metà sinistra sull'asse centrale del modello originale. Escludere la basilica già unita e i due campioni esterni al suo ingombro. Salvare mesh leggibili in Assets/Data/Construction/Architecture. Alleggerire il capitello tramite raggruppamento spaziale dei vertici, senza importare milioni di vertici nella scena.

**Fasi:** conservare fondamenta e pavimento (0–1); aggiungere colonnati e colonne (2), capitelli (3), pareti e ingressi (4), architravi (5), piano superiore (6), tetti inferiori (7), tetti superiori (8), scale (9). Ogni fase avrà una ricetta e una riga nel banco, usando il sistema esistente di sacchi che assumono la mesh dello slot. Prezzi iniziali e credito rimangono quelli esistenti; le macchine di estrazione finanziano il seguito dei lavori.

**Posa:** i nuovi slot mantengono la geometria in quota, con un piccolo punto di consegna alla quota del pavimento e un trigger accessibile da terra. Un pezzo deve riempire un solo slot, il trigger deve disattivarsi dopo la posa, e i pezzi posati devono appartenere al cantiere. La comparsa finale nasconde il cantiere e le sue parti per evitare sovrapposizioni.

- [x] Aggiungere test per mesh/fasi/ricette salvate e per consumo singolo, trigger e sostituzione finale; osservarli fallire.
- [x] Correggere la gestione della posa e aggiungere il riferimento opzionale al cantiere nel completamento.
- [x] Implementare uno strumento Editor ripetibile che prepara mesh, slot, punti di consegna, ricette e UI senza cancellare gli elementi esistenti.
- [x] Eseguire lo strumento sulla scena corrente, verificare immagini e corrispondenza con il modello finale.
- [x] Eseguire la suite e una prova guidata delle fasi fino alla sostituzione finale; aggiornare la documentazione.

**Verifiche mirate:** non aggiungere due volte gli slot; non duplicare la basilica al completamento; garantire mesh leggibili a runtime e triangoli con winding corretto nei pezzi specchiati; non concludere la costruzione dopo i soli otto elementi iniziali; rendere disponibili le ricette delle nuove fasi.
