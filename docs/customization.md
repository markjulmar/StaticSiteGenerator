MDPGen supports custom extensions to the Markdown processing that can be added to your pages either through C# scripts (Roslyn), or through compiled classes included in a referenced DLL assembly. In both cases, the job of the extension is to take a set of optional parameters and produce HTML in response which is then used to replace the extension call in the final produced document.

## Running an extension in a Markdown page
Extensions are always prefaced with '@' in the Markdown and must start at the beginning of the line, excluding whitespace.

> Content will not be identified as an extension if it's not at the beginning of the line.

Here are some rules about finding extensions:

- Extension names are case-insensitive, so `@MyExtension` and `@myExtension` would resolve to the same extension.
- The parser searches the C# script extension collection first, followed by the compiled class extensions. If the given extension name is not found in either area, an exception is thrown.
- The parser will search for a type with that specific name, and then for a type with the name + "Extension" suffix. So, "@My" and "@MyExtension" would resolve to the same extension.
- When parsed, the extension expects an '(' and ')' to bracket parameters. These are mandatory - even if there are no parameters (in that case, you would have "()").
- The extension is parsed up to the closing brace, all text remaining on that line is ignored.
- Parameters can be on separate lines - the parser will continue reading until the closing ')' is found.

### Passing parameters to extensions
Parameters are parsed in several ways:

- Strings must be surrounded with either double quotes ("") or single quotes ('').
- Numerics are parsed as int, long and double based on the value encountered.
- Arrays are defined by surrounding comma-delimited values with [ ]. For example, a string array can be defined like this: `[ "one", "two", 'three' ]`.
- Markdown can be passed by surrounding the whole markdown block with braces `{ markdown here }`; the parser reads up to the brace and supports multiple lines of content.
- C# types can be created by expressing the object in JSON format: `{ name: "Mark", age: 48, weight: 165.65, Dob: 12/25/2017 }`
- Arrays of types are also supported using JSON to represent the type.

> **Note:** Notice that we are allowing a more lenient JSON syntax above, normally the properties must also be quoted. Our JSON parser supports both quoted and unquoted name values.

Here are some examples:

```md
@AddVideo("youtube_identifier")

@Accordion( [ "Header 1", "Header 2", ...], [ { Markdown content },{ Markdown content },...])

@NavigationButtons("<< Previous", "Next >>")

@CollapsingBlock({ Markdown Block })
```

When parameters are passed into the extension type itself, the format they are coerced to depends on the way the extension is executed.

#### C# Script extensions
C# script extensions always receive the parameters to the script as `string` types. Json values and arrays are passed in as literal strings which must be parsed out to the proper types as necessary. This is because C# scripts are always dynamic and there's no way to determine the parameter types at compile-time.

To convert parameters, you can use `Newtonsoft.Json` which is automatically referenced.

#### Compiled C# extensions
Compiled extensions coerce parameters at runtime based on the constructors available. The engine will scan the constructors and find the most logical fit based on the # of parameters and identified types.

## Writing extensions

## C# Scripts
C# scripts are the easiest way to build an extension. We utilize the open-source Roslyn C# scripting support. MDPGen supports all C# 7 keywords and each script you execute is run independently - so no script interferes with another.

### Script configuration
Scripts must be _configured_ in the `StaticSiteGenerator` class, either programmatically, or through a `site.json` file. Here is an example of the `site.json` configuration - this corresponds to `ScriptConfiguration` property exposed on the generator class.

```json
"scriptConfig": {
    "namespaces": [ "System.Data" ],
    "assemblies": [ "System.Data", "System.Web" ],
    "scriptsFolder": "scripts",
    "hooks": {
        "onPageInit": "onPageInitHook"
    }
},
```

| Tag/Property | Description |
|--------------|-------------|
| `namespaces` | This is a collection of `string`s which identify the C# namespaces that will be automatically added to every script that is executed. These represent `using` statements. |
| `assemblies` | Another collection of `string`s which identify the `Assembly` references that will be added to every script that is executed. |
| `scriptsFolder` | This is the folder where all the C# scripts are located in the content. |
| `hooks`      | This is a set of optional scripting hooks you can be notified about. |

