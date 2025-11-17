using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Rainmeter;


// Overview: This is a simple Blur plugin for Rainmeter, that uses a trick of grabbing the current wallpaper
// It identifies where your widget is and crops out that part to be used as an image in the Widget. 
// For the "Blur" effect, it actually just resizes the image by 10x so it looks blurry
// I thought it's rather clever, because after the initial load the performance hit is 0
// It caches the resized wallpaper in the temp file, and the image areas of the widgets, so if you start RM again it will just use those
// Only downside is that RM Widgets need to be refreshed if you move your Widgets around or change the wallpaper
//
// Must always provide XPos, YPos, XWidth, XHeight (To-Do: Find a cool way to get it from the API)
// Additional options include: Disabled=0, Intensity=3 (Blur Strength), Zoom=1, DontResize=0, XOffset=0 (Offset in Pixels), YOffset=0
// You can provide an ImagePath image, like image.jpg and it will instead use that as the background


namespace EasyBlur
{
    class Measure
    {
        private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "RainBlurCache");
        public string TempImagePath;
		public string WallpaperPath;
        public string ImagePath;
        public int ScreenAreaWidth;
        public int ScreenAreaHeight;
        public int XPos;
        public int YPos;
        public int XWidth;
        public int XHeight;
        public int Intensity;
        public double Zoom;
        public int XOffset;
        public int YOffset;
        public int Disabled;
        public int DontBlur;
        public int FastBlur;
        public Rainmeter.API Api;
        
		
        public void ReadOptions()
		{
            // Api.Log(API.LogType.Notice, "EasyBlur.dll Reading Options");
            Disabled = Api.ReadInt("Disabled", 0);
            DontBlur = Api.ReadInt("DontBlur", 0);
            FastBlur = Api.ReadInt("FastBlur", 0);
            Api.Log(API.LogType.Notice, $"Fast Blur: {FastBlur}");
            XPos = Api.ReadInt("XPos", 600);
            YPos = Api.ReadInt("YPos", 600);
            XWidth = Api.ReadInt("XWidth", 600);
            XHeight = Api.ReadInt("XHeight", 600);
            Intensity = Api.ReadInt("Intensity", 3);
            ImagePath = Api.ReadString("ImagePath", "");

            // Check if a image is specified in the Plugin
            if (Disabled != 1 && ImagePath.Length > 4)
            {
                WallpaperPath = ImagePath;
            }
            // Read wallpaper path from registry
            else if (Disabled != 1)
            {
                WallpaperPath = GetWallpaperPath();
                // Api.Log(API.LogType.Notice, $"Returning dest path {WallpaperPath}");
            }
        }

