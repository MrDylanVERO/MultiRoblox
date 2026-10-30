// Multi Roblox 1.1 - programma principale
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Microsoft.Win32;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Lang.Load();
        Themes.Load();

        bool first;
        using (Mutex own = new Mutex(true, "MultiRoblox_App_Instance", out first))
        {
            if (!first) return; // il programma e' gia' aperto
            Application.Run(new MainForm());
        }
    }
}

// ---------- Account ----------
class Account
{
    public string Id;
    public string Name;
    public long RobloxUserId;
    public bool LoggedIn;
    public bool Favorite;
    public bool Hidden;
    public string Group = "";
    public bool Selected;

    public override string ToString() { return Name ?? ""; }

    public string ProfileDir
    {
        get { return Path.Combine(Path.Combine(Paths.DataDir, "Profili"), Id); }
    }

    public string AvatarPath
    {
        get { return Path.Combine(Path.Combine(Paths.DataDir, "Avatar"), RobloxUserId + ".png"); }
    }
}

class RobloxUserItem { public long id { get; set; } public string name { get; set; } }
class RobloxUserResponse { public RobloxUserItem[] data { get; set; } }
class RobloxThumbItem { public string state { get; set; } public string imageUrl { get; set; } }
class RobloxThumbResponse { public RobloxThumbItem[] data { get; set; } }
class GitHubRelease
{
    public string tag_name { get; set; }
    public string name { get; set; }
    public string body { get; set; }
    public string html_url { get; set; }
}

static class UpdateChecker
{
    public const string CurrentVersion = "3.2.9";
    const string LatestReleaseApi = "https://api.github.com/repos/MrDylanVERO/MultiRoblox/releases/latest";

    public static void Check(Form owner, bool silent)
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string jsonText;
                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.UserAgent] = "MultiRoblox/" + CurrentVersion;
                    wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    jsonText = wc.DownloadString(LatestReleaseApi);
                }
                GitHubRelease release = new JavaScriptSerializer().Deserialize<GitHubRelease>(jsonText);
                string tag = release == null ? "" : (release.tag_name ?? "").TrimStart('v', 'V');
                Version latest, current;
                if (!Version.TryParse(tag, out latest) || !Version.TryParse(CurrentVersion, out current)) return;
                owner.BeginInvoke((MethodInvoker)delegate
                {
                    if (latest > current)
                    {
                        string notes = string.IsNullOrWhiteSpace(release.body) ? release.name : release.body;
                        if (notes != null && notes.Length > 900) notes = notes.Substring(0, 900) + "…";
                        string message = string.Format(Lang.Get("updateAvailable"), release.tag_name, notes ?? "");
                        if (MessageBox.Show(owner, message, "Multi Roblox", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            Process.Start(release.html_url);
                    }
                    else if (!silent)
                        MessageBox.Show(owner, Lang.Get("noUpdates"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch
            {
                if (!silent && !owner.IsDisposed)
                    owner.BeginInvoke((MethodInvoker)delegate { MessageBox.Show(owner, Lang.Get("fail") + "GitHub", "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Warning); });
            }
        });
    }
}
class BackupAccount
{
    public string Id;
    public string Name;
    public long RobloxUserId;
    public bool LoggedIn;
    public bool Favorite;
    public bool Hidden;
    public string Group;
}
class BackupModel
{
    public int Version = 1;
    public string Link;
    public string Theme;
    public string Language;
    public List<BackupAccount> Accounts = new List<BackupAccount>();
}

static class TransferCrypto
{
    public static string NewCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        byte[] random = new byte[8];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) rng.GetBytes(random);
        StringBuilder code = new StringBuilder(9);
        for (int i = 0; i < random.Length; i++)
        {
            if (i == 4) code.Append('-');
            code.Append(chars[random[i] % chars.Length]);
        }
        return code.ToString();
    }

    public static byte[] Encrypt(string text, string code)
    {
        code = CleanCode(code);
        byte[] salt = new byte[16], iv = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); rng.GetBytes(iv); }
        byte[] keys;
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(code, salt, 100000)) keys = kdf.GetBytes(64);
        byte[] cipher;
        using (Aes aes = Aes.Create())
        {
            aes.Key = Sub(keys, 0, 32); aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            using (ICryptoTransform enc = aes.CreateEncryptor()) cipher = enc.TransformFinalBlock(Encoding.UTF8.GetBytes(text), 0, Encoding.UTF8.GetByteCount(text));
        }
        byte[] body = Join(Encoding.ASCII.GetBytes("MRBT1"), salt, iv, cipher);
        byte[] mac;
        using (HMACSHA256 h = new HMACSHA256(Sub(keys, 32, 32))) mac = h.ComputeHash(body);
        return Join(body, mac);
    }

    public static string Decrypt(byte[] file, string code)
    {
        string exact = (code ?? "").Trim().ToUpperInvariant();
        string clean = CleanCode(exact);
        string formatted = clean.Length == 8 ? clean.Substring(0, 4) + "-" + clean.Substring(4) : clean;
        Exception last = null;
        foreach (string candidate in new[] { exact, clean, formatted })
        {
            if (string.IsNullOrEmpty(candidate)) continue;
            try { return DecryptWithCode(file, candidate); }
            catch (Exception ex) { last = ex; }
        }
        throw last ?? new CryptographicException();
    }

    static string DecryptWithCode(byte[] file, string code)
    {
        if (file == null || file.Length < 69 || Encoding.ASCII.GetString(file, 0, 5) != "MRBT1") throw new InvalidDataException();
        byte[] salt = Sub(file, 5, 16), iv = Sub(file, 21, 16);
        byte[] cipher = Sub(file, 37, file.Length - 69), savedMac = Sub(file, file.Length - 32, 32), keys;
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(code, salt, 100000)) keys = kdf.GetBytes(64);
        byte[] body = Sub(file, 0, file.Length - 32), actualMac;
        using (HMACSHA256 h = new HMACSHA256(Sub(keys, 32, 32))) actualMac = h.ComputeHash(body);
        int difference = 0; for (int i = 0; i < 32; i++) difference |= actualMac[i] ^ savedMac[i];
        if (difference != 0) throw new CryptographicException();
        using (Aes aes = Aes.Create())
        {
            aes.Key = Sub(keys, 0, 32); aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            using (ICryptoTransform dec = aes.CreateDecryptor()) return Encoding.UTF8.GetString(dec.TransformFinalBlock(cipher, 0, cipher.Length));
        }
    }

    static byte[] Sub(byte[] value, int start, int count) { byte[] r = new byte[count]; Buffer.BlockCopy(value, start, r, 0, count); return r; }
    static byte[] Join(params byte[][] parts) { int len = 0, pos = 0; foreach (byte[] p in parts) len += p.Length; byte[] r = new byte[len]; foreach (byte[] p in parts) { Buffer.BlockCopy(p, 0, r, pos, p.Length); pos += p.Length; } return r; }
    static string CleanCode(string code) { StringBuilder b = new StringBuilder(); foreach (char c in (code ?? "").ToUpperInvariant()) if (char.IsLetterOrDigit(c)) b.Append(c); return b.ToString(); }
}

