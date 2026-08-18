using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

class GameLauncherPro : Form
{
    // =========================================================================
    // CUSTOMIZATION SECTION
    // =========================================================================
    float GradientAngle = 145f;
    Color BackgroundStartColor = Color.FromArgb(70, 10, 140); // Vibrant Deep Purple
    Color BackgroundEndColor = Color.FromArgb(10, 5, 15);     // Dark almost-black red/purple

    Color HeaderTextColor = Color.FromArgb(235, 230, 255);
    Color TileTextColor = Color.FromArgb(240, 240, 240);
    Color ButtonBackColor = Color.FromArgb(35, 25, 45); // Dark translucent pill
    Color ButtonBorderColor = Color.FromArgb(90, 80, 110);

    const int TileWidth = 320;
    const int TileHeight = 180;
    const int MaxGamesPerPage = 8;
    // =========================================================================

    Label lblTitle;
    PillButton btnDisable;
    GameGridControl gameGrid;

    string gamesFolder = "Games";
    string imagesFolder = "Images";

    List<GameItem> loadedGames = new List<GameItem>();

    public GameLauncherPro()
    {
        // 1. Setup Full Screen Form
        this.Text = "Offline Games";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.DoubleBuffered = true;

        // 2. Load Assets
        LoadGamesData();

        // 3. UI Components
        lblTitle = new Label()
        {
            Text = "Offline Games",
            Font = new Font("Segoe UI", 36f, FontStyle.Regular),
            ForeColor = HeaderTextColor,
            AutoSize = true,
            Location = new Point(80, 60),
            BackColor = Color.Transparent
        };
        this.Controls.Add(lblTitle);

        btnDisable = new PillButton()
        {
            Text = "Disable GameMode",
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            BorderColor = ButtonBorderColor,
            Size = new Size(200, 48),
            Cursor = Cursors.Hand
        };
        btnDisable.Click += (s, e) =>
        {
            if (File.Exists("Disable.bat"))
                Process.Start(new ProcessStartInfo("Disable.bat") { WindowStyle = ProcessWindowStyle.Hidden });
            Application.Exit();
        };
        this.Controls.Add(btnDisable);

        // 4. Custom Game Grid Viewer (Handles Pagination & Rendering)
        gameGrid = new GameGridControl(loadedGames, TileWidth, TileHeight)
        {
            BackColor = Color.Transparent
        };
        this.Controls.Add(gameGrid);

        // 5. Layout Engine
        this.Resize += ProcessLayout;
        this.Load += (s, e) => ProcessLayout(null, null);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Draw the custom angled gradient background
        using (LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle, BackgroundStartColor, BackgroundEndColor, GradientAngle))
        {
            e.Graphics.FillRectangle(brush, this.ClientRectangle);
        }
    }

    void ProcessLayout(object sender, EventArgs e)
    {
        if (this.Width == 0 || this.Height == 0) return;

        btnDisable.Location = new Point(this.Width - btnDisable.Width - 80, 70);

        // Center the game grid logically
        int gridAreaY = 160;
        gameGrid.Location = new Point(0, gridAreaY);
        gameGrid.Size = new Size(this.Width, this.Height - gridAreaY);
    }

    void LoadGamesData()
    {
        if (!Directory.Exists(gamesFolder)) Directory.CreateDirectory(gamesFolder);
        if (!Directory.Exists(imagesFolder)) Directory.CreateDirectory(imagesFolder);

        string noImagePath = Path.Combine(imagesFolder, "no_image.png");
        if (!File.Exists(noImagePath))
        {
            using (Bitmap bmp = new Bitmap(TileWidth, TileHeight))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(40, 40, 40));
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                TextRenderer.DrawText(g, "No Tile", new Font("Segoe UI", 32f, FontStyle.Regular), new Rectangle(0, 0, TileWidth, TileHeight), Color.FromArgb(120, 255, 255, 255), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                bmp.Save(noImagePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        Image fallbackImg = Image.FromFile(noImagePath);

        foreach (var file in Directory.GetFiles(gamesFolder, "*.lnk"))
        {
            string gameName = Path.GetFileNameWithoutExtension(file);
            string imgPath = Path.Combine(imagesFolder, gameName + ".png");

            Image gameImg = fallbackImg;
            if (File.Exists(imgPath))
            {
                gameImg = Image.FromFile(imgPath);
            }

            loadedGames.Add(new GameItem { Name = gameName, ShortcutPath = file, TileImage = gameImg });
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new GameLauncherPro());
    }
}

// =========================================================================
// DATA MODELS & CUSTOM HIGH-PERFORMANCE CONTROLS
// =========================================================================

public class GameItem
{
    public string Name { get; set; }
    public string ShortcutPath { get; set; }
    public Image TileImage { get; set; }
}

public class GameGridControl : Control
{
    List<GameItem> games;
    int tileW, tileH;
    int currentPage = 0;
    int totalPages;

    // Layout
    const int cols = 4;
    const int rows = 2;
    const int spacingX = 30;
    const int spacingY = 70; // Extra for text

    int hoveredIndex = -1;

    // Pagination bounds
    Rectangle btnPrevRect, btnNextRect;
    bool hoverPrev = false, hoverNext = false;

    public GameGridControl(List<GameItem> gameList, int tw, int th)
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        this.DoubleBuffered = true;
        this.games = gameList;
        this.tileW = tw;
        this.tileH = th;
        this.totalPages = (int)Math.Ceiling(games.Count / 8.0);
        if (totalPages == 0) totalPages = 1;

        this.MouseMove += GameGridControl_MouseMove;
        this.MouseClick += GameGridControl_MouseClick;
        this.MouseLeave += (s, e) => { hoveredIndex = -1; hoverPrev = false; hoverNext = false; Invalidate(); };
    }

    private void GameGridControl_MouseMove(object sender, MouseEventArgs e)
    {
        bool redraw = false;

        // Check Arrow Hovers
        bool hPrev = btnPrevRect.Contains(e.Location);
        bool hNext = btnNextRect.Contains(e.Location);
        if (hPrev != hoverPrev || hNext != hoverNext)
        {
            hoverPrev = hPrev;
            hoverNext = hNext;
            redraw = true;
        }

        // Check Tile Hovers
        int newHover = -1;
        int gridTotalW = (cols * tileW) + ((cols - 1) * spacingX);
        int startX = (this.Width - gridTotalW) / 2;
        int startY = 20;

        for (int i = 0; i < 8; i++)
        {
            int globalIndex = (currentPage * 8) + i;
            if (globalIndex >= games.Count) break;

            int c = i % cols;
            int r = i / cols;
            int x = startX + (c * (tileW + spacingX));
            int y = startY + (r * (tileH + spacingY));

            if (e.X >= x && e.X <= x + tileW && e.Y >= y && e.Y <= y + tileH)
            {
                newHover = globalIndex;
                break;
            }
        }

        if (newHover != hoveredIndex)
        {
            hoveredIndex = newHover;
            this.Cursor = (hoveredIndex != -1 || hoverPrev || hoverNext) ? Cursors.Hand : Cursors.Default;
            redraw = true;
        }

        if (redraw) Invalidate();
    }

    private void GameGridControl_MouseClick(object sender, MouseEventArgs e)
    {
        if (hoverPrev && currentPage > 0)
        {
            currentPage--;
            hoveredIndex = -1;
            Invalidate();
            return;
        }
        if (hoverNext && currentPage < totalPages - 1)
        {
            currentPage++;
            hoveredIndex = -1;
            Invalidate();
            return;
        }

        if (hoveredIndex != -1 && hoveredIndex < games.Count)
        {
            string path = games[hoveredIndex].ShortcutPath;
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        int gridTotalW = (cols * tileW) + ((cols - 1) * spacingX);
        int startX = (this.Width - gridTotalW) / 2;
        int startY = 20;

        // Render Pages
        DrawPage(g, currentPage, startX, startY, 1f);

        // Render Pagination Arrows if needed
        if (totalPages > 1)
        {
            int centerBottom = startY + (2 * (tileH + spacingY));
            
            btnPrevRect = new Rectangle(this.Width / 2 - 80, centerBottom, 50, 50);
            btnNextRect = new Rectangle(this.Width / 2 + 30, centerBottom, 50, 50);

            if (currentPage > 0) DrawArrowButton(g, btnPrevRect, true, hoverPrev);
            if (currentPage < totalPages - 1) DrawArrowButton(g, btnNextRect, false, hoverNext);
        }
    }

    private void DrawPage(Graphics g, int pageNum, int startX, int startY, float opacity)
    {
        for (int i = 0; i < 8; i++)
        {
            int globalIndex = (pageNum * 8) + i;
            if (globalIndex >= games.Count) break;

            int c = i % cols;
            int r = i / cols;
            int x = startX + (c * (tileW + spacingX));
            int y = startY + (r * (tileH + spacingY));

            Rectangle tileRect = new Rectangle(x, y, tileW, tileH);
            GameItem game = games[globalIndex];

            // 1. Draw Image with rounded corners
            using (GraphicsPath path = GetRoundedRectPath(tileRect, 12))
            {
                g.SetClip(path);

                if (game.TileImage != null)
                {
                    // Calculate zooming to fill 16:9 box correctly
                    float imgRatio = (float)game.TileImage.Width / game.TileImage.Height;
                    float boxRatio = (float)tileW / tileH;
                    int drawW = tileW, drawH = tileH;
                    
                    if (imgRatio > boxRatio) 
                        drawW = (int)(tileH * imgRatio);
                    else 
                        drawH = (int)(tileW / imgRatio);

                    Rectangle imgDest = new Rectangle(x + (tileW - drawW)/2, y + (tileH - drawH)/2, drawW, drawH);
                    g.DrawImage(game.TileImage, imgDest);
                }
                else
                {
                    using (SolidBrush fallBack = new SolidBrush(Color.FromArgb(40, 40, 40)))
                        g.FillRectangle(fallBack, tileRect);
                }

                // Hover overlay effect
                if (globalIndex == hoveredIndex)
                {
                    using (SolidBrush overlay = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                        g.FillRectangle(overlay, tileRect);
                }
                
                g.ResetClip();
                
                // Outline to match 2D flat border design
                using (Pen borderPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 2. Draw Game Title
            Rectangle textRect = new Rectangle(x, y + tileH + 15, tileW, 35);
            TextRenderer.DrawText(
                g, game.Name, new Font("Segoe UI", 12f, FontStyle.Regular),
                textRect, Color.FromArgb((int)(255 * opacity), 250, 250, 250),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.EndEllipsis
            );
        }
    }

    private void DrawArrowButton(Graphics g, Rectangle rect, bool isLeft, bool isHovered)
    {
        Color backColor = isHovered ? Color.FromArgb(70, 70, 90) : Color.FromArgb(40, 40, 60);
        
        using (SolidBrush brush = new SolidBrush(backColor))
        using (Pen borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), 1.5f))
        {
            g.FillEllipse(brush, rect);
            g.DrawEllipse(borderPen, rect);
        }

        using (Pen arrowPen = new Pen(Color.White, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
        {
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            int size = 6;
            int offset = isLeft ? -2 : 2;

            Point[] pts = isLeft 
                ? new Point[] { new Point(cx + size + offset, cy - size * 2), new Point(cx - size + offset, cy), new Point(cx + size + offset, cy + size * 2) }
                : new Point[] { new Point(cx - size + offset, cy - size * 2), new Point(cx + size + offset, cy), new Point(cx - size + offset, cy + size * 2) };

            g.DrawLines(arrowPen, pts);
        }
    }

    private GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public class PillButton : Control
{
    // Fixed property: Compatible with all C# versions and triggers a redraw on change
    private Color borderColor = Color.Transparent;
    
    public Color BorderColor 
    { 
        get { return borderColor; } 
        set 
        { 
            borderColor = value; 
            this.Invalidate(); // Forces the control to repaint when the color changes
        } 
    }

    public PillButton() 
    { 
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        this.DoubleBuffered = true; 
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        int r = 36; // Windows 10 pill button style radius

        using (GraphicsPath path = new GraphicsPath())
        {
            path.AddArc(0, 0, r, r, 180, 90);
            path.AddArc(this.Width - r - 1, 0, r, r, 270, 90);
            path.AddArc(this.Width - r - 1, this.Height - r - 1, r, r, 0, 90);
            path.AddArc(0, this.Height - r - 1, r, r, 90, 90);
            
            path.CloseFigure();

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(120, 35, 25, 45)))
            {
                g.FillPath(bgBrush, path);
            }
            
            if (BorderColor != Color.Transparent)
            {
                using (Pen borderPen = new Pen(BorderColor, 1.5f))
                    g.DrawPath(borderPen, path);
            }
        }

        TextRenderer.DrawText(
            g, this.Text, this.Font,
            new Rectangle(0, 0, this.Width, this.Height),
            this.ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
        );
    }
}
