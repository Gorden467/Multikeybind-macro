using System.Windows.Forms;

namespace Multikeys
{
    /// <summary>
    /// Wandelt virtuelle Tastencodes in lesbare Namen (und zurueck).
    /// Die WinForms-Enum <see cref="Keys"/> entspricht direkt den VK-Codes.
    /// </summary>
    public static class KeyNames
    {
        public static string NameOf(int vkCode)
        {
            Keys key = (Keys)vkCode;
            string name = key.ToString();
            // Reine Zahl -> unbekannter Code
            int dummy;
            if (int.TryParse(name, out dummy))
                return "VK 0x" + vkCode.ToString("X2");
            return name;
        }
    }
}
