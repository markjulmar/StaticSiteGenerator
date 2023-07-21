# Defining a site
MDPGen creates a static HTML website from a _site configuration file_ and a set of linked metadata files named `meta.json`. The site configuration file contains a JSON block which provides all the necessary information to construct the site, and the `meta.json` files define the navigation structure and what pages will be generated for the site.

## Creating a new site
The easiest way to start a new site is to use the `MDPGen.exe` command-line tool to create an initial site. This will generate a single page site with the configuration file and meta.json to describe the page in the folder you run the command in.

```
C:\> MDPGen -i
Generating new site.

C:\> DIR /b
content
pageTemplate.html
siteInfo.json
```

## Site Configuration
You can have multiple site configuration files (for testing, local dev, and release configurations for example), but the `meta.json` files are used for all configurations - so they never change based on the build.

Here's an example site configuration file:

```json
{
    "defaultPageTemplate": "pageTemplate.cshtml",
    "contentFolder": "content",

    "searchFolders": [
        "templates",
        "include"
    ],

    "siteConstantsFilename": "templates/siteValues.json",

    "extensions": [
        "XamU.SGL.Extensions"
    ],

    "scriptConfig": {
        "namespaces": [
            "XamU.SGL.Extensions"
        ],

        "scriptsFolder": "scripts",

        "hooks": {
            "onPageInit": "onPageInitHook"
        }
    },

    "overrideTypes": {
        "metadataLoaderType": "XamU.SGL.Extensions.XamUPageMetadataLoader, XamU.SGL.Extensions"
    },

    "processingChain": [
        "MDPGen.Core.Blocks.IfDirective",
        "MDPGen.Core.Blocks.IncludeDirective",
        "MDPGen.Core.Blocks.RunMarkdownExtensions",
        "MDPGen.Core.Blocks.ConvertMarkdownToHtml",
        "MDPGen.Core.Blocks.RenderRazorTemplate",
        "MDPGen.Core.Blocks.ReplaceTokens",
        "MDPGen.Core.Blocks.MinifyAndVersion",
        "MDPGen.Core.Blocks.CompressHtml",
        "MDPGen.Core.Blocks.WriteFile"
    ],

    "constants": [
        { "key": "Urls.XamU", "value": "%XamarinUniversityUrl%" }
    ]
}
```

#### Page Template
The **defaultPageTemplate** property identifies the HTML template which will be used by default when a Markdown content file is parsed and emitted to HTML. The value indicates the relative path (from the site configuration file), or an absolute path (**not recommended if you want to do cross-platform generation**).

