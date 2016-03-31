using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsFromXml
{
    internal class LoadedDllDetails
    {
        public FileInfo OriginalFile { get; set; }
        public string ShadowCopy { get; set; }
        public System.Reflection.Assembly Assembly { get; set; }
        public IEnumerable<Type> ImplementedTypes { get; set; }
    }
}
