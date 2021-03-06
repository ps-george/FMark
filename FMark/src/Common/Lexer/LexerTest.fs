module LexerTest

open Expecto
open Preprocessor

/// Higher order function to make tests and remove boilerplate
let makeTestList inf outf testf name listOfPairs =
    let makeSingleTest index (n, i, o) =
        testCase (sprintf "%s:%s:%d" name n index) <| fun () ->
            Expect.equal (inf i |> testf) (o |> outf) (sprintf "%A" i)
    List.indexed listOfPairs
    |> List.map (fun (i, pair) -> makeSingleTest i pair)
    |> Expecto.Tests.testList name

/// simple test that does not transform the input or output
let makeSimpleTestList f = makeTestList id id f

let makeTestFromCharList l e =
    List.map (fun (a, b) -> (sprintf "Token %s" a), a, [b; e]) l

// --------------------------------------------------
// Unit Tests
// --------------------------------------------------

// Preprocessor tests

/// Make small test for next token function
[<Tests>]
let preprocessorNextTokenTest =
    makeSimpleTestList nextToken "PreprocessorNextToken" [
        "Openeval", "{{", (OPENEVAL, "")
        "Closeeval", "}}", (CLOSEEVAL, "")
        "Opendef", "{%", (OPENDEF, "")
        "Semicolon", ";", (SEMICOLON, "")

        "Long random text",
        "This is random text, and should stop here; This should not be included",
        (LITERAL "This", " is random text, and should stop here; This should not be included")
    ]

/// Check if all the special characters get tokenized correctly
[<Tests>]
let preprocessorTokenizeTokenTest =
    let tokenTests = makeTestFromCharList charList ENDLINE
    makeSimpleTestList tokenize "PreprocessorTokenize" tokenTests

/// Tokenize tests for the preprocessor
[<Tests>]
let preprocessorTokenizeTest =
    makeSimpleTestList tokenize "PreprocessorTokenize" [
        "Macro",
        "{% macro x() y %}",
        [OPENDEF; LITERAL " "; MACRO; LITERAL " "; LITERAL "x"; LBRA
         RBRA; LITERAL " "; LITERAL "y"; LITERAL " "; CLOSEDEF; ENDLINE]

        "Substitution",
        "{{ x(arg1; arg2) }}",
        [OPENEVAL; LITERAL " "; LITERAL "x"; LBRA; LITERAL "arg1"; SEMICOLON
         LITERAL " "; LITERAL "arg2"; RBRA; LITERAL " "; CLOSEEVAL; ENDLINE]

        "Normal markdown",
        "# Title _with emphasis_",
        [LITERAL "#"; LITERAL " "; LITERAL "Title"; LITERAL " "; LITERAL "_with"; LITERAL " "
         LITERAL "emphasis_"; ENDLINE]

        "Escaped character in sentence",
        "Escaped Semicolon \; \(it becomes a literal\)",
        [LITERAL "Escaped"; LITERAL " "; LITERAL "Semicolon"; LITERAL " "
         LITERAL ";"; LITERAL " "; LITERAL "("; LITERAL "it"; LITERAL " "
         LITERAL "becomes"; LITERAL " "; LITERAL "a"; LITERAL " "
         LITERAL "literal"; LITERAL ")"; ENDLINE]
    ]

/// Tests for the find until token function
[<Tests>]
let findParseUntilTest =
    let makeSomeTest = makeTestList id Some
    makeSomeTest (findParseUntil LBRA RBRA) "FindParseUntil" [
        "Simple finding of bracket",
        [LITERAL "Start"; RBRA; ENDLINE],
        ([LITERAL "Start"], [ENDLINE])

        "More complicated matches",
        [MACRO; INCLUDE; LITERAL "Hello world, this is really random"
         RBRA; LITERAL "Even more random stuff"; ENDLINE],
        ([MACRO; INCLUDE; LITERAL "Hello world, this is really random"],
         [LITERAL "Even more random stuff"; ENDLINE])

        "Nested",
        [LITERAL "Hello"; LBRA; LBRA; RBRA; RBRA; RBRA; LITERAL "Other"],
        ([LITERAL "Hello"; LBRA; LBRA; RBRA; RBRA], [LITERAL "Other"])
    ]

