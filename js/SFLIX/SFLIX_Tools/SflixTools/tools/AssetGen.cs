// Erzeugt beim Bauen das Programm-Icon (app.ico), das Symbol des BiB-Fensters (app_pip.ico)
// und eine Vorschau des Kopfbilds (banner.png).
// Aufruf: AssetGen.exe <Zielordner> <Version>

using System;
using System.Drawing.Imaging;
using System.IO;

namespace Sflix
{
    static class AssetGen
    {
        static int Main(string[] args)
        {
            string dir = args.Length > 0 ? args[0] : ".";
            string version = args.Length > 1 ? args[1] : "";
            Directory.CreateDirectory(dir);
            Branding.WriteIcon(Path.Combine(dir, "app.ico"));
            Branding.WriteIcon(Path.Combine(dir, "app_pip.ico"), true);
            using (var bmp = Branding.RenderLogo(256, true)) bmp.Save(Path.Combine(dir, "logo256_pip.png"), ImageFormat.Png);
            using (var bmp = Branding.RenderBanner(1600, 300, version)) bmp.Save(Path.Combine(dir, "banner.png"), ImageFormat.Png);
            using (var bmp = Branding.RenderLogo(256)) bmp.Save(Path.Combine(dir, "logo256.png"), ImageFormat.Png);
            Console.WriteLine("Icon und Kopfbild erstellt: " + dir);
            return 0;
        }
    }
}
