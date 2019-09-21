# FMark [![Build Status](https://travis-ci.org/ymherklotz/FMark.svg?branch=master)](https://travis-ci.org/ymherklotz/FMark)
FSharp markdown implementation with additional functionality.

Namely:

- Macros
- Includes
- Spreadsheet functionality
- Table of Contents generation
- Citations and footnotes

See [example.fmark](examples/example.fmark) for usage examples.

# How to use

## FMark Plugin Visual Studio Code

Functions:

- Open Preview (ctrl+shift+M)

Open a live preview of the HTML that will be generated by FMark.

- Create HTML File

Create an HTML file in the same directory as the .fmark file, with .html instead of .fmark.

## Command Line Interface
```
USAGE: FMark [--help] [--output <path>] [--loglevel <debug|info|warning|error|fatal>] [--format <html|markdown>] [--test [<sequential>]] [<path>]

INPUT:
    <path>                specify input file path.

OPTIONS:
    --input, -i <path> CLI flag for input (not necessary, default argument is input.)
    --output, -o <path> specify output file path.
    --loglevel, -l <debug|info|warning|error|fatal> set the log level  ('0:DEBUG', '1:INFO', '2:WARNING', '3:ERROR' ,'4:FATAL').
    --format, -f <html|markdown> specify format, by default: html.
    --test, -t [<sequential>] run CI tests.
    --help display this list of options.
```
Note: Markdown generation is incomplete (some will unncessarily be in HTML).

# Modules

```
Overall Flowchart:

            ┌─────────────────────┐                      ┌───────────────┐
Source ───> │ Lex and Preprocessor│ ───> Token list ───> │ TOCite Parse  │ ──> Token list with identifiers ┐
            └─────────────────────┘           │          └───────────────┘                                 │
                                    │         │                                                            │
                                    │         └──────────────────────────────> Header+Footer list ────>────┤
                                    │                                                                      │
                                    │          ┌──────────────┐                                            │
                                    └────────> │ Markalc Parse│────────── Table ────────────>──────────────│
                                               └──────────────┘                                            │
                                                                                                           │
                                   ┌─────────┐                            ┌─────────────┐                  │
             Final Document  <──── │ HTMLGen │ <──── ParsedObj list <──── │ Main Parser │ <────────────────┘
                                   └─────────┘                            └─────────────┘
```

1. [Lexer and Preprocessor](FMark/src/Common/Lexer/README.md)
2. [TOCite: Preparsing headers and footers](FMark/src/Common/TOCite/README.md)
3. [Markalc: Spreadsheet functionality](FMark/src/Common/Markalc/README.md)
4. [Main Parser](FMark/src/Common/Parser/README.md)

# Vanilla Markdown 