[<Tests>]
let splitListTest =
    makeSimpleTestList splitListEval "SplitList" [
        "Simple",
        [LITERAL "Hello"; SEMICOLON; LITERAL "World"],
        [[LITERAL "Hello"]; [LITERAL "World"]]

        "Longer",
        [LITERAL "This"; LITERAL "not"; LITERAL "nothing"; SEMICOLON; LITERAL "is"; SEMICOLON; LITERAL "So"; SEMICOLON
         LITERAL "Random"; LITERAL "not"; LITERAL "nothing"],
        [[LITERAL "This"; LITERAL "not"; LITERAL "nothing"]; [LITERAL "is"]; [LITERAL "So"];
         [LITERAL "Random"; LITERAL "not"; LITERAL "nothing"]]

        "Nested",
        [LITERAL "This"; LITERAL "is"; LITERAL "The"; LITERAL "Best"; SEMICOLON; LITERAL "Something"
         LITERAL "more"; OPENEVAL; LITERAL "Even"; LITERAL "More"; SEMICOLON; LITERAL "And"
         LITERAL "more"; CLOSEEVAL],
        [[LITERAL "This"; LITERAL "is"; LITERAL "The"; LITERAL "Best"];
         [LITERAL "Something"; LITERAL "more"; OPENEVAL; LITERAL "Even"; LITERAL "More"; SEMICOLON
          LITERAL "And"; LITERAL "more"; CLOSEEVAL]]
    ]

[<Tests>]
let stripWhiteSpaceTest =
    makeSimpleTestList stripWhiteSpace "StripWhiteSpace" [
        "Simple",
        [LITERAL "      "; LITERAL "Hello"; LITERAL "World"; LITERAL "             "],
        [LITERAL "Hello"; LITERAL "World"]
    ]

/// Parse tests for the preprocessor
[<Tests>]
let preprocessorParseTest =
    let makeParseTestList = makeTestList tokenize id
    makeParseTestList parse "PreprocessorParse" [
        "Macro with no args",
        "{% macro Hello Body %}",
        [MacroDefinition {Name="Hello"; Args=[]; Body=[ParseText "Body"]}]

        "Macro with no args but brackets",
        "{% macro Hello() Body %}",
        [MacroDefinition {Name="Hello"; Args=[]; Body=[ParseText "Body"]}]

        "Macro with one argument and inline body",
        "{% macro Hello(arg1) Body %}",
        [MacroDefinition {Name="Hello"; Args=["arg1"]; Body=[ParseText "Body"]}]

        "Macro with multiple arguments and inline body",
        "{% macro Hello(arg1; arg2) Body %}",
        [MacroDefinition {Name="Hello"; Args=["arg1"; "arg2"]; Body=[ParseText "Body"]}]

        "Substitution",
        "{{ x }}",
        [MacroSubstitution {Name="x"; Args=[]; Raw="{{ x }}"}; ParseNewLine]

        "Substitution with argument",
        "{{ x(arg1) }}",
        [MacroSubstitution {Name="x"; Args=[[ParseText "arg1"]]; Raw="{{ x(arg1) }}"}; ParseNewLine]

        "Substitution with multiple arguments",
        "{{ x(arg1; arg2) }}",
        [MacroSubstitution {Name="x"; Args=[[ParseText "arg1"]; [ParseText "arg2"]]; Raw="{{ x(arg1; arg2) }}"}; ParseNewLine]

        "Substitution with argument and spaces",
        "{{ x(This is the first argument; This is the second argument) }}",
        [MacroSubstitution {Name="x"; Args=[[ParseText "This"; ParseText " "; ParseText "is";
                                             ParseText " "; ParseText "the"; ParseText " "
                                             ParseText "first"; ParseText " "; ParseText "argument"]
                                            [ParseText "This"; ParseText " ";ParseText "is"; ParseText " ";
                                             ParseText "the"; ParseText " ";ParseText "second"; ParseText " ";
                                             ParseText "argument"]]
                            Raw="{{ x(This is the first argument; This is the second argument) }}"}
         ParseNewLine]
    ]

