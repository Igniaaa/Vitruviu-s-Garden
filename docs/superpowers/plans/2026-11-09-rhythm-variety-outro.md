# Ritmo: varietà, anticipo e chiusura

**Obiettivo:** più pattern, minigioco compatto con meno anticipo visivo, ultimo giudizio leggibile e chiusura graduale.

**Scelte:** 16 motivi combinabili su griglia di mezzo battito con intervalli di 1–4 mezzi battiti; nessuna ripetizione immediata. Primo bersaglio allineato al metronomo dopo almeno 2,4 secondi di preparazione. Pannello 900 × 620, corsia 640 × 100, linea a X 128 e velocità 340 px/s: circa 1,5 secondi di visibilità anticipata. Chiusura con 0,45 secondi di lettura del giudizio e 0,35 secondi di dissolvenza del pannello e dell'audio.

La sequenza termina al giudizio dell'ultima nota, ma la lavorazione resta aperta durante la presentazione finale. Gli input successivi e Annulla sono ignorati in questa fase. Pausa e perdita del focus congelano anche la chiusura. Materiali ed esito vengono liquidati una sola volta, al passaggio al risultato; rimozione del banco e disattivazione conservano il rimborso tecnico.

- [x] Test inizialmente falliti per nuove pause ritmiche, anticipo, ultimo giudizio, pausa e dissolvenza.
- [x] Ampliare generazione dei pattern e aggiungere stato finale della lavorazione, fade UI/audio e ripristino per il tentativo successivo.
- [x] Applicare il layout compatto alla scena senza ricostruire la UI a runtime e aggiornare il preparatore iniziale.
- [x] Suite, prova in Play Mode e verifica visiva della chiusura; aggiornare la guida.

Verifica conclusa: 78/78 test EditMode superati. Due prove Play Mode con 8/8 colpi temporizzati dal clock DSP hanno verificato ultimo giudizio, attesa, blocco di input/annullamento, pausa e ripresa, dissolvenza audio/UI, produzione singola, consumo dimezzato al perfetto, ripristino del tentativo seguente e rimborso tecnico durante la chiusura. Verificata anche l’esclusione del tempo precedente all’ultimo input nel suo stesso frame. Pannello e ultimo giudizio ispezionati visivamente.
