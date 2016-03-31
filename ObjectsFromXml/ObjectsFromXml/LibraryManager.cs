using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ObjectsFromXml
{
    internal class LibraryManager
    {
        public static LibraryManager Instance { get; internal set; }

        static LibraryManager()
        {
            Instance = new LibraryManager();
        }

        private List<LoadedDllDetails> _knownDlls;
        private Dictionary<string, Type> _dllTypes;

        private LibraryManager()
        {
            _knownDlls = new List<LoadedDllDetails>();
            _dllTypes = new Dictionary<string, Type>();
            AddSystemTypes();
            CheckNewDlls();
        }

        private void AddSystemTypes()
        {
            _dllTypes.Add("BINARYREADER", typeof(System.IO.BinaryReader));
            _dllTypes.Add("BINARYWRITER", typeof(System.IO.BinaryWriter));
            _dllTypes.Add("CULTUREINFO", typeof(System.Globalization.CultureInfo));
            _dllTypes.Add("BOOLEAN", typeof(System.Boolean));
            _dllTypes.Add("BYTE", typeof(System.Byte));
            _dllTypes.Add("CHAR", typeof(System.Char));
            _dllTypes.Add("DATETIME", typeof(System.DateTime));
            _dllTypes.Add("DECIMAL", typeof(System.Decimal));
            _dllTypes.Add("DOUBLE", typeof(System.Double));
            _dllTypes.Add("TIMESPAN", typeof(System.TimeSpan));
            _dllTypes.Add("SINGLE", typeof(System.Single));
            _dllTypes.Add("INT", typeof(System.Int32));
            _dllTypes.Add("LONG", typeof(System.Int64));
            _dllTypes.Add("SHORT", typeof(System.Int16));
            _dllTypes.Add("STRING", typeof(System.String));
            _dllTypes.Add("UNSIGNEDINT", typeof(System.UInt32));
            _dllTypes.Add("UNSIGNEDLONG", typeof(System.UInt64));
            _dllTypes.Add("UNSIGNEDSHORT", typeof(System.UInt16));
            _dllTypes.Add("GUID", typeof(System.Guid));
            _dllTypes.Add("UUID", typeof(System.Guid));
            _dllTypes.Add("DIRECTORY", typeof(System.IO.Directory));
            _dllTypes.Add("DIRECTORYINFO", typeof(System.IO.DirectoryInfo));
            _dllTypes.Add("DRIVEINFO", typeof(System.IO.DriveInfo));
            _dllTypes.Add("FILE", typeof(System.IO.File));
            _dllTypes.Add("FILEINFO", typeof(System.IO.FileInfo));
            _dllTypes.Add("TEXTREADER", typeof(System.IO.TextReader));
            _dllTypes.Add("TEXTWRITER", typeof(System.IO.TextWriter));
            _dllTypes.Add("URI", typeof(System.Uri));
            _dllTypes.Add("URIBUILDER", typeof(System.UriBuilder));
        }

        private void CheckNewDlls()
        { 
            IEnumerable<FileInfo> DLLs = GetCurrentPathDlls();
            IEnumerable<FileInfo> newDlls = DLLs.Where(x => !_knownDlls.Any(y => y.OriginalFile.LastWriteTime == x.LastWriteTime && y.OriginalFile.Name == x.Name));
            foreach(var dll in newDlls)
            {
                if (ContainsRelevantTypes(dll))
                {
                    FileInfo shadowCopy = CopyToTemp(dll);
                    var newDll = new LoadedDllDetails()
                    {
                        OriginalFile = dll,
                        ShadowCopy = shadowCopy.FullName
                    };
                    System.Reflection.Assembly theAssem;
                    var newTypes = GetRelevantTypes(shadowCopy,out theAssem);
                    newDll.Assembly = theAssem;
                    newDll.ImplementedTypes = newTypes;

                    foreach (Type theType in newTypes)
                        _dllTypes[theType.Name.ToUpper()] = theType;
                    
                    _knownDlls.Add(newDll);

                }
            }

        }

        private IEnumerable<FileInfo> GetCurrentPathDlls()
        {
            var assemblyFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.GetDirectoryName(assemblyFile);
            var dirInfo = new DirectoryInfo(path);
            var files = dirInfo.GetFiles("*.dll");
            return files.AsEnumerable();
        }

        private bool ContainsRelevantTypes(FileInfo dll)
        {
            bool result = false;
            Type[] dllTypes = new Type[] {};

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve +=
            (s, e) => Assembly.ReflectionOnlyLoad(e.Name);

            try
            {
                var dllReflection = System.Reflection.Assembly.ReflectionOnlyLoadFrom(dll.FullName);
                dllTypes = dllReflection.GetTypes();
            }
            catch(Exception ex)
            {
                return false;
            }

            foreach (var theType in dllTypes )
            {
                var attribs = CustomAttributeData.GetCustomAttributes(theType);
                if (CustomAttributeData.GetCustomAttributes(theType).Any(x => x.AttributeType.FullName == typeof(ObjectBuilderAttribute).FullName))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private FileInfo CopyToTemp(FileInfo dll)
        {
            string name = GetTempFilename(dll.FullName);
            dll.CopyTo(name);
            return new FileInfo(name);
        }

        private string GetTempFilename(string dllName)
        {
            var tempPath = Path.GetTempPath();
            var baseFile = Path.GetFileNameWithoutExtension(dllName);
            var extension = Path.GetExtension(dllName);
            var search = string.Format("{0}*.{1}", baseFile, extension);
            var existingFiles = Directory.GetFiles(tempPath, search);
            int i = 0;
            do
            {
                var tempFile = string.Format("{0}{1}{2}.{3}", tempPath, baseFile, i, extension);
                if (!existingFiles.Contains(tempFile))
                    return tempFile;
                i++;
            } while (i < int.MaxValue);

            return string.Empty;
        }

        private IEnumerable<Type> GetRelevantTypes(FileInfo shadowCopy, out System.Reflection.Assembly theDll)
        {
            List<Type> result;
            result = new List<Type>();
            theDll = Assembly.LoadFrom(shadowCopy.FullName);
            foreach (var theType in theDll.GetTypes())
            {
                if (CustomAttributeData.GetCustomAttributes(theType).Any(x => x.AttributeType.FullName == typeof(ObjectBuilderAttribute).FullName))
                {
                    result.Add(theType);
                }
            }

            return result;
        }

        internal bool HasType(string objectType, Type target)
        {
            if (!_dllTypes.ContainsKey(objectType.ToUpper()))
                return false;

            if (target == typeof(object))
                return true;

            var theType = GetType(objectType.ToUpper());
            if (theType == target)
                return true;

            if (theType.GetInterface(target.FullName) != null)
                return true;

            if (theType.GetNestedTypes().Contains(target))
                return true;
            
            return false;
        }

        internal Type GetType(string objectType)
        {
            if (!_dllTypes.ContainsKey(objectType.ToUpper()))
                return default(Type);

            return _dllTypes[objectType.ToUpper()];
        }
    }
}