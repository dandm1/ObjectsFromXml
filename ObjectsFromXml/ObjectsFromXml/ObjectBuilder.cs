using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using log4net;

namespace ObjectsFromXml
{
    public class ObjectBuilder
    {
        private Stream fileStream;
        private XDocument doc;
        private const string ARRAY_TYPE = "Array";
        private const string LIST_TYPE = "List";
        private const string DICTIONARY_TYPE = "Dictionary";
        private const string ENUMERABLE_TYPE = "Enumerable";
        private const string REF_TYPE = "Ref";
        private const string ROOT_NODE = "ObjectSet";
        private const string COMMON_NODE = "Common";
        private const string RESOURCE_NODE = "Resources";
        private const string OBJECTS_NODE = "Objects";

        private Dictionary<string, object> _namedObjects;
        private Dictionary<string, dynamic> _commonParameters;
        private Type _outType;

        private ILog _logger;
        private ILog Logger
        {
            get
            {
                if (_logger == null)
                    _logger = LogManager.GetCurrentLoggers()[0];

                return _logger;
            }

        }

        public ObjectBuilder(Stream fileStream) : this (fileStream,null)
        { }

        public ObjectBuilder(Stream fileStream,Type outType)
        {
            this.fileStream = fileStream;
            doc = XDocument.Load(fileStream);
            _outType = outType;
        }

        public ObjectBuilder(string content)
        {
            doc = XDocument.Load(content);
        }

        private Dictionary<string,dynamic> _externalObjects;
        public Dictionary<string,dynamic> ExternalObjects
        {
            get
            {
                if (_externalObjects == null) _externalObjects = new Dictionary<string, dynamic>();
                return _externalObjects;
            }
            set { _externalObjects = value; }
        }

        public IEnumerable<X> Build<X>()
        {
            if (_outType != null)
            {
                if (typeof(X) != _outType)
                    Logger.Error(string.Format("Cannot use genric output with type {0} with out type of {1}.",typeof(X).FullName,_outType.FullName));
            }
            else
                _outType = typeof(X);

            var innerList = Build();
            List<X> castResults = new List<X>();
            foreach (object innerItem in innerList)
                castResults.Add((X)innerItem);

            return castResults;
        }

        public IEnumerable<object> Build()
        {
            
            _namedObjects = new Dictionary<string, object>(ExternalObjects);

            var result = new List<dynamic>();

            XElement root = (XElement)doc.FirstNode;
            if (root.Name != ROOT_NODE)
                Logger.Error(string.Format("Root node must be type {0}.", ROOT_NODE));

            MakeCommonParameters(root);

            MakeResources(root);

            var objectNodes = root.Nodes().Where(x => ((XElement)x).Name == OBJECTS_NODE);
            if (objectNodes.Any())
            {
                if ( _outType == null)
                {
                    var typeAttrib = ((XElement)objectNodes.First()).Attribute("Type");
                    if (typeAttrib != null)
                    {
                        Type tempType = ResolveType(typeAttrib.Value, typeof(object));
                        if (tempType != null)
                            _outType = tempType;
                    }

                    if (_outType == null)
                        _outType = typeof(object);
                }

                var allNodes = objectNodes.SelectMany(x => (((XElement)x).Nodes()));
                foreach (XElement node in allNodes)
                {
                    dynamic theJob = ConstructInstance(_outType, node);
                    result.Add(theJob);
                }
            }

            return result.AsEnumerable();
        }

        private void MakeResources(XElement root)
        {
            var resourceNodes = root.Nodes().Where(x => ((XElement)x).Name == RESOURCE_NODE);
            if (resourceNodes.Any())
            {
                foreach (XElement node in (((XElement)resourceNodes.First()).Nodes()))
                {
                    ConstructInstance(typeof(object), node);
                }
            }
        }

        private void MakeCommonParameters(XElement root)
        {
            _commonParameters = new Dictionary<string, dynamic>();
            var commonNodes = root.Nodes().Where(x => ((XElement)x).Name == COMMON_NODE);
            
            if (commonNodes.Any())
            {
                var allCommonVariables = commonNodes.SelectMany(x => ((XElement)x).Nodes());

                foreach (XElement node in allCommonVariables)
                {
                    XElement paramsXml = (XElement)node.FirstNode;
                    _commonParameters[node.Name.LocalName] = ConstructInstance(typeof(object), paramsXml);
                }
            }
        }

        private void SaveObject(object theObject, XElement paramsXml)
        {
            var nameAttribute = paramsXml.Attribute("Name");
            if (nameAttribute != null)
            {
                var name = nameAttribute.Value;
                if (name.Length > 0)
                    _namedObjects[name] = theObject;
            }
        }

