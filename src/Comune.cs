// Multi Roblox - parti in comune (lingue, colori, percorsi)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

static class Paths
{
    public static string InstallDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\MultiRoblox"); }
    }
    public static string ExePath
    {
        get { return Path.Combine(InstallDir, "MultiRoblox.exe"); }
    }
    public static string DataDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiRoblox"); }
    }
    public static string MenuDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Multi Roblox"); }
    }
    public static string DesktopLink
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Multi Roblox.lnk"); }
    }
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MultiRoblox";
}

static class Lang
{
    public static readonly string[] Codes = { "it", "de", "en", "fr", "es" };
    public static readonly string[] Names = { "Italiano", "Deutsch", "English", "Français", "Español" };
    public static string Current = "en";

    // Ordine: it, de, en, fr, es
    static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
    {
        { "sub", new[] { "Gioca con più account insieme", "Spiele mit mehreren Konten gleichzeitig", "Play with multiple accounts at once", "Joue avec plusieurs comptes en même temps", "Juega con varias cuentas a la vez" } },
        { "on", new[] { "ATTIVO", "AKTIV", "ACTIVE", "ACTIF", "ACTIVO" } },
        { "off", new[] { "DISATTIVATO", "INAKTIV", "INACTIVE", "INACTIF", "INACTIVO" } },
        { "bOn", new[] { "ATTIVA", "AKTIVIEREN", "ACTIVATE", "ACTIVER", "ACTIVAR" } },
        { "bOff", new[] { "DISATTIVA", "DEAKTIVIEREN", "DEACTIVATE", "DÉSACTIVER", "DESACTIVAR" } },
        { "count", new[] { "Finestre Roblox aperte: ", "Offene Roblox-Fenster: ", "Open Roblox windows: ", "Fenêtres Roblox ouvertes : ", "Ventanas de Roblox abiertas: " } },
        { "site", new[] { "Apri il sito di Roblox", "Roblox-Website öffnen", "Open the Roblox website", "Ouvrir le site de Roblox", "Abrir la web de Roblox" } },
        { "info", new[] {
            "COME SI USA\r\n\r\n1. Premi \"+ Aggiungi account\" e scrivi un nome.\r\n2. Premi Gioca: si apre un browser solo per quell'account. La prima volta fai il login.\r\n3. Sul sito scegli un gioco e premi Gioca.\r\n4. Ripeti con gli altri account: ognuno apre la sua finestra di Roblox.",
            "SO FUNKTIONIERT'S\r\n\r\n1. Drücke \"+ Konto hinzufügen\" und gib einen Namen ein.\r\n2. Drücke Spielen: ein Browser nur für dieses Konto öffnet sich. Beim ersten Mal meldest du dich an.\r\n3. Wähle auf der Website ein Spiel und drücke Spielen.\r\n4. Wiederhole es mit den anderen Konten: jedes öffnet sein eigenes Roblox-Fenster.",
            "HOW TO USE\r\n\r\n1. Press \"+ Add account\" and type a name.\r\n2. Press Play: a browser just for that account opens. The first time, log in.\r\n3. On the website pick a game and press Play.\r\n4. Repeat with your other accounts: each one opens its own Roblox window.",
            "MODE D'EMPLOI\r\n\r\n1. Appuie sur \"+ Ajouter un compte\" et écris un nom.\r\n2. Appuie sur Jouer : un navigateur rien que pour ce compte s'ouvre. La première fois, connecte-toi.\r\n3. Sur le site, choisis un jeu et appuie sur Jouer.\r\n4. Recommence avec tes autres comptes : chacun ouvre sa fenêtre Roblox.",
            "CÓMO SE USA\r\n\r\n1. Pulsa \"+ Añadir cuenta\" y escribe un nombre.\r\n2. Pulsa Jugar: se abre un navegador solo para esa cuenta. La primera vez, inicia sesión.\r\n3. En la web elige un juego y pulsa Jugar.\r\n4. Repite con tus otras cuentas: cada una abre su propia ventana de Roblox." } },
        // Account
        { "accTitle", new[] { "I TUOI ACCOUNT", "DEINE KONTEN", "YOUR ACCOUNTS", "TES COMPTES", "TUS CUENTAS" } },
        { "hideList", new[] { "Nascondi lista", "Liste ausblenden", "Hide list", "Masquer la liste", "Ocultar lista" } },
        { "showList", new[] { "Mostra lista", "Liste anzeigen", "Show list", "Afficher la liste", "Mostrar lista" } },
        { "settings", new[] { "⚙ Impostazioni", "⚙ Einstellungen", "⚙ Settings", "⚙ Paramètres", "⚙ Ajustes" } },
        { "hideAccounts", new[] { "Nascondi la lista degli account", "Kontoliste ausblenden", "Hide the account list", "Masquer la liste des comptes", "Ocultar la lista de cuentas" } },
        { "save", new[] { "SALVA", "SPEICHERN", "SAVE", "ENREGISTRER", "GUARDAR" } },
        { "theme", new[] { "Tema grafico", "Design", "Theme", "Thème", "Tema visual" } },
        { "link", new[] { "Link del gioco (facoltativo):", "Spiel-Link (optional):", "Game link (optional):", "Lien du jeu (facultatif) :", "Enlace del juego (opcional):" } },
        { "add", new[] { "+ Aggiungi account", "+ Konto hinzufügen", "+ Add account", "+ Ajouter un compte", "+ Añadir cuenta" } },
        { "play", new[] { "Gioca", "Spielen", "Play", "Jouer", "Jugar" } },
        { "empty", new[] { "Nessun account.\r\nPremi \"+ Aggiungi account\".", "Keine Konten.\r\nDrücke \"+ Konto hinzufügen\".", "No accounts yet.\r\nPress \"+ Add account\".", "Aucun compte.\r\nAppuie sur \"+ Ajouter un compte\".", "Ninguna cuenta.\r\nPulsa \"+ Añadir cuenta\"." } },
        { "newName", new[] { "Nome dell'account (es. Principale, Alt 1):", "Name des Kontos (z. B. Haupt, Alt 1):", "Account name (e.g. Main, Alt 1):", "Nom du compte (ex. Principal, Alt 1) :", "Nombre de la cuenta (ej. Principal, Alt 1):" } },
        { "robloxUser", new[] { "Username Roblox:", "Roblox-Benutzername:", "Roblox username:", "Nom d'utilisateur Roblox :", "Usuario de Roblox:" } },
        { "userNotFound", new[] { "Username Roblox non trovato o connessione non disponibile.", "Roblox-Benutzername nicht gefunden oder keine Verbindung.", "Roblox username not found or connection unavailable.", "Nom Roblox introuvable ou connexion indisponible.", "Usuario de Roblox no encontrado o conexión no disponible." } },
        { "loginDone", new[] { "Hai completato l'accesso a Roblox con questo account?", "Hast du dich mit diesem Konto bei Roblox angemeldet?", "Did you finish signing in to Roblox with this account?", "As-tu terminé la connexion à Roblox avec ce compte ?", "¿Has terminado de iniciar sesión en Roblox con esta cuenta?" } },
        { "loggedIn", new[] { "Accesso effettuato", "Angemeldet", "Signed in", "Connecté", "Sesión iniciada" } },
        { "notLoggedIn", new[] { "Accesso necessario", "Anmeldung erforderlich", "Sign-in required", "Connexion nécessaire", "Inicio de sesión necesario" } },
        { "search", new[] { "Cerca account...", "Konten suchen...", "Search accounts...", "Rechercher des comptes...", "Buscar cuentas..." } },
        { "allGroups", new[] { "Tutti i gruppi", "Alle Gruppen", "All groups", "Tous les groupes", "Todos los grupos" } },
        { "groupName", new[] { "Nome del gruppo:", "Gruppenname:", "Group name:", "Nom du groupe :", "Nombre del grupo:" } },
        { "noGroup", new[] { "Nessun gruppo", "Keine Gruppe", "No group", "Aucun groupe", "Sin grupo" } },
        { "launchSelected", new[] { "AVVIA SELEZIONATI", "AUSGEWÄHLTE STARTEN", "LAUNCH SELECTED", "LANCER LA SÉLECTION", "ABRIR SELECCIONADOS" } },
        { "selectAccount", new[] { "Seleziona almeno un account.", "Wähle mindestens ein Konto aus.", "Select at least one account.", "Sélectionne au moins un compte.", "Selecciona al menos una cuenta." } },
        { "backup", new[] { "Backup", "Sicherung", "Backup", "Sauvegarde", "Copia de seguridad" } },
        { "restore", new[] { "Ripristina", "Wiederherstellen", "Restore", "Restaurer", "Restaurar" } },
        { "updates", new[] { "Controlla aggiornamenti", "Nach Updates suchen", "Check for updates", "Rechercher des mises à jour", "Buscar actualizaciones" } },
        { "transferPc", new[] { "Trasferisci su un altro PC", "Auf einen anderen PC übertragen", "Transfer to another PC", "Transférer vers un autre PC", "Transferir a otro PC" } },
        { "transferInfo", new[] { "Esporta account, ordine, gruppi, preferiti e impostazioni. Sul nuovo PC importa il file e accedi nuovamente a Roblox. Password, cookie e sessioni non vengono inseriti nel file.", "Exportiert Konten, Reihenfolge, Gruppen, Favoriten und Einstellungen. Auf dem neuen PC importieren und erneut bei Roblox anmelden. Passwörter, Cookies und Sitzungen sind nicht enthalten.", "Exports accounts, order, groups, favorites and settings. Import the file on the new PC and sign in to Roblox again. Passwords, cookies and sessions are not included.", "Exporte les comptes, l'ordre, les groupes, les favoris et les paramètres. Importe le fichier sur le nouveau PC et reconnecte-toi à Roblox. Les mots de passe, cookies et sessions ne sont pas inclus.", "Exporta cuentas, orden, grupos, favoritos y ajustes. Importa el archivo en el nuevo PC e inicia sesión de nuevo en Roblox. No se incluyen contraseñas, cookies ni sesiones." } },
        { "exportTransfer", new[] { "ESPORTA DAL VECCHIO PC", "VOM ALTEN PC EXPORTIEREN", "EXPORT FROM OLD PC", "EXPORTER DE L'ANCIEN PC", "EXPORTAR DEL PC ANTIGUO" } },
        { "importTransfer", new[] { "IMPORTA NEL NUOVO PC", "AUF NEUEM PC IMPORTIEREN", "IMPORT ON NEW PC", "IMPORTER SUR LE NOUVEAU PC", "IMPORTAR EN EL PC NUEVO" } },
        { "transferImported", new[] { "Trasferimento completato. Ora MultiRoblox ti guiderà nell'accesso degli account, uno alla volta.", "Übertragung abgeschlossen. MultiRoblox führt dich jetzt Konto für Konto durch die Anmeldung.", "Transfer completed. MultiRoblox will now guide you through signing in to each account.", "Transfert terminé. MultiRoblox va maintenant te guider pour reconnecter chaque compte.", "Transferencia completada. MultiRoblox te guiará para iniciar sesión en cada cuenta." } },
        { "transferLoginPrompt", new[] { "Vuoi effettuare ora il login di {0}?\r\nSì: apre il profilo. No: passa al prossimo. Annulla: termina.", "Jetzt bei {0} anmelden?\r\nJa: Profil öffnen. Nein: weiter. Abbrechen: beenden.", "Sign in to {0} now?\r\nYes: open its profile. No: go to the next. Cancel: stop.", "Se connecter à {0} maintenant ?\r\nOui : ouvrir son profil. Non : suivant. Annuler : arrêter.", "¿Iniciar sesión en {0} ahora?\r\nSí: abrir su perfil. No: siguiente. Cancelar: terminar." } },
        { "transferCodeShown", new[] { "File di trasferimento creato.\r\n\r\nIL TUO CODICE: {0}\r\n\r\nAnnotalo: servirà sul nuovo PC e non sarà possibile recuperarlo.", "Übertragungsdatei erstellt.\r\n\r\nDEIN CODE: {0}\r\n\r\nNotiere ihn: Er wird auf dem neuen PC benötigt und kann nicht wiederhergestellt werden.", "Transfer file created.\r\n\r\nYOUR CODE: {0}\r\n\r\nWrite it down: it is required on the new PC and cannot be recovered.", "Fichier de transfert créé.\r\n\r\nTON CODE : {0}\r\n\r\nNote-le : il sera nécessaire sur le nouveau PC et ne pourra pas être récupéré.", "Archivo de transferencia creado.\r\n\r\nTU CÓDIGO: {0}\r\n\r\nAnótalo: será necesario en el nuevo PC y no se podrá recuperar." } },
        { "transferEnterCode", new[] { "Inserisci il codice mostrato sul vecchio PC:", "Gib den Code vom alten PC ein:", "Enter the code shown on the old PC:", "Saisis le code affiché sur l'ancien PC :", "Introduce el código mostrado en el PC antiguo:" } },
        { "transferWrongCode", new[] { "Codice errato oppure file di trasferimento non valido.", "Falscher Code oder ungültige Übertragungsdatei.", "Wrong code or invalid transfer file.", "Code incorrect ou fichier de transfert invalide.", "Código incorrecto o archivo de transferencia no válido." } },
        { "noUpdates", new[] { "Hai già la versione più recente.", "Du hast bereits die neueste Version.", "You already have the latest version.", "Tu as déjà la dernière version.", "Ya tienes la versión más reciente." } },
        { "updateAvailable", new[] { "È disponibile la versione {0}.\r\n\r\n{1}\r\n\r\nVuoi aprire la pagina di download?", "Version {0} ist verfügbar.\r\n\r\n{1}\r\n\r\nDownload-Seite öffnen?", "Version {0} is available.\r\n\r\n{1}\r\n\r\nOpen the download page?", "La version {0} est disponible.\r\n\r\n{1}\r\n\r\nOuvrir la page de téléchargement ?", "La versión {0} está disponible.\r\n\r\n{1}\r\n\r\n¿Abrir la página de descarga?" } },
        { "cancel", new[] { "Annulla", "Abbrechen", "Cancel", "Annuler", "Cancelar" } },
        { "delQ", new[] {
            "Vuoi togliere \"{0}\" dalla lista?\r\nIl login salvato per questo account verrà cancellato.",
            "Möchtest du \"{0}\" aus der Liste entfernen?\r\nDie gespeicherte Anmeldung für dieses Konto wird gelöscht.",
            "Remove \"{0}\" from the list?\r\nThe saved login for this account will be deleted.",
            "Retirer \"{0}\" de la liste ?\r\nLa connexion enregistrée pour ce compte sera supprimée.",
            "¿Quitar \"{0}\" de la lista?\r\nSe borrará el inicio de sesión guardado de esta cuenta." } },
        { "noBrowser", new[] {
            "Non trovo Google Chrome né Microsoft Edge sul computer.",
            "Google Chrome oder Microsoft Edge wurde nicht gefunden.",
            "Google Chrome or Microsoft Edge was not found on this computer.",
            "Google Chrome ou Microsoft Edge est introuvable sur cet ordinateur.",
            "No se encuentra Google Chrome ni Microsoft Edge en el ordenador." } },
        { "warn", new[] {
            "Roblox è già aperto.\r\nChiudi tutte le finestre di Roblox e riprova.",
            "Roblox ist bereits geöffnet.\r\nSchließe alle Roblox-Fenster und versuche es erneut.",
            "Roblox is already open.\r\nClose all Roblox windows and try again.",
            "Roblox est déjà ouvert.\r\nFerme toutes les fenêtres Roblox et réessaie.",
            "Roblox ya está abierto.\r\nCierra todas las ventanas de Roblox e inténtalo de nuevo." } },
        // Installer
        { "setup", new[] { "Installazione", "Installation", "Setup", "Installation", "Instalación" } },
        { "desk", new[] { "Crea icona sul desktop", "Desktop-Symbol erstellen", "Create desktop icon", "Créer une icône sur le bureau", "Crear icono en el escritorio" } },
        { "inst", new[] { "INSTALLA", "INSTALLIEREN", "INSTALL", "INSTALLER", "INSTALAR" } },
        { "run", new[] { "AVVIA", "STARTEN", "LAUNCH", "LANCER", "ABRIR" } },
        { "ok", new[] { "Installato! Lo trovi nel menu Start.", "Installiert! Du findest es im Startmenü.", "Installed! You can find it in the Start menu.", "Installé ! Tu le trouves dans le menu Démarrer.", "¡Instalado! Lo encontrarás en el menú Inicio." } },
        { "fail", new[] { "Errore: ", "Fehler: ", "Error: ", "Erreur : ", "Error: " } },
        { "unLink", new[] { "Disinstalla Multi Roblox", "Multi Roblox deinstallieren", "Uninstall Multi Roblox", "Désinstaller Multi Roblox", "Desinstalar Multi Roblox" } },
        { "unQ", new[] { "Vuoi disinstallare Multi Roblox?", "Möchtest du Multi Roblox deinstallieren?", "Do you want to uninstall Multi Roblox?", "Veux-tu désinstaller Multi Roblox ?", "¿Quieres desinstalar Multi Roblox?" } },
        { "unDone", new[] { "Multi Roblox è stato disinstallato.", "Multi Roblox wurde deinstalliert.", "Multi Roblox has been uninstalled.", "Multi Roblox a été désinstallé.", "Multi Roblox se ha desinstalado." } },
    };