static class RobloxProfile
{
    public static Account Find(string username)
    {
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            JavaScriptSerializer json = new JavaScriptSerializer();
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                string body = json.Serialize(new { usernames = new[] { username }, excludeBannedUsers = true });
                RobloxUserResponse users = json.Deserialize<RobloxUserResponse>(wc.UploadString("https://users.roblox.com/v1/usernames/users", "POST", body));
                if (users == null || users.data == null || users.data.Length == 0) return null;

                RobloxUserItem user = users.data[0];
                Account account = new Account { Id = Guid.NewGuid().ToString("N").Substring(0, 10), Name = user.name, RobloxUserId = user.id };
                DownloadAvatar(account, wc, json);
                return account;
            }
        }
        catch { return null; }
    }

    static void DownloadAvatar(Account account, WebClient wc, JavaScriptSerializer json)
    {
        string endpoint = "https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds=" + account.RobloxUserId + "&size=100x100&format=Png&isCircular=true";
        RobloxThumbResponse thumbs = json.Deserialize<RobloxThumbResponse>(wc.DownloadString(endpoint));
        if (thumbs == null || thumbs.data == null || thumbs.data.Length == 0 || string.IsNullOrEmpty(thumbs.data[0].imageUrl)) return;
        Uri imageUri;
        if (!Uri.TryCreate(thumbs.data[0].imageUrl, UriKind.Absolute, out imageUri) || imageUri.Scheme != Uri.UriSchemeHttps || !imageUri.Host.EndsWith(".rbxcdn.com")) return;
        Directory.CreateDirectory(Path.GetDirectoryName(account.AvatarPath));
        wc.DownloadFile(imageUri, account.AvatarPath);
    }
}

static class Accounts
{
    static string FilePath { get { return Path.Combine(Paths.DataDir, "account.txt"); } }
    static string LinkPath { get { return Path.Combine(Paths.DataDir, "link.txt"); } }

    public static List<Account> Load()
    {
        List<Account> list = new List<Account>();
        try
        {
            if (!File.Exists(FilePath)) return list;
            foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                int i = line.IndexOf('|');
                if (i <= 0) continue;
                Account a = new Account();
                a.Id = line.Substring(0, i);
                string[] fields = line.Split('|');
                a.Name = fields.Length > 1 ? fields[1] : line.Substring(i + 1);
                long uid;
                if (fields.Length > 2 && long.TryParse(fields[2], out uid)) a.RobloxUserId = uid;
                bool logged;
                if (fields.Length > 3 && bool.TryParse(fields[3], out logged)) a.LoggedIn = logged;
                bool favorite;
                if (fields.Length > 4 && bool.TryParse(fields[4], out favorite)) a.Favorite = favorite;
                if (fields.Length > 5) a.Group = fields[5];
                bool hidden;
                if (fields.Length > 6 && bool.TryParse(fields[6], out hidden)) a.Hidden = hidden;
                list.Add(a);
            }
        }
        catch { }
        return list;
    }

    public static void Save(List<Account> list)
    {
        try
        {
            Directory.CreateDirectory(Paths.DataDir);
            List<string> lines = new List<string>();
            foreach (Account a in list) lines.Add(a.Id + "|" + a.Name + "|" + a.RobloxUserId + "|" + a.LoggedIn + "|" + a.Favorite + "|" + (a.Group ?? "").Replace("|", " ") + "|" + a.Hidden);
            File.WriteAllLines(FilePath, lines.ToArray(), Encoding.UTF8);
        }
        catch { }
    }

    public static string LoadLink()
    {
        try { if (File.Exists(LinkPath)) return File.ReadAllText(LinkPath).Trim(); } catch { }
        return "";
    }

    public static void SaveLink(string link)
    {
        try
        {
            Directory.CreateDirectory(Paths.DataDir);
            File.WriteAllText(LinkPath, link);
        }
        catch { }
    }

    // Accetta solo link https di roblox.com, altrimenti apre la home
    public static string SafeUrl(string link)
    {
        Uri u;
        if (!string.IsNullOrEmpty(link) && Uri.TryCreate(link.Trim(), UriKind.Absolute, out u)
            && u.Scheme == Uri.UriSchemeHttps
            && (u.Host == "roblox.com" || u.Host.EndsWith(".roblox.com")))
        {
            return u.AbsoluteUri;
        }
        return "https://www.roblox.com/home";
    }

    public static string FindBrowser()
    {
        string la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string[] roots = {
            Environment.GetEnvironmentVariable("ProgramW6432"),
            Environment.GetEnvironmentVariable("ProgramFiles"),
            Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
            la
        };
        string[] rel = {
            @"Google\Chrome\Application\chrome.exe",
            @"Microsoft\Edge\Application\msedge.exe"
        };
        foreach (string r in rel)
        {
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                string p = Path.Combine(root, r);
                if (File.Exists(p)) return p;
            }
        }
        return null;
    }

    public static bool Open(Account a, string link)
    {
        string browser = FindBrowser();
        if (browser == null) return false;
        Directory.CreateDirectory(a.ProfileDir);
        string args = "--user-data-dir=\"" + a.ProfileDir + "\" --no-first-run --no-default-browser-check \"" + SafeUrl(link) + "\"";
        Process.Start(new ProcessStartInfo(browser, args));
        return true;
    }
}

