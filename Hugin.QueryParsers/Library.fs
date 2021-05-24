namespace Hugin.QueryParsers

open FParsec

module SubmissionParser =

    type Reference =
        | LectureOwnerAccount
        | LectureName
        | ActivityName
        | UserAccount
        | UserName
        | Tag
        | Page
        | Hash
        | Grade
        | SubmittedAt
        | Deadline
        | ResubmitDeadline
        | MarkedAt
        | Count
        | HasSubmission
        | State


    type StringAtom =
        | LectureOwnerAccount
        | LectureName
        | ActivityName
        | UserAccount
        | UserName
        | Tag
        | Page
        | Hash
        | Grade
        | StringLiteral of string
    and StringExpr =
        | StringAtom of StringAtom
    
    type IntegerAtom =
        | Count
        | IntegerLiteral of int
    and IntegerExpr =
        | IntegerAtom of IntegerAtom
        | Minus of IntegerExpr
        | Sum of IntegerExpr * IntegerExpr
        | Sub of IntegerExpr * IntegerExpr
        | Mul of IntegerExpr * IntegerExpr
        | Div of IntegerExpr * IntegerExpr

    type DateTimeSpan =
        | Day
        | Hour
        | Minute

    type DateTimeAtom =
        | SubmittedAt
        | Deadline
        | ResubmitDeadline
        | MarkedAt
        | DateTimeLiteral of int * int * int * int * int * int
        | Today
        | Now
    and DateTimeExpr =
        | DateTimeAtom of DateTimeAtom
        | AddDate of DateTimeExpr * IntegerExpr
        | ReduceDate of DateTimeExpr * IntegerExpr

    type BooleanAtom =
        | HasSubmission
        | True
        | False
        | SEq of StringExpr * StringExpr
        | SNe of StringExpr * StringExpr
        | SStartWith of StringExpr * StringExpr
        | SEndWith of StringExpr * StringExpr
        | SHas of StringExpr * StringExpr
        | DeEq of IntegerExpr * IntegerExpr
        | DeNe of IntegerExpr * IntegerExpr
        | DeGt of IntegerExpr * IntegerExpr
        | DeGe of IntegerExpr * IntegerExpr
        | DeLt of IntegerExpr * IntegerExpr
        | DeLe of IntegerExpr * IntegerExpr
        | DtEq of DateTimeExpr * DateTimeExpr
        | DtNe of DateTimeExpr * DateTimeExpr
        | DtGt of DateTimeExpr * DateTimeExpr
        | DtGe of DateTimeExpr * DateTimeExpr
        | DtLt of DateTimeExpr * DateTimeExpr
        | DtLe of DateTimeExpr * DateTimeExpr
    and BooleanExpr =
        | BooleanAtom of BooleanAtom
        | Eq of BooleanExpr * BooleanExpr
        | Ne of BooleanExpr * BooleanExpr
        | Neg of BooleanExpr
        | Conj of BooleanExpr * BooleanExpr
        | Disj of BooleanExpr * BooleanExpr
        | Impl of BooleanExpr * BooleanExpr

    type SortingAttribute =
        | LectureName
        | ActivityName
        | UserAccount
        | Grade
        | SubmittedAt
        | Deadline
        | ResubmitDeadline
        | MarkedAt
        | Count
        | State
    type SortingOrder =
        | Asc
        | Desc

    type Ordering = SortingAttribute * SortingOrder
    type Query = (BooleanExpr list * (Ordering list) option)

    //------
    
    let pipe6 p1 p2 p3 p4 p5 p6 f =
           p1 >>= fun x1 ->
            p2 >>= fun x2 ->
             p3 >>= fun x3 ->
              p4 >>= fun x4 ->
               p5 >>= fun x5 ->
                p6 >>= fun x6 -> preturn (f x1 x2 x3 x4 x5 x6)


    //------

    type UserState = unit

    let pstring_ws s : Parser<string, UserState> = pstring s .>> spaces
    let pinteger_ws : Parser<int, UserState> = pint16 |>> int .>> spaces
    let pbetweenParentheses_ws p = pchar '(' >>. spaces >>. p .>> spaces .>> pchar ')' .>> spaces


    // String parser
    
    let pstringUserAccount : Parser<StringAtom, UserState> = stringReturn "user-account" StringAtom.UserAccount
    let pstringUserName : Parser<StringAtom, UserState> = stringReturn "user-name" StringAtom.UserName
    let pstringLectureOwnerAccount : Parser<StringAtom, UserState> = stringReturn "lecture-owner-account" StringAtom.LectureOwnerAccount
    let pstringLectureName : Parser<StringAtom, UserState> = stringReturn "lecture-name" StringAtom.LectureName
    let pstringActivityName : Parser<StringAtom, UserState> = stringReturn "acitivty-name" StringAtom.ActivityName
    let pstringGrade : Parser<StringAtom, UserState> = stringReturn "grade" StringAtom.Grade
    let pstringHash : Parser<StringAtom, UserState> = stringReturn "hash" StringAtom.Hash
    let pstringPage : Parser<StringAtom, UserState> = stringReturn "page" StringAtom.Page
    let pstringHasTag : Parser<StringAtom, UserState> = stringReturn "tag" StringAtom.Tag

    let pstringLiteral : Parser<(StringAtom), UserState> =
        manyChars (noneOf ['"']) |> between (pchar '"') (pchar '"') .>> spaces |>> string |>> StringLiteral
    let pstringAtom = 
        (attempt pstringUserAccount <|> attempt pstringUserName <|> attempt pstringLectureOwnerAccount <|> attempt pstringLectureName <|> attempt pstringActivityName <|> attempt pstringGrade <|> attempt pstringHash <|> attempt pstringPage <|> attempt pstringHasTag <|> attempt pstringLiteral <|> pstringHasTag)
        .>> spaces
    let pstringExpr = pstringAtom |>> StringAtom
    

    // IntegerExpr parser
    let pintegerCount : Parser<IntegerAtom, UserState> = stringReturn "count" IntegerAtom.Count

    let pintegerLiteral : Parser<(IntegerAtom), UserState> = pint64 |>> int |>> IntegerLiteral

    let pintegerAtom : Parser<IntegerAtom, UserState> =
        (attempt pintegerLiteral <|> pintegerCount) .>> spaces

    let pintegerOp1 =
        choice
            [ pchar '+' .>> spaces >>% (fun x y -> Sum(x, y) )
              pchar '-' .>> spaces >>% (fun x y -> Sub(x, y) ) ]
    let pintegerOp2 =
        choice
            [ pchar '*' .>> spaces >>% (fun x y -> Mul(x, y) )
              pchar '\\' .>> spaces >>% (fun x y -> Div(y, x) ) ]
        
    let pintegerExpr, pintegerExprImpl = createParserForwardedToRef ()
    let pintegerFactor : Parser<IntegerExpr, UserState> =
        attempt (pbetweenParentheses_ws pintegerExpr)
        <|> (pstring "-" >>. spaces >>. pintegerExpr |>> Minus)
        <|> (pintegerAtom |>> IntegerAtom)
    let pintegerTerm : Parser<IntegerExpr, UserState> =
        chainl1 pintegerFactor pintegerOp2
    pintegerExprImpl :=
        chainl1 pintegerTerm pintegerOp1

        
    // DateTimeExpr parser
    let pdatetimeSubmittedAt : Parser<DateTimeAtom, UserState> = stringReturn "submitted-at" DateTimeAtom.SubmittedAt
    let pdatetimeDeadline : Parser<DateTimeAtom, UserState> = stringReturn "deadline" DateTimeAtom.Deadline
    let pdatetimeResubmitDeadline : Parser<DateTimeAtom, UserState> = stringReturn "resubmit-deadline" DateTimeAtom.ResubmitDeadline
    let pdatetimeMarkedAt : Parser<DateTimeAtom, UserState> = stringReturn "marked-at" DateTimeAtom.MarkedAt



    let pdateLiteral : Parser<(DateTimeAtom), UserState> =
        pipe6 pint16 (pstring "/" >>. pint16) (pstring "/" >>. pint16) (pstring "-" >>. pint16)  (pstring ":" >>. pint16) (pstring ":" >>. pint16)
              (fun y m d h min s -> DateTimeLiteral(int y, int m, int d, int h, int min, int s))
              
    let pdateToday : Parser<DateTimeAtom, UserState> = stringReturn "today" Today
    let pdateNow : Parser<DateTimeAtom, UserState> = stringReturn "now" Now

    let pdateAtom : Parser<DateTimeAtom, UserState> =
        (attempt pdateLiteral <|> attempt pdateToday <|> attempt pdatetimeSubmittedAt <|> attempt pdatetimeDeadline <|> pdatetimeResubmitDeadline <|> pdatetimeMarkedAt <|> pdateNow) .>> spaces
    let pdateExpr, pdateExprImpl = createParserForwardedToRef ()
    let pdateFactor : Parser<DateTimeExpr, UserState> =
        attempt (pbetweenParentheses_ws pdateExpr)
        <|> (pdateAtom |>> DateTimeAtom)
    let pdateTerm : Parser<DateTimeExpr, UserState> =
        attempt (pipe2 pdateFactor (spaces >>. pchar '+' >>. spaces >>. pintegerExpr) (fun x y -> AddDate(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pchar '+' >>. spaces >>. pdateFactor) (fun x y -> AddDate(y, x)))
        <|> attempt (pipe2 pdateFactor (spaces >>. pchar '-' >>. spaces >>. pintegerExpr) (fun x y -> ReduceDate(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pchar '+' >>. spaces >>. pdateFactor) (fun x y -> ReduceDate(y, x)))
        <|> pdateFactor
    pdateExprImpl :=
        pdateTerm
    

    // Boolean parser
    let pbooleanExpr, pbooleanExprImpl = createParserForwardedToRef ()

    let pbooleanHasSubmission : Parser<BooleanAtom, UserState> = stringReturn "hsa-submission" BooleanAtom.HasSubmission

    let pbooleanLiteral : Parser<(BooleanAtom), UserState> = 
        attempt (stringReturn "true" BooleanAtom.True) <|> stringReturn "false" BooleanAtom.False
    
    let pbooleanOp1 =
        choice
            [ pstring "&&" .>> spaces >>% (fun x y -> Conj(x, y) )
              pstring "||" .>> spaces >>% (fun x y -> Disj(x, y) )
              pstring "->" .>> spaces >>% (fun x y -> Impl(x, y) ) ]

    let pbooleanOp2 =
        choice
            [ pstring "==" .>> spaces >>% (fun x y -> Eq(x, y) )
              pstring "!=" .>> spaces >>% (fun x y -> Ne(x, y) ) ]

    let pbooleanAtom : Parser<BooleanAtom, UserState> =
        ( attempt pbooleanLiteral .>> spaces
        <|> attempt pbooleanHasSubmission
        <|> attempt (pipe2 pstringExpr (spaces >>. pstring "==" >>. spaces >>. pstringExpr) (fun x y -> SEq(x, y)))
        <|> attempt (pipe2 pstringExpr (spaces >>. pstring "!=" >>. spaces >>. pstringExpr) (fun x y -> SNe(x, y)))
        <|> attempt (pipe2 pstringExpr (spaces >>. pstring "=*" >>. spaces >>. pstringExpr) (fun x y -> SStartWith(x, y)))
        <|> attempt (pipe2 pstringExpr (spaces >>. pstring "*=" >>. spaces >>. pstringExpr) (fun x y -> SEndWith(x, y)))
        <|> attempt (pipe2 pstringExpr (spaces >>. pstring "->" >>. spaces >>. pstringExpr) (fun x y -> SHas(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pstring "==" >>. spaces >>. pintegerExpr) (fun x y -> DeEq(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pstring "!=" >>. spaces >>. pintegerExpr) (fun x y -> DeNe(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pstring ">"  >>. spaces >>. pintegerExpr) (fun x y -> DeGt(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pstring ">=" >>. spaces >>. pintegerExpr) (fun x y -> DeGe(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pstring "<"  >>. spaces >>. pintegerExpr) (fun x y -> DeLt(x, y)))
        <|> attempt (pipe2 pintegerExpr (spaces >>. pstring "<=" >>. spaces >>. pintegerExpr) (fun x y -> DeLe(x, y)))
        <|> attempt (pipe2 pdateExpr (spaces >>. pstring "==" >>. spaces >>. pdateExpr) (fun x y -> DtEq(x, y)))
        <|> attempt (pipe2 pdateExpr (spaces >>. pstring "!=" >>. spaces >>. pdateExpr) (fun x y -> DtNe(x, y)))
        <|> attempt (pipe2 pdateExpr (spaces >>. pstring ">"  >>. spaces >>. pdateExpr) (fun x y -> DtGt(x, y)))
        <|> attempt (pipe2 pdateExpr (spaces >>. pstring ">=" >>. spaces >>. pdateExpr) (fun x y -> DtGe(x, y)))
        <|> attempt (pipe2 pdateExpr (spaces >>. pstring "<"  >>. spaces >>. pdateExpr) (fun x y -> DtLt(x, y)))
        <|> attempt (pipe2 pdateExpr (spaces >>. pstring "<=" >>. spaces >>. pdateExpr) (fun x y -> DtLe(x, y)))
        ) .>> spaces


    let pbooleanFactor : Parser<BooleanExpr, UserState> =
        attempt (pbetweenParentheses_ws pbooleanExpr)
        <|> (pstring "!" >>. spaces >>. pbooleanExpr |>> Neg)
        <|> (pbooleanAtom |>> BooleanAtom)

    let pbooleanTerm : Parser<BooleanExpr, UserState> = 
        chainl1 pbooleanFactor pbooleanOp2
    pbooleanExprImpl :=
        chainl1 pbooleanTerm pbooleanOp1



    // Sorting parser

    let pattribute : Parser<SortingAttribute, UserState> =
        attempt (stringReturn "lecture-name" SortingAttribute.LectureName)
        <|> attempt (stringReturn "activity-name" SortingAttribute.ActivityName)
        <|> attempt (stringReturn "count" SortingAttribute.Count)
        <|> attempt (stringReturn "user-account" SortingAttribute.UserAccount)
        <|> attempt (stringReturn "submitted-at" SortingAttribute.SubmittedAt)
        <|> attempt (stringReturn "deadline" SortingAttribute.Deadline)
        <|> attempt (stringReturn "grade" SortingAttribute.Grade)
        <|> attempt (stringReturn "marked-at" SortingAttribute.MarkedAt)
        <|> attempt (stringReturn "resubmit-deadline" SortingAttribute.ResubmitDeadline)
        <|> (stringReturn "state" SortingAttribute.State)
    let pordering : Parser<Ordering, UserState> =
        (
        attempt (pchar '~' >>. pattribute |>> (fun x -> (x, Desc)))
        <|> (pattribute |>> (fun x -> (x, Asc)))
        ) .>> spaces


    // Query Parser

    let pcondition : Parser<BooleanExpr list, UserState> =
        sepBy pbooleanExpr (pchar ',' .>> spaces)

    let porderings : Parser<Ordering list, UserState> =
        sepBy pordering (pchar ',' .>> spaces)

    let pquery : Parser<Query, UserState> =
        spaces >>. 
            (
            attempt (pipe2 pcondition (spaces >>. pstring "order by" >>. spaces >>. porderings) (fun x y -> (x, Some(y))))
            <|> (pcondition |>> (fun x -> (x, None)))
            ) .>> eof


    // Functions

    let ParseQuery queryString = 
        match run pquery queryString with
        | Success (result, _, _) -> Some(result)
        | Failure (_, _, _) -> None
        
    let HasError queryString = 
        match run pquery queryString with
        | Success (_, _, _) ->  None
        | Failure (errmsg, _, _) -> Some(errmsg)




    let BooleanExprToPgsqlExprString (expr : BooleanExpr) : string =
        let rec _booleanAtom (atom : BooleanAtom) =
            match atom with
            | BooleanAtom.True -> "TRUE"
            | BooleanAtom.False -> "FALSE"
            | BooleanAtom.HasSubmission -> @"(h.""Flag"" = 1)"
            | BooleanAtom.SEq (lhs, rhs) -> sprintf "(%s = %s)" (_stringExpr lhs) (_stringExpr rhs)
            | BooleanAtom.SNe (lhs, rhs) -> sprintf "(%s <> %s)" (_stringExpr lhs) (_stringExpr rhs)
            | BooleanAtom.SStartWith (lhs, rhs) ->
                sprintf "(%s LIKE '%s%%' AND %s IS NOT NULL)"
                    (_stringExpr lhs)
                    ((_stringExpr rhs).Trim('\'').Replace("'", "''").Replace("_", "\\_").Replace("%", "\\%"))
                    (_stringExpr lhs)
            | BooleanAtom.SEndWith (lhs, rhs) ->
                sprintf "(%s LIKE '%%%s' AND %s IS NOT NULL)"
                    (_stringExpr lhs)
                    ((_stringExpr rhs).Trim('\'').Replace("'", "''").Replace("_", "\\_").Replace("%", "\\%"))
                    (_stringExpr lhs)
            | BooleanAtom.SHas (lhs, rhs) ->
                sprintf "(%s LIKE '%% %s %%' AND %s IS NOT NULL)"
                    (_stringExpr lhs)
                    ((_stringExpr rhs).Trim('\'').Replace("'", "''").Replace("_", "\\_").Replace("%", "\\%"))
                    (_stringExpr lhs)
            | BooleanAtom.DeEq (lhs, rhs) -> sprintf "(%s = %s)" (_integerExpr lhs) (_integerExpr rhs)
            | BooleanAtom.DeNe (lhs, rhs) -> sprintf "(%s <> %s)" (_integerExpr lhs) (_integerExpr rhs)
            | BooleanAtom.DeGt (lhs, rhs) -> sprintf "(%s > %s)" (_integerExpr lhs) (_integerExpr rhs)
            | BooleanAtom.DeGe (lhs, rhs) -> sprintf "(%s >= %s)" (_integerExpr lhs) (_integerExpr rhs)
            | BooleanAtom.DeLt (lhs, rhs) -> sprintf "(%s < %s)" (_integerExpr lhs) (_integerExpr rhs)
            | BooleanAtom.DeLe (lhs, rhs) -> sprintf "(%s <= %s)" (_integerExpr lhs) (_integerExpr rhs)
            | BooleanAtom.DtEq (lhs, rhs) -> sprintf "(%s = %s)" (_datetimeExpr lhs |> fst) (_datetimeExpr rhs |> fst)
            | BooleanAtom.DtNe (lhs, rhs) -> sprintf "(%s <> %s)" (_datetimeExpr lhs |> fst) (_datetimeExpr rhs |> fst)
            | BooleanAtom.DtGt (lhs, rhs) -> sprintf "(%s > %s)" (_datetimeExpr lhs |> fst) (_datetimeExpr rhs |> fst)
            | BooleanAtom.DtGe (lhs, rhs) -> sprintf "(%s >= %s)" (_datetimeExpr lhs |> fst) (_datetimeExpr rhs |> fst)
            | BooleanAtom.DtLt (lhs, rhs) -> sprintf "(%s < %s)" (_datetimeExpr lhs |> fst) (_datetimeExpr rhs |> fst)
            | BooleanAtom.DtLe (lhs, rhs) -> sprintf "(%s <= %s)" (_datetimeExpr lhs |> fst) (_datetimeExpr rhs |> fst)
        
        and _stringAtom (atom : StringAtom) : string =
            match atom with
            | StringAtom.LectureOwnerAccount -> @"hlu.""Account"""
            | StringAtom.LectureName -> @"hl.""Name"""
            | StringAtom.ActivityName -> @"h.""ActivityName"""
            | StringAtom.UserAccount -> @"hu.""Account"""
            | StringAtom.UserName -> @"hu.""DisplayName"""
            | StringAtom.Grade -> @"h.""Grade"""
            | StringAtom.Hash -> @"h.""Hash"""
            | StringAtom.Page -> @"h.""Page"""            
            | StringAtom.Tag -> @"(' ' || h.""Tags"" || ' ')"
            | StringAtom.StringLiteral literal -> sprintf "'%s'" (literal.Replace("'", "''"))

        and _integerAtom (atom : IntegerAtom) : string =
            match atom with
            | IntegerAtom.Count -> @"h.""Count"""
            | IntegerAtom.IntegerLiteral literal -> literal.ToString()

        and _datetimeAtom (atom : DateTimeAtom) : (string * DateTimeSpan) =
            match atom with
            | DateTimeAtom.SubmittedAt -> (@"h.""SubmittedAt""", DateTimeSpan.Day)
            | DateTimeAtom.Deadline -> (@"h.""Deadline""", DateTimeSpan.Day)
            | DateTimeAtom.MarkedAt -> (@"h.""MarkedAt""", DateTimeSpan.Day)
            | DateTimeAtom.ResubmitDeadline -> (@"h.""ResubmitDeadline""", DateTimeSpan.Day)
            | DateTimeAtom.DateTimeLiteral (y, m, d, h, min, s) -> (sprintf "(timestamp '%04d-%02d-%02d %02d:%02d:%02d')" y m d h min s, DateTimeSpan.Day)
            | DateTimeAtom.Today ->
                let today = System.DateTime.Today;
                in (sprintf "(timestamp '%04d-%02d-%02d 00:00:00')" today.Year today.Month today.Day, DateTimeSpan.Day)
            | DateTimeAtom.Now ->
                let today = System.DateTime.Now;
                in (sprintf "(timestamp '%04d-%02d-%02d %02d:%02d:%02d')" today.Year today.Month today.Day today.Hour today.Minute today.Second, DateTimeSpan.Minute)

        and _booleanExpr (expr : BooleanExpr) : string =
            match expr with
            | BooleanAtom atom -> _booleanAtom atom
            | Eq (lhs, rhs) -> sprintf "(%s = %s)" (_booleanExpr lhs) (_booleanExpr rhs)
            | Ne (lhs, rhs) -> sprintf "(%s <> %s)" (_booleanExpr lhs) (_booleanExpr rhs)
            | Neg inner -> sprintf "(NOT %s)" (_booleanExpr inner)
            | Conj (lhs, rhs) -> sprintf "(%s AND %s)" (_booleanExpr lhs) (_booleanExpr rhs)
            | Disj (lhs, rhs) -> sprintf "(%s OR %s)" (_booleanExpr lhs) (_booleanExpr rhs)
            | Impl (lhs, rhs) -> sprintf "((NOT %s) OR %s)" (_booleanExpr lhs) (_booleanExpr rhs)
        
        and _stringExpr (expr : StringExpr) : string =
            match expr with
            | StringAtom atom -> _stringAtom atom
        
        and _integerExpr (expr : IntegerExpr) : string =
            match expr with
            | IntegerAtom atom -> _integerAtom atom
            | Sum (lhs, rhs) -> sprintf "(%s + %s)" (_integerExpr lhs) (_integerExpr rhs)
            | Sub (lhs, rhs) -> sprintf "(%s - %s)" (_integerExpr lhs) (_integerExpr rhs)
            | Mul (lhs, rhs) -> sprintf "(%s * %s)" (_integerExpr lhs) (_integerExpr rhs)
            | Div (lhs, rhs) -> sprintf "(%s / %s)" (_integerExpr lhs) (_integerExpr rhs)
            | Minus inner -> sprintf "(-%s)" (_integerExpr inner)
        and _datetimeExpr (expr : DateTimeExpr) : (string * DateTimeSpan) =
            match expr with
            | DateTimeAtom atom -> _datetimeAtom atom
            | AddDate (lhs, rhs) ->
                let date, span = _datetimeExpr lhs
                match span with
                | DateTimeSpan.Day ->  (sprintf "(%s + (interval '%s day'))" date (_integerExpr rhs), DateTimeSpan.Day)
                | DateTimeSpan.Hour ->  (sprintf "(%s + (interval '%s hour'))" date (_integerExpr rhs), DateTimeSpan.Hour)
                | DateTimeSpan.Minute ->  (sprintf "(%s + (interval '%s minute'))" date (_integerExpr rhs), DateTimeSpan.Minute)
            | ReduceDate (lhs, rhs) ->
                let date, span = _datetimeExpr lhs
                match span with
                | DateTimeSpan.Day ->  (sprintf "(%s - (interval '%s day'))" date (_integerExpr rhs), DateTimeSpan.Day)
                | DateTimeSpan.Hour ->  (sprintf "(%s - (interval '%s hour'))" date (_integerExpr rhs), DateTimeSpan.Hour)
                | DateTimeSpan.Minute ->  (sprintf "(%s - (interval '%s minute'))" date (_integerExpr rhs), DateTimeSpan.Minute)
        _booleanExpr expr

    let GetReferences (expr : BooleanExpr) : Reference list =
        let rec _booleanAtom (atom : BooleanAtom) (xs : Reference list) =
            match atom with
            | BooleanAtom.HasSubmission -> Reference.HasSubmission::xs
            | BooleanAtom.SEq (lhs, rhs) -> (_stringExpr lhs xs) @ (_stringExpr rhs xs) @ xs
            | BooleanAtom.SNe (lhs, rhs) -> (_stringExpr lhs xs) @ (_stringExpr rhs xs) @ xs
            | BooleanAtom.SStartWith (lhs, rhs) -> (_stringExpr lhs xs) @ (_stringExpr rhs xs) @ xs
            | BooleanAtom.SEndWith (lhs, rhs) -> (_stringExpr lhs xs) @ (_stringExpr rhs xs) @ xs
            | BooleanAtom.SHas (lhs, rhs) -> (_stringExpr lhs xs) @ (_stringExpr rhs xs) @ xs
            | BooleanAtom.DeEq (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | BooleanAtom.DeNe (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | BooleanAtom.DeGt (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | BooleanAtom.DeGe (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | BooleanAtom.DeLt (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | BooleanAtom.DeLe (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | BooleanAtom.DtEq (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_datetimeExpr rhs xs) @ xs
            | BooleanAtom.DtNe (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_datetimeExpr rhs xs) @ xs
            | BooleanAtom.DtGt (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_datetimeExpr rhs xs) @ xs
            | BooleanAtom.DtGe (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_datetimeExpr rhs xs) @ xs
            | BooleanAtom.DtLt (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_datetimeExpr rhs xs) @ xs
            | BooleanAtom.DtLe (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_datetimeExpr rhs xs) @ xs
            | _ -> xs
        and _stringAtom (atom : StringAtom) (xs : Reference list) : Reference list =
            match atom with
            | StringAtom.LectureOwnerAccount -> Reference.LectureOwnerAccount::xs
            | StringAtom.LectureName -> Reference.LectureName::xs
            | StringAtom.ActivityName -> Reference.ActivityName::xs
            | StringAtom.UserAccount -> Reference.UserAccount::xs
            | StringAtom.UserName -> Reference.UserName::xs
            | StringAtom.Grade -> Reference.Grade::xs
            | StringAtom.Hash -> Reference.Hash::xs
            | StringAtom.Page -> Reference.Page::xs
            | StringAtom.Tag -> Reference.Tag::xs
            | _ -> xs
        and _integerAtom (atom : IntegerAtom) (xs : Reference list) : Reference list =
            match atom with
            | IntegerAtom.Count -> Reference.Count::xs
            | _ -> xs
        and _datetimeAtom (atom : DateTimeAtom) (xs : Reference list) : Reference list=
            match atom with
            | DateTimeAtom.SubmittedAt -> Reference.SubmittedAt::xs
            | DateTimeAtom.Deadline -> Reference.Deadline::xs
            | DateTimeAtom.MarkedAt -> Reference.MarkedAt::xs
            | DateTimeAtom.ResubmitDeadline -> Reference.ResubmitDeadline::xs
            | _ -> xs
        and _booleanExpr (expr : BooleanExpr) (xs : Reference list) : Reference list =
            match expr with
            | BooleanAtom atom -> (_booleanAtom atom xs) @ xs
            | Eq (lhs, rhs) -> (_booleanExpr lhs xs) @ (_booleanExpr rhs xs) @ xs
            | Ne (lhs, rhs) -> (_booleanExpr lhs xs) @ (_booleanExpr rhs xs) @ xs
            | Neg inner -> (_booleanExpr inner xs) @ xs
            | Conj (lhs, rhs) -> (_booleanExpr lhs xs) @ (_booleanExpr rhs xs) @ xs
            | Disj (lhs, rhs) -> (_booleanExpr lhs xs) @ (_booleanExpr rhs xs) @ xs
            | Impl (lhs, rhs) -> (_booleanExpr lhs xs) @ (_booleanExpr rhs xs) @ xs
        and _stringExpr (expr : StringExpr) (xs : Reference list) : Reference list =
            match expr with
            | StringAtom atom -> (_stringAtom atom xs) @ xs
        and _integerExpr (expr : IntegerExpr) (xs : Reference list) : Reference list =
            match expr with
            | IntegerAtom atom -> (_integerAtom atom xs) @ xs
            | Sum (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | Sub (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | Mul (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | Div (lhs, rhs) -> (_integerExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | Minus inner -> _integerExpr inner xs
        and _datetimeExpr (expr : DateTimeExpr) (xs : Reference list) : Reference list =
            match expr with
            | DateTimeAtom atom -> (_datetimeAtom atom xs) @ xs
            | AddDate (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_integerExpr rhs xs) @ xs
            | ReduceDate (lhs, rhs) -> (_datetimeExpr lhs xs) @ (_integerExpr rhs xs) @ xs
        _booleanExpr expr []