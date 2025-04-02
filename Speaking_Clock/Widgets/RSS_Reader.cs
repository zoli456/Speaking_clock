using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Xml;
using Speaking_Clock;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using Vortice.WinForms;
using Timer = System.Timers.Timer;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using Task = System.Threading.Tasks.Task;
using static Vanara.PInvoke.User32;

namespace Speaking_clock.Widgets;

internal class RSSReader : RenderForm
{
    private static readonly HttpClient httpClient = new();
    private readonly ID2D1Factory1 _d2dFactory;
    private readonly int fontSize = Beallitasok.RSS_Reader_Section["Betűméret"].IntValue;
    private readonly int form_index;
    private readonly string headerColor;
    private readonly IDWriteTextFormat listFormat;
    private readonly int maxDisplayItems = Beallitasok.RSS_Reader_Section["Elemszám"].IntValue;
    private readonly int spacing = Beallitasok.RSS_Reader_Section["Helyköz"].IntValue;
    private readonly int titleFontSize = Beallitasok.RSS_Reader_Section["Cím_betűméret"].IntValue;
    private readonly IDWriteTextFormat titleFormat;
    private readonly ToolTip toolTip = new();
    private readonly Timer updateTimer;
    private readonly int windowWidth = Beallitasok.RSS_Reader_Section["Ablak_szélesség"].IntValue;
    private ID2D1HwndRenderTarget _renderTarget;
    private ID2D1SolidColorBrush brush;
    private bool dragging;
    private Point dragStart;
    private ID2D1SolidColorBrush headerBrush;
    private int hoveredIndex = -1;
    private List<(string Title, string Link)> items = new();
    private int scrollOffset;

    internal RSSReader(int form_index, string feedUrl, string title, string headerColor = "blue",
        int startX = 100,
        int startY = 100, int updateTime = 600000)

    {
        this.headerColor = headerColor;
        this.form_index = form_index;
        var itemHeight = fontSize + spacing;
        var headerHeight = titleFontSize + spacing * 2;
        var windowHeight = headerHeight + maxDisplayItems * itemHeight;

        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Black;
        Opacity = Beallitasok.RSS_Reader_Section["Átlátszóság"].DoubleValue;
        ClientSize = new Size(windowWidth, windowHeight);
        ShowInTaskbar = false;
        TopLevel = true;
        DoubleBuffered = false;

        StartPosition = FormStartPosition.Manual;
        Location = new Point(startX, startY);

        Region = CreateRoundedRectangleRegion(windowWidth, windowHeight, 20);

        toolTip.AutoPopDelay = 5000;
        toolTip.InitialDelay = 100;
        toolTip.ReshowDelay = 100;
        toolTip.ShowAlways = true;


        // Event Handlers
        MouseDown += (sender, e) =>
        {
            if (e.Button == MouseButtons.Left && e.Y <= headerHeight)
            {
                dragging = true;
                dragStart = e.Location;
            }
        };

        MouseMove += (sender, e) =>
        {
            if (dragging && Beallitasok.RSS_Reader_Section["Húzás"].BoolValue)
            {
                Left += e.X - dragStart.X;
                Top += e.Y - dragStart.Y;
            }
            else
            {
                if (e.Y > headerHeight)
                {
                    var index = (e.Y - headerHeight + scrollOffset * itemHeight) / itemHeight;
                    if (index >= 0 && index < items.Count)
                    {
                        if (hoveredIndex != index)
                        {
                            hoveredIndex = index;
                            toolTip.SetToolTip(this, items[index].Title);
                            Invalidate();
                            Debug.WriteLine("tooltip");
                        }
                    }
                    else
                    {
                        hoveredIndex = -1;
                        toolTip.SetToolTip(this, string.Empty);
                        Invalidate();
                    }
                }
                else
                {
                    hoveredIndex = -1;
                    toolTip.SetToolTip(this, string.Empty);
                    Invalidate();
                }
            }
        };

        MouseUp += (sender, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;
                SaveSettings(this.form_index, Left, Top);
            }
        };

        MouseLeave += (sender, e) =>
        {
            hoveredIndex = -1;
            toolTip.SetToolTip(this, string.Empty);
            Invalidate();
        };

        MouseWheel += (sender, e) =>
        {
            scrollOffset = Math.Max(0, Math.Min(scrollOffset - e.Delta / 120, items.Count - maxDisplayItems));
            Invalidate();
        };