// ---------- Finestrella per scrivere il nome ----------
static class InputBox
{
    public static string Ask(IWin32Window owner, string prompt)
    {
        using (Form f = new Form())
        {
            Ui.SetupForm(f, 380, 150);
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowInTaskbar = false;
            f.MinimizeBox = false;

            Ui.Lbl(f, prompt, 10, false, Ui.White, 20, 15, 340, 25, ContentAlignment.MiddleLeft);

            TextBox tb = new TextBox();
            tb.MaxLength = 30;
            tb.BackColor = Ui.Card;
            tb.ForeColor = Ui.White;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = Ui.F(11, false);
            tb.SetBounds(20, 45, 340, 30);
            f.Controls.Add(tb);

            Button ok = Ui.Btn(f, Ui.Green, 10, 200, 95, 160, 38);
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            Button no = Ui.Btn(f, Ui.Card, 10, 20, 95, 160, 38);
            no.Text = Lang.Get("cancel");
            no.DialogResult = DialogResult.Cancel;
            f.AcceptButton = ok;
            f.CancelButton = no;

            if (f.ShowDialog(owner) != DialogResult.OK) return null;
            string name = tb.Text.Replace("|", " ").Trim();
            return name.Length == 0 ? null : name;
        }
    }
}

// ---------- Pallino di stato ----------
class StatusPanel : Panel
{
    public bool Active;

    public StatusPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color c = Active ? Ui.Green : Ui.Red;
        using (SolidBrush glow = new SolidBrush(Color.FromArgb(60, c)))
            e.Graphics.FillEllipse(glow, 22, 22, 66, 66);
        using (SolidBrush dot = new SolidBrush(c))
            e.Graphics.FillEllipse(dot, 32, 32, 46, 46);
    }
}

// ---------- Immagine profilo con iniziali ----------
class AvatarPanel : Panel
{
    readonly string initials;
    readonly Color avatarColor;
    Image avatarImage;

    public AvatarPanel(Account account)
    {
        string name = string.IsNullOrWhiteSpace(account.Name) ? "?" : account.Name.Trim();
        string[] parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        initials = parts.Length > 1
            ? (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant()
            : name.Substring(0, Math.Min(2, name.Length)).ToUpperInvariant();

        int hash = account.Id == null ? 0 : account.Id.GetHashCode();
        Color[] colors = { Ui.Blue, Ui.Green, Color.FromArgb(155, 89, 182), Color.FromArgb(241, 196, 15), Color.FromArgb(230, 126, 34) };
        avatarColor = colors[(hash & 0x7fffffff) % colors.Length];
        try
        {
            if (account.RobloxUserId > 0 && File.Exists(account.AvatarPath))
            {
                byte[] bytes = File.ReadAllBytes(account.AvatarPath);
                using (MemoryStream ms = new MemoryStream(bytes))
                using (Image source = Image.FromStream(ms)) avatarImage = new Bitmap(source);
            }
        }
        catch { avatarImage = null; }
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (avatarImage != null)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(1, 1, Width - 2, Height - 2);
                GraphicsState old = e.Graphics.Save();
                e.Graphics.SetClip(path);
                e.Graphics.DrawImage(avatarImage, 1, 1, Width - 2, Height - 2);
                e.Graphics.Restore(old);
            }
            return;
        }
        using (SolidBrush b = new SolidBrush(avatarColor)) e.Graphics.FillEllipse(b, 1, 1, Width - 2, Height - 2);
        using (Font f = Ui.F(9, true))
        using (SolidBrush b = new SolidBrush(Color.White))
        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            e.Graphics.DrawString(initials, f, b, ClientRectangle, sf);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && avatarImage != null) avatarImage.Dispose();
        base.Dispose(disposing);
    }
}

// ---------- Finestra principale ----------
class MainForm : Form
{
    static Color RowColor { get { return ControlPaint.Light(Ui.Card, 0.08f); } }

    readonly List<Mutex> mutexes = new List<Mutex>();
    readonly List<Account> accounts;
    readonly ComboBox combo;
    readonly Label title, sub, stato, contatore, info, accTitle, linkLabel;
    readonly Button btn, btnAdd, btnToggleList, btnLaunchSelected;
    readonly TextBox linkBox, searchBox;
    readonly ComboBox groupFilter;
    readonly FlowLayoutPanel accList;
    readonly StatusPanel panel;
    readonly System.Windows.Forms.Timer timer;
    bool accountListVisible = false;

    string ListSettingPath { get { return Path.Combine(Paths.DataDir, "lista-account.txt"); } }

    bool Active { get { return mutexes.Count > 0; } }

