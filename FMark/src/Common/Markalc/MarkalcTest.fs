module MarkalcTest

open Types
open MarkalcShared
open Markalc
open Expression
open Expecto.ExpectoFsCheck
open Expecto
let EQTest func fname name inp outp =
    testCase name <| fun () ->
    Expect.equal (func inp) outp (sprintf "%s" fname)

// ####################### DATA ###################
let expressionData = [
    "Nested binary expression test with addition",
    "10+10+10",
    30.0 |> Ok;
    "Simple triple multiplication",
    "3*7*5",
    105.0 |> Ok;
    "Left associativity with triple division",
    "60/2/3",
    10.0 |> Ok;
    "Left associativity with triple subtraction",
    "7-5-2",
    0.0 |> Ok;
    "Bracketed subtraction",
    "7-(2-1)",
    6.0 |> Ok;
    "Bracketed subtraction then addition",
    "7-(2-1)+5",
    11.0 |> Ok;
    "Operator precedence changed with brackets",
    "7*(2-3)+5",
    -2.0 |> Ok;
    "Operator precedence",
    "7*2-3+5",
    16.0 |> Ok;
    "Lots of brackets",
    "7*2-(((((((((3+5)))))))))",
    6.0 |> Ok;
    "Testing cellref evaluation (without table)",
    "1+([1,1]+[1,2])",
    27.0 |> Ok; // For test evaluation function without table CellRefs evaluate to 13.0
    "Test left associativity with extra whitespace",
    "2 -4 +6 -1 -1- 0 +8",
    10.0 |> Ok;
    "Pow precendence test",
    "2 ^4 +6 -1 -1- 0 +8",
    28.0 |> Ok;
    "Decimal place test",
    "1/3,2",
    0.33 |> Ok;
    "Decimal place test 3dp",
    "1/3,3",
    0.333 |> Ok
]
let parseDefaultRowData = [
    "All Pipes",
    "|hello |mynameis|",
    [defaultCellU[LITERAL("hello"); WHITESPACE(1)]; defaultCellU[LITERAL("mynameis")]];
    "Only middle pipe",
    "hello |my name is",
    [defaultCellU[LITERAL("hello"); WHITESPACE(1)];defaultCellU[LITERAL("my name is")]];
    "Empty pipes",
    "|||",
    [defaultCellU[];defaultCellU[]];
    "One pipe",
    "hi|",
    [defaultCellU[LITERAL("hi")];defaultCellU[]];
    "No end pipe",
    "|hello my |name",
    [defaultCellU [LITERAL("hello my");WHITESPACE(1)] ; defaultCellU[LITERAL("name")]];
    "No start pipe",
    "hello my |name|",
    [defaultCellU [LITERAL("hello my");WHITESPACE(1)] ; defaultCellU[LITERAL("name")]];
    "Empty pipes in middle",
    "some test||stuff 0398 test",
    [defaultCellU [LITERAL("some test")];defaultCellU[] ; defaultCellU[LITERAL("stuff");WHITESPACE(1);NUMBER("0398");WHITESPACE(1);LITERAL("test")]]
]
let alignmentData = [
    "No alignments",
    "---|----|---",
    [NoAlign;NoAlign;NoAlign] |> Ok;
    "Middle right aligned",
    "---|---:|---",
    [NoAlign;Right;NoAlign] |> Ok;
    "No alignments, superfluous pipes both sides",
    "|---|---|---|",
    [NoAlign;NoAlign;NoAlign] |> Ok;
    "All centre aligned, both outside pipes",
    "|:---:|:---:|:---:|",
    [Centre;Centre;Centre] |> Ok;
    "All centre aligned, no rhs pipe",
    "|:---:|:---:|:---:",   
    [Centre;Centre;Centre] |> Ok;
    "All centre aligned, no superfluous pipes",
    ":---:|:---:|:---:",   
    [Centre;Centre;Centre] |> Ok;
    "Simple all align types",
    "---|:---:|---:",
    [NoAlign;Centre;Right] |> Ok;
    "Complex non-symmetric using all aligns w/o outside pipes",
    "---|:---:|---:|:---:|:---|---:|:---:|---:|:---",
    [NoAlign;Centre;Right;Centre;Left;Right;Centre;Right;Left] |> Ok;
    "Complex non-symmetric using all aligns with outside pipes",
    "|---|:---:|---:|:---:|:---|---:|:---:|---:|:---|",
    [NoAlign;Centre;Right;Centre;Left;Right;Centre;Right;Left] |> Ok
]
let basicTableData = [
    "Table parse into cells with no functions",
    List.map simpleLex ["header1|header2|header3";
                          ":------|:-----:|------:";
                          "Somedfs|tesdfst|stduff"] ,
    // Answer, two lists of aligned cells
    [Cells([Contents ([LITERAL "header1"],true,Left);Contents ([LITERAL "header2"],true,Centre);Contents ([LITERAL "header3"],true,Right)],true)] @
    [Cells([Contents ([LITERAL "Somedfs"],false,Left);Contents ([LITERAL "tesdfst"],false,Centre);Contents ([LITERAL "stduff"],false,Right)],false)] |> Ok;
]