## Implemented
- HTML passthrough
- Lists
- Links (relative links within file don't work in VSCode, use `Create HTML File` and preview that.)
- Images (images don't work in VSCode, use `Create HTML File` and preview that.)
- Headers
- Styling (bold, italic, bold and italic, strikethrough)
- Paragraphs
- Quotes
- Code blocks
- Tables

## Design decisions

### Lists

* A list ends with two endlines, i.e., `\r\n\r\n`
* List items are separated by one endline
* List type is determined by the first item.
  * `NUMBER ; DOT` will be ordered list
  * ` - ` or ` * ` will be unordered list
* If the first item does not start with `NUMDER;DOT;Space| "- " | "* "`, the list type will be default to unordered list
* Ordered list start number is determined by the first item
  * The subsequent counting is `+1` for each list item
* Two spaces as one level of indentation.
* A tab, `\t` character will be interpreted as four spaces, therefore two levels of indentation.

### Header

* All headers must be preceded by two endlines, with an exception made for the first line in a file.

`\n\n# header\n` is a header, otherwise unless the header is the first line in a file it won't be processed as a header.

### Styling

* When using underscores for styling, there must be a space (or endline on the right) on either side in order for it to be recognised.
* Asterisks for styling work as usual.

e.g.

`[ ]_em_[ ]`, `[ ]__a strong__[ ]`, or `[ ]_em_$`, `[ ]__a strong__$`

### Paragraphs

A paragraph is some characters that does not match:
* CodeBlock
* Header
* ContentTable (Table of Contents object)
* List
* Quote
* Table
* Reference

It terminates with two endlines. Any elements mentioned above cannot exist in a paragraph. Otherwise, they will become normal text, and will not be rendered as expected.

# Building

To build fmark, dotnet and fable have to be installed first. To build the javascript as well, yarn or npm are needed.

## Using the Build script

``` shell
# build only javascript
build -b js

# build and test every single module
build -b testall

# build js and cli
build -b all
```

# Markdown extensions

## Table of Contents

Table of Contents can be build anywhere with `%%TOC`. Relative linking is supported, clicking an item from the contents table will jump to the respective header when viewed in html.

|Feature|Example|Exaplanation
|---|---|---|
|depth|`%%TOC depth=3`|Build a TOC with headers level less than 3
|exclude|`%%TOC excludes=[Appendix;Acknowledgement]`|Exclude headers with name `Appendix` and `Acknowledgement`|

Multiple features can be stacked with `,` like so:

`%%TOC depth=3, excludes=[Appendix;Acknowledgement]`

## Citations

FMark supports simple footers and styled references.

* If multiple citations with the same reference ID is found, the first citation will be used inline.
  * One `[^fmark]` inline, and two `[^fmark], some author`. The first will be used.
* Reference to non-exist citation will result in `"[Reference: %refID not found!]"` in the HTML generated.

* The list is sorted, with footnotes in order of their numerical IDs first, & References in order of apperance after. Thus the order each explanatory text comes in the fmark file does not matter.

### Simple Footers

Simple footers have numerical IDs.

```
this is inline[^1], and so on.

[^1], this is the text to explain such line.
      this line as well.
But not this line.
```

The body will show up with a superscripted link:
> this is inline<sup>1</sup> , and so on.
>
> But not this line.

The footer texts will be collected at the end of the document.
> 1: this is the text to explain such line. this line as well.

When page break & numbering are supported, footer texts will collect at the end of the current page.

### Styled References

Styled references have alphabetic IDs.

Supported rendering styles:

||Chicago|Harvard|IEEE|
|---|---|---|---|
|Book|Yes|Yes|-|
|Website|Yes|Yes|-|

Supported data fields

|Field|Explanation|
|---|---|
|type|`'Book'` or `'Website'`|
|author|Author with surname at the end|
|title|Title|
|year|The year it is written|
|url|Address for website|
|access|Date of access for websites, in `yyyy-mm-dd` format|

Pick a style with `%%RefStyle`, or it will default to Harvard.
```
%%RefStyle = Chicago
```

Write your reference id followed by `field1=data1, field2=data2, ...` to assign values to a reference id.

Spaces around equal sign are allowed, so `f = d`, `f= d`, `f =d` or `f=d`all have the same effect.

```
This is a citation[^Mark]. This is another[^FMark] one.

[^Mark], type= Book, author= Mark Smith, title= Not a real book, year= 2018
[^FMark], type= Website, author= FMark Smith, title= Not a real website, year= 2017 url= www.example.com/website access= 2018-3-4
```

With Harvard, it will look like this:
> This is a citation(Smith, 2018). This is another(Smith, 2017) one.

At the end of the document:
> Smith, M. (2018) *Not a real book*.
> 
> Smith, F. (2017) *Not a real website*. Available from: www.example.com/website [Accessed 4th March 2018].

if Chicago style is chosen:

> This is a citation(Smith 2018). This is another(Smith 2017) one.

At the end of the document:
> Mark Smith. 2018. *Not a real book*.
> 
> FMark Smith. 2017. "Not a real website." Accessed March 3, 2018. https://www.example.com/website

## Latex maths rendering

Fmark includes support for Latex maths rendering.

Surround an equation written in Latex with `$$` in order to use this feature.

Example: `$$2+2=4-1=3$$`
## Spreadsheet functionality

Spreadsheet functions will evaluate in-place, if they are incorrectly formatted then Markalc will leave the cell unchanged as if it were normal text inside. 

To delineate an expression, start the cell with the `=` operator, e.g.

Calcs|39|42|
|---|---|---|
||`=6*5+SUM{4,5}`|`=[1,1]+3`|

### Supported

#### Arithmetic

|Function|Syntax|Effect|Tested|
|---|---|---|---|
|Modulo|`a%b`|a modulo b|Property based, all integer inputs.
|Power|`a^b`|a to the power of b|Property based, all integer inputs.
|Multiply|`a*b`|a times b|Property based, all integer inputs.
|Division|`a/b`|a divided by b|Property based, all integer inputs.
|Subtraction|`a-b`|a minus b|Property based, all integer inputs.
|Addition|`a+b`|a plus b|Property based, all integer inputs.
|Brackets|`a+(b-c)`|Specify order of operation.|Unit tested.
|Cell References|`[0,2]`|Evaluates to contents of cell specified (row 0, col 2)|Unit tested|
|Range Cell Ref|`[0,0]:[2,0]`|Evaluates to list of cell references.|Unit tested.|
|Keyword Cell Ref|`[col=2,row=0]`|Alternative syntax, order doens't matter.|Unit tested|
|Specify Decimal Places|`=5.333,2`|Comma then a number specifies number of decimal places.|Unit tested|

##### Cell reference behaviour

|Referenced cell|Return|
|---|---|
|Contains valid expression `=...`|Contents of cell|
|Does not contain valid expression|nan|
|Is not in table|nan|
|Contains circular reference|nan|

#### Functions

Supported functions:

|Function name| Effect| Tested|
|---|---|---|
|`SUM`|Adds all arguments.|Unit tested.|
|`AVG`|Calculates mean of all arguments.|Unit tested.|
|`MIN`|Returns minimum of all arguments.|Unit tested.|
|`MAX`|Returns maximum of all arguments.|Unit tested.|

Functions support Range Cell References: `[0,0]:[2,0]` will evaluate to `[0,0],[1,0],[2,0]`. This can be used with other arguments, e.g. `SUM{[0,0]:[2,0],1,3+2,[0,0]}`

### Unsupported

|Feature|Example|Workaround|
|---|---|---|
|Negative numbers  |`=-a+b`|Subtract number from zero in brackets e.g. `=(0-a)+b`|
|Integer operations|`=5+2`|All calculations done in floating point.|
|Allow formatting around equation|`*=5+2*`|None|
|String operations|e.g. Excel CONCAT|None|
|Assume empty cells are zero|Summing over column with empty cells|Put `=0` in each cell|


