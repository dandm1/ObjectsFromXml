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
            {
                X castItem = (X)innerItem;
                 castResults.Add(castItem);
            }

            return castResults;
        }

        public IEnumerable<object> Build()
        {
            _namedObjects = new Dictionary<string, object>(ExternalObjects);

            var result = new List<dynamic>();

            XElement root = (XElement)doc.FirstNode;
            if (root.Name != ROOT_NODE)
                Logger.Error(string.Format("Root node must be type {0}.", ROOT_NODE));

            try
            {
                MakeCommonParameters(root);
            }
            catch(Exception ex)
            {
                Logger.Error(string.Format("Error while making common parameters.  Error is {0}",ex),ex);
                return new List<object>();
            }

            try { 
            MakeResources(root);
        }
            catch(Exception ex)
            {
                Logger.Error(string.Format("Error while making resources.  Error is {0}", ex),ex);
                return new List<object>();
            }

    var objectNodes = root.Nodes().Where(x => ((XElement)x).Name == OBJECTS_NODE);
            if (objectNodes.Any())
            {
                try
                {
                    if (_outType == null)
                    {
                        DetermineOutputType(objectNodes);
                    }
                }
                catch(Exception ex)
                {
                    Logger.Error(string.Format("Error determining output object type."), ex);
                }

                var allNodes = objectNodes.SelectMany(x => (((XElement)x).Nodes()));
                if (!allNodes.Any())
                {
                    Logger.Error
                        (
                        string.Format("No output nodes found in input.  Input code is {0}",
                        root.ToString()
                        )
                        );
                }

                foreach (XElement node in allNodes)
                {
                    try
                    {
                        dynamic theJob = ConstructInstance(_outType, node);
                        result.Add(theJob);
                    }
                    catch(Exception ex)
                    {
                        Logger.ErrorFormat("Error constructing object from node {0}.  The error is:{1}", node.Value, ex);
                    }
                }
            }
            else
            {
                Logger.Error(string.Format("Error building objects - no node {0} found!", OBJECTS_NODE));
            }

            return result.AsEnumerable();
        }

        private void DetermineOutputType(IEnumerable<XNode> objectNodes)
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
                    var attribs = paramsXml.Attributes();
                    var keyTypeName = attribs.Where(x => x.Name == "KeyType").Select(x => x.Value).FirstOrDefault();
                    var valueTypeName = attribs.Where(x => x.Name == "KeyType").Select(x => x.Value).FirstOrDefault();
                    Type keyType = LibraryManager.Instance.GetType(keyTypeName);
                    Type valueType = LibraryManager.Instance.GetType(valueTypeName);
                    result = ConstructDictionaryInstance(objectType, keyType, valueType, paramsXml);
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
            dynamic result;
            
            var genericListType = typeof(Dictionary<,>).MakeGenericType(keyTarget,valueTarget);
            result = Activator.CreateInstance(genericListType);

            foreach (XElement item in paramsXml.Nodes())
            {
                XElement keyElement = FindAttributeOrDescendant(item, "Key", keyTarget);
                XElement valueElement = FindAttributeOrDescendant(item, "Value", valueTarget);
                dynamic childKey = ConstructInstance(keyTarget, keyElement);
                dynamic childValue = ConstructInstance(valueTarget, valueElement);
                result[childKey] = childValue;
            }

            return result;
        }

        private XElement FindAttributeOrDescendant(XElement item, string name, Type outType)
        {
            var decendants = item.Descendants();
            var descMatch = decendants.Where(x => x.Name == name).FirstOrDefault();

            if (descMatch != null)
            {
                return ValueAsXElement(outType, descMatch);
            }

            var attributes = item.Attributes();
            var matchAttrib = attributes.Where(x => x.Name == name).FirstOrDefault();

            if (matchAttrib != null)
                return new XElement(outType.Name, matchAttrib.Value );

            if (name == "Key")
            {
                string itemName = item.Name.ToString();
                return new XElement(outType.Name, itemName);
            }
            else if (name == "Value")
            {
                return ValueAsXElement(outType, item);
            }

            return null;
        }

        private static XElement ValueAsXElement(Type outType, XElement descMatch)
        {
            var firstElement = descMatch.FirstNode;
            if (typeof(XElement).IsAssignableFrom(firstElement.GetType()))
                return (XElement)descMatch.FirstNode;
            else
                return new XElement(outType.Name, firstElement);
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
            if (targetType == null)
                throw new Exception(string.Format("Unable to resolve object type when constructing object.  No type {0} was found that supports type {1}.",objectType,target.Name));

            var textNode = paramsXml.Nodes().Where(x => x.GetType() == typeof(XText)).FirstOrDefault();
            string content;
            if (textNode == null)
                content = string.Empty;
            else
                content= ((XText)textNode).Value;

            dynamic result = null;
            try
            {
                result = ApplyConstructor(targetType, content);
            }
            catch(Exception ex)
            {
                throw new Exception(string.Format("Error while applying constructor for type {0} with content '{1}'.",targetType,content),ex);
            }

            try
            { 
                AddCommonParameters(result, targetType, paramsXml);
            }
            catch(Exception ex)
            {
                throw new Exception(string.Format("Error while adding common parameters to type {0}", targetType),ex);
            }

            try
            { 
                AddAttributes(result, targetType, paramsXml);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error while adding attributes to type {0}.", targetType), ex);
            }

            try
            { 
                AddDescendants(result, targetType, paramsXml);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error while adding descendants to type {0}.", targetType), ex);
            }

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
                try
                {
                    result = Activator.CreateInstance(targetType, content);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error creating instance of {0} using a parameter of {1}.",targetType.Name,content), ex);
                }
            }
            else if (zeroParameterConstructors.Any())
            {
                try
                {
                    result = Activator.CreateInstance(targetType);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error creating instance of {0} using a constructor with no parameters.", targetType.Name), ex);
                }

                try
                {
                    AddContentParameter(content, result);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error adding content '{1}' as a content property of type {0}.", targetType.Name, content), ex);
                }
            }
            else if (content.Length > 0 || targetType == typeof(string))
            {
                try
                {
                    result = Convert.ChangeType(content, targetType);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error constructing object.  Type {1} has no 0 parameter constructors, no constructors with a single parameter to which '{0}' can be assigned and an error occured while converting value '{0}' directly to type {1}.", content, targetType.Name), ex);
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
            LibraryManager libManager;
            try
            {
                libManager = LibraryManager.Instance;
            }
            catch(Exception ex)
            {
                throw new Exception("Exception encountered while accessing library of DLLs and classes.", ex);
            }

            if (!libManager.HasType(objectType, target))
            {
                throw new Exception(string.Format("No type named {0} available which can be used as a {1}.", objectType, target.Name));
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
                    dynamic value = null;
                    if (propType.IsAssignableFrom(parameter.Value.GetType()))
                        value = parameter.Value;
                    else
                    {
                        try
                        {
                            value = Convert.ChangeType(parameter.Value, propType);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Error converting parameter value '{0}' to type {1} when setting parameters", parameter.Value, propType.Name), ex);
                        }
                    }
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
                    try
                    {
                        var theProperty = matchingProperties.First();
                        if (theProperty.CanWrite)
                        {
                            var propType = theProperty.PropertyType;
                            var value = Convert.ChangeType(parameter.Value, propType);
                            theProperty.SetValue(result, value);
                        }
                        else
                        {
                            Logger.WarnFormat("Found value '{0}' for parameter {1} on object type {2} in node {3}.  This cannot be assigned as parameter {1} is read only.",parameter.Value,parameter.Name,targetType.Name,paramsXml.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Error assigning value {0} to property {1} on type {2}",parameter.Value,parameter.Name,targetType.Name),ex);
                    }
                }
                else if (resAsExtraParams != null)
                {
                    resAsExtraParams.AddParameter(parameter.Name.LocalName, parameter.Value);
                }
                else
                {
                    Logger.WarnFormat("Unable to assign value '{0}' to object {1}.  No property named {2} is available and the object does not take additional parameters.", parameter.Value, targetType.Name, parameter.Name);
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
                if (desc == null)
                {
                    Logger.WarnFormat("Found content {0} in XML element {1} that cannot be converted to a node.  Skipping",desc.Value,paramsXml.Value);
                    continue;
                }
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
                        if (propType.GetInterfaces().Contains(typeof(System.Collections.IDictionary)) && propType.IsGenericType)
                        {
                            Type keyType = propType.GetGenericArguments()[0];
                            Type valueType = propType.GetGenericArguments()[1];
                            var value = ConstructDictionaryInstance(DICTIONARY_TYPE, keyType, valueType, (XElement)desc);
                        }
                        else if (propType.IsArray || isGenericList)
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
                    else
                    {
                        Logger.WarnFormat("Found value '{0}' for parameter {1} on object type {2} in node {3}.  This cannot be assigned as parameter {1} is read only.", descXn.ToString(), theProperty.Name, targetType.Name, paramsXml.Value);
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
