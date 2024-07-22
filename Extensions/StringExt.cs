﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VLib
{
    public static class StringExt
    {
        public static string RecombinePieces(this string[] pieces, string excluding, string addBefore = "", string addAfter = "")
        {
            string completeString = "";
            for (int i = 0; i < pieces.Length; i++)
            {
                if (false == pieces[i].Contains(excluding))
                    completeString += addBefore + pieces[i] + addAfter;
            }
            return completeString;
        }
        
        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }
        
        public static readonly HashSet<string> ReservedWords = new()
        {
            "CON", "PRN", "AUX", "NUL", "CLOCK$", 
            "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static readonly HashSet<char> InvalidFileNameChars = new()
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*'
        };
        
        public static string ToSafeFilePath(this string input)
        {
            if (ReservedWords.Contains(input.ToUpper()))
                input = $"{input}_";
            
            var charArray = input.ToCharArray();
            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (InvalidFileNameChars.Contains(c))
                    charArray[i] = '_';
            }

            return new string(charArray);
        }
    } 
}