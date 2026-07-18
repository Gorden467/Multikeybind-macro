using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;

namespace MultikeysSetup
{
    // Kleiner Installer fuer Multikeys:
    // laedt die App herunter, legt Verknuepfungen an und startet sie.
    // (Dass dieses Setup laeuft, beweist bereits, dass .NET Framework 4
    //  vorhanden ist - die App braucht denselben Unterbau.)
    internal static class Program
    {
        private const string Base =
            "https://raw.githubusercontent.com/Gorden467/Multikeybind-macro/main/dist";

        private static void Main()
        {
            try
            {
                Console.Title = "Multikeys Setup";
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("======================================");
                Console.WriteLine("  Multikeys - Installation");
                Console.WriteLine("======================================");
                Console.ResetColor();
                Console.WriteLine();

                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Multikeys");
                Directory.CreateDirectory(dir);

                Info(".NET Framework 4 ist vorhanden (dieses Setup laeuft darauf).");

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2

                string exe = Path.Combine(dir, "Multikeys.exe");
                string icon = Path.Combine(dir, "icon.ico");

                Download(Base + "/Multikeys.exe", exe);
                Download(Base + "/icon.ico", icon);

                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Multikeys.lnk"),
                    exe, dir, icon, "Multikeys - Tastatur-Makros");
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Multikeys.lnk"),
                    exe, dir, icon, "Multikeys - startet mit Windows");
                Info("Verknuepfungen erstellt (Desktop + Autostart).");

                Process.Start(new ProcessStartInfo { FileName = exe, WorkingDirectory = dir });

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine("Fertig! Multikeys wurde installiert und gestartet.");
                Console.WriteLine("Installationsordner: " + dir);
                Console.ResetColor();
            }
            catch (WebException)
            {
                Fail("Download nicht moeglich. Ist das Repository schon auf 'oeffentlich' gestellt\n"
                     + "  und besteht eine Internetverbindung?");
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Zum Schliessen eine Taste druecken ...");
            try { Console.ReadKey(); } catch { }
        }

        private static void Download(string url, string path)
        {
            Info("Lade " + Path.GetFileName(path) + " ...");
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "Multikeys-Setup");
                wc.DownloadFile(url, path);
            }
        }

        // Erstellt eine .lnk-Verknuepfung ueber WScript.Shell (per Reflection, ohne COM-Verweis).
        private static void CreateShortcut(string linkPath, string target, string workDir, string icon, string desc)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(t);
            object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { linkPath });
            Type lt = lnk.GetType();
            lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
            lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workDir });
            if (File.Exists(icon))
                lt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { icon + ",0" });
            lt.InvokeMember("Description", BindingFlags.SetProperty, null, lnk, new object[] { desc });
            lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
        }

        private static void Info(string s)
        {
            Console.WriteLine("  " + s);
        }

        private static void Fail(string s)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("Fehler: " + s);
            Console.ResetColor();
        }
    }
}
