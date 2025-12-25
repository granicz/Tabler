
module Lexer
open System
open Parser
open FSharp.Text.Lexing
open UISynth/// Rule main
val main: lexbuf: LexBuffer<char> -> token
/// Rule string_literal
val string_literal: pos1: obj -> s: obj -> lexbuf: LexBuffer<char> -> token
/// Rule html
val html: pos1: obj -> acc: obj -> lexbuf: LexBuffer<char> -> token
