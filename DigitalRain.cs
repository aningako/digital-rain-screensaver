// Digital Rain screensaver for Windows: green falling-character "code rain".
// Build (Windows):  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+
//                   /out:DigitalRain.scr DigitalRain.cs
// Screensaver arguments: /s = run full screen, /p <hwnd> = preview in the Settings dialog, /c = settings.
// Plain C# 5 so the compiler that ships with Windows (.NET Framework 4.x) can build it.
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Shown in the file's Properties -> Details in Windows.
[assembly: AssemblyTitle("Digital Rain screensaver")]
[assembly: AssemblyDescription("Digital Rain screensaver. Created by aning with the help of Claude.")]
[assembly: AssemblyProduct("Digital Rain")]
[assembly: AssemblyCompany("aning")]
[assembly: AssemblyCopyright("MIT License. Created by aning with the help of Claude (Anthropic).")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

static class Program
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main(string[] args)
    {
        try { SetProcessDPIAware(); } catch { }          // real pixels on scaled (e.g. 125%) displays
        Application.EnableVisualStyles();

        string mode = "/c";
        string arg = null;
        if (args.Length > 0)
        {
            string a = args[0].ToLowerInvariant().Trim();
            mode = a.Length >= 2 ? a.Substring(0, 2) : a;   // "/p:1234" or "/p 1234"
            if (a.Length > 3) arg = a.Substring(3);
            else if (args.Length > 1) arg = args[1];
        }

        if (mode == "/s")
        {
            foreach (Screen s in Screen.AllScreens) new RainForm(s.Bounds).Show();
            Application.Run();
        }
        else if (mode == "/p" && arg != null)
        {
            Application.Run(new RainForm(new IntPtr(long.Parse(arg))));
        }
        else
        {
            MessageBox.Show("Digital Rain screensaver\n\nGreen falling-character code rain.\nMove the mouse or press a key to exit.\n\nNo settings to change.\n\nVersion 1.0.0. Created by aning with the help of Claude.\nhttps://github.com/aningako/digital-rain-screensaver",
                "Digital Rain", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

class RainForm : Form
{
    [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int index, int value);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }

    // Half-width katakana, digits and a few symbols.
    static readonly char[] Glyphs = BuildGlyphs();
    static char[] BuildGlyphs()
    {
        var l = new List<char>();
        for (char c = '\uFF66'; c <= '\uFF9D'; c++) l.Add(c);
        foreach (char c in "0123456789Z:.\"=*+-<>|") l.Add(c);
        return l.ToArray();
    }

    readonly bool preview;
    readonly Random rnd = new Random();
    readonly Timer timer = new Timer();
    Bitmap canvas;
    Graphics g;
    Font font;
    int cell;                 // character cell size in pixels
    Column[] cols;
    Point? mouseStart;
    readonly SolidBrush fade = new SolidBrush(Color.FromArgb(50, 0, 0, 0));
    int frame;
    readonly SolidBrush head = new SolidBrush(Color.FromArgb(235, 255, 235));
    readonly SolidBrush[] greens;

    class Column { public float Y; public float Speed; public int Trail; public char[] Chars; public int Brightness; }

    RainForm(bool isPreview)
    {
        preview = isPreview;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Black;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        greens = new SolidBrush[6];
        for (int i = 0; i < greens.Length; i++)
        {
            int v = 255 - i * 30;
            greens[i] = new SolidBrush(Color.FromArgb(i == 0 ? 120 : 0, v, i == 0 ? 120 : 30));
        }
        timer.Interval = 40;                         // 25 frames per second
        timer.Tick += delegate { Step(); Invalidate(); };
    }

    // Full screen on one monitor.
    public RainForm(Rectangle bounds) : this(false)
    {
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        TopMost = true;
        Cursor.Hide();
        Load += delegate { Setup(ClientSize); timer.Start(); };
        MouseMove += OnMouseMove;
        MouseDown += delegate { Quit(); };
        KeyDown += delegate { Quit(); };
    }

    // Small preview inside the Screen Saver Settings dialog.
    public RainForm(IntPtr previewWnd) : this(true)
    {
        Load += delegate
        {
            SetParent(Handle, previewWnd);
            SetWindowLong(Handle, -16, GetWindowLong(Handle, -16) | 0x40000000);   // GWL_STYLE |= WS_CHILD
            RECT r; GetClientRect(previewWnd, out r);
            Location = new Point(0, 0);
            Size = new Size(r.Right - r.Left, r.Bottom - r.Top);
            Setup(Size);
            timer.Start();
        };
    }

    void Setup(Size size)
    {
        cell = preview ? 8 : Math.Max(14, size.Height / 48);   // ~48 rows of characters
        font = new Font(PickFont(), cell * 0.9f, FontStyle.Bold, GraphicsUnit.Pixel);
        canvas = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height));
        g = Graphics.FromImage(canvas);
        g.Clear(Color.Black);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.CompositingQuality = CompositingQuality.HighSpeed;
        int n = size.Width / cell + 1, rows = size.Height / cell + 1;
        cols = new Column[n];
        for (int i = 0; i < n; i++) { cols[i] = NewColumn(rows); cols[i].Y = rnd.Next(-rows, rows); }
    }

    static string PickFont()
    {
        foreach (FontFamily f in FontFamily.Families)
            if (f.Name == "MS Gothic") return "MS Gothic";             // has half-width katakana
        return "Consolas";
    }

    Column NewColumn(int rows)
    {
        var c = new Column();
        c.Y = -rnd.Next(0, rows);
        c.Speed = 0.25f + (float)rnd.NextDouble() * 0.75f;            // rows per frame
        c.Trail = 8 + rnd.Next(rows / 2 + 1);
        c.Chars = new char[rows + 1];
        for (int i = 0; i < c.Chars.Length; i++) c.Chars[i] = Glyphs[rnd.Next(Glyphs.Length)];
        c.Brightness = rnd.Next(10) < 2 ? 2 : 0;                      // some columns dimmer
        return c;
    }

    void Step()
    {
        if (g == null) return;
        int rows = canvas.Height / cell + 1;
        // Fade old characters into trails. Every other frame (with a stronger fade) keeps CPU use low.
        if ((frame++ & 1) == 0) g.FillRectangle(fade, 0, 0, canvas.Width, canvas.Height);
        for (int i = 0; i < cols.Length; i++)
        {
            Column c = cols[i];
            int prevRow = (int)c.Y;
            c.Y += c.Speed;
            int row = (int)c.Y;
            // Clear cells the trail has passed: the alpha fade never reaches pure black, so without
            // this faint "ghost" characters build up all over the background.
            for (int r = prevRow - c.Trail; r < row - c.Trail; r++)
                if (r >= 0 && r < rows) g.FillRectangle(Brushes.Black, i * cell, r * cell, cell, cell);
            if (row != prevRow && row >= 0 && row < rows)
            {
                // the character just behind the head turns green, the head is drawn bright
                if (row - 1 >= 0) Draw(i, row - 1, c.Chars[row - 1], greens[c.Brightness]);
                if (rnd.Next(8) == 0) c.Chars[row] = Glyphs[rnd.Next(Glyphs.Length)];
                Draw(i, row, c.Chars[row], head);
            }
            // occasional flicker somewhere in the trail
            if (rnd.Next(30) == 0 && row > 2)
            {
                int r = Math.Max(0, row - 1 - rnd.Next(Math.Min(c.Trail, row)));
                if (r < rows) { c.Chars[r] = Glyphs[rnd.Next(Glyphs.Length)]; Draw(i, r, c.Chars[r], greens[Math.Min(5, c.Brightness + 1)]); }
            }
            if (row - c.Trail > rows) cols[i] = NewColumn(rows);
        }
    }

    void Draw(int col, int row, char ch, Brush b)
    {
        int x = col * cell, y = row * cell;
        g.FillRectangle(Brushes.Black, x, y, cell, cell);
        g.DrawString(ch.ToString(), font, b, x - cell * 0.1f, y - cell * 0.1f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (canvas != null) e.Graphics.DrawImageUnscaled(canvas, 0, 0);
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (mouseStart == null) { mouseStart = e.Location; return; }
        if (Math.Abs(e.X - mouseStart.Value.X) > 8 || Math.Abs(e.Y - mouseStart.Value.Y) > 8) Quit();
    }

    void Quit() { if (!preview) Application.Exit(); }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        if (preview) Application.Exit();
        base.OnFormClosed(e);
    }
}