        MouseClick += (sender, e) =>
        {
            if (!dragging && e.Button == MouseButtons.Left)
            {
                var index = (e.Y - headerHeight + scrollOffset * itemHeight) / itemHeight;
                if (index >= 0 && index < items.Count)
                    Process.Start(new ProcessStartInfo { FileName = items[index].Link, UseShellExecute = true });
            }
        };
        /* MouseEnter += (sender, e) =>
         {
             hoveredIndex = -1;
             toolTip.SetToolTip(this, string.Empty);
             Debug.WriteLine(SetActiveWindow(Handle));
         };*/

        // Initialize Direct2D factory
        _d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();

        var dwriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
        titleFormat = dwriteFactory.CreateTextFormat("Arial", titleFontSize);
        titleFormat.TextAlignment = TextAlignment.Center;
        titleFormat.ParagraphAlignment = ParagraphAlignment.Center;

        listFormat = dwriteFactory.CreateTextFormat("Arial", fontSize);
        listFormat.TextAlignment = TextAlignment.Leading;

        LoadRSSAsync(feedUrl);

        updateTimer = new Timer(updateTime);
        updateTimer.Elapsed += async (sender, e) =>
        {
            Debug.WriteLine("RSS frissítés!");
            await LoadRSSAsync(feedUrl);
        };
        updateTimer.Start();

        FormClosed += (sender, e) =>
        {
            updateTimer.Stop();
            updateTimer.Dispose();
            brush.Dispose();
            headerBrush.Dispose();
            _renderTarget.Dispose();
        };

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        //SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, (SetWindowPosFlags)(SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER));;
        /*SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0,
            SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOMOVE |
            SetWindowPosFlags.SWP_NOZORDER);*/
        Show();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Set the WS_EX_TOOLWINDOW style to hide from Alt+Tab
            cp.ExStyle |= (int)WindowStylesEx.WS_EX_TOOLWINDOW; // WS_EX_TOOLWINDOW
            return cp;
        }
    }