let align =  ":------|:-----:|------:"
let ans head a b c = Cells ([Contents (a,head,Left)  ;
                            Contents (b,head,Centre);
                            Contents (c,head,Right) ], head)

let fullTestData = [
    "No content after header pipe",
    ["|"; "---|";],
    [Cells([Contents ([],true,NoAlign);],true)] |> Ok;
    "No content after pipe",
    ["hi"; "---|";"|"; ],
    [Cells([Contents ([LITERAL("hi")],true,NoAlign);],true);
     Cells([Contents ([],false,NoAlign);],false)] |> Ok;
    "Two empty pipes",
    ["hi|you"; "---|---";"||"; ],
    [Cells([Contents ([LITERAL("hi")],true,NoAlign);Contents ([LITERAL("you")],true,NoAlign)],true);
     Cells([Contents ([],false,NoAlign);Contents ([],false,NoAlign)],false)] |> Ok;
    "Single cell table no header pipe",
    ["=2+2"; "---|"; ],
    [Cells([Contents ([NUMBER("4")],true,NoAlign);],true)] |> Ok;
    "Single cell table",
    ["=2+2|"; "---|"; ],
    [Cells([Contents ([NUMBER("4")],true,NoAlign);],true)] |> Ok;
    "Two rows no pipes cell reference",
    ["=2+2"; "---|"; "=[0,0]+3";],
    [Cells ([Contents ([NUMBER("4")],true,NoAlign);],true);Cells ([Contents ([NUMBER("7")],false,NoAlign);],false)] |> Ok;
    "Single row table",
    ["=2+2|header2|header3"; align; ],
    [ans true [NUMBER "4"] [LITERAL "header2"] [LITERAL "header3"]] |> Ok;
    "Deciaml point test",
    ["=1/3,2|=[0,0]*3|=[0,0],5"; align; ],
    [ans true [NUMBER "0.33"] [NUMBER "1"] [NUMBER "0.33333"]] |> Ok;
    "Full evaluation test with cell references",
    ["=2+2|header2|header3"; align;
     "=[0,0]+1|tesdfst|stduff";
     "=2+3|tesdfst|=[1,0]+[0,0]" ],
    [ans true [NUMBER "4"] [LITERAL "header2"] [LITERAL "header3"];
     ans false [NUMBER "5"] [LITERAL "tesdfst"] [LITERAL "stduff"];
     ans false [NUMBER "5"] [LITERAL "tesdfst"] [NUMBER "9"]]|> Ok;
    "Circular cell reference",
    ["=2+2|header2|header3"; align;
    "=[0,0]+[2,2]|tesdfst|stduff";
    "=2+3|tesdfst|=[1,0]+[0,0]"],
    [ans true [NUMBER "4"] [LITERAL "header2"] [LITERAL "header3"];
     ans false [NUMBER "NaN"] [LITERAL "tesdfst"] [LITERAL "stduff"];
     ans false [NUMBER "5"] [LITERAL "tesdfst"] [NUMBER "NaN"]] |> Ok;
    "SUM range function call",
    ["=5|header2|header3";
    ":------|:-----:|------:";
    "=7|tesdfst|stduff";
    "=2+3|=SUM{[0,0]:[2,0]}|0"],
    [ans true [NUMBER "5"] [LITERAL "header2"] [LITERAL "header3"];
     ans false [NUMBER "7"] [LITERAL "tesdfst"] [LITERAL "stduff"];
     ans false [NUMBER "5"] [NUMBER "17"] [NUMBER "0"]] |> Ok;
    "SUM and AVG function calls",
    ["=5|header2|header3";
    ":------|:-----:|------:";
    "=AVG{1,6,8}|=8|stduff";
    "=2+3|=SUM{[0,0],[2,0],[1,1]}|0"],
    [ans true [NUMBER "5"] [LITERAL "header2"] [LITERAL "header3"];
     ans false [NUMBER "5"] [NUMBER "8"] [LITERAL "stduff"];
     ans false [NUMBER "5"] [NUMBER "18"] [NUMBER "0"]] |> Ok;
    "Horizontal cell ref range",
    ["Calcs|=39|=42";
    ":------|:-----:|------:";
    "||=SUM{[0,1]:[0,2]}|=[1,1]+3"],
    [ans true [LITERAL "Calcs"] [NUMBER "39"] [NUMBER "42"];
     ans false [] [NUMBER "81"] [NUMBER "84"]] |> Ok
    "Function with cell ref range and other arguments",
    ["Calcs|39|42";
    ":------|:-----:|------:";
    "||=39|=42";
    "||=6*5+SUM{[1,1]:[1,2],4,5}-(39+42)|=[1,1]+3"],
    [ans true [LITERAL "Calcs"] [NUMBER "39"] [NUMBER "42"];
     ans false [] [NUMBER "39"] [NUMBER "42"];
     ans false [] [NUMBER "39"] [NUMBER "42"]] |> Ok;
    "Function within a function",
    ["Calcs|=39|=42";
    ":------|:-----:|------:";
    "||=6*5+SUM{SUM{2,7},8,5}|=[1,1]-10"],
    [ans true [LITERAL "Calcs"] [NUMBER "39"] [NUMBER "42"];
     ans false [] [NUMBER "52"] [NUMBER "42"]] |> Ok;
    "MIN/MAX function test",
    ["=MIN{3,4,2,5,3,2,4,5,7,1,20}|=MAX{3,4,2,5,3,2,4,5,7,1,20}"; "---|---"; ],
    [Cells ([Contents ([NUMBER("1")],true,NoAlign);Contents ([NUMBER("20")],true,NoAlign)],true)] |> Ok;
    "Cell reference with key word functionality",
    ["=5|header2|header3";
    ":------|:-----:|------:";
    "=7|tesdfst|stduff";
    "=2+3|=[1,0]+12|=[col=0,row=2]+[row=1,col=0]"],
    [ans true [NUMBER "5"] [LITERAL "header2"] [LITERAL "header3"];
     ans false [NUMBER "7"] [LITERAL "tesdfst"] [LITERAL "stduff"];
     ans false [NUMBER "5"] [NUMBER "19"] [NUMBER "12"]] |> Ok;
]
// ####################### FUNCTIONS #####################
let addTestList test name dataTransform data = 
    (List.map (dataTransform >> (unfoldTuple3 test)) data)
    |> Expecto.Tests.testList name