/// Complete tests for the preprocessor with evaluation
[<Tests>]
let preprocessTest =
    makeSimpleTestList preprocess "Preprocess" [
        "Simple text does not change",
        "This should stay the same",
        "This should stay the same"

        "Simple text does not change with special chars",
        "This should identify the ';', but return the same string",
        "This should identify the ';', but return the same string"

        "Simple macro with no arguments",
        "{% macro x y %} {{ x }}",
        "y"

        "Simple macro with empty brackets",
        "{% macro x() y %} {{ x }}",
        "y"

        "Simple macro evaluation",
        "{% macro x(y) {{ y }} y %} {{ x(argument) }}",
        "argument y"

        "Print out the input when substitution not in scope",
        "{{ x(argument) }}",
        "{{ x(argument) }}"

        "Escaping macro bracket should make the original input appear",
        "\\{% macro x (y) {{ y }} y %} {{ x(argument one;) }}",
        "{% macro x (y) {{ y }} y %} {{ x(argument one;) }}"

        "Shaadowed macros and arguments",
        "{% macro x () macro X %} {% macro y (x) macro Y {{ x }} %} {{ x }}, {{ y(: not x) }}",
        "macro X, macro Y : not x"

        "Shadowed macros",
        "{% macro x() x1 %} {% macro y() {% macro x() x2 %} {{ x }} %} {{ x }}, {{ y }}",
        "x1, x2"

        "Macro with different arguments",
        "{% macro x(arg1; arg2) {{arg1}}, {{arg2}} %} {{x(a; b)}}; {{ x(c;d) }}; {{ x(e; f)}}",
        "a, b; c, d; e, f"

        "Macro with long name",
        "{% macro this_is_a_macro_with_a_long_name(arg1; arg2) {{arg1}}, {{arg2}} %} {{ this_is_a_macro_with_a_long_name(a 1; a 2) }}",
        "a 1, a 2"

        "Calling macro with eval",
        "{% macro x(a) res: {{a}} %} {% macro y(a) {{a}} {{a}} %} {{ x({{y(Hello)}}) }}",
        "res: Hello Hello"

        "Calling nested macro with two arguments",
        "{% macro x(a; b) {{a}} {{b}} %} {{x(a; {{x(b; c)}})}}",
        "a b c"

        "Semicolon should not change",
        "%%TOC excludes=[h2;h22]",
        "%%TOC excludes=[h2;h22]"
    ]

/// Complete multiline tests for the preprocessor
[<Tests>]
let preprocessListTest =
    makeSimpleTestList preprocessList "PreprocessList" [
        "Multiline macro evaluation with newline",
        ["{% macro x"; "Evaluated x"; "%}"; "{{ x }}"],
        ["Evaluated x"; ""]

        "Multiline macro without newline",
        ["{% macro x"; "Evaluated x %}"; "{{ x }}"],
        ["Evaluated x"]

        "Multiline macro with arguments",
        ["{% macro x(arg1; arg2; arg3; arg4)"; "{{arg1}}, {{arg2}}"; "{{arg3}}, {{arg4}}"; "%}"
         "{{ x(arg 1; arg 2; arg 3; arg 4)}}"],
        ["arg 1, arg 2"; "arg 3, arg 4"; ""]
    ]

