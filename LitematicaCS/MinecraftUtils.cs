using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal static class MinecraftUtils
    {
        public static string assertValidIdentifier(string identifier)
        {
            if (!isValidIdentifier(identifier)) {
                throw new InvalidIdentifier(identifier);
            }
            return identifier;
        }
        public static bool isValidIdentifier(string identifier)
        {
            string allowedChars = "_-abcdefghijklmnopqrstuvwxyz0123456789.:";
            bool seperator = false;
            foreach (char c in allowedChars)
            {
                if (!allowedChars.Contains(c))
                {
                    return false;
                }
                if (c == ':')
                {
                    seperator = true;
                    allowedChars = "_-abcdefghijklmnopqrstuvwxyz0123456789./";
                }
            }
            return seperator;
        }
    }
}