let expressionTest = EQTest parseExpTest "parseExpTop"
let parseRowTest = EQTest parseDefaultRow "parseDefaultRow"
let parseAlignmentRowTest = EQTest parseAlignRow "parseAlignRow"
let transformTableTest = EQTest transformTable "transformTable"
let fullTest = EQTest lexParseEvaluate "evaluation"

// Not tests
let testMarkdown print =
    let printTestMarkdown name lst =
        (sprintf "## %s\r\n\r\n|UnitTest|Pass/Fail\r\n|---|---|\r\n" name) 
        + (List.fold (fun s x -> s + (sprintf "|%s|Pass|\r\n" x)) "" lst)
        + "\r\n"
    let getFst3 lst = List.map (fun (x,_,_)->x) lst
    
    let testTable = "# Tests\r\n" +
                    (printTestMarkdown "Expression parser and evaluator" (getFst3 expressionData)) +
                    (printTestMarkdown "Default row parser" (getFst3 parseDefaultRowData)) +
                    (printTestMarkdown "Alignment row parser" (getFst3 alignmentData)) +
                    (printTestMarkdown "Basic table parse" (getFst3 basicTableData)) +
                    (printTestMarkdown "Full Markalc test" (getFst3 fullTestData))
    // printToFile "TESTS.md" //sprintf "%s" |>
    if print then FileIO.writeToFile "TESTS.md" testTable else ()
let funcList = [( % ),"%";( ** ),"^";( + ),"+";( - ),"-"; ( * ),"*"; ( / ),"/"]
let expressionPropertyTest op = 
    testProperty (sprintf "Num %A Num is Num %A Num" op op) <|
    (fun (x:int,y:int) ->
        let tostr = function | i when i < 0 -> sprintf "(0-%i)" (-i)
                             | i -> string i
        let instr = tostr x + (snd op) + tostr y
        //sprintf "%i%s%i" x (snd op) y
        Expect.equal
            (instr |> simpleLex |> parseExpTest |> (function | Ok(x)->string x | Error(e) -> string e)) // Actual
            ((fst op) (x|>float) (y|>float) |> string) // Expected
            (sprintf "x %A y is x %A y" op op)
    )

// ####################### TESTS ########################
[<Tests>]
let tests = 
    testList "Should pass" [
        addTestList expressionTest "Expression tests" lexY expressionData;
        addTestList parseRowTest "parseDefaultRow tests" lexY parseDefaultRowData;
        addTestList parseAlignmentRowTest "parseAlignmentRow tests" lexY alignmentData;
        addTestList transformTableTest "transformTable tests" id basicTableData;
        addTestList fullTest "fullTable tests" id fullTestData;
]


    
[<Tests>]
let propertyTests =
    List.map (expressionPropertyTest) funcList
    |> Expecto.Tests.testList "Expression property tests."


    
//let runMarkalcTests = Expecto.Tests.runTestsInAssembly Expecto.Tests.defaultConfig [||] |> ignore 
    
    