/*protected override void WndProc(ref Message m)
    {
       /* const int WM_WINDOWPOSCHANGING = 0x0046;
        const int WM_ACTIVATE = 0x0006;
        const int WA_INACTIVE = 0;

        if (m.Msg == WM_ACTIVATE)
            // Check if the window is becoming inactive
            if (unchecked((int)m.WParam) == WA_INACTIVE)
            {
                // Ensure the window remains active
                SetForegroundWindow(Handle);

                return; // Prevent the window from deactivating
            }

        if (m.Msg == WM_WINDOWPOSCHANGING)
        {
            // Access the WINDOWPOS structure using Vanara's P/Invoke
            var pos = m.LParam.ToStructure<WINDOWPOS>();

            // Preserve the Z-order by ensuring the flags include SWP_NOZORDER
            pos.flags |= SetWindowPosFlags.SWP_NOZORDER;

            // Copy the modified structure back to the message's lParam
            Marshal.StructureToPtr(pos, m.LParam, false);
        }*/

    /* const int WM_ACTIVATE = 0x0006;
     const int WA_INACTIVE = 0;
     const int WM_KILLFOCUS = 0x0008;

     if (m.Msg == WM_ACTIVATE)
     {
         // Check if the window is becoming inactive
         if ((int)m.WParam == WA_INACTIVE)
         {
             // Bring the window back to the foreground
             SetForegroundWindow(this.Handle);
             return; // Prevent further processing of this message
         }
     }*/

    /* if (m.Msg == WM_KILLFOCUS)
     {
         // Regain focus automatically
         SetForegroundWindow(this.Handle);
         return; // Prevent further processing of this message
     }*/

    /*  if (m.Msg == (int)WindowMessage.WM_WINDOWPOSCHANGED)
      {
          var pos = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);
          if ((pos.flags & SetWindowPosFlags.SWP_NOZORDER) == 0)
          {
              Debug.WriteLine("Reapply nozorder");
              SetForegroundWindow(Handle);
              SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0,
                   SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOZORDER);
          }
              // Reapply topmost to maintain Z-order

      }

    base.WndProc(ref m);
}*/

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            updateTimer.Stop();
            updateTimer.Dispose();
            brush.Dispose();
            headerBrush.Dispose();
            _renderTarget.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CreateRenderTarget();
    }

    private void CreateRenderTarget()
    {
        _renderTarget?.Dispose();
        _renderTarget = _d2dFactory.CreateHwndRenderTarget(new RenderTargetProperties(), new HwndRenderTargetProperties
        {
            Hwnd = Handle,
            PixelSize = new SizeI(Width, Height),
            PresentOptions = PresentOptions.None
        });

        brush ??= _renderTarget.CreateSolidColorBrush(new Color4(1, 1, 1));

        if (headerBrush == null)
        {
            var headerColorValue = headerColor.ToLower() switch
            {
                "red" => new Color4(1.0f, 0.0f, 0.0f),
                "orange" => new Color4(1.0f, 0.5f, 0.0f),
                "green" => new Color4(0.0f, 1.0f, 0.0f),
                "blue" => new Color4(0.0f, 0.5f, 1.0f),
                _ => new Color4(0.2f, 0.6f, 0.8f)
            };
            headerBrush = _renderTarget.CreateSolidColorBrush(headerColorValue);
        }
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_renderTarget == null)
            return;

        _renderTarget.BeginDraw();
        _renderTarget.Clear(new Color4(0, 0, 0, 0));

        _renderTarget.FillRectangle(
            new RectangleF(0, 0, ClientSize.Width, titleFontSize + spacing * 2),
            headerBrush);

        _renderTarget.DrawText(
            Text, titleFormat,
            new Rect(new RectangleF(0, 0, ClientSize.Width,
                titleFontSize + spacing * 2)),
            brush);

        // Draw list items
        var y = titleFontSize + spacing * 2;
        for (var i = scrollOffset;
             i < Math.Min(scrollOffset + maxDisplayItems, items.Count);
             i++)
        {
            /* var maxChars = (windowWidth - 15) / (fontSize / 2);
             var truncatedTitle = items[i].Title.Length > maxChars
                 ? items[i].Title.Substring(0, maxChars - 4) + "..."
                 : items[i].Title;*/
            var displayText = "• " + items[i].Title;

            var highlightBrush = hoveredIndex == i ? headerBrush : brush;
            _renderTarget.DrawText(displayText, listFormat,
                new Rect(new RectangleF(10, y, ClientSize.Width - 12,
                    fontSize + spacing)),
                highlightBrush, DrawTextOptions.Clip);
            y += fontSize + spacing;
        }

        _renderTarget.EndDraw();
    }


    private Region CreateRoundedRectangleRegion(int width, int height, int cornerRadius)
    {
        var path = new GraphicsPath();
        path.AddArc(0, 0, cornerRadius * 2, cornerRadius * 2, 180, 90);
        path.AddArc(width - cornerRadius * 2, 0, cornerRadius * 2, cornerRadius * 2, 270, 90);
        path.AddArc(width - cornerRadius * 2, height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
        path.AddArc(0, height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
        path.CloseAllFigures();
        return new Region(path);
    }

    private async Task LoadRSSAsync(string url)
    {
        try
        {
            var rss = await httpClient.GetStringAsync(url);

            var doc = new XmlDocument();
            doc.LoadXml(rss);

            var itemsList = doc.SelectNodes("//item");
            var newItems = new List<(string Title, string Link)>();

            foreach (XmlNode item in itemsList)
            {
                var title = item["title"].InnerText;
                var link = item["link"].InnerText;
                newItems.Add((title, link));
            }

            if (!newItems.SequenceEqual(items))
            {
                items = newItems;
                scrollOffset = 0;
                Invalidate();
            }
        }
        catch (Exception e)
        {
        }
    }

    private static void SaveSettings(int index, int Left, int Top)
    {
        Debug.WriteLine($"RSS olvasó helye mentve({Left}:{Top})");
        switch (index)
        {
            case 1:
            {
                Beallitasok.RSS_Reader_Section["Olvasó_1_Pos_X"].IntValue = Left;
                Beallitasok.RSS_Reader_Section["Olvasó_1_Pos_Y"].IntValue = Top;
                break;
            }
            case 2:
            {
                Beallitasok.RSS_Reader_Section["Olvasó_2_Pos_X"].IntValue = Left;
                Beallitasok.RSS_Reader_Section["Olvasó_2_Pos_Y"].IntValue = Top;
                break;
            }
            case 3:
            {
                Beallitasok.RSS_Reader_Section["Olvasó_3_Pos_X"].IntValue = Left;
                Beallitasok.RSS_Reader_Section["Olvasó_3_Pos_Y"].IntValue = Top;
                break;
            }
            case 4:
            {
                Beallitasok.RSS_Reader_Section["Olvasó_4_Pos_X"].IntValue = Left;
                Beallitasok.RSS_Reader_Section["Olvasó_4_Pos_Y"].IntValue = Top;
                break;
            }
        }

        Beallitasok.ConfigParser.SaveToFile($"{Beallitasok.Path}\\{Beallitasok.SetttingsFileName}");
    }
}