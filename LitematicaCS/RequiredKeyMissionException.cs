using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class RequiredKeyMissionException : Exception
    {
        private readonly string key;
        private readonly string message;
        public RequiredKeyMissionException(string key, string message = "The required key is missing in the (Tile) Entity's NBT Compound") : base(message)
        {
            this.key = key;
            this.message = message;
        }
        public override string ToString()
        {
            return $"{key} -> {message}";
        }
    }
}
