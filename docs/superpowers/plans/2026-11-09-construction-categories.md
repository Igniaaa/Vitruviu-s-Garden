# Categorie e silhouette nel banco da lavoro

**Richiesta:** suddividere la UI di costruzione in categorie di pezzi e mostrarne le silhouette nere.

**Scelta di implementazione:** sette categorie a sinistra (Fondamenta, Pavimenti, Colonne, Muri, Supporti tetti, Tetti, Scale); griglia di schede a destra con una silhouette nera della mesh rappresentativa di ogni ricetta. Le dieci ricette e le rispettive fasi rimangono invariate. Le immagini sono derivate dai placeholder reali, con sfondo trasparente, e mostrate su un riquadro chiaro. Tutti gli oggetti della UI restano salvati nella scena e modificabili nell'Inspector.

- [x] Aggiungere test di filtro, conservazione della selezione, ritorno dal crafting, immagini nere valide e assenza di duplicati; osservare il fallimento.
- [x] Aggiungere categoria alle righe e pulsanti di categoria, collegando filtro e indicazione della selezione a ConstructionUI.
- [x] Preparare uno strumento Editor che riutilizza le schede, aggiunge la navigazione e genera le silhouette dalle mesh.
- [x] Applicare la configurazione alla scena, controllare layout e immagini, verificare l'idempotenza dello strumento.
- [x] Eseguire suite e verifica in Play Mode di categorie, pulsanti e crafting; aggiornare la guida.

**Comportamento:** la prima apertura e ogni cambio di fase mostrano la categoria della fase corrente; una scelta manuale resta selezionata al ritorno al banco finché la fase non cambia. Cambiare categoria riporta la griglia all'inizio. Anche i pezzi futuri restano consultabili, con il pulsante CREA bloccato secondo le regole esistenti.

Verifica: 67 test EditMode superati; pulsanti, raycast, filtro, blocchi delle ricette future e ciclo CREA/annulla/ritorno verificati in Play Mode. Due ulteriori test per click e cambio fase sono stati aggiunti e compilati; la successiva esecuzione completa non ha restituito risultati a causa del collegamento Unity non rispondente. Il controllo diretto dell’Editor non è stato autorizzato in tempo. Ripetizione della configurazione verificata con 353 oggetti prima e dopo e nessun nuovo percorso.

Verifica successiva durante il lavoro sul ritmo: la suite completa di 78 test è passata, compresi i test di categoria e cambio fase precedentemente rimasti senza esito.