        private dynamic ConstructInstance(Type target,XElement paramsXml)
        {
            string objectType = paramsXml.Name.LocalName;
            dynamic result;

            switch (objectType)
            {
                case ARRAY_TYPE:
                case LIST_TYPE:
                case ENUMERABLE_TYPE:
                    result = ConstructListInstance(objectType, target, paramsXml);
                    break;
                case DICTIONARY_TYPE:
                    result = ConstructDictionaryInstance(objectType, target, target, paramsXml);
                    break;
                case REF_TYPE:
                    result = ConstructRefInstance(objectType, target, paramsXml);
                    break;
                default:
                    result = ConstructObjectInstance(objectType, target, paramsXml);
                    break;

            }
            SaveObject(result, paramsXml);

            return result;
        }

        private dynamic ConstructRefInstance(string objectType, Type target, XElement paramsXml)
        {
            var sourceName = paramsXml.Attribute("Source");
            if (sourceName == null)
            {
                Logger.Error("Ref object needs a source name.");
                return null;
            }

            if (!_namedObjects.ContainsKey(sourceName.Value))
            {
                Logger.Error(string.Format("No source object named {0} found.  Referred objects must be earlier in the input file.", sourceName.Value));
                return null;
            }

            dynamic result = _namedObjects[sourceName.Value];
            Type resultType = result.GetType();
            if (!target.IsAssignableFrom(resultType))
            {
                Logger.Error(string.Format("Error looking up reference type.  Target type {0} cannot be filled with a {1}",target.Name,resultType.Name));
                return null;
            }

            return result;
        }

        private dynamic ConstructDictionaryInstance(string objectType, Type keyTarget, Type valueTarget, XElement paramsXml)
        {
            throw new NotImplementedException();
        }

        private dynamic ConstructListInstance(string objectType, Type target, XElement paramsXml)
        {
            dynamic result;

            Type[] genericAttributes;
            if (target.IsGenericType)

                genericAttributes = target.GetGenericArguments();
            else if (target.IsArray)
            {
                genericAttributes = new Type[] { target.GetElementType() };
            }
            else
            {
                Logger.Error("Can only make generic lists and arrays.");
                return null;
            }

            if (genericAttributes.Length != 1)
            {
                Logger.Error("List type must have one generic argument.");
            }

            Type genericType = genericAttributes[0];
            
            var listOfTypeAttr = paramsXml.Attribute("Type");
            string listOfType;
            if (listOfTypeAttr == null)
                listOfType = genericType.Name;
            else
                listOfType = listOfTypeAttr.Value;

            Type targetType = ResolveType(listOfType, genericType);

            var genericListType = typeof(List<>).MakeGenericType(targetType);
            result = Activator.CreateInstance(genericListType);
            
            foreach(XElement descendant in paramsXml.Nodes())
            {
                dynamic child = ConstructInstance(genericType, descendant);
                result.Add(child);
            }

            switch(objectType)
            {
                case ARRAY_TYPE:
                    return result.ToArray();
                case ENUMERABLE_TYPE:
                    return result;
                default:
                    return result;
            }
        }

        private dynamic ConstructObjectInstance(string objectType,Type target, XElement paramsXml)
        {
            Type targetType = ResolveType(objectType, target);
            var textNode = paramsXml.Nodes().Where(x => x.GetType() == typeof(XText)).FirstOrDefault();
            string content;
            if (textNode == null)
                content = string.Empty;
            else
                content= ((XText)textNode).Value;

            dynamic result = ApplyConstructor(targetType, content);

            AddCommonParameters(result, targetType, paramsXml);

            AddAttributes(result, targetType, paramsXml);

            AddDescendants(result, targetType, paramsXml);

        return result;
        }

        private dynamic ApplyConstructor(Type targetType, string content)
        {
            dynamic result = null;
            var typeConstructors = targetType.GetConstructors();

            var zeroParameterConstructors = typeConstructors.Where(x => x.GetParameters().Count() == 0);
            var singleParameterConstructors = typeConstructors.Where(x => x.GetParameters().Count() == 1);


            if
                (
                content.Length > 0 &&
                singleParameterConstructors.Any
                (
                    x =>
                    (x.GetParameters()[0]).ParameterType.IsAssignableFrom(typeof(string))
                )
                )
            {
                result = Activator.CreateInstance(targetType, content);
            }
            else if (zeroParameterConstructors.Any())
            {
                result = Activator.CreateInstance(targetType);
                AddContentParameter(content, result);
            }
            else if (content.Length > 0 || targetType == typeof(string))
            {
                try
                {
                    result = Convert.ChangeType(content, targetType);
                }
                catch (Exception ex)
                {
                }
            }

            if ( result == null )
            {
                Logger.Error(string.Format("Cannot find appropriate constructor for type {0} with{1} content.",targetType.Name,content.Length == 0 ? " no" : ""));
            }

            return result;
        }

