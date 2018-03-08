module Shared

open Types

// Helpers

/// Inverse the values in a tuple
let invTuple (a, b) = b, a

/// Try to find a key in a map, return Some value when it is found, otherwise it
/// returns None
let mapTryFind k (map: Map<'a, 'b>) = map.TryFind(k)

/// Try find key in a list
let listTryFind s =
    List.map invTuple >> Map.ofList >> mapTryFind s

/// The list of characters used in the parser
let charList = ["#", HASH; "|", PIPE; "=", EQUAL; "-", MINUS; "+", PLUS; "*", ASTERISK
                ".", DOT; "**", DASTERISK; "***", TASTERISK; "_", UNDERSCORE; "__", DUNDERSCORE
                "___", TUNDERSCORE; "~", TILDE; "~~", DTILDE; "~~~", TTILDE; "[", LSBRA
                "]", RSBRA; "(", LBRA; ")", RBRA; @"\", BSLASH; "/", SLASH; "<", LABRA
                ">", RABRA; "{", LCBRA; "}", RCBRA; "`", BACKTICK;
                "!", EXCLAMATION; ":", COLON; "^", CARET; "%", PERCENT; ",", COMMA]

let charMap = charList |> List.map invTuple |> Map.ofList

let (|CharTok|_|) tok =
    mapTryFind tok charMap

let mapTok = function
    | CharTok s -> s
    | CODEBLOCK _ -> "CODEBLOCK"
    | FOOTER n -> sprintf "FOOTER %d" n
    | HEADER n -> sprintf "FOOTER %d" n
    | NUMBER s -> s
    | LITERAL s -> s
    | WHITESPACE n -> String.replicate n " "
    | _ -> "\n"


let mapLang lang =
    match lang with
    | Python -> "python"
    | FSharp -> "fsharp"
    | CPP -> "cpp"
    | C -> "c"
    | Empty -> ""

    