    public static int Index
    {
        get { int i = Array.IndexOf(Codes, Current); return i < 0 ? 2 : i; }
    }

    public static string Get(string key)
    {
        return S[key][Index];
    }

    static string FilePath
    {
        get { return Path.Combine(Paths.DataDir, "lingua.txt"); }
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string c = File.ReadAllText(FilePath).Trim();
                if (Array.IndexOf(Codes, c) >= 0) { Current = c; return; }
            }
        }
        catch { }
        string sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        Current = Array.IndexOf(Codes, sys) >= 0 ? sys : "en";
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Paths.DataDir);
            File.WriteAllText(FilePath, Current);
        }
        catch { }
    }
}

static class Themes
{
    public static string Current = "Neon";
    public static readonly string[] Names = { "Neon", "Notte", "Chiaro", "Tramonto", "Smeraldo", "Oceano", "Rosso", "Oro", "Viola", "Pastello" };
    static string FilePath { get { return Path.Combine(Paths.DataDir, "tema.txt"); } }

    public static void Load()
    {
        try { if (File.Exists(FilePath)) Current = File.ReadAllText(FilePath).Trim(); } catch { }
        if (Array.IndexOf(Names, Current) < 0) Current = "Neon";
        Apply();
    }

    public static void Save(string name)
    {
        Current = Array.IndexOf(Names, name) >= 0 ? name : "Neon";
        Directory.CreateDirectory(Paths.DataDir);
        File.WriteAllText(FilePath, Current);
        Apply();
    }