The `hooks` collection is a `ScriptHooks` object that provides extension points that can execute scripting code while the page processing is being performed.

| Hook             | Description |
|------------------|-------------|
| `OnPageInitHook` | This hook is called right before each Markdown page is processed. It provides an opportunity to add new tokens to the replacement token collection and to process custom Yaml header tags. |

### Scripts folder
All C# scripts must be included in the identified `scripts` folder. This folder is scanned at tool startup and all `.cs` files contained in this folder are loaded into the scripting engine.

### References
The script engine will automatically include core MDPGen assemblies, `System.Dynamic`, and any custom type assemblies. This allows scripts to get to code defined in most common assemblies being used. You can also add references through the `#r` directive at the top of the script file.

For example, the following would load `System.Data.dll`, `System.Xml.dll`, and `System.Xml.Linq`.

```csharp
#r "System.Data"
#r "System.Xml"
#r "System.Xml.Linq"

var name = "Mark";
```

### Namespace imports
The script engine will automatically import several namespaces in every script. Conceptually, this means every script starts with:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using MDPGen.Core;
using MDPGen.Core.Services;
using MDPGen.Core.Infrastructure;
```

These core namespaces include the base things that most MDPGen scripts need when building the site. You can also add custom imports at the top of the script through normal `using` statement blocks. For example:

```csharp
#r "System.Data"

using System.Data;

var name = "Mark";
```

Would bring the core types from `System.Data` into the script.

### Built-in arguments
When scripts are executed by MDPGen, they are passed a set of built in parameters/arguments which are accessible through global values in the script.

| Value           | Purpose |
|-----------------|---------|
| ServiceProvider | This is an implementation of `IServiceProvider` which allows the script to locate other services provided by the MDPGen tools. |
| Page | This is the current `ContentPage` being rendered. |
| Args | This is the formal list of parameters being passed to the script that were parsed out of the Markdown page |
| ViewBag | A `dynamic` type which provides a _session_ specific to this one page being generated. This is shared with the Razor rendering engine to pass information to the final rendering of the page. |
| Tokens | The [ITokenCollection](./services/itokencollection.md) of values collected. |
| IdGen | A unique identifier generation service ([IIdGenerator(./services/iidgenerator.md)]) which can be used to ensure any dynamic HTML ids are unique within the page. |

#### Passed parameters
One of the built-in arguments passed is the `Args` property. This is a collection of arguments passed from the Markdown page parsing into the script. It is an untyped `dynamic` object created based on the parsed elements encountered in the Markdown so the values contained here will be based on the callee.

| Value | Purpose |
|-------|---------|
| Args.Count | # of arguments passed |
| Args.p1 | First parameter |
| Args.p2 | Second parameter |
| Args.pn | nth parameter |

All of the parameter values except `Count` are `string` types and must be coerced or parsed into the expected type (number, object, etc.). Many scripts commonly using `Newtonsoft.Json` to do this parsing - although any technique is available.

For example, assuming we executed a script with the following parameters:

```md
@MyExtension([ {'name': 'Mark', 'age': 21}, {'name': 'Adrian', 'age': 21} ])
```

We could handle this in our script like this:

```csharp
using "Newtonsoft.Json";

public class Data
{
    public string Name {get;set;}
    public int Age {get;set;}
}

