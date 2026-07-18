using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Multikeys
{
    /// <summary>
    /// Laedt und speichert die Konfiguration als JSON unter
    /// %APPDATA%\Multikeys\config.json.
    /// </summary>
    public static class ConfigStore
    {
        public static string ConfigDirectory
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "Multikeys");
            }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(ConfigDirectory, "config.json"); }
        }

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return new AppConfig();

                string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                    return new AppConfig();

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                AppConfig config = serializer.Deserialize<AppConfig>(json);
                return config ?? new AppConfig();
            }
            catch
            {
                // Bei beschaedigter Datei mit leerer Konfiguration starten.
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            Directory.CreateDirectory(ConfigDirectory);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(config);
            File.WriteAllText(ConfigPath, Prettify(json), Encoding.UTF8);
        }

        // Kleiner Einrueckungs-Formatierer, damit die JSON-Datei von Hand lesbar bleibt.
        private static string Prettify(string json)
        {
            StringBuilder sb = new StringBuilder();
            int indent = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.Append('\n');
                        indent++;
                        sb.Append(' ', indent * 2);
                        break;
                    case '}':
                    case ']':
                        sb.Append('\n');
                        indent--;
                        sb.Append(' ', indent * 2);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        sb.Append('\n');
                        sb.Append(' ', indent * 2);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