    public MainForm()
    {
        Ui.SetupForm(this, 1200, 750);
        accounts = Accounts.Load();

        combo = Ui.LangCombo(this);
        combo.Left = 1030;
        title = Ui.Lbl(this, "MULTI ROBLOX", 26, true, Ui.Blue, 0, 48, 1200, 50, ContentAlignment.MiddleCenter);
        sub = Ui.Lbl(this, "", 11, false, Ui.Gray, 0, 98, 1200, 25, ContentAlignment.MiddleCenter);

        // --- Colonna sinistra ---
        panel = new StatusPanel();
        panel.BackColor = Ui.Card;
        panel.SetBounds(30, 140, 340, 110);
        Controls.Add(panel);
        stato = Ui.Lbl(panel, "", 18, true, Ui.Red, 100, 22, 235, 35, ContentAlignment.MiddleLeft);
        contatore = Ui.Lbl(panel, "", 10, false, Ui.Gray, 102, 60, 235, 25, ContentAlignment.MiddleLeft);

        btn = Ui.Btn(this, Ui.Green, 16, 30, 265, 340, 62);

        info = Ui.Lbl(this, "", 9.5f, false, Ui.Gray, 30, 342, 340, 283, ContentAlignment.TopLeft);
        info.BackColor = Ui.Card;
        info.Padding = new Padding(12);

        // --- Colonna destra: account ---
        accTitle = Ui.Lbl(this, "", 11, true, Ui.White, 400, 128, 620, 25, ContentAlignment.MiddleLeft);
        btnToggleList = Ui.Btn(this, Ui.Card, 9.5f, 10, 10, 180, 36);
        linkLabel = Ui.Lbl(this, "", 9, false, Ui.Gray, 400, 155, 770, 20, ContentAlignment.MiddleLeft);

        linkBox = new TextBox();
        linkBox.BackColor = Ui.Card;
        linkBox.ForeColor = Ui.White;
        linkBox.BorderStyle = BorderStyle.FixedSingle;
        linkBox.Font = Ui.F(10, false);
        linkBox.SetBounds(400, 177, 770, 26);
        linkBox.Text = Accounts.LoadLink();
        Controls.Add(linkBox);

        searchBox = new TextBox();
        searchBox.BackColor = Ui.Card;
        searchBox.ForeColor = Ui.White;
        searchBox.BorderStyle = BorderStyle.FixedSingle;
        searchBox.Font = Ui.F(10, false);
        searchBox.SetBounds(400, 212, 560, 28);
        Controls.Add(searchBox);

        groupFilter = new ComboBox();
        groupFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        groupFilter.BackColor = Ui.Card;
        groupFilter.ForeColor = Ui.White;
        groupFilter.Font = Ui.F(9, false);
        groupFilter.SetBounds(970, 212, 200, 28);
        Controls.Add(groupFilter);

        accList = new FlowLayoutPanel();
        accList.FlowDirection = FlowDirection.TopDown;
        accList.WrapContents = false;
        accList.AutoScroll = true;
        accList.BackColor = Ui.Card;
        accList.Padding = new Padding(8);
        accList.SetBounds(400, 250, 770, 370);
        Controls.Add(accList);

        btnLaunchSelected = Ui.Btn(this, Ui.Green, 10, 400, 630, 770, 38);
        btnAdd = Ui.Btn(this, Ui.Blue, 11, 400, 678, 770, 48);

        try
        {
            if (File.Exists(ListSettingPath)) accountListVisible = File.ReadAllText(ListSettingPath).Trim() != "0";
        }
        catch { }

        // --- Eventi ---
        combo.SelectedIndexChanged += (s, e) =>
        {
            Lang.Current = Lang.Codes[combo.SelectedIndex];
            Lang.Save();
            UpdateTexts();
            BuildList();
        };
        btn.Click += (s, e) => { if (Active) Deactivate(); else Activate(false); };
        btnAdd.Click += (s, e) => AddAccount();
        btnLaunchSelected.Click += (s, e) => LaunchSelected();
        btnToggleList.Click += (s, e) => ShowSettings();
        linkBox.Leave += (s, e) => Accounts.SaveLink(linkBox.Text.Trim());
        searchBox.TextChanged += (s, e) => BuildList();
        searchBox.Enter += (s, e) => { if (searchBox.Text == Lang.Get("search")) { searchBox.Text = ""; searchBox.ForeColor = Ui.White; } };
        searchBox.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(searchBox.Text)) { searchBox.Text = Lang.Get("search"); searchBox.ForeColor = Ui.Gray; } };
        groupFilter.SelectedIndexChanged += (s, e) => BuildList();

        timer = new System.Windows.Forms.Timer();
        timer.Interval = 1000;
        timer.Tick += (s, e) => { contatore.Text = Lang.Get("count") + CountRoblox(); };
        timer.Start();

        Shown += (s, e) => { Activate(true); UpdateChecker.Check(this, true); };
        FormClosing += (s, e) =>
        {
            timer.Stop();
            Accounts.SaveLink(linkBox.Text.Trim());
            Deactivate();
        };

        UpdateTexts();
        RefreshGroupFilter();
        ApplyAccountListVisibility();
        BuildList();
    }

    static int CountRoblox()
    {
        Process[] list = Process.GetProcessesByName("RobloxPlayerBeta");
        int n = list.Length;
        foreach (Process p in list) p.Dispose();
        return n;
    }

    void Activate(bool silent)
    {
        if (Active) return;
        if (CountRoblox() > 0)
        {
            if (!silent)
                MessageBox.Show(this, Lang.Get("warn"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        foreach (string name in new[] { "ROBLOX_singletonMutex", "ROBLOX_singletonEvent" })
        {
            bool created;
            mutexes.Add(new Mutex(true, name, out created));
        }
        UpdateTexts();
    }

    void Deactivate()
    {
        foreach (Mutex m in mutexes)
        {
            try { m.ReleaseMutex(); } catch { }
            m.Dispose();
        }
        mutexes.Clear();
        UpdateTexts();
    }

    void UpdateTexts()
    {
        sub.Text = Lang.Get("sub");
        info.Text = Lang.Get("info");
        accTitle.Text = Lang.Get("accTitle");
        linkLabel.Text = Lang.Get("link");
        btnAdd.Text = Lang.Get("add");
        btnLaunchSelected.Text = Lang.Get("launchSelected");
        if (string.IsNullOrEmpty(searchBox.Text)) searchBox.Text = Lang.Get("search");
        btnToggleList.Text = Lang.Get("settings");
        contatore.Text = Lang.Get("count") + CountRoblox();
        stato.Text = Active ? Lang.Get("on") : Lang.Get("off");
        stato.ForeColor = Active ? Ui.Green : Ui.Red;
        btn.Text = Active ? Lang.Get("bOff") : Lang.Get("bOn");
        Ui.Paint(btn, Active ? Ui.Red : Ui.Green);
        panel.Active = Active;
        panel.Invalidate();
    }

    void ShowSettings()
    {
        using (Form f = new Form())
        {
            Ui.SetupForm(f, 430, 465);
            f.Text = Lang.Get("settings").Replace("⚙ ", "");
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowInTaskbar = false;
            f.MinimizeBox = false;

            CheckBox hide = new CheckBox();
            hide.Text = Lang.Get("hideAccounts");
            hide.Checked = !accountListVisible;
            hide.ForeColor = Ui.White;
            hide.BackColor = Ui.Bg;
            hide.Font = Ui.F(11, false);
            hide.SetBounds(30, 35, 370, 34);
            f.Controls.Add(hide);

            Ui.Lbl(f, Lang.Get("theme"), 10, true, Ui.Gray, 30, 82, 370, 24, ContentAlignment.MiddleLeft);
            ComboBox theme = new ComboBox();
            theme.DropDownStyle = ComboBoxStyle.DropDownList;
            theme.BackColor = Ui.Card;
            theme.ForeColor = Ui.White;
            theme.Font = Ui.F(10.5f, false);
            theme.Items.AddRange(Themes.Names);
            theme.SelectedItem = Themes.Current;
            theme.SetBounds(30, 110, 370, 30);
            f.Controls.Add(theme);

            Button backup = Ui.Btn(f, Ui.Card, 10, 30, 175, 175, 42);
            backup.Text = Lang.Get("backup");
            backup.Click += (s, e) => BackupData(f);

            Button restore = Ui.Btn(f, Ui.Card, 10, 225, 175, 175, 42);
            restore.Text = Lang.Get("restore");
            restore.Click += (s, e) => RestoreData(f);

            Button updates = Ui.Btn(f, Ui.Card, 10, 30, 230, 370, 42);
            updates.Text = Lang.Get("updates");
            updates.Click += (s, e) => UpdateChecker.Check(this, false);

            Button hiddenAccounts = Ui.Btn(f, Ui.Card, 10, 30, 282, 370, 42);
            hiddenAccounts.Text = Lang.Get("hiddenAccounts");
            hiddenAccounts.Click += (s, e) => ShowHiddenAccounts(f);

            Button save = Ui.Btn(f, Ui.Blue, 11, 105, 372, 220, 48);
            save.Text = Lang.Get("save");
            save.DialogResult = DialogResult.OK;
            f.AcceptButton = save;

            if (f.ShowDialog(this) != DialogResult.OK) return;
            string oldTheme = Themes.Current;
            string selectedTheme = theme.SelectedItem == null ? oldTheme : theme.SelectedItem.ToString();
            accountListVisible = !hide.Checked;
            try
            {
                Directory.CreateDirectory(Paths.DataDir);
                File.WriteAllText(ListSettingPath, accountListVisible ? "1" : "0");
            }
            catch { }
            Themes.Save(selectedTheme);
            if (oldTheme != Themes.Current)
            {
                Application.Restart();
                Close();
                return;
            }
        }
        ApplyAccountListVisibility();
        UpdateTexts();
    }

    void ShowHiddenAccounts(IWin32Window owner)
    {
        using (Form f = new Form())
        {
            Ui.SetupForm(f, 430, 410);
            f.Text = Lang.Get("hiddenAccounts");
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowInTaskbar = false;
            ListBox list = new ListBox();
            list.BackColor = Ui.Card; list.ForeColor = Ui.White; list.Font = Ui.F(11, false);
            list.SetBounds(30, 30, 370, 270);
            foreach (Account a in accounts) if (a.Hidden) list.Items.Add(a);
            list.DisplayMember = "Name";
            f.Controls.Add(list);
            Button show = Ui.Btn(f, Ui.Green, 10, 30, 320, 370, 46);
            show.Text = Lang.Get("showAccount");
            show.Click += (s, e) =>
            {
                Account a = list.SelectedItem as Account;
                if (a == null) return;
                a.Hidden = false; Accounts.Save(accounts); list.Items.Remove(a); BuildList();
            };
            f.ShowDialog(owner);
        }
    }

    void ShowTransfer(IWin32Window owner)
    {
        using (Form f = new Form())
        {
            string transferCode = null;
            Ui.SetupForm(f, 470, 440);
            f.Text = Lang.Get("transferPc") + " - v" + UpdateChecker.CurrentVersion;
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowInTaskbar = false;
            f.MinimizeBox = false;
            Ui.Lbl(f, Lang.Get("transferInfo"), 10, false, Ui.White, 30, 25, 410, 105, ContentAlignment.MiddleLeft);
            Button export = Ui.Btn(f, Ui.Blue, 10, 30, 145, 195, 48);
            export.Text = Lang.Get("exportTransfer");
            Button import = Ui.Btn(f, Ui.Green, 10, 245, 145, 195, 48);
            import.Text = Lang.Get("importTransfer");
            import.Click += (s, e) => { if (ImportTransfer(f)) f.Close(); };

            Ui.Lbl(f, Lang.Get("transferCodeHere"), 9, true, Ui.Gray, 30, 215, 410, 22, ContentAlignment.MiddleCenter);
            Label codeBox = new Label();
            codeBox.BackColor = Ui.Card;
            codeBox.ForeColor = Ui.Green;
            codeBox.BorderStyle = BorderStyle.FixedSingle;
            codeBox.Font = Ui.F(20, true);
            codeBox.TextAlign = ContentAlignment.MiddleCenter;
            codeBox.Text = Lang.Get("pressExportForCode");
            codeBox.SetBounds(80, 242, 310, 50);
            f.Controls.Add(codeBox);

            Button copy = Ui.Btn(f, Ui.Card, 10, 135, 300, 200, 42);
            copy.Text = Lang.Get("copyCode");
            copy.Enabled = false;
            copy.Click += (s, e) => { if (!string.IsNullOrEmpty(transferCode)) Clipboard.SetText(transferCode); };

            export.Click += (s, e) =>
            {
                string newCode = TransferCrypto.NewCode();
                if (ExportTransfer(f, newCode))
                {
                    transferCode = newCode;
                    codeBox.Text = transferCode;
                    codeBox.Refresh();
                    copy.Enabled = true;
                    MessageBox.Show(f, Lang.Get("transferFileCreated"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Button close = Ui.Btn(f, Ui.Card, 10, 135, 360, 200, 42);
            close.Text = Lang.Get("cancel");
            close.Click += (s, e) => f.Close();
            f.ShowDialog(owner);
        }
    }

    BackupModel CreateBackupModel()
    {
        return CreateBackupModel(accounts);
    }

    BackupModel CreateBackupModel(IEnumerable<Account> accountsToExport)
    {
        BackupModel data = new BackupModel { Link = linkBox.Text.Trim(), Theme = Themes.Current, Language = Lang.Current };
        foreach (Account a in accountsToExport)
            data.Accounts.Add(new BackupAccount { Id = a.Id, Name = a.Name, RobloxUserId = a.RobloxUserId, LoggedIn = a.LoggedIn, Favorite = a.Favorite, Hidden = a.Hidden, Group = a.Group });
        return data;
    }

    bool ExportTransfer(IWin32Window owner, string code)
    {
        List<Account> selected = accounts.FindAll(a => a.Selected);
        if (selected.Count == 0)
        {
            MessageBox.Show(owner, Lang.Get("selectTransferAccounts"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Filter = "MultiRoblox Transfer (*.mrbtransfer)|*.mrbtransfer";
            dialog.FileName = "MultiRoblox-trasferimento-" + DateTime.Now.ToString("yyyy-MM-dd") + ".mrbtransfer";
            if (dialog.ShowDialog(owner) != DialogResult.OK) return false;
            string json = new JavaScriptSerializer().Serialize(CreateBackupModel(selected));
            File.WriteAllBytes(dialog.FileName, TransferCrypto.Encrypt(json, code));
            return true;
        }
    }

    bool ImportTransfer(IWin32Window owner)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "MultiRoblox Transfer (*.mrbtransfer)|*.mrbtransfer";
            if (dialog.ShowDialog(owner) != DialogResult.OK) return false;
            string code = InputBox.Ask(owner, Lang.Get("transferEnterOldCode"));
            if (code == null) return false;
            try
            {
                string json = TransferCrypto.Decrypt(File.ReadAllBytes(dialog.FileName), code.Trim().ToUpperInvariant());
                BackupModel data = new JavaScriptSerializer().Deserialize<BackupModel>(json);
                if (data == null || data.Accounts == null) throw new InvalidDataException();
                foreach (BackupAccount b in data.Accounts)
                {
                    Account existing = accounts.Find(a => a.RobloxUserId != 0 && a.RobloxUserId == b.RobloxUserId);
                    if (existing == null)
                        accounts.Add(new Account { Id = b.Id, Name = b.Name, RobloxUserId = b.RobloxUserId, LoggedIn = false, Favorite = b.Favorite, Hidden = b.Hidden, Group = b.Group ?? "" });
                    else
                    {
                        existing.Name = b.Name; existing.Favorite = b.Favorite; existing.Hidden = b.Hidden; existing.Group = b.Group ?? "";
                    }
                }
                Accounts.Save(accounts);
                if (data.Link != null) { linkBox.Text = data.Link; Accounts.SaveLink(data.Link); }
                if (!string.IsNullOrEmpty(data.Language) && Array.IndexOf(Lang.Codes, data.Language) >= 0) { Lang.Current = data.Language; Lang.Save(); }
                if (!string.IsNullOrEmpty(data.Theme) && Array.IndexOf(Themes.Names, data.Theme) >= 0) Themes.Save(data.Theme);
                RefreshGroupFilter(); BuildList();
                MessageBox.Show(this, Lang.Get("transferImported"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Information);
                StartReloginWizard();
                return true;
            }
            catch
            {
                MessageBox.Show(this, Lang.Get("transferWrongCode"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }

    void BackupData(IWin32Window owner)
    {
        using (SaveFileDialog dialog = new SaveFileDialog())
        {
            dialog.Filter = "MultiRoblox Backup (*.mrbbackup)|*.mrbbackup";
            dialog.FileName = "MultiRoblox-backup-" + DateTime.Now.ToString("yyyy-MM-dd") + ".mrbbackup";
            if (dialog.ShowDialog(owner) != DialogResult.OK) return;
            BackupModel data = CreateBackupModel();
            File.WriteAllText(dialog.FileName, new JavaScriptSerializer().Serialize(data), Encoding.UTF8);
        }
    }

    void RestoreData(IWin32Window owner)
    {
        RestoreData(owner, false);
    }

    bool RestoreData(IWin32Window owner, bool transferredFromAnotherPc)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "MultiRoblox Backup (*.mrbbackup)|*.mrbbackup";
            if (dialog.ShowDialog(owner) != DialogResult.OK) return false;
            try
            {
                BackupModel data = new JavaScriptSerializer().Deserialize<BackupModel>(File.ReadAllText(dialog.FileName, Encoding.UTF8));
                if (data == null || data.Accounts == null) throw new InvalidDataException();
                accounts.Clear();
                foreach (BackupAccount b in data.Accounts)
                    accounts.Add(new Account { Id = b.Id, Name = b.Name, RobloxUserId = b.RobloxUserId, LoggedIn = transferredFromAnotherPc ? false : b.LoggedIn, Favorite = b.Favorite, Hidden = b.Hidden, Group = b.Group ?? "" });
                Accounts.Save(accounts);
                if (data.Link != null) { linkBox.Text = data.Link; Accounts.SaveLink(data.Link); }
                if (!string.IsNullOrEmpty(data.Language) && Array.IndexOf(Lang.Codes, data.Language) >= 0) { Lang.Current = data.Language; Lang.Save(); }
                if (!string.IsNullOrEmpty(data.Theme) && Array.IndexOf(Themes.Names, data.Theme) >= 0) Themes.Save(data.Theme);
                RefreshGroupFilter();
                BuildList();
                if (transferredFromAnotherPc)
                {
                    MessageBox.Show(this, Lang.Get("transferImported"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    StartReloginWizard();
                }
                else
                    MessageBox.Show(this, "Backup ripristinato.", "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch
            {
                MessageBox.Show(this, Lang.Get("fail") + "backup non valido", "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }

    void StartReloginWizard()
    {
        foreach (Account a in accounts)
        {
            string prompt = string.Format(Lang.Get("transferLoginPrompt"), a.Name);
            DialogResult answer = MessageBox.Show(this, prompt, "Multi Roblox", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel) break;
            if (answer == DialogResult.No) continue;
            try
            {
                if (!Accounts.Open(a, "https://www.roblox.com/login"))
                {
                    MessageBox.Show(this, Lang.Get("noBrowser"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
                if (MessageBox.Show(this, Lang.Get("loginDone"), "Multi Roblox", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    a.LoggedIn = true;
            }
            catch { }
        }
        Accounts.Save(accounts);
        BuildList();
    }

    void ApplyAccountListVisibility()
    {
        ClientSize = new Size(accountListVisible ? 1200 : 400, accountListVisible ? 750 : 650);
        title.Width = ClientSize.Width;
        sub.Width = ClientSize.Width;
        combo.Left = accountListVisible ? 1030 : 220;
        btnToggleList.Left = 10;
        btnToggleList.Top = 10;
        accTitle.Visible = accountListVisible;
        linkLabel.Visible = accountListVisible;
        linkBox.Visible = accountListVisible;
        searchBox.Visible = accountListVisible;
        groupFilter.Visible = accountListVisible;
        accList.Visible = accountListVisible;
        btnLaunchSelected.Visible = accountListVisible;
        btnAdd.Visible = accountListVisible;
    }

    void BuildList()
    {
        accList.SuspendLayout();
        Control[] old = new Control[accList.Controls.Count];
        accList.Controls.CopyTo(old, 0);
        accList.Controls.Clear();
        foreach (Control c in old) c.Dispose();

        int w = accList.ClientSize.Width - accList.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;

        string query = searchBox == null ? "" : searchBox.Text.Trim();
        if (query == Lang.Get("search")) query = "";
        string selectedGroup = groupFilter == null || groupFilter.SelectedIndex <= 0 ? "" : groupFilter.SelectedItem.ToString();
        List<Account> visibleAccounts = new List<Account>();
        foreach (Account a in accounts)
        {
            if (a.Hidden) continue;
            bool matchesText = query.Length == 0 || a.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || (a.Group ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            bool matchesGroup = selectedGroup.Length == 0 || string.Equals(a.Group ?? "", selectedGroup, StringComparison.OrdinalIgnoreCase);
            if (matchesText && matchesGroup) visibleAccounts.Add(a);
        }
        visibleAccounts.Sort((a, b) => a.Favorite == b.Favorite ? accounts.IndexOf(a).CompareTo(accounts.IndexOf(b)) : (a.Favorite ? -1 : 1));

        if (visibleAccounts.Count == 0)
        {
            Label empty = new Label();
            empty.Text = Lang.Get("empty");
            empty.ForeColor = Ui.Gray;
            empty.Font = Ui.F(10, false);
            empty.TextAlign = ContentAlignment.MiddleCenter;
            empty.Size = new Size(w, 200);
            accList.Controls.Add(empty);
        }

        foreach (Account a in visibleAccounts)
        {
            Account acc = a;
            Panel row = new Panel();
            row.BackColor = RowColor;
            row.Size = new Size(w, 46);
            row.Margin = new Padding(0, 0, 0, 6);

            CheckBox select = new CheckBox();
            select.Checked = acc.Selected;
            select.BackColor = RowColor;
            select.SetBounds(5, 15, 20, 22);
            select.CheckedChanged += (s, e) => acc.Selected = select.Checked;
            row.Controls.Add(select);

            AvatarPanel avatar = new AvatarPanel(acc);
            avatar.SetBounds(29, 6, 34, 34);
            row.Controls.Add(avatar);

            Label name = Ui.Lbl(row, acc.Name, 10.5f, true, Ui.White, 70, 0, w - 365, 27, ContentAlignment.MiddleLeft);
            name.AutoEllipsis = true;

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem hideAccount = new ToolStripMenuItem(Lang.Get("hideAccount"));
            hideAccount.Click += (s, e) => { acc.Hidden = true; acc.Selected = false; Accounts.Save(accounts); BuildList(); };
            menu.Items.Add(hideAccount);
            row.ContextMenuStrip = menu;
            name.ContextMenuStrip = menu;
            avatar.ContextMenuStrip = menu;

            Button group = Ui.Btn(row, Ui.Card, 7.5f, 70, 27, Math.Max(100, w - 365), 18);
            group.Text = string.IsNullOrEmpty(acc.Group) ? Lang.Get("noGroup") : acc.Group;
            group.Click += (s, e) => EditGroup(acc);

            Button favorite = Ui.Btn(row, Ui.Card, 12, w - 266, 8, 28, 30);
            favorite.Text = acc.Favorite ? "★" : "☆";
            favorite.ForeColor = acc.Favorite ? Color.Gold : Ui.Gray;
            favorite.Click += (s, e) => ToggleFavorite(acc);

            Button loginStatus = Ui.Btn(row, Ui.Card, 13, w - 234, 8, 28, 30);
            loginStatus.Text = "●";
            loginStatus.ForeColor = acc.LoggedIn ? Ui.Green : Color.FromArgb(245, 158, 11);
            ToolTip tip = new ToolTip();
            tip.SetToolTip(loginStatus, acc.LoggedIn ? Lang.Get("loggedIn") : Lang.Get("notLoggedIn"));
            loginStatus.Click += (s, e) => ToggleLoginStatus(acc);

            Button up = Ui.Btn(row, Ui.Card, 10, w - 202, 8, 28, 30);
            up.Text = "↑";
            up.Enabled = accounts.IndexOf(acc) > 0;
            up.Click += (s, e) => MoveAccount(acc, -1);

            Button down = Ui.Btn(row, Ui.Card, 10, w - 170, 8, 28, 30);
            down.Text = "↓";
            down.Enabled = accounts.IndexOf(acc) < accounts.Count - 1;
            down.Click += (s, e) => MoveAccount(acc, 1);

            Button play = Ui.Btn(row, Ui.Green, 9, w - 138, 8, 88, 30);
            play.Text = Lang.Get("play");
            play.Click += (s, e) => PlayAccount(acc);

            Button del = Ui.Btn(row, Ui.Red, 10, w - 42, 8, 32, 30);
            del.Text = "×";
            del.Click += (s, e) => RemoveAccount(acc);

            accList.Controls.Add(row);
        }
        accList.ResumeLayout();
    }

    void MoveAccount(Account account, int direction)
    {
        int oldIndex = accounts.IndexOf(account);
        int newIndex = oldIndex + direction;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= accounts.Count) return;
        accounts.RemoveAt(oldIndex);
        accounts.Insert(newIndex, account);
        Accounts.Save(accounts);
        BuildList();
    }

    void ToggleFavorite(Account account)
    {
        account.Favorite = !account.Favorite;
        Accounts.Save(accounts);
        BuildList();
    }

    void EditGroup(Account account)
    {
        string value = InputBox.Ask(this, Lang.Get("groupName"));
        if (value == null) return;
        account.Group = value.Trim();
        Accounts.Save(accounts);
        RefreshGroupFilter();
        BuildList();
    }

    void RefreshGroupFilter()
    {
        string previous = groupFilter.SelectedItem == null ? "" : groupFilter.SelectedItem.ToString();
        groupFilter.Items.Clear();
        groupFilter.Items.Add(Lang.Get("allGroups"));
        List<string> groups = new List<string>();
        foreach (Account a in accounts)
        {
            string g = (a.Group ?? "").Trim();
            if (g.Length > 0 && !groups.Exists(x => string.Equals(x, g, StringComparison.OrdinalIgnoreCase))) groups.Add(g);
        }
        groups.Sort(StringComparer.CurrentCultureIgnoreCase);
        foreach (string g in groups) groupFilter.Items.Add(g);
        int index = groupFilter.Items.IndexOf(previous);
        groupFilter.SelectedIndex = index >= 0 ? index : 0;
    }

    void LaunchSelected()
    {
        List<Account> selected = accounts.FindAll(a => a.Selected);
        if (selected.Count == 0)
        {
            MessageBox.Show(this, Lang.Get("selectAccount"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Activate(false);
        Accounts.SaveLink(linkBox.Text.Trim());
        foreach (Account a in selected)
        {
            try { Accounts.Open(a, linkBox.Text); } catch { }
            Application.DoEvents();
            Thread.Sleep(1200);
        }
    }

    void ToggleLoginStatus(Account account)
    {
        account.LoggedIn = !account.LoggedIn;
        Accounts.Save(accounts);
        BuildList();
    }

    void AddAccount()
    {
        string username = InputBox.Ask(this, Lang.Get("robloxUser"));
        if (username == null) return;
        Cursor = Cursors.WaitCursor;
        Account a = RobloxProfile.Find(username);
        Cursor = Cursors.Default;
        if (a == null)
        {
            MessageBox.Show(this, Lang.Get("userNotFound"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        accounts.Add(a);
        Accounts.Save(accounts);
        RefreshGroupFilter();
        BuildList();
    }

    void RemoveAccount(Account a)
    {
        string q = string.Format(Lang.Get("delQ"), a.Name);
        if (MessageBox.Show(this, q, "Multi Roblox", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        accounts.Remove(a);
        Accounts.Save(accounts);
        RefreshGroupFilter();
        try { if (Directory.Exists(a.ProfileDir)) Directory.Delete(a.ProfileDir, true); } catch { }
        BuildList();
    }

    void PlayAccount(Account a)
    {
        Activate(false);
        Accounts.SaveLink(linkBox.Text.Trim());
        try
        {
            if (!Accounts.Open(a, linkBox.Text))
                MessageBox.Show(this, Lang.Get("noBrowser"), "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (!a.LoggedIn && MessageBox.Show(this, Lang.Get("loginDone"), "Multi Roblox", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                a.LoggedIn = true;
                Accounts.Save(accounts);
                BuildList();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, Lang.Get("fail") + ex.Message, "Multi Roblox", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}


