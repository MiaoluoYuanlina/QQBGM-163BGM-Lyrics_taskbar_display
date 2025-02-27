using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Taskbar;
using System.Timers;

namespace Lyrics_taskbar_display
{

    internal class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        const int GWL_EX_STYLE = -20;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int SW_HIDE = 0;
        static IntPtr HWND_MESSAGE = new IntPtr(-3);

        [STAThread]
        static void Main(string[] args)
        {
            #region 启动参数配置
            int start_FPS = 30;
            int start_OffsetX = 0;
            int start_OffsetY = 0;
            int start_TYPE = 0;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--fps":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int fps))
                        {
                            start_FPS = fps;
                            i++; // 跳过已处理的参数值
                        }
                        break;

                    case "--type":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int type))
                        {
                            start_TYPE = type;
                            i++;
                        }
                        break;

                    case "--offsetx":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int x))
                        {
                            start_OffsetX = x;
                            i++;
                        }
                        break;

                    case "--offsety":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int y))
                        {
                            start_OffsetY = y;
                            i++;
                        }
                        break;

                    case "--X":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int x2))
                        {
                            start_OffsetX = x2;
                            i++;
                        }
                        break;

                    case "--Y":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int y2))
                        {
                            start_OffsetY = y2;
                            i++;
                        }
                        break;

                    case "-noui":
                        // 隐藏控制台窗口
                        IntPtr handle = GetConsoleWindow();

                        SetParent(handle, HWND_MESSAGE);
                        SetWindowLong(handle, GWL_EX_STYLE,
                        GetWindowLong(handle, GWL_EX_STYLE) | WS_EX_TOOLWINDOW);
                        ShowWindow(handle, SW_HIDE);

                        break;
                }
            }
            Console.WriteLine($"FPS={start_FPS}, X={start_OffsetX}, Y={start_OffsetY}, TYPE={start_TYPE}");
            #endregion


            Rectangle primaryScreenArea = Screen.PrimaryScreen.WorkingArea;
            Console.WriteLine($"工作区域分辨率: {primaryScreenArea.Width}x{primaryScreenArea.Height}");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new TransparentForm(start_TYPE);
            form.Start_parameters(start_FPS, start_OffsetX, start_OffsetY, start_TYPE);//传递启动参数
            form.StartCapture();  // 启动截图循环
            Application.Run(form);


            

        }

        #region 辅助方法
        private static bool TryParseNextArg(string[] args, ref int index, out int result)
        {
            result = 0;
            if (index + 1 >= args.Length)
            {
                LogError($"参数 {args[index]} 缺少值");
                return false;
            }

            if (!int.TryParse(args[index + 1], out result))
            {
                LogError($"参数 {args[index]} 的值无效: {args[index + 1]}");
                return false;
            }

            index++; // 正确移动索引
            return true;
        }

        private static void Log(string message) => Console.WriteLine($"[Config] {message}");
        private static void LogWarning(string message) => Console.WriteLine($"[Warning] {message}");
        private static void LogError(string message) => Console.WriteLine($"[Error] {message}");
        #endregion
    }
    
    public class TransparentForm : Form
    {
        //启动参数
        int start_FPS = 20;
        int start_OffsetX = 0;
        int start_OffsetY = 0;
        int start_TYPE = 0;

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize,
            IntPtr hdcSrc, ref Point pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        // 截图相关API
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, int nFlags);

        const int PW_RENDERFULLCONTENT = 0x2; // 关键：即使窗口隐藏也能截图

        //任务栏置顶
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,int X, int Y, int cx, int cy, uint uFlags);

        //原歌词透明

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);



        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;      // 保持窗口可见


        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_COLORKEY = 0x1;
        private const int LWA_ALPHA = 0x2;


        private Rectangle primaryScreenArea = Screen.PrimaryScreen.WorkingArea;//获取屏幕工作区域

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        private Bitmap currentBitmap;
        private bool isDragging;
        private Point lastMousePos;

        private IntPtr targetWindowHandle;
        private Thread captureThread;
        private bool isCapturing;

        private System.Windows.Forms.Timer windowCheckTimer;
        private int retryCount = 0;
        private const int MAX_RETRY = 3;


        // 窗口操作标志
        /// <summary>
        /// 设置窗口置顶并修改尺寸
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="width">新宽度</param>
        /// <param name="height">新高度</param>
        public static void SetTopMostWithSize(IntPtr hWnd, int width, int height)
        {
            // 同时设置尺寸和置顶（SWP_NOMOVE 保留原位置）
            SetWindowPos(
                hWnd,
                HWND_TOPMOST,
                0, 0,
                width, height,
                SWP_SHOWWINDOW | SWP_NOMOVE
            );
        }
        /// <summary>
        /// 获取窗口当前尺寸
        /// </summary>
        public static (int Width, int Height) GetWindowSize(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT rect);
            return (rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public void Start_parameters(int FPS ,int X, int Y,int TYPE)
        {
            start_FPS = FPS;
            start_OffsetX = X;
            start_OffsetY = Y;
            start_TYPE = TYPE;

        }

        public TransparentForm(int start_TYPE = 0)
        {

            // 基础设置
            TopMost = true;         // 置顶显示
            ShowInTaskbar = false;  // 不在任务栏显示
            FormBorderStyle = FormBorderStyle.None; // 无边框
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;



            // 修改窗口扩展样式
            int extendedStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            extendedStyle |= WS_EX_TOOLWINDOW;  // 隐藏从任务视图
            extendedStyle |= WS_EX_NOACTIVATE;  // 防止窗口激活
            SetWindowLong(Handle, GWL_EXSTYLE, extendedStyle);

            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

            MouseDown += (s, e) => StartDrag(e);
            MouseMove += (s, e) => UpdateDrag(e);
            MouseUp += (s, e) => EndDrag();


            while (true)
            {
                if (start_TYPE == 0)
                {
                    Console.WriteLine("针对于 QQ音乐 的启动方式。");
                    targetWindowHandle = FindWindow(null, "桌面歌词");
                    var size = GetWindowSize(targetWindowHandle);
                    //int width = size.Width;
                    //int height = size.Height; 
                    SetTopMostWithSize(targetWindowHandle, size.Width, 72);
                }
                else if (start_TYPE == 1)
                {
                    Console.WriteLine("针对于 网易云音乐 的启动方式。");
                    targetWindowHandle = FindWindow(null, "桌面歌词");
                    var size = GetWindowSize(targetWindowHandle);
                    //int width = size.Width;
                    //int height = size.Height; 
                    SetTopMostWithSize(targetWindowHandle, size.Width, 97);
                }

                if (targetWindowHandle != IntPtr.Zero)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("未能找到可用窗口。");
                }
                

                Thread.Sleep(1000);

            }



            Console.WriteLine($"歌词窗口句柄: {targetWindowHandle}");
            if (targetWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("未找到桌面歌词窗口");
                Environment.Exit(1);
            }

            System.Timers.Timer timer = new System.Timers.Timer(3000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true; // 设置为 true 以重复触发
            timer.Enabled = true;   // 启动 Timer
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            targetWindowHandle = FindWindow(null, "桌面歌词");
        }

        public void StartCapture()
        {
            isCapturing = true;
            captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true
            };
            captureThread.Start();
        }

        public void StopCapture()
        {
            isCapturing = false;
            captureThread?.Join();
        }

        private void CaptureLoop()
        {
            while (isCapturing)
            {
                if ($"{targetWindowHandle}" != "0")
                {
                    this.Invoke((MethodInvoker)UpdateWindowPosition); // 位置更新
                }
                else
                {
                    // 替换透明图片核心代码
                    this.Invoke((MethodInvoker)(() =>
                    {
                        using (var bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
                        {
                            bmp.SetPixel(0, 0, Color.Transparent);
                            UpdateImage(bmp);
                        }
                    }));
                }
                Thread.Sleep(1000/start_FPS);  //FPS
            }
        }

        private const string TrayToolbarClassName = "ToolbarWindow32";
        private const string TrayClassName = "Shell_TrayWnd";
        public static RECT GetSystemTrayPosition()
        {
            var hTray = FindWindow(TrayClassName, null);
            var hTrayToolbar = FindWindowEx(hTray, IntPtr.Zero, TrayToolbarClassName, null);

            GetWindowRect(hTrayToolbar, out RECT rect);
            return rect;
        }

        private const string TaskbarClassName = "Shell_TrayWnd";
        public static RECT GetTaskbarPosition()
        {
            var hTaskbar = FindWindow(TaskbarClassName, null);
            GetWindowRect(hTaskbar, out RECT rect);
            return rect;
        }

        private void UpdateWindowPosition()
        {
            // 获取位置
            var trayRect = GetTaskbarPosition();

            Console.WriteLine($"trayRect.Left:{trayRect.Left} trayRect.Top:{trayRect.Top}");

            //默认QQ音乐
            int newX = trayRect.Left + 110 + start_OffsetX;
            int newY = trayRect.Top - 7 + start_OffsetY;
            if (start_TYPE == 1)//网易云音乐
            {

            }

            // 更新窗口位置
            if (this.Location.X != newX || this.Location.Y != newY)
            {
                this.Location = new Point(newX, newY);
            }

            if (primaryScreenArea.Height - 3 >= trayRect.Top || primaryScreenArea.Height == trayRect.Top)//任务栏隐藏不刷新歌词
            {
                Console.WriteLine($"刷新歌词");
                CaptureTargetWindow();//截图刷新歌
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            }

        }


        private void CaptureTargetWindow()
        {
            RECT rect;
            GetWindowRect(targetWindowHandle, out rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0) return;

            // 创建临时位图用于处理
            Bitmap finalBmp = null;

            try
            {
                using (Bitmap originalBmp = new Bitmap(width, height))
                {
                    // 获取原始图像
                    using (Graphics g = Graphics.FromImage(originalBmp))
                    {
                        IntPtr hdc = g.GetHdc();
                        bool success = PrintWindow(targetWindowHandle, hdc, 0x2); // 0x2: PW_RENDERFULLCONTENT
                        g.ReleaseHdc(hdc);

                        if (!success)
                        {
                            Console.WriteLine("截图失败！错误代码: " + Marshal.GetLastWin32Error());
                            return;
                        }
                    }

                    // 裁剪处理
                    int i = 1;
                    if (start_TYPE == 0)//QQ音乐
                    {
                        i = 4;
                    }
                    else if (start_TYPE == 1)//网易云
                    {
                        i = 3;
                    }else
                    {
                        i = 0;
                        Console.WriteLine($"未知TYPE类型");
                    }
                    int croppedHeight = height - height / i;
                    Rectangle cropArea = new Rectangle(0, height / i, width, croppedHeight);
                    // 创建独立的位图副本
                    using (Bitmap croppedBmp = originalBmp.Clone(cropArea, PixelFormat.Format32bppArgb))
                    {
                        // 创建最终位图的深拷贝
                        finalBmp = new Bitmap(croppedBmp.Width, croppedBmp.Height, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(finalBmp))
                        {
                            g.DrawImage(croppedBmp, Point.Empty);
                        }

                        // 处理黑色像素（在新的副本上操作）
                        ProcessBlackPixels(finalBmp); // 注意这里传递的是finalBmp
                    }
                }

                // 跨线程安全更新
                var tempBmp = finalBmp; // 转移所有权
                finalBmp = null; // 防止重复使用

                this.Invoke((MethodInvoker)delegate
                {
                    UpdateImage(tempBmp);
                });
            }
            finally
            {
                finalBmp?.Dispose(); // 确保异常情况下也能释放资源
            }
        }

        private void ProcessBlackPixels(Bitmap bitmap)
        {
            // 使用锁确保单线程访问
            lock (bitmap)
            {
                BitmapData bmpData = null;
                try
                {
                    bmpData = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb);

                    // 移除并行处理改用单线程
                    int bytesPerPixel = 4;
                    byte[] pixelBuffer = new byte[bmpData.Stride * bmpData.Height];

                    // 复制数据到缓冲区
                    Marshal.Copy(bmpData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

                    // 处理像素（单线程）
                    for (int y = 0; y < bmpData.Height; y++)
                    {
                        int currentLine = y * bmpData.Stride;
                        for (int x = 0; x < bmpData.Width * bytesPerPixel; x += bytesPerPixel)
                        {
                            int blue = pixelBuffer[currentLine + x];
                            int green = pixelBuffer[currentLine + x + 1];
                            int red = pixelBuffer[currentLine + x + 2];

                            if (red == 0 && green == 0 && blue == 0)
                            {
                                pixelBuffer[currentLine + x + 3] = 0; // 设置Alpha通道
                            }
                        }
                    }

                    // 写回数据
                    Marshal.Copy(pixelBuffer, 0, bmpData.Scan0, pixelBuffer.Length);
                }
                finally
                {
                    if (bmpData != null)
                    {
                        bitmap.UnlockBits(bmpData);
                    }
                }
            }
        }


        public void UpdateImage(Bitmap newImage)
        {
            lock (this)
            {
                // 直接替换引用
                var oldBmp = Interlocked.Exchange(ref currentBitmap, null);
                oldBmp?.Dispose();

                currentBitmap = newImage?.Clone() as Bitmap;
                UpdateWindow(currentBitmap);
            }
        }


        private void UpdateWindow(Bitmap bitmap)
        {
            if (bitmap == null) return;

            IntPtr screenDc = IntPtr.Zero;
            IntPtr memDc = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                screenDc = GetDC(IntPtr.Zero);
                memDc = CreateCompatibleDC(screenDc);

                hBitmap = bitmap.GetHbitmap(System.Drawing.Color.FromArgb(0));
                oldBitmap = SelectObject(memDc, hBitmap);

                var size = new Size(bitmap.Width, bitmap.Height);
                var pointSource = new Point(0, 0);
                var topPos = new Point(Left, Top);
                var blend = new BLENDFUNCTION
                {
                    BlendOp = 0,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = 1
                };

                UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc,
                    ref pointSource, 0, ref blend, 2);

                Size = size;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, oldBitmap);
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
            }
        }

        private void StartDrag(MouseEventArgs e)
        {
            isDragging = true;
            lastMousePos = e.Location;
            Capture = true;
        }

        private void UpdateDrag(MouseEventArgs e)
        {
            if (!isDragging) return;

            var newX = Left + (e.X - lastMousePos.X);
            var newY = Top + (e.Y - lastMousePos.Y);
            Location = new Point(newX, newY);
        }

        private void EndDrag()
        {
            isDragging = false;
            Capture = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                currentBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        // 新增结构体用于窗口尺寸
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopCapture();
            base.OnFormClosing(e);
        }

        [DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
    }
}