        private void AddContentParameter(string content, dynamic result)
        {
            var resAsExtraParams = result as IExtraParameters;
            if (resAsExtraParams != null && content.Length > 0)
            {
                resAsExtraParams.AddParameter("Content", content);
            }
        }

        private Type ResolveType(string objectType,Type target)
        {
            LibraryManager libManager = LibraryManager.Instance;

            if (!libManager.HasType(objectType, target))
            {
                Logger.Error(string.Format("No type named {0} available which implements interface {1}.", objectType, target.Name));
                return null;
            }

            return libManager.GetType(objectType);
        }

        private void AddCommonParameters(dynamic result, Type targetType, XElement paramsXml)
        {
            IEnumerable<PropertyInfo> targetProperties = targetType.GetProperties().AsEnumerable();

            var resAsExtraParams = result as IExtraParameters;
            foreach (var parameter in _commonParameters)
            {
                var matchingProperties = targetProperties.Where(x => x.Name == parameter.Key && x.CanWrite);
                if (matchingProperties.Any())
                {
                    var theProperty = matchingProperties.First();
                    var propType = theProperty.PropertyType;
                    dynamic value;
                    if (propType.IsAssignableFrom(parameter.Value.GetType()))
                        value = parameter.Value;
                    else
                        value = Convert.ChangeType(parameter.Value, propType);
                    theProperty.SetValue(result, value);
                }
                else if (resAsExtraParams != null)
                {
                    resAsExtraParams.AddParameter(parameter.Key, parameter.Value);
                }

            }
        }


        private void AddAttributes(dynamic result, Type targetType, XElement paramsXml)
        {
            IEnumerable<PropertyInfo> targetProperties = targetType.GetProperties().AsEnumerable();

            var resAsExtraParams = result as IExtraParameters;
            foreach (var parameter in paramsXml.Attributes())
            {
                var matchingProperties = targetProperties.Where(x => x.Name == parameter.Name);
                if (matchingProperties.Any())
                {
                    var theProperty = matchingProperties.First();
                    if (theProperty.CanWrite)
                    {
                        var propType = theProperty.PropertyType;
                        var value = Convert.ChangeType(parameter.Value, propType);
                        theProperty.SetValue(result, value);
                    }
                }
                else if (resAsExtraParams != null)
                {
                    resAsExtraParams.AddParameter(parameter.Name.LocalName, parameter.Value);
                }

            }
        }

        private void AddDescendants(dynamic result,Type targetType, XElement paramsXml)
        {
            IEnumerable<PropertyInfo> targetProperties = targetType.GetProperties().AsEnumerable();

            var resAsExtraParams = result as IExtraParameters;
            foreach (var descXn in paramsXml.Nodes())
            {
                XElement desc = descXn as XElement;
                if (desc == null) continue;

                var matchingProperties = targetProperties.Where(x => x.Name == desc.Name);
                if (matchingProperties.Any())
                {
                    var theProperty = matchingProperties.First();
                    if (theProperty.CanWrite)
                    {
                        var propType = theProperty.PropertyType;
                        var isGenericList = propType.IsGenericType && 
                            propType.GetInterfaces().Contains(typeof(System.Collections.IEnumerable)) &&
                            propType != typeof(string);
                        if (propType.IsArray || isGenericList)
                        {
                            string listType;
                            if (propType.IsArray)
                                listType = ARRAY_TYPE;
                            else if (propType.IsAssignableFrom(typeof(List<>)))
                                listType = LIST_TYPE;
                            else
                                listType = ENUMERABLE_TYPE;

                            var value = ConstructListInstance(listType, propType, (XElement)desc);
                            theProperty.SetValue(result, value);
                        }
                        else
                        {
                            var value = ConstructInstance(propType, (XElement)desc.FirstNode);
                            theProperty.SetValue(result, value);
                        }
                    }
                }
                else if (resAsExtraParams != null)
                {
                    var value = ConstructInstance(typeof(object), (XElement)desc.FirstNode);
                    resAsExtraParams.AddParameter(desc.Name.LocalName, value);
                }
            }
        }

        public void Dispose()
        {
            
        }
    }
}
