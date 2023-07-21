MDPGen supports all [GitHub Flavored Markdown](https://guides.github.com/features/mastering-markdown/) syntax with one exception - indenting text does not automatically create a code block. Also, MDPGen adds new syntax to support additional functionality. The base parser included with the tools is a derived form of the [Markdown Deep](http://www.toptensoftware.com/markdowndeep/) open source parser.

## Yaml Header

Each Markdown file processed by MDPGen can include a Yaml header at the top. This header is optional, but if included must be placed at the top of the file and must take the form of a valid YAML document between triple-dashed lines. The Yaml header is considered to be the metadata for the Markdown file. Here is a basic example:

```md
---
id: 2293ee7b-84eb-4206-b49e-a7f65f0ea13e
title: Title for this page
---
```

There are three known elements you can put into the Yaml header:

| Tag     | Purpose |
|---------|---------|
| `id`    | A unique identifier for this page. Must be a string, defaults to the filename. |
| `title` | A text-based title for the page. |
| `template` | The page template to use to render this page. If not supplied, the [default page template](./defining-a-site.md#page-template) for the site is used. |
| `tags`  | A collection of `key` and `value` pair objects which are added to the replacement token collection |

A full example of the Yaml header is:

```md
---
id: 1972d8a4-7319-4e2d-b092-02268903a166
title: Welcome to our website!
template: customTemplate.cshtml
tags:
    - key: pageTemplate
      value: firstPageTemplate.html
    - key: page-tags
      value: main, default, index, Check this out
---
```

Following the ending Yaml marker, MDPGen expects to find the actual Markdown content.

## Cross Referencing files with `id`

Cross references allows you to link to another topic by using its unique identifier instead of using the known file path (which may change).

Markdown files processed by MDPGen always have an `Id` assigned to them - either assigned by the tool where it defaults to the filename + extension, or defined by adding an `id` tag in the YAML header:

```md
---
id: identifier_of_the_file
---

This is a page with `id` specified.
```

The MDPGen tools uses the syntax `{{xref:id}}` in the Markdown files to indicate a cross-reference replacement tag. When the parser encounters this, it will lookup the correct page using the specific `id` and replace the entire tag with the relative URL of the given page.

> If the identifier cannot be found, the tag will be commented out in the resulting HTML file.

As an example:

```md
---
id: A.md
---
We can [link to another page]({{xref:B.md}}) using cross-reference links.
```

```md
---
id: B.md
---
Or can use <a href="{{xref:A.md}}#jumpto">HTML tags<a> too.
```

These would render to:

```html
We can <a href="B.html">link to another page</a> using cross-reference links.
```

```html
Or can use <a href="A.html#jumpto">HTML tags</a> too.
```

## Including files

MDPGen supports breaking large Markdown files into separate physical files for either maintenance, or to share portions of the content. The included file will also be considered Markdown and processed accordingly.

> The YAML header is **not** supported when the file is added as an include. YAML headers can only be added to root level files.

To include external files, you use the `[[include=xxx]]` tag, where `xxx` is the filename you want to include. The filename can be fully qualified, relative to the current file, in the current folder, or found along a configured search path.

```md
This is the main document

[[include=footer.html]]
```

This would inject a file named `footer.html` into the document as part of the processing.

This replacement can occur mid-line as well - for example, the following syntax is valid:

```md
# Header

Welcome to our website. [[include=common-intro.md]] **We are so glad you are here!**
```

In this case, the contents of `common-intro.md` would be read and inserted as a replacement for the `[[include]]` tag.

## Replacement tokens
The tools support [mustache template replacement tokens](https://mustache.github.io/).

The `{{placeholder}}` markers can be used in the Markdown files - the actual replacement is done as one of the final steps of the page generation, so any tokens in the source files will be replaced when the HTML is finally rendered. You can also _define_ token values in your Markdown files by adding an optional [YAML](http://www.yaml.org/) header block at the very top of the Markdown source file using the `token` collection.

For example, if we had the following HTML template:

```
<html>
   <head>
      <title>{{title}}</title>
      ...
```

And then defined a constant in our site configuration file to supply a default value, we could _override_ that value in a specific Markdown file through a YAML header:

```
---
title: This title would replace the title defined in the site.config!
---

# Normal markdown follows the header.
```

> The YAML block must be the very first thing the parser finds in the file or it will be treated as Markdown. In addition, the block must be ended with the `---` three slashes. Everything in between the two slash lines is treated as YAML.

We currently support two features of the YAML spec:

1. Properties - these are key/value pairs separated by a colon. Each line defines a separate property. If you want to include a CR/LF in the property value, then HTML encode it (`&amp;#13;&amp;#10;`). Spaces are stripped off the end of both the key and value.
2. Comments - all lines starting with '#' are treated as comments.

> We actually do _two_ passes of the file for token replacement - this allows us to catch tokens in the Markdown and `meta.json` files too!

## Conditional blocks

Another extension available in MDPGen is support for _conditional blocks_. These are blocks of Markdown which are only processed and emitted when a given condition is satisfied. This allows for the website to be variable based on external considerations such as environment variable values.

Conditions are evaluated using the `[[if xxx]]` tag, where `xxx` is the condition to check. This tag must start the line (whitespace is ignored) and can be the only content on the line.

The condition value can be one of three types:

1. `{{token}}` Check to see if a given `token` is in the replacement token set.
2. `%VARIABLE%` Check to see if a given environment `VARIABLE` is defined.
3. `value` Check to see if a given `value` is defined as a build value passed to MDPGen.

If the condition passes, then the parser reads all the content starting at the `[[if]]` tag up to a closing `[[endif]]` tag. If this tag is missing, an error is emitted from the tool, but processing will continue.

```md
# This is always used

[[if %DEBUG%]]
## This is only evaluated if the symbol `DEBUG` is defined as an environment variable when the MDPGen tool is generating the site and parsing this page.
[[endif]]

# This is always used
```

## Other Markdown features
There are several other deviations from the Markdown specification we have implemented in our Markdown parser.

### Support for marked text
In non-code blocks, you can surround blocks of text with `==` to have it automatically be marked in HTML with the `<mark>` tag. For example:
<pre>
<code>
==This== is a test
</code>
</pre>

Would generate a highlight around the "This" word with HTML:
<pre>
<code>
&lt;mark&gt;This&lt;/mark&gt; is a test
</code>
</pre>

For multi-line `<mark>` blocks, tag each individual line to keep from having line-wrapped blocks of highlight.

<pre>
<code>
void SomeExistingMethod() {
    &lt;mark&gt;var newVariableAdded = true;&lt;/mark&gt;
    &lt;mark&gt;while (newVariableAdded) {&lt;/mark&gt;
    &lt;mark&gt;    doSomething();&lt;/mark&gt;
    &lt;mark&gt;}&lt;/mark&gt;
}
</code>
</pre>

### Supplying HTML attributes

You can set several attributes on header, code block, links and images using an attribute block. The attribute block is always placed at the end of the line, surrounded by curly braces and can specify an ID, CSS classes to add, and HTML attribute key/values.

```markdown
![My Image](images/xamagon.png "Title of Image") {#id .some-class some_attr='value'}
```

**Setting an HTML ID**<br>Put the desired id prefixed by a hash inside curly brackets after the header, code block or link/image at the end of the line, like this:

```markdown
## Title {#myTitle}
```

Then you can link back to this using the following syntax - note that the id's must match:
```markdown
[Link back to header 1](#myTitle)
```

The order of the items in an attribute block are not important, however you can only define an ID once (#id). You can add multiple class names (space-separated) and if you have attribute values you want to use with embedded spaces, surround the value with either single or double quotes.

For example, this will create a Bootstrap button with a tooltip of "Hooray":
<pre>
<code>
[Hover over me](#) {#theButton .btn .btn-info role='button' data-toggle="tooltip" title="Hooray!"}
</code>
</pre>

The HTML generated will be:
<pre>
<code>
&lt;a href="#" id="theButton" class="btn btn-info" role="button" data-toggle="tooltip" title="Hooray!">Hover Over Me&lt;/a>
</code>
</pre>

### Tables
You can generate HTML tables using the pipe symbol to separate the columns. Each row will generate a new row in the table. You can use Markdown inside the cells.

For example:
<pre>
<code>
| First Header  | Second Header |
| ------------- | ------------- |
| Content Cell  | Content Cell  |
| Content Cell  | Content Cell  |
</code>
</pre>

Will create the following table:

<pre>
<code>
&lt;table>
    &lt;thead>
        &lt;tr>
            &lt;th>First Header</th>
            &lt;th>Second Header</th>
        &lt;/tr>
    &lt;/thead>
    &lt;tbody>
        &lt;tr>
            &lt;td>Content Cell</td>
            &lt;td>Content Cell</td>
        &lt;/tr>
        &lt;tr>
            &lt;td>Content Cell</td>
            &lt;td>Content Cell</td>
        &lt;/tr>
    &lt;/tbody>
&lt;/table>
</code>
</pre>

You can specify alignment for each column by adding colons to the separator lines. A colon at the left of the separator line will make the column left-aligned; a colon on the right of the line will make the column right-aligned; colons at both side means the column is center-aligned. For example, to right align the `Value` column, add the colon to the end:

<pre>
<code>
| Item      | Value |
| --------- | -----<mark>:</mark>|
| Computer  | $1600 |
| Phone     |   $12 |
| Pipe      |    $1 |
</code>
</pre>

Finally, you can set a CSS class definition on the table by adding a `{ .classname }` tag on the header line. For example, you can set the table to have alternating rows by applying the `table-striped` CSS class:
<pre>
<code>
| First Header  | Second Header | <mark>{.table .table-striped}</mark>
| ------------- | ------------- |
| Content Cell  | Content Cell  |
| Content Cell  | Content Cell  |
</code>
</pre>

#### Escaping pipe "|" characters

Since pipe characters (`|`) are used to mark cell divisions in the table syntax, if you want a pipe character within your cell, it will require using the `&#124;` HTML entity code.

### Definition Lists
We support definition lists of terms and definitions of these terms, much like in a dictionary. A simple definition list is made of a single-line term followed by a colon and the definition for that term, each definition must be separated by a newline:
<pre>
<code>
Apple
:   Pomaceous fruit of plants of the genus Malus in the family Rosaceae.

Orange
:   The fruit of an evergreen tree of the genus Citrus.
</code>
</pre>

This will generate the following HTML:
<pre>
<code>
&lt;dl>
    &lt;dt>Apple</dt>
    &lt;dd>Pomaceous fruit of plants of the genus Malus in the family Rosaceae.</dd>
    &lt;dt>Orange</dt>
    &lt;dd>The fruit of an evergreen tree of the genus Citrus.</dd>
&lt;/dl>
</code>
</pre>

The CSS styling will **bold** the titles and add an underline emphasis.

### Footnotes
Footnotes work mostly like reference-style links. A footnote is made of two things: a marker in the text that will become a superscript number and a footnote definition that will be placed in a list of footnotes at the end of the document.

A footnote looks like this:
<pre>
<code>
That's some text with a footnote.<mark>[^1]</mark>

<mark>[^1]:</mark> And here's the footnote text.
</code>
</pre>

Footnote definitions can be placed anywhere in the document, but footnotes will always be listed in the order they are linked to in the text. Note that you cannot make two links to the same footnotes: if you try, the second footnote reference will be left as plain text.

### Abbreviations
We also support abbreviation/definitions in the documents. Create an abbreviation definition like this:
<pre>
<code>
*[HTML]: Hyper Text Markup Language
*[W3C]:  World Wide Web Consortium*
</code>
</pre>

then, elsewhere in the document, write text such as:
<pre>
<code>
The HTML specification
is maintained by the W3C.
</code>
</pre>

and any instance of those words in the text will become:
<pre>
<code>
The &lt;abbr title="Hyper Text Markup Language">HTML&lt;/abbr> specification
is maintained by the &lt;abbr title="World Wide Web Consortium">W3C&lt;/abbr>.
</code>
</pre>

### Code blocks
A common thing we do in pages is to add code. There are two ways to add code. First, you can use three back-tick [\`] markers to delimit the code block. This will generate a `pre` tag with the `class="prettyprint"` to surround the code. The language used by the prettyprint library is determined by the [language code](https://github.com/github/linguist/blob/master/lib/linguist/languages.yml) after the first triple-backtick.

<pre>
<code>
```csharp
// Code goes here
string s = "Hello";
List<string> arr = new List<string>(); // no need to escape brackets!
```
</code>
</pre>

> IMPORTANT: This approach will automatically escape any HTML or brackets, so prefer this approach if you are not adding additional highlighting into the code block.

#### Add highlights
If you want to add a highlight effect into the code block, then you will need to use the traditional HTML approach of a `<pre>` tag along with the proper class name. Surround the block you want to highlight with `<mark>` and `</mark>` tags as shown here:

<pre>
<code>
// Code goes here
// Highlight code using &lt;mark> and &lt;/mark> tags:
&lt;mark>string s = "Hello";&lt;/mark>
</code>
</pre>

### Collapsing code blocks
You can create "collapsing" code blocks by using three tilde [~] markers:

<pre>
<code>
~~~csharp
 // Collapsing Code goes here
~~~
</code>
</pre>

This will generate a button with the text "Show Code", when you click it, the above code block will be displayed and the text will change to "Hide Code".

### Embedding custom HTML
The parser supports HTML tags in the markdown and will treat them as proper HTML. This allows you to create specific HTML for a given page which is rendered straight out.

> The parser supports Markdown _inside_ the HTML blocks. It also ignores tabs/spaces when parsing HTML chunks - this is in violation to standard markdown which would turn that into a code block.

### Hint blocks
The parser has an extension to the block quote support `>` with two additional styles: `>>` and `>>>`:

<pre>
<code>
> This will be a standard HTML BlockQuote
</code>
</pre>

<pre>
<code>
&lt;blockquote>
This will be a standard HTML BlockQuote
&lt;/blockquote>
</code>
</pre>

<pre>
<code>
>> This will be an "info" HTML BlockQuote
</code>
</pre>

<pre>
<code>
&lt;blockquote class="info-quote">
This will be an "info" HTML BlockQuote
&lt;/blockquote>
</code>
</pre>

<pre>
<code>
>>> This will be a "danger" HTML BlockQuote
</code>
</pre>

<pre>
<code>
&lt;blockquote class="danger-quote">
This will be an "danger" HTML BlockQuote
&lt;/blockquote>
</code>
</pre>

### Markdown comments

If you need to document something in Markdown, but do not want the note in the resulting generated HTML, there are a couple Markdown hacks that allow for these types of comments.

These two approaches are both confirmed to work in the current Markdown processor used in this tool.

```markdown
[//]: # (This text will not end up in the HTML)
[comment]: <> (This text will not end up in the HTML)
```

While the former is shorter, the latter might be more slightly descriptive to the next person editing the file.
