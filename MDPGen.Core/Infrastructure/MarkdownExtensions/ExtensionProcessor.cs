using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using System.IO;
using MDPGen.Core.Data;

namespace MDPGen.Core.MarkdownExtensions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExtensionInitAttribute : Attribute
    {
        // Must be applied to an Action type.
    }

    /// <summary>
    /// This class provides the extension support using .NET types.
    /// </summary>
    /// <example>
    /// @Name(param1,param2,...) is mapped to NameExtension and parameters are mapped to the constructor.
    /// </example>
    public class ExtensionProcessor
    {
        private const string ExtensionSuffix = "Extension";

        private static readonly List<Type> availableExtensions = new List<Type>();

        private static CSharpScriptRunner scriptRunner;

        private int currentPos;
        private string content;

        private readonly Tuple<char, char>[] jsonBlocks = {
                    Tuple.Create('{', '}'),
                    Tuple.Create('[', ']'),
                    Tuple.Create('\'', '\''),
                    Tuple.Create('"', '"')
                };

        /// <summary>
        /// The available extensions we have located.
        /// </summary>
        public static IReadOnlyList<Type> AvailableExtensions => availableExtensions;

        /// <summary>
        /// Remove all extensions
        /// </summary>
        public static void Reset()
        {
            availableExtensions.Clear();
        }

        /// <summary>
        /// Initialize the C# scripts engine.
        /// </summary>
        /// <param name="folder">Folder to load scripts from</param>
        /// <param name="assemblies">Assemblies to load</param>
        /// <param name="namespaces">Namespaces to add to C# script context</param>
        public static IEnumerable<string> InitializeScripts(string folder, IEnumerable<Assembly> assemblies, IEnumerable<string> namespaces = null)
        {
            scriptRunner = null;

            if (Directory.Exists(folder))
            {
                var requiredNamespaces = namespaces?.Union(ScriptConfig.RequiredNamespaces) 
                    ?? ScriptConfig.RequiredNamespaces;

                scriptRunner = new CSharpScriptRunner(folder, assemblies, 
                    requiredNamespaces.Distinct().ToList());
                return scriptRunner.EnumerateLoadedScripts();
            }

            return null;
        }

        /// <summary>
        /// Initialize the extension engine with a list of extension types.
        /// </summary>
        /// <param name="extensions">Variable list of types to add to extensions</param>
        public static void Init(params Type[] extensions)
        {
            var existing = availableExtensions.ToList();
            availableExtensions.AddRange(extensions.Distinct()
                .Where(e => !existing.Contains(e)));
        }

        /// <summary>
        /// Initialize the extension engine using a set of assemblies.
        /// This code will scan the assemblies and look for IMarkdownExtension implementations.
        /// </summary>
        /// <param name="assemblies">List of assemblies</param>
        public static void Init(params Assembly[] assemblies)
        {
            var existing = availableExtensions.ToList();
            foreach (var asm in assemblies)
            {
                availableExtensions.AddRange(asm.ExportedTypes
                    .Where(t => !existing.Contains(t) && t.GetTypeInfo().ImplementedInterfaces
                    .Contains(typeof(IMarkdownExtension))));
            }
        }

        /// <summary>
        /// This is used to run all the initializers on all the registered extensions.
        /// </summary>
        /// <param name="pageVars">Page variables to use during initialization</param>
        /// <param name="scriptInitFile">Optional script initialization hook</param>
        public static void InitializeExtensions(PageVariables pageVars, string scriptInitFile = null)
        {
            // Run any C# local script initialization
            if (!string.IsNullOrEmpty(scriptInitFile))
            {
                RunScript(scriptInitFile, pageVars);
            }

            // Locate all compiled extensions and execute the static ones.
            foreach (var ext in availableExtensions)
            {
                var method = ext.GetTypeInfo().DeclaredMethods
                    .FirstOrDefault(mi => mi.IsStatic
                            && mi.GetParameters().Length == 1
                            && mi.GetParameters()[0].ParameterType == typeof(IServiceProvider)
                            && mi.GetCustomAttribute<ExtensionInitAttribute>() != null);
                method?.Invoke(null, new object[] { pageVars });
            }
        }

        /// <summary>
        /// Runs a C# script by name with a given set of parameters.
        /// </summary>
        /// <param name="name">Name of script (filename)</param>
        /// <param name="pageVars"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object RunScript(string name, PageVariables pageVars, params string[] parameters)
        {
            if (pageVars == null)
            {
                throw new ArgumentNullException(nameof(pageVars));
            }

            name = Path.GetFileNameWithoutExtension(name) ?? throw new ArgumentNullException(nameof(name));

            return scriptRunner.RunScript(name, pageVars, parameters?.ToList());
        }

        /// <summary>
        /// Executes an extension. This method will parse a @block, locate the proper extension by name and return the output.
        /// </summary>
        /// <param name="pageVars">Service Provider</param>
        /// <param name="reader">Start of the extension</param>
        /// <returns>Raw output to add to HTML; null if extension was not found or no output was produced.</returns>
        public string Run(PageVariables pageVars, LineReader reader)
        {
            // First parse out the block.
            var extensionLine = Parse(reader);
            if (extensionLine == null)
                return null;

            currentPos = 0;
            content = extensionLine;

            // Parse out the extension type.
            string name = content.GrabTo(currentPos, '(');
            if (name == null)
                return null;

            // Skip the name and parse the parameters.
            currentPos += name.Length;
            Skip(1);
            SkipBlanks();

            // Read out parameters - can be string, number, or JSON object
            List<string> parameters = ParseParameters().ToList();

            // Check scripts first. 
            if (scriptRunner?.HasScript(name) == true)
            {
                var result = scriptRunner.RunScript(name, pageVars, parameters);
                return result?.ToString();
            }
            // Not found. Find the compiled extension and use it instead.
            else
            {
                Type extensionType = AvailableExtensions?
                    .SingleOrDefault(t => string.Equals(t.Name, name, StringComparison.CurrentCultureIgnoreCase));
                if (extensionType == null && !name.EndsWith(ExtensionSuffix))
                {
                    name += ExtensionSuffix;
                    extensionType = AvailableExtensions?
                        .SingleOrDefault(t => string.Equals(t.Name, name, StringComparison.CurrentCultureIgnoreCase));
                }

                if (extensionType == null)
                    throw new ArgumentException($"No markdown extension found named {name}");

                var extension = InstantiateExtension(extensionType, parameters);
                return extension?.Process(pageVars);
            }
        }

        /// <summary>
        /// Instantiates the IMarkdownExtension type using the passed parameters.
        /// This uses reflection to identify a constructor on the given type and
        /// creates it.
        /// </summary>
        /// <param name="extensionType">Extension type to use.</param>
        /// <param name="parameters">Parameter list in the form of strings.</param>
        /// <returns>IMarkdownExtension if it was created</returns>
        IMarkdownExtension InstantiateExtension(Type extensionType, List<string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return Activator.CreateInstance(extensionType) as IMarkdownExtension;

            TypeInfo ti = extensionType.GetTypeInfo();
            // Look for a constructor.
            var availableCtors = ti.DeclaredConstructors.Where(ci =>
                    ci.IsPublic && ci.GetParameters().Length == parameters.Count).ToList();

            // If we have a Ctor which takes all strings, move it to the end of our list so we check it _last_
            // This is because when we parse, we end up with strings for all parameters - so that ctor will ALWAYS match.
            var allStringCtor =
                availableCtors.SingleOrDefault(ct => ct.GetParameters().All(p => p.ParameterType == typeof(string)));
            if (allStringCtor != null)
            {
                availableCtors.Remove(allStringCtor);
                availableCtors.Add(allStringCtor);
            }

            // Walk through and try each constructor - we use the first one which
            // fits our parameter set.
            foreach (var ctor in availableCtors)
            {
                var cParams = ctor.GetParameters();
                try
                {
                    List<object> theParams = new List<object>();
                    for (int index = 0; index < cParams.Length; index++)
                    {
                        var pt = cParams[index];
                        var tp = parameters[index];

                        // Try an ENUM first.
                        if (pt.ParameterType.GetTypeInfo().IsEnum)
                        {
                            try
                            {
                                theParams.Add(Enum.Parse(pt.ParameterType, tp, true));
                                // Success!
                                continue;
                            }
                            catch
                            {
                                // Ignore
                            }
                        }

                        if (pt.ParameterType == typeof(bool))
                        {
                            theParams.Add(bool.Parse(tp));
                        }
                        else if (pt.ParameterType == typeof(int))
                        {
                            theParams.Add(int.Parse(tp));
                        }
                        else if (pt.ParameterType == typeof(long))
                        {
                            theParams.Add(long.Parse(tp));
                        }
                        else if (pt.ParameterType == typeof(double))
                        {
                            theParams.Add(double.Parse(tp));
                        }
                        else if (pt.ParameterType == typeof(string))
                        {
                            theParams.Add(tp);
                        }
                        else
                        {
                            theParams.Add(Newtonsoft.Json.JsonConvert
                                .DeserializeObject(tp, pt.ParameterType));
                        }
                    }
                    return ctor.Invoke(theParams.ToArray()) as IMarkdownExtension;
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    // Ignore
                }
                catch (Exception ex)
                {
                    TraceLog.Write(TraceType.Diagnostic, $"Caught {ex.GetType().Name} creating {extensionType.Name}: {ex.Message}");
                }
            }

            throw new ArgumentException($"Unable to locate constructor for {extensionType.Name} which takes: " + string.Join(",", parameters));
        }

        /// <summary>
        /// Walks the parameter list (between the () elements) and returns a set of strings.
        /// </summary>
        /// <returns>Unique string for each parameter</returns>
        private IEnumerable<string> ParseParameters()
        {
            while (currentPos < content.Length)
            {
                char ch = content[currentPos];
                if (ch == ')')
                    yield break;
                if (ch == ',')
                    Skip(1);

                SkipBlanks();
                ch = content[currentPos];
                switch (ch)
                {
                    case '"': // string literal
                    case '\'':
                        Skip(1);
                        string strData = content.GrabTo(currentPos, ch);
                        if (strData == null)
                            yield break;

                        currentPos += strData.Length;
                        SkipTo(",)");
                        yield return strData;
                        break;
                    case '{': // JSON object
                        var js = ParseJsonData('}');
                        if (js == null)
                            yield break;
                        yield return js;
                        break;
                    case '[': // JSON array
                        var ja = ParseJsonData(']');
                        if (ja == null)
                            yield break;
                        yield return ja;
                        break;
                    default: // Number or bool
                        if (char.IsDigit(ch))
                        {
                            yield return ParseNumber();
                            break;
                        }
                        if (string.Equals(PeekChars(4), "true", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Skip(4);
                            yield return "true";
                        }
                        else if (string.Equals(PeekChars(5), "false", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Skip(5);
                            yield return "false";
                        }
                        // Hmm.. unknown.
                        else yield break;
                        break;
                }
            }
        }

        private string PeekChars(int count)
        {
            int len = Math.Min(count, content.Length - currentPos);
            return content.Substring(currentPos, len);
        }

        /// <summary>
        /// Parses a number from the parameter list.
        /// </summary>
        /// <returns>String with the number (integer or double)</returns>
        private string ParseNumber()
        {
            StringBuilder sb = new StringBuilder();
            while (currentPos < content.Length)
            {
                char ch = content[currentPos];

                if (char.IsDigit(ch) || ch == '.')
                    sb.Append(ch);
                else break;

                currentPos++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses a JSON block - either an array or JSON object.
        /// </summary>
        /// <param name="lookFor">JSON ending character</param>
        /// <returns>String with entire JSON block</returns>
        private string ParseJsonData(char lookFor)
        {
            int start = currentPos;
            Skip(1);

            int end = content.SmartIndexOf(currentPos, lookFor, jsonBlocks);
            if (end == -1)
                return null;

            end++;
            currentPos = end;

            return content.Substring(start, end - start);
        }

        /// <summary>
        /// Skips "n" characters from the input
        /// </summary>
        /// <param name="count"># of characters to skip (can be negative)</param>
        void Skip(int count)
        {
            currentPos += count;
        }

        /// <summary>
        /// Skips whitespace/blanks in the input.
        /// </summary>
        void SkipBlanks()
        {
            while (content.Length > currentPos
                   && content[currentPos] == ' ')
                currentPos++;
        }

        /// <summary>
        /// Skips the input up to a specific character.
        /// </summary>
        /// <param name="chars">List of characters to look for</param>
        void SkipTo(string chars)
        {
            while (content.Length > currentPos
                   && chars.Contains(content[currentPos]) == false)
                currentPos++;
        }

        /// <summary>
        /// Parser to extract a single extension call from Markdown block.
        /// </summary>
        /// <param name="reader">Block Processor</param>
        /// <returns>Entire extension call</returns>
        private string Parse(LineReader reader)
        {
            StringBuilder sb = new StringBuilder();

            // Skip any blanks
            reader.SkipLinespace();

            // Sitting on '@' marker?
            if (reader.Current == '@')
                reader.Skip(1);

            // Locate the parameter list.
            while (reader.Current != '(' && !reader.IsEof)
            {
                if (!char.IsLetterOrDigit(reader.Current))
                    return null;

                sb.Append(reader.Current);
                reader.Skip(1);
            }

            if (reader.IsEof)
                return null;

            // Add starting paren
            sb.Append(reader.Current);
            reader.Skip(1);

            // Skip any blanks to 1st parameter
            reader.SkipLineSpaceAndEol();

            if (reader.IsEof)
                return null;

            const string lookFor = "'\"{}";
            Stack<char> openQuoteTypes = new Stack<char>();
            while (!reader.IsEof && (reader.Current != ')' || openQuoteTypes.Count > 0))
            {
                if (lookFor.IndexOf(reader.Current) >= 0)
                {
                    if (openQuoteTypes.Count > 0)
                    {
                        char lastQuoteType = openQuoteTypes.Peek();
                        if ((lastQuoteType != '{' && reader.Current == lastQuoteType) ||
                            (lastQuoteType == '{' && reader.Current == '}'))
                        {
                            openQuoteTypes.Pop();
                        }
                        else if (reader.Current == openQuoteTypes.Peek())
                        {
                            if (reader.IsBof || reader[reader.CurrentPos - 1] != '\\')
                                openQuoteTypes.Push(reader.Current);
                        }
                    }
                    else
                    {
                        openQuoteTypes.Push(reader.Current);
                    }
                }

                if (openQuoteTypes.Count > 0 || !reader.SkipLineSpaceAndEol())
                {
                    sb.Append(reader.Current);
                    reader.Skip(1);
                }
            }

            if (openQuoteTypes.Count > 0)
            {
                TraceLog.Write(TraceType.Warning, $"{openQuoteTypes.Count} open block(s) found while parsing extension, last open char was {openQuoteTypes.Peek()}.");
            }

            if (reader.IsEof)
                return null;

            sb.Append(reader.Current);
            reader.SkipToEol();

            return sb.ToString();
        }
    }
}

