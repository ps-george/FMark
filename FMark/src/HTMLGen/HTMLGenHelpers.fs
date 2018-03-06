module HTMLGenHelpers

open Types

type TagStyle = INLINE | NonInline of indentStr: string


/// atach HTML tag to a given string, both start and end tag
/// inline style does not insert newline after start tag and before end tag
/// non-inline style will have indent set to desired string
let attachHTMLTag (tagName, attributes, style, needCloseTag) (content: string) =
    let attr =
        match List.isEmpty attributes with
        | true -> ""
        | false -> " " + String.concat " " attributes // space before attributes and tagName
    let transformedContent =
        match style with
        | INLINE -> content
        | NonInline ind -> content.Replace("\n", "\n"+ind)
    "<" + tagName + attr + ">"
    + transformedContent
    + if needCloseTag then "</" + tagName + ">" else ""

/// concat attributeName and value
/// with quotes
let toAttr attributeName value =
    attributeName + "=\"" + value + "\""

let toAttrs attrs =
    let mapper attr =
        match attr with | (attrName, value) -> toAttr attrName value
    List.map mapper attrs