		private static string GetWallpaperPath()
		{
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\\Desktop", false))
                {
                    if (key == null)
                        return null;

                    var value = key.GetValue("WallPaper") as string;
                    if (string.IsNullOrWhiteSpace(value) || !File.Exists(value))
                        return null;

                    // Create a temp folder called BlurCache
                    string blurCacheDir = Path.Combine(Path.GetTempPath(), "RainBlurCache");
                    Directory.CreateDirectory(blurCacheDir);

                    // Destination path inside BlurCache
                    long wallpaperLength = new FileInfo(value).Length;
                    bool isRainmeter = Path.GetFileName(value).IndexOf("Wallpaper.bmp", StringComparison.OrdinalIgnoreCase) >= 0;

                    string destPath = Path.Combine(blurCacheDir, $"{wallpaperLength.ToString()}-Original.jpg");

                    // Workaround for wallpapers set by Rainmeter, because we can't reliably check if it was set before due to filesize
                    if (isRainmeter)
                    {
         
                        destPath = Path.Combine(blurCacheDir, $"{wallpaperLength.ToString()}{System.IO.File.GetLastWriteTime(value).ToString("ss")}-RainmeterOriginal.jpg");

                    }

                    // Copy only if it doesn't already exist
                    if (!File.Exists(destPath))
                    {
                        File.Copy(value, destPath);
                    }

                    return destPath;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error copying wallpaper: " + ex.Message);
                return null;
            }
        }
        public void GenerateImage()
        {
            if (Disabled != 1)
            {
                // CleanupTemp();
                // Api.Log(API.LogType.Notice, $"EasyBlur.dll Widget is at X{XPos} Y{YPos} {XWidth}x{XHeight}");


                // Api.Log(API.LogType.Notice, $"Will try {WallpaperPath}");
                try
                {
                    Directory.CreateDirectory(TempDirectory);
                    //CleanTranscodedWallpapers();

                    //Also cache the options:



                    ScreenAreaWidth = Api.ReadInt("ScreenAreaWidth", 1024);
                    ScreenAreaHeight = Api.ReadInt("ScreenAreaHeight", 600);
                    Zoom = Api.ReadDouble("Zoom", 1);
                    XOffset = Api.ReadInt("XOffset", 0);
                    YOffset = Api.ReadInt("YOffset", 0);

                    CleanWallpapers(Path.GetFileNameWithoutExtension(WallpaperPath).ToString().Replace("-Original", ""));
                    // Api.Log(API.LogType.Notice, $"Will clean jpg except {Path.GetFileNameWithoutExtension(WallpaperPath).ToString().Replace("-Original", "")}");

                    Bitmap resizedWall = new Bitmap(WallpaperPath);

                    string optName = $"{Api.GetSkinName().Replace(@"\", "-")}@I{Intensity.ToString()}XO{XOffset.ToString()}YO{YOffset.ToString()}Z{Zoom.ToString()}.txt";
                    string optPath = Path.Combine(TempDirectory, optName);


                    if (FastBlur == 0 && resizedWall.Width == ScreenAreaWidth && resizedWall.Height == ScreenAreaHeight || Api.ReadInt("DontResize", 0) != 0)
                    {
                        // Api.Log(API.LogType.Notice, $"Wallpaper is the same size screen size or DontResize is specified");

                    }

                    else
                    {
                        long wallpaperLength = new FileInfo(WallpaperPath).Length;
                        string tempName = $"{wallpaperLength.ToString()}.jpg";

                        bool isRainmeter = Path.GetFileName(WallpaperPath).IndexOf("Rainmeter", StringComparison.OrdinalIgnoreCase) >= 0;
                        // Workaround for wallpapers set by Rainmeter, because we can't reliably check if it was set before due to filesize
                        if (isRainmeter)
                        {
                            // Api.Log(API.LogType.Notice, $"It's Rainmeter.bmp");
                            tempName = $"{wallpaperLength.ToString()}{System.IO.File.GetLastWriteTime(WallpaperPath).ToString("ss")}.jpg";
                        }
                         string tempPath = Path.Combine(TempDirectory, tempName);


                        if (File.Exists(tempPath))
                        {
                            // Api.Log(API.LogType.Notice, $"Wallpaper is already in the temp folder and resized!");
                            resizedWall = new Bitmap(tempPath);
                        }
                        else
                        {
                            CleanupAllTemp();
                            if (FastBlur == 1)
                            {
                                ScreenAreaWidth = ScreenAreaWidth / 6 / Intensity;
                                ScreenAreaHeight = ScreenAreaHeight / 6 / Intensity;
                            }


                            //CleanTranscodedWallpapers();
                            Api.Log(API.LogType.Notice, $"Wallpaper is being resized from: {resizedWall.Width}x{resizedWall.Height} to {(int)ScreenAreaWidth}x{(int)ScreenAreaHeight}");
                            Bitmap b = new Bitmap((int)ScreenAreaWidth, (int)ScreenAreaHeight);
                            using (Graphics g = Graphics.FromImage(b))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                g.DrawImage(resizedWall, 0, 0, (int)ScreenAreaWidth, (int)ScreenAreaHeight);

                                //Api.Log(API.LogType.Notice, $"Wallpaper {tempName} is being saved to {tempPath}:");
                                b.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                                resizedWall = b;
                                g.Dispose();

                            }
                        }
                    }
                    using (var src = resizedWall)
                    {
                        // Clamp crop rectangle to image bounds
                        int x = Math.Max(0, XPos + XOffset);
                        int y = Math.Max(0, YPos + YOffset);
                        int w = Math.Max(1, XWidth);
                        int h = Math.Max(1, XHeight);

                        if (FastBlur == 1)
                        {
                            x = Math.Max(0, XPos + XOffset) / 6 / Intensity;
                            y = Math.Max(0, YPos + YOffset) / 6 / Intensity;
                            w = Math.Max(1, XWidth) / 6 / Intensity;
                            h = Math.Max(1, XHeight) / 6 / Intensity;
                        }

                        if (x >= src.Width || y >= src.Height)
                        {
                            TempImagePath = string.Empty;
                            return;
                        }

                        if (x + w > src.Width) w = src.Width - x;
                        if (y + h > src.Height) h = src.Height - y;
                        double zoom = Zoom; // Little magnifying glass trick
                        float centerX = x + w / 2f;
                        float centerY = y + h / 2f;
                        double newW = w / zoom;
                        double newH = h / zoom;
                        double newX = centerX - newW / 2f;
                        double newY = centerY - newH / 2f;
                        Rectangle cropRect = new Rectangle(
                            (int)Math.Round(newX),
                            (int)Math.Round(newY),
                            (int)Math.Round(newW),
                            (int)Math.Round(newH)
                        );
                        long wallpaperLength = new FileInfo(WallpaperPath).Length;
                        string tempName = $"{Api.GetSkinName().Replace(@"\", "-")}@{newX}x{newY}x{newW}x{newH}-{wallpaperLength}.png";

                        bool isRainmeter =  Path.GetFileName(WallpaperPath).IndexOf("Rainmeter", StringComparison.OrdinalIgnoreCase) >= 0;
                        // Workaround for wallpapers set by Rainmeter, because we can't reliably check if it was set before due to filesize
                        if (isRainmeter)
                        {
                            Api.Log(API.LogType.Notice, $"It's Rainmeter.bmp");
                            tempName = $"{Api.GetSkinName().Replace(@"\", "-")}@{newX}x{newY}x{newW}x{newH}-{wallpaperLength}{System.IO.File.GetLastWriteTime(WallpaperPath).ToString("ss")}.png";

                        }
                        //string tempName = $"{Api.GetSkinName().Replace(@"\", "-")}@{newX}x{newY}x{newW}x{newH}-{Path.GetFileNameWithoutExtension(WallpaperPath)}.png";
                        string tempPath = Path.Combine(TempDirectory, tempName);


                        if (File.Exists(tempPath) && File.Exists(optPath))
                        {
                            // Api.Log(API.LogType.Notice, $"Skin was processed before, so no need to create a new blur");
                            TempImagePath = tempPath;
                        }
                        else
                        {
                            // Api.Log(API.LogType.Notice, $"Re-doing the Blur Cache Chunks");
                            // Don't need to clean anything because we will clean it all on big reload when a wallpaper changed
                            //CleanFile(Api.GetSkinName().Replace(@"\", "-"));
                            //CleanFileOpts(Api.GetSkinName().Replace(@"\", "-"));
      
                            //Create options cache file
                            using (System.IO.File.Create(optPath))

                            using (var cropped = new Bitmap(cropRect.Width, cropRect.Height))
                            {
                                using (var g = Graphics.FromImage(cropped))
                                {
                                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                    g.SmoothingMode = SmoothingMode.HighQuality;
                                    g.CompositingQuality = CompositingQuality.HighQuality;
                                    g.DrawImage(src, new Rectangle(0, 0, cropped.Width, cropped.Height), cropRect, GraphicsUnit.Pixel);
                                }

                                int resizedW = Math.Max(1, (int)cropped.Width);
                                int resizedH = Math.Max(1, (int)cropped.Height);
                                if (FastBlur == 0)
                                {
                                    resizedW = Math.Max(1, (int)Math.Round(cropped.Width * 0.1 / Intensity));
                                    resizedH = Math.Max(1, (int)Math.Round(cropped.Height * 0.1 / Intensity));
                                }
                                // Resize to 10% (aka apply Blur strength, doesn't work with FastBlur!)
                                if (DontBlur == 1)
                                {
                                    resizedW = Math.Max(1, (int)(cropped.Width));
                                    resizedH = Math.Max(1, (int)(cropped.Height));
                                }


                                using (var resized = new Bitmap(resizedW, resizedH))
                                {
                                    using (var gr = Graphics.FromImage(resized))
                                    {
                                        gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                        gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                        gr.SmoothingMode = SmoothingMode.HighQuality;
                                        gr.CompositingQuality = CompositingQuality.HighQuality;
                                        gr.DrawImage(cropped, new Rectangle(0, 0, resizedW, resizedH));
                                        gr.Dispose();
                                    }

                                    // Using unique-ish skin names for filenames

                                    resized.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                                    TempImagePath = tempPath;
                                    // Api.Log(API.LogType.Notice, $"EasyBlur.dll Generating a blurry image {tempPath}");
                                }
                            }
                        }
                    }
                }
                catch
                {
                    Api.Log(API.LogType.Error, $"EasyBlur.dll Could not create Blur Cache");
                    // TempImagePath = string.Empty;
                }
            }
        }

        

        public void CleanTranscodedWallpapers()
        {
            try
            {
                if (Directory.Exists(TempDirectory))
                {

                    foreach (var file in Directory.GetFiles(TempDirectory, "TranscodedWallpaper"))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            Api.Log(API.LogType.Error, $"EasyBlur.dll Could not clear TranscodedWallpaper cache");
                        }
                    }

                }
            }
            catch
            {
                Api.Log(API.LogType.Error, $"EasyBlur.dll Some TranscodedWallpaper cache error");
            }
        }
        public void CleanupAllTemp()
        {
            try
            {
                if (Directory.Exists(TempDirectory))
                {
                    foreach (var file in Directory.GetFiles(TempDirectory, "*.*"))
                    {
 
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            Api.Log(API.LogType.Warning, $"EasyBlur.dll Some Error Deleting cache, try Restarting Rainmeter");
                        }
                    }
                }
            }
            catch
            {
                Api.Log(API.LogType.Error, $"EasyBlur.dll Some general error while clearing the cache");
            }
        }
        
        
        // Delete the old wallpaper
        public void CleanWallpapers(string keepSubstring)
        {
            try
            {
                if (Directory.Exists(TempDirectory))
                {
                    foreach (var file in Directory.GetFiles(TempDirectory, "*.jpg"))
                    {
                        // Skip files that contain the substring
                        if (file.IndexOf(keepSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;

                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            Api.Log(API.LogType.Warning, $"EasyBlur.dll Some Error Deleting cache {file}, try Restarting Rainmeter");
                        }
                    }
                }
            }
            catch
            {
                Api.Log(API.LogType.Error, $"EasyBlur.dll Some general error while clearing the cache");
            }
        }



        // Delete the old blur files
        public void CleanFile(string Name)
        {
        try
        {
              //  Api.Log(API.LogType.Notice, $"Trying to delete blur cache for:  {Name}");
                string[] files = Directory.GetFiles(TempDirectory, $"{Name}*.png");

            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                   //     Api.Log(API.LogType.Notice, $"Deleted blur cache for: {Name}");
                }
                catch (Exception ex)
                {
                        Api.Log(API.LogType.Warning, $"EasyBlur.dll: Could not delete cache {file}; no problem, will delete when Rainmeter restarts");
                        //   Api.Log(API.LogType.Notice, $"Could not Delete blur cache for: {Name}");

                    }
            }
        }
        catch (Exception ex)
        {
                Api.Log(API.LogType.Error, $"EasyBlur.dll Error accessing directory {ex.Message}");
 
        }
        }
        public void CleanFileOpts(string Name)
        {
            try
            {
                //  Api.Log(API.LogType.Notice, $"Trying to delete blur cache for:  {Name}");
                string[] files = Directory.GetFiles(TempDirectory, $"{Name}*.txt");

                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                        //     Api.Log(API.LogType.Notice, $"Deleted blur cache for: {Name}");
                    }
                    catch (Exception ex)
                    {
                        //   Api.Log(API.LogType.Notice, $"Could not Delete blur cache for: {Name}");

                    }
                }
            }
            catch (Exception ex)
            {
                Api.Log(API.LogType.Error, $"EasyBlur.dll Error accessing directory {ex.Message}");

            }
        }
        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }
    }

    public class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
			data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            //measure?.CleanupAllTemp();
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm)
        {
			Measure measure = (Measure)data;
			measure.Api = (Rainmeter.API)rm;
			measure.ReadOptions();
            measure.GenerateImage();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)data;
            return Rainmeter.StringBuffer.Update(measure.TempImagePath ?? string.Empty);
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)] string args)
        {
            Measure measure = (Measure)data;
            try
            {
                if (measure?.Api != null)
                {
                    IntPtr skin = measure.Api.GetSkin();
                    if (skin != IntPtr.Zero && args == "!ReloadBlur")
                    {

                        measure.ReadOptions();
                        measure.GenerateImage();
                        API.Execute(skin, "[!UpdateMeter Blur][!Redraw]");
                        measure?.Api.Log(API.LogType.Notice, $"EasyBlur.dll Received bang {args}, so re-loading it without refreshing");
                        // API.Execute(skin, "!Refresh");
                        // API.Execute(skin, "!UpdateMeter Blur");
                        // API.Execute(skin, "!Redraw");
                    } 
                        
                }
            }
            catch (Exception ex)
            {
                //System.IO.File.AppendAllText("C:\\RainmeterPluginDebug.txt", ex + Environment.NewLine);
            }
        }

    }
}