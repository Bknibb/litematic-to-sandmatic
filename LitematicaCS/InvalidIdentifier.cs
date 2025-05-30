using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class InvalidIdentifier : Exception
    {
        public InvalidIdentifier(string identifier) : base($"Invalid identifier \"{identifier}\"") { }
    }
}
