# MultiRoblox

MultiRoblox è un account manager desktop per organizzare e aprire più profili Roblox tramite profili browser separati.

## Funzioni

- Account con avatar Roblox e stato di accesso manuale
- Ricerca, preferiti, gruppi e ordinamento
- Selezione e avvio di più account
- Temi e interfaccia multilingua
- Trasferimento cifrato con codice degli account selezionati, senza password o cookie
- Account nascondibili con il tasto destro e ripristinabili dalle Impostazioni
- Controllo aggiornamenti tramite GitHub Releases
- Installer Windows per utente, senza privilegi amministrativi

## Sicurezza e privacy

Il programma non salva password né esporta cookie. I profili browser restano localmente in `%LocalAppData%\MultiRoblox` e non vengono inclusi nel repository o nei backup organizzativi.

## Compilazione

Requisiti:

- Windows 10/11
- .NET Framework 4.x
- Inno Setup 6 per creare l'installer

Eseguire:

```powershell
.\build.ps1
```

I file compilati vengono creati in `dist/`.

## Aggiornamenti

Il programma controlla la Release più recente di `MrDylanVERO/MultiRoblox`. Pubblicare nuove versioni con tag nel formato `v3.2.8`.

## Nota

MultiRoblox non è affiliato, sponsorizzato o approvato da Roblox Corporation. Usa il programma nel rispetto delle regole di Roblox e delle singole esperienze.