[<PTests>]
let includeTest =
    makeSimpleTestList (preprocessWithDir "./tests") "Include" [
        "Simple include",
        "{{ include /home/yannherklotz/Github/FMark/FMark/src/Common/Lexer/tests/include.fmark }}",
        "Hello, world\n"

        "Nested include",
        "{{ include /home/yannherklotz/Github/FMark/FMark/src/Common/Lexer/tests/include.fmark }}",
        "Hello, world\n"

        "Relative path include",
        "{{ include include2.fmark }} {{ x(x1; x2) }}",
        "x1 x2"
    ]

// Lexer tests

open Types
open Shared
open Lexer

/// Check if all the tokens are lexed properly
[<Tests>]
let lexTokenizeTokenTest =
    let tokenTests = makeTestFromCharList charList ENDLINE
    makeSimpleTestList lex "PreprocessorTokenize" tokenTests

/// Tests for the complete lexer
[<Tests>]
let lexTest =
    makeSimpleTestList lex "Lex" [
        "Literal",
        "Hello",
        [LITERAL "Hello"; ENDLINE]

        "Number",
        "9",
        [NUMBER "9"; ENDLINE]

        "WhiteSpace",
        "d          ",
        [LITERAL "d"; WHITESPACE 10; ENDLINE]

        "Very simple markdown",
        "Hello, world",
        [Types.Token.LITERAL "Hello"; COMMA; WHITESPACE 1; Types.Token.LITERAL "world"; Types.Token.ENDLINE]

        "With special characters",
        "There is _nothing_ to do",
        [LITERAL "There"; WHITESPACE 1; LITERAL "is"; WHITESPACE 1; UNDERSCORE
         LITERAL "nothing"; UNDERSCORE; WHITESPACE 1; LITERAL "to"; WHITESPACE 1
         LITERAL "do"; ENDLINE]

        "Escaping characters",
        @"\_\\\***\%\+",
        [LITERAL "_"; LITERAL @"\"; LITERAL "***"; LITERAL "%"; LITERAL "+"; ENDLINE]

        "Whitespace",
        "          d    ",
        [WHITESPACE 10; LITERAL "d"; WHITESPACE 4; ENDLINE]

        "One line codeblock",
        "``` python",
        [CODEBLOCK ("", Python); ENDLINE]

        "One line html",
        "<span>This is a span element</span>",
        [LITERAL "<span>"; LITERAL "This is a span element"; LITERAL "</span>"; ENDLINE]

        "Online closing html",
        "<img src=\"https://github.com/IMAGE.png\" />",
        [LITERAL "<img src=\"https://github.com/IMAGE.png\" />"; ENDLINE]

        "HTML with non-HTML start",
        "This is an image: <span>Hello World</span>",
        [LITERAL "This"; WHITESPACE 1; LITERAL "is"; WHITESPACE 1; LITERAL "an"
         WHITESPACE 1; LITERAL "image"; COLON; WHITESPACE 1; LITERAL "<span>"
         LITERAL "Hello World"; LITERAL "</span>"; ENDLINE]

        "Singleton HTML passthrough",
        "Singleton <br> passthrough and more text",
        [LITERAL "Singleton"; WHITESPACE 1; LITERAL "<br>"; WHITESPACE 1; LITERAL "passthrough"
         WHITESPACE 1; LITERAL "and"; WHITESPACE 1; LITERAL "more"; WHITESPACE 1
         LITERAL "text"; ENDLINE]

        "HTML image tag",
        "Embedding an <img src=\"https://github.com/IMAGE\"> in text",
        [LITERAL "Embedding"; WHITESPACE 1; LITERAL "an"; WHITESPACE 1; LITERAL "<img src=\"https://github.com/IMAGE\">"
         WHITESPACE 1; LITERAL "in"; WHITESPACE 1; LITERAL "text"; ENDLINE]

        "A lot of nested tags",
        "<p><p><p><p><p><p><p> </p></p></p></p></p></p></p>",
        [LITERAL "<p>"; LITERAL "<p>"; LITERAL "<p>"; LITERAL "<p>"; LITERAL "<p>"; LITERAL "<p>"
         LITERAL "<p>"; LITERAL " "; LITERAL "</p>"; LITERAL "</p>"; LITERAL "</p>"; LITERAL "</p>"
         LITERAL "</p>"; LITERAL "</p>"; LITERAL "</p>"; ENDLINE]

        "Half opened tag should just be outputted",
        "<a><",
        [LITERAL "<a>"; LITERAL "<"]

        "Half opened with text after should be as expected",
        "<a><This text should appear as normal",
        [LITERAL "<a>"; LITERAL "<This text should appear as normal"]

        "Wrong html close tag should be passed through",
        "<p></>s",
        [LITERAL "<p>"; LITERAL "</>s"]

        "Semi colon in TOC",
        "%%TOC excludes=[h2;h22]",
        [PERCENT; PERCENT; LITERAL "TOC"; WHITESPACE 1; LITERAL "excludes"; EQUAL; LSBRA; LITERAL "h2"; SEMICOLON
         LITERAL "h22"; RSBRA; ENDLINE]
    ]

/// Tests for the complete lexers with a string list as input
[<Tests>]
let lexListTest =
    makeSimpleTestList lexList "LexList" [
        "Very simple multiline markdown",
        ["Hello, world"; "Line 2"],
        [LITERAL "Hello"; COMMA; WHITESPACE 1; LITERAL "world"; ENDLINE; LITERAL "Line"; WHITESPACE 1
         NUMBER "2"; ENDLINE]

        "With special characters",
        ["__Bold__"; "_Emphasis_"],
        [DUNDERSCORE; LITERAL "Bold"; DUNDERSCORE; ENDLINE; UNDERSCORE; LITERAL "Emphasis"; UNDERSCORE
         ENDLINE]

        "Escaping characters",
        [@"\_\\\***\%\+"; @"\_\\\***\%\+"; @"\_\\\*\%\+"],
        [LITERAL "_"; LITERAL @"\"; LITERAL "***"; LITERAL "%"; LITERAL "+"; ENDLINE
         LITERAL "_"; LITERAL @"\"; LITERAL "***"; LITERAL "%"; LITERAL "+"; ENDLINE
         LITERAL "_"; LITERAL @"\"; LITERAL "*"; LITERAL "%"; LITERAL "+"; ENDLINE]

        "Whitespace",
        ["          d    "],
        [WHITESPACE 10; LITERAL "d"; WHITESPACE 4; ENDLINE]

        "Multiline codeblock",
        ["```python"; "This is inside the code block"; "```"],
        [CODEBLOCK ("This is inside the code block\n", Python); ENDLINE]

        "HTML passthrough",
        ["This should not be passed through"; "<div>This should just all be passed through, </div>"
         "This should not, <span>This should not be tokenized []</span>"],
        [LITERAL "This"; WHITESPACE 1; LITERAL "should"; WHITESPACE 1; LITERAL "not"
         WHITESPACE 1; LITERAL "be"; WHITESPACE 1; LITERAL "passed"; WHITESPACE 1
         LITERAL "through"; ENDLINE; LITERAL "<div>"; LITERAL "This should just all be passed through, "; LITERAL "</div>"
         ENDLINE; LITERAL "This"; WHITESPACE 1; LITERAL "should"; WHITESPACE 1; LITERAL "not"; COMMA; WHITESPACE 1
         LITERAL "<span>"; LITERAL "This should not be tokenized []"; LITERAL "</span>"; ENDLINE]
    ]

// --------------------------------------------------
// Property Tests
// --------------------------------------------------
/// Check if output of preprocessor is the same if passed through the preprocessor again
[<Tests>]
let preprocessorPropertyTest =
    testProperty "PreprocessorPropertyTest" <| fun (s: string) ->
        let str =
            // The functions will not work with a null string
            if isNull s then ""
            // There is also a weird interaction with '\' because it escapes itself
            else s.Replace("\\", "")
        let preprocess1 = str |> preprocess
        let preprocess2 = str |> preprocess |> preprocess
        Expect.equal preprocess1 preprocess2 ""