// First parameter is list of peeps
List<Data> people = JsonConvert.Deserialize<List<Data>>(Args.p1);
foreach (var person in people)
{
    ...
}
```

> Note that when you define custom types in scripts, you are not in the global scope - therefore any built-in values you want to use must be _passed_ into the defined classes as part of the script. For example `IdGen`.

### Returning values
All C# scripts used as extensions must either return no value (e.g. no `return` statement), `null`, or a final value which will be used as HTML. The engine will automatically perform a `ToString` on any supplied return value.

If a return value is supplied, the extension call in the Markdown page will be replaced with the return value text. Otherwise, the extension call will just be removed from the resulting output.

## Compiled C# Types
The second type of extension that can be used are .NET types which implement the `IMarkdownExtension` interface and are used at runtime to generate dynamic HTML from passed parameters. These are located in custom assemblies and can be identified in the **site.json** file under the `extensions` section:

```json
"extensions": [ "XamU.SGL.Extensions" ]
```

Each assembly in the given Json array is loaded by name - which can either be a simple name, or the full assembly name. The runtime engine will scan all the types in each assembly and look for `IMarkdownExtension` implementations. Each identified type will then be available to any page being rendered.

The interface looks like this:

```csharp
namespace MDPGen.Core
{
    /// <summary>
    /// XamU: interface to extension the Markdown parser.
    /// </summary>
    public interface IMarkdownExtension
    {
        /// <summary>
        /// Process the extension
        /// </summary>
        /// <returns>HTML content to inject, null if none.</returns>
        string Process(IServiceProvider provider);
    }
}
```

When a compiled extension is invoked from the page rendering, a few things happen. First, the type is scanned to identify the proper constructor to use based on the parameters in the Markdown. For example given the following basic extension:

```csharp
public class UppercaseExtension : IMarkdownExtension
{
    string[] values;

    public UppercaseExtension(string value)
    {
        this.values = value.ToArray();
    }

    public UppercaseExtension(string[] values)
    {
        this.values = values;
    }

    public string Process(IServiceProvider provider)
    {
        return String.Join(" ", this.values.Select(v => v.ToUpper()));
    }
}
```

> **Note:** the above is a bad example of a compiled extension. Generally speaking, the compiled extension model is for more complex extensions which have a lot of code. The above could be easily accomplished through a scripting extension instead.

Depending on the parameters passed in through the Markdown page, we would invoke one of the two constructors. For example:

```md
@Uppercase("one")
```

Would result in a call to the single `string` constructor, whereas:

```md
@Uppercase([ "one", "two", "three" ]);
```

would call the `string[]` based constructor. Once the extension is instantiated, then the runtime engine will call `Process` and pass in the service provider. The service provider gives the extension access to all the internal services of MDPGen. 

Extensions can retrieve services through the `GetService<T>` extension method, or through the `GetService(Type)` method. Here's an example:

```csharp
dynamic pageCache = provider.GetService<DynamicPageCache>();
ContentPage current = pageCache.CurrentPage;

var tokens = provider.GetService<ITokenCollection>();
tokens["Test"] = ...;
```

Currently, the list of accessible services include the following object types:

| Type              | Description |
|-------------------|-------------|
| [IMarkdownParser](./services/IMarkdownParser.md) | The markdown parser object which can be used to translate Markdown to HTML for the extension. |
| [ITokenCollection](./services/ITokenCollection.md) | The token collection (read-write) which contains all the replaceable tokens which can be merged into the final page templates. |
| [IIdGenerator](./services/IIdGenerator.md) | Unique id generator which can create unique HTML id and names for a page. |
| `DynamicPageCache` | Dynamic ViewBag object which can be used to pass data into the rendering HTML pipeline. |

### Returning values
Compiled extensions are called through the `Process` method which returns a `string`. This return value is used as the replacement HTML to represent the extension and parameters. The extension can also return `null` to indicate "no result". In this case, the extension call will just be removed from the resulting output.

### Implicit extensions
Most extensions are invoked explicitly - directly on a page. However, in some cases, a site might need an extension to run on _every_ page without being called. A common example is to generate a navigation tree, or a set of navigation buttons.

Compiled extensions can be setup to be implicitly executed on each page through the use of a method-level attribute: `[ExtensionInit]` applied to a static method with the following signature:

`public static void Method_Name(IServiceProvider provider)`

In these cases, the static method is called as part of the page rendering. The method can then influence page tokens, add items to the `ViewBag`, etc. to affect the output from the HTML rendering. Notice that there is no return value - this form of invoke is not intended to generate HTML in a specific location on the page. Instead, it's intended to manipulate replacement tokens which will then get rendered into the final page template.