    public static void Apply()
    {
        if (Current == "Notte")
        {
            Ui.SetPalette(Color.FromArgb(10, 14, 24), Color.FromArgb(22, 29, 45), Color.FromArgb(34, 211, 238), Color.FromArgb(244, 63, 94), Color.FromArgb(99, 102, 241), Color.White, Color.FromArgb(160, 174, 192));
        }
        else if (Current == "Chiaro")
        {
            Ui.SetPalette(Color.FromArgb(238, 244, 252), Color.White, Color.FromArgb(22, 163, 74), Color.FromArgb(220, 38, 38), Color.FromArgb(37, 99, 235), Color.FromArgb(20, 30, 48), Color.FromArgb(80, 95, 115));
        }
        else if (Current == "Tramonto")
        {
            Ui.SetPalette(Color.FromArgb(35, 20, 38), Color.FromArgb(57, 31, 55), Color.FromArgb(251, 146, 60), Color.FromArgb(244, 63, 94), Color.FromArgb(217, 70, 239), Color.White, Color.FromArgb(220, 180, 210));
        }
        else if (Current == "Smeraldo")
        {
            Ui.SetPalette(Color.FromArgb(8, 28, 27), Color.FromArgb(18, 50, 46), Color.FromArgb(52, 211, 153), Color.FromArgb(248, 113, 113), Color.FromArgb(20, 184, 166), Color.White, Color.FromArgb(160, 200, 190));
        }
        else if (Current == "Oceano")
        {
            Ui.SetPalette(Color.FromArgb(5, 24, 46), Color.FromArgb(9, 48, 78), Color.FromArgb(45, 212, 191), Color.FromArgb(251, 113, 133), Color.FromArgb(14, 165, 233), Color.White, Color.FromArgb(148, 196, 220));
        }
        else if (Current == "Rosso")
        {
            Ui.SetPalette(Color.FromArgb(31, 12, 17), Color.FromArgb(58, 22, 29), Color.FromArgb(248, 113, 113), Color.FromArgb(220, 38, 38), Color.FromArgb(239, 68, 68), Color.White, Color.FromArgb(215, 160, 170));
        }
        else if (Current == "Oro")
        {
            Ui.SetPalette(Color.FromArgb(29, 24, 13), Color.FromArgb(55, 44, 20), Color.FromArgb(250, 204, 21), Color.FromArgb(239, 68, 68), Color.FromArgb(245, 158, 11), Color.White, Color.FromArgb(220, 196, 135));
        }
        else if (Current == "Viola")
        {
            Ui.SetPalette(Color.FromArgb(24, 14, 39), Color.FromArgb(47, 28, 71), Color.FromArgb(192, 132, 252), Color.FromArgb(244, 63, 94), Color.FromArgb(139, 92, 246), Color.White, Color.FromArgb(196, 170, 220));
        }
        else if (Current == "Pastello")
        {
            Ui.SetPalette(Color.FromArgb(248, 241, 250), Color.FromArgb(255, 252, 255), Color.FromArgb(52, 180, 145), Color.FromArgb(225, 95, 120), Color.FromArgb(126, 125, 214), Color.FromArgb(48, 42, 64), Color.FromArgb(105, 95, 125));
        }
        else
        {
            Ui.SetPalette(Color.FromArgb(22, 22, 34), Color.FromArgb(34, 34, 52), Color.FromArgb(46, 204, 113), Color.FromArgb(231, 76, 60), Color.FromArgb(0, 162, 255), Color.White, Color.FromArgb(150, 150, 175));
        }
    }
}