Each page will use this default template unless it overrides it using the [`template`](./markdown-syntax.md#yaml-header) YAML header value.

The page template is an HTML file with a set of place holders in it. Each placeholder is identified by `{{NAME}}` where **NAME** is the text key that identifies the content to put into this placeholders location. For example:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
 	<meta name="description" content="{{Constants.pageDescription}}" />
	<meta name="keywords" content="{{Constants.pageTags}}" />
    <title>My Blog | {{Constants.pageTitle}}</title>
</head>
<body>
	<h1>This is a title</h1>
	{{Content.body}}
</body>
</html>

```

In this fragment, we have four placeholders:

1. **{{Constants.pageDescription}}**
2. **{{Constants.pageTags}}**
3. **{{Constants.pageTitle}}**
4. **{{Content.body}}**

The actual names are decided by the developer with a few exceptions. Here we assume that _something_ will provide values for these three placeholders and when the final HTML is generated, these will have real values.

#### Site Constants
The **siteConstantsFilename** identifies a JSON file with a list of site constants which are shared across all builds of the site. Since the **siteConfig** file represents a specific build type (release vs. debug), this constants file allows us to share common constants across all site configurations. The file identified should be formatted exactly like the **constants** section with key/value pairs defining the constants. Here's an example:

```json
[
    { "key": "page-description", "value": "Learn how to design &amp; build a mobile app using Xamarin’s cross-platform development software which simplifies mobile application creation. Start a free trial today." },
    { "key": "page-tags", "value": "training, app development, mobile app development, build your own app" },
    { "key": "quiz-title", "value": "Flash Quiz" },
    { "key": "quiz-nav-title", "value": "Test Your Knowledge" },
    { "key": "default-previous-button-text", "value": "Previous" },
    { "key": "default-next-button-text", "value": "Next" }
]
```

Each of these **key** items becomes a replacement token in the list which can be used to provide the **value** in the final generated HTML. For example:

```html
<h1>{{quiz-title}}</h1>
```

Would become:

```html
<h1>Flash Quiz</h1>
```

Using the above constant information.

#### Content Folders

The **contentFolder** property defines the single folder where all the static content will be copied from, and where the Markdown content that generates pages is located. This should be a folder which contains all your SASS/CSS files, scripts and any global images your site needs to function. The generator tools will copy all the files from this folder (with the exception of `.md` files) into the output folder directly. If the destination file already exists, it will be replaced if the source file has a newer date.

#### Include Search Folders

This is a list of folders to search for when using the [`[[include=]]`](./markdown-syntax.md#including-files) feature for a Markdown source file. The generator will look in the current directory first, and then use this list of folders to locate a file to embed.

#### Extensions

This is a list of .NET assemblies to load with Markdown extensions. Each identified assembly will be scanned to find all classes which implement [`IMarkdownExtension`](../Parser/MDPGen.Core/IMarkdownExtension.cs).

#### Script configuration
This is a JSON object which describes how to setup the Rosylyn scripting environment. It has several properties:

| Property | Description |
|----------|-------------|
| `namespaces` | This is an array of strings which define the default namespaces to include on every script. By default, the tool always includes: `MDPGen.Core.Infrastructure`, `MDPGen.Core`, and `MDPGen.Core.Services`. |
| `scriptsFolder` | This is a string which identifies the folder where all the C# scripts are to be loaded from. All `.cs` files in this folder will be loaded by the site generator. |
| `hooks` | This is a collection of interception points for the site generator. We currently have one hook defined: `onPageInit` which is called just before any page is generated. The value for each of these hooks defines the specific script file to load and execute when this interception point is encountered. |

#### Override Types
This is a JSON object which defines the internal types that have custom implementations the tools should use. The valid types are:

| Type | Description |
|------|-------------|
| `PageLoaderType` | The .NET type which loads each [`ContentPage`](../Parser/MDPGen.Core/Infrastructure/Navigation/ContentPage.cs). This can be replaced to load pages from non-file locations, or to customize where content is loaded from. |
| `MetadataLoaderType` | The .NET type which loads the page metadata. This can be replaced/extended to add new supported keys to the header, or to load the metadata from a different location. By default it is always loaded from a YAML header on each page. |

#### PageLoader Type
As mentioned above in the **Override Types**, this is the .NET type (fully qualified) which is used to load the navigation structure. It must implement [`IContentPageNavigationLoader`](https://github.com/xamarinhq/XamU-StaticContentGenerator/blob/master/Parser/MDPGen.Core/Infrastructure/Navigation/IContentPageNavigationLoader.cs).

The default version, [`ContentPageLoader`](../Parser/MDPGen.Core/Infrastructure/Navigation/ContentPageLoader.cs), loads `meta.json` files (described below) and generates a navigation structure consisting of sets of [`ContentPage`](../Parser/MDPGen.Core/Infrastructure/Navigation/ContentPage.cs) elements. 

#### MetadataLoaderType
By default, each page has a [YAML header](https://github.com/xamarinhq/XamU-StaticContentGenerator/blob/master/docs/markdown-syntax.md#yaml-header) which is used to provide metadata for that page. The default metadata loader supports an `id`, `title`, and a list of `tags` which are added to the replacement token list. The `tags` can also _replace_ existing tokens - allowing the page to override a value.

The metadata loader can be extended or replaced through the **overrideTypes** defined above by specifying a type that implements [`IPageMetadataLoader`](https://github.com/xamarinhq/XamU-StaticContentGenerator/blob/master/Parser/MDPGen.Core/Infrastructure/Metadata/IPageMetadataLoader.cs). The `Load` method returns a [`DocumentMetadata`](https://github.com/xamarinhq/XamU-StaticContentGenerator/blob/master/Parser/MDPGen.Core/Infrastructure/Metadata/DocumentMetadata.cs) type which can be extended to add more information into the header. In addition, `Load` can get the metadata from other locations - the default is always the YAML header of the page itself.

#### Processing Chain
This is an array of processing blocks to run on each [`ContentPage`](../Parser/MDPGen.Core/Infrastructure/Navigation/ContentPage.cs) identified by the page loader. Each processing block is a .NET type (fully qualified if not in the MDPCore assembly) that implements [`IProcessingBlock<TInput,TOuput>`](../Parser/MDPGen.Core/IProcessingBlock.cs). The system will verify that the output from one block can be passed as the input to the next in the resulting chain and stop the processing if not.

The processing blocks will load the identified content (Markdown by default) and generate an output file (HTML by default). However, this is an extensible system which allows users to replace, remove, or add other blocks as necessary to process the output. 

Each block is passed a [`PageVariables`](../Parser/MDPGen.Core/Infrastructure/PageVariables.cs) object which has the page state for the page being generated. The processing chain itself is duplicated and run in parallel based on the # of processors - this means multiple pages are generated simultaneously. 

The built-in blocks provided with the tool include:

| Block | Description |
|-------|-------------|
| [`MDPGen.Core.Blocks.IfDirective`](../Parser/MDPGen.Core/Blocks/IfDirective.cs) | This takes an input string and looks for `[[if xxx]]` conditional blocks, evaluates then and returns the text with the `if` blocks removed. If the condition is satisfied, the text in between the `[[if]]` and `[[endif]]` is added to the returning data. If the condition is _not_ satisfied, then the content in between the markers is omitted. |
| [`MDPGen.Core.Blocks.IncludeDirective`](../Parser/MDPGen.Core/Blocks/IncludeDirective.cs) | This takes an input string and looks for `[[include ]]` directives. It sucks in the specified file and adds it to the returning string. |
| [`MDPGen.Core.Blocks.RunMarkdownExtensions`](../Parser/MDPGen.Core/Blocks/RunExtensions.cs) | This takes an input string of Markdown + Extension content and identifies all the extensions and executes them. It replaces the extension line with the result from the extension. |
| [`MDPGen.Core.Blocks.ConvertMarkdownToHtml`](../Parser/MDPGen.Core/Blocks/ConvertMarkdownToHtml.cs) | This takes an input string of Markdown and runs our `MarkdownDeep` parser on it. The returning data is the output from the parser (HTML) |
| [`MDPGen.Core.Blocks.RenderMustachePageTemplate`](../Parser/MDPGen.Core/Blocks/RenderMustachePageTemplate.cs) | This takes an input string of HTML content as the body and identifies the appropriate page template (either the default, `meta.json` specified, or YAML specified, in that order) and inserts the input HTML string into the page template. |
| [`MDPGen.Core.Blocks.RenderRazorTemplate`](../Parser/MDPGen.Core/Blocks/RenderRazorTemplate.cs) | This takes an input string of HTML content as the body and identifies the appropriate page template (either the default, `meta.json` specified, or YAML specified, in that order) and inserts the input HTML string into the page template using the Razor HTML engine - this allows dynamic C# scripts to be run as part of the template generation. |
| [`MDPGen.Core.Blocks.ReplaceTokens`](../Parser/MDPGen.Core/Blocks/ReplaceTokens.cs) | This takes an input string of HTML with replacement tokens and finds each token and replaces it with text from the token collection generated while building the page, or from the `constants` collection in the site configuration. If the token cannot be located, it is commented out with HTML style comments. The returning data is the fully filled out HTML output. |
| [`MDPGen.Core.Blocks.MinifyAndVersion`](../Parser/MDPGen.Core/Blocks/MinifyAndVersion.cs) | This is an optional block which minifies the CSS and JavaScript files as well as adding a `?vxxx` element to the URL to make it unique for browser caching. |
| [`MDPGen.Core.Blocks.CompressHtml`](../Parser/MDPGen.Core/Blocks/CompressHtml.cs) | This is an optional block which compresses the HTML pages - removing spaces and unnecessary content such as comments. |
| [`MDPGen.Core.Blocks.WriteFile`](../Parser/MDPGen.Core/Blocks/WriteFile.cs) | This takes a string and writes it to the output file identified by the page variables. |

Site builders can create their own blocks by creating a public class that implements `IProcessingBlock<string, string>` and insert the type into the processing chain defined by their site configuration.

##### Running the processing chain
The blocks are chained together with each block's output passed into the next block defined by the processing chain. This is done by _multiple threads_ so that pages are generated concurrently. This means each block _must_ be thread-safe - either by ensuring the logic has no external requirements, or by providing the appropriate synchronization.

##### Cleaning up processing blocks
Processing blocks which have cleanup requirements can implement `IDisposable`. The block will be disposed when all threads have finished using the block and the entire site has been processed.

#### Constants
This is a dictionary of Key/Value pairs which define any placeholders for your site. These values are available to any extensions and can also be placed into the HTML templates.

The values can be literals, environment variable values, or a combination. To specify an environment variable, surround the text with `%`. For example `%UserName%` would lookup the environment value `UserName` and use that as the constant value.

## Directory metadata
The site configuration defines the root _content_ folder. That folder can contain sub-folders as necessary to define the content and structure of the site. Within each of these content folders, two files should _always_ exist:

1. `meta.json`
2. `default.md`

#### meta.json
The `meta.json` file is the directory metadata. It is used to organize the navigation of the content and, ultimately, decides what pages are available to in the site and in what order they are presented. The contents of `meta.json` is a JSON array of source files and folders. For example:

```json
[ "default", "folder1", "file", "folder2" ]
```

The first entry in the array _must_ identify a page to render as the contents for the folder itself. By contention this is `default.md`. Note that we do not need to specify an extension (although you can if you want to be explicit). It will search for `.md`, `.html`, `.htm`, `.aspx` in that order. 

> If no file is specified, but a `default.md` file is present in the folder, it will be used and a warning will be emitted by the `MDPGen` tooling.

Other entries in the array define the additional files and folders which should be considered as part of the content. Identified folders will be scanned for a `meta.json` file (which must be present) and the process continues recursively.

Files identified in `meta.json` are processed by the tool and will eventually emit some HTML file in the output site.

#### default.md
The primary page for each directory is by convention named `default.md`. This should _always_ be the first entry in array defined in the `meta.json`. It is always used if you navigate directly to the folder URL (e.g. `/forms/xam120`) or if you click on the folder node in the ToC tree view.

> You will always need a `default.md` file which defines the landing page for that folder. Even if it's just a navigational aid to get into the content.