static class Ui
{
    public static Color Bg, Card, Green, Red, Blue, White, Gray;

    public static void SetPalette(Color bg, Color card, Color green, Color red, Color blue, Color white, Color gray)
    {
        Bg = bg; Card = card; Green = green; Red = red; Blue = blue; White = white; Gray = gray;
    }

    public static Font F(float size, bool bold)
    {
        return new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
    }

    public static void SetupForm(Form f, int w, int h)
    {
        f.Text = "Multi Roblox";
        f.ClientSize = new Size(w, h);
        f.StartPosition = FormStartPosition.CenterScreen;
        f.FormBorderStyle = FormBorderStyle.FixedSingle;
        f.MaximizeBox = false;
        f.BackColor = Bg;
        f.ForeColor = White;
        f.Font = F(10, false);
        Icon ic = AppIcon(32);
        if (ic != null) f.Icon = ic;
    }

    public static Label Lbl(Control parent, string text, float size, bool bold, Color color, int x, int y, int w, int h, ContentAlignment align)
    {
        Label l = new Label();
        l.Text = text;
        l.Font = F(size, bold);
        l.ForeColor = color;
        l.BackColor = parent.BackColor;
        l.TextAlign = align;
        l.SetBounds(x, y, w, h);
        parent.Controls.Add(l);
        return l;
    }

    public static Button Btn(Control parent, Color back, float size, int x, int y, int w, int h)
    {
        Button b = new Button();
        b.Font = F(size, true);
        b.ForeColor = White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.Cursor = Cursors.Hand;
        b.SetBounds(x, y, w, h);
        Paint(b, back);
        parent.Controls.Add(b);
        return b;
    }

    public static void Paint(Button b, Color back)
    {
        b.BackColor = back;
        b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.3f);
        b.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back, 0.1f);
    }

    public static ComboBox LangCombo(Control parent)
    {
        ComboBox c = new ComboBox();
        c.DropDownStyle = ComboBoxStyle.DropDownList;
        c.FlatStyle = FlatStyle.Flat;
        c.BackColor = Card;
        c.ForeColor = White;
        c.SetBounds(270, 12, 135, 28);
        c.Items.AddRange(Lang.Names);
        c.SelectedIndex = Lang.Index;
        parent.Controls.Add(c);
        return c;
    }

    public static Icon AppIcon(int size)
    {
        try
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("icon.ico");
            if (s == null) return null;
            using (s) { return new Icon(s, size, size); }
        }
        catch { return null; }
    }
}
