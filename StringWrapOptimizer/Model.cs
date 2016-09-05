// Model.cs
// 
// Copyright (c) 2016-2016 midolin limegreen All right reserved.
// 
// License:
// See LICENSE.md or README.md in solution root directory.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NMeCab;

namespace KinokoBreaker
{
    public class Model
    {
        public static string DictionaryDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\dic");

        public static void LoadConfig(ref int lineWidth, ref string font, ref int fontSize)
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.txt")))
            {
                using (var reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.txt")))
                {
                    lineWidth = int.Parse(reader.ReadLine() ?? "0");
                    font = reader.ReadLine() ?? "メイリオ";
                    fontSize = int.Parse(reader.ReadLine() ?? "0");
                    var dic = reader.ReadLine();

                    if (Directory.Exists(dic) && File.Exists(Path.Combine(dic, "dicrc")))
                        DictionaryDirectory = dic;
                    else
                        DictionaryDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\dic");
                }
            }
        }

        public static string Optimize(string text, string font = "メイリオ", int fontSize = 24, bool bold = false,
                                      bool italic = false, int lineWidth = 1920,
                                      bool skipBreaks = false, bool breakPerElement = false,
                                      bool breakPerCharacter = false)
        {
            // 文字ごとに処理
            if (breakPerCharacter)
                return AllocateStringInPen(font, fontSize, bold, italic, lineWidth, text);

            var words = new List<string>();

            using (var meCabTagger = MeCabTagger.Create(new MeCabParam {DicDir = DictionaryDirectory}))
            {
                var node = meCabTagger.ParseToNode(text);

                var surface = "";
                var isWordTerminated = false;
                var preBreakWord = false;
                var currentPos = 0;
                var splitter = new[] {"助詞", "助動詞", "記号"};
                // この文字から始まる要素は、前の要素とくっつけて考える
                var ignoreCharas = new[]
                {
                    'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ', 'ゃ', 'ゅ', 'ょ', 'を', 'ん',
                    'ァ', 'ィ', 'ゥ', 'ェ', 'ォ', 'ャ', 'ュ', 'ョ',
                    'ｧ', 'ｨ', 'ｩ', 'ｪ', 'ｫ', 'ｬ', 'ｭ', 'ｮ'
                };

                while ((node = node.Next) != null)
                {
                    if (node.Stat != MeCabNodeStat.Bos
                        && node.Stat != MeCabNodeStat.Eos)
                    {
                        // 名詞が小文字から始まる / 名詞が1文字である → スキップ
                        if ((node.Next != null
                             && ignoreCharas.Contains(node.Next.Surface[0]))
                            || (node.Feature.StartsWith("名詞")
                                && node.Surface.Length == 1
                                && !char.IsPunctuation(node.Surface[0]) && !char.IsLower(node.Surface[0])))
                        {
                            ForwardChars(ref text, ref node, ref surface, ref currentPos, skipBreaks);
                            continue;
                        }

                        if (splitter.Contains(node.Feature.Split(',')[0]))
                        {
                            // 開き括弧の後は改行させない
                            if (node.Feature.Split(',')[1].Contains("括弧開"))
                                preBreakWord = true;
                            if (node.Feature.Split(',')[1].Contains("括弧閉"))
                                isWordTerminated = true;

                            // "助詞" - "助詞" など、連続する場合があるため、最後に結合させる
                            if (node.Next?.Stat != MeCabNodeStat.Eos
                                && !splitter.Contains(node.Next?.Feature.Split(',')[0]))
                                isWordTerminated = true;
                            else if (new[] {"句点", "読点"}.Contains(node.Feature.Split(',')[1]))
                                isWordTerminated = true;
                        }

                        // 細かい改行が要求されている → 助詞以外で改行させる
                        if (!isWordTerminated && breakPerElement
                            && (!splitter.Contains(node.Feature.Split(',')[0])
                                && node.Next == null
                                ||
                                (node.Next.Stat != MeCabNodeStat.Eos
                                && !splitter.Contains(node.Next.Feature.Split(',')[0]))))
                            isWordTerminated = true;

                        /*
                        if (isWordTerminated)
                        {
                            // 次が助詞、助動詞、記号でない→改行できる場所である
                            isWordTerminated = true;
                        }
                        */
                        if (preBreakWord)
                            AddWord(ref words, ref surface);

                        ForwardChars(ref text, ref node, ref surface, ref currentPos, skipBreaks);

                        if (new[] {' ', '　'}.Contains(surface[surface.Length - 1]))
                            isWordTerminated = true;

                        if (!preBreakWord && isWordTerminated)
                            AddWord(ref words, ref surface);

                        if (preBreakWord || isWordTerminated)
                        {
                            preBreakWord = false;
                            isWordTerminated = false;
                        }
                    }

                    // 探索文字列の終端に達した場合、文字列を最後まで追加する
                    if (node.Next == null)
                        words.Add(surface);
                }
            }
            return AllocateStringInPen(font, fontSize, bold, italic, lineWidth, words);
        }

        public static void SaveConfig(int lineWidth, string font, int fontSize)
        {
            using (var writer = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.txt"), false))
            {
                writer.WriteLine(lineWidth);
                writer.WriteLine(font);
                writer.WriteLine(fontSize);
                writer.WriteLine(Path.GetFullPath(DictionaryDirectory));
            }
        }

        private static void AddWord(ref List<string> wordList, ref string surface)
        {
            wordList.Add(surface);
            surface = "";
        }

        /// <summary>
        ///     指定された枠に、語を配置します。はみ出す場合、改行します。
        /// </summary>
        /// <param name="font"></param>
        /// <param name="fontSize"></param>
        /// <param name="bold"></param>
        /// <param name="italic"></param>
        /// <param name="lineWidth"></param>
        /// <param name="currentText"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        private static string Allocate(string font, int fontSize, bool bold, bool italic, int lineWidth,
                                       string currentText, string word)
        {
            var renderText = currentText;
            var formattedText = new FormattedText(
                renderText + word, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily(font), italic ? FontStyles.Italic : FontStyles.Normal,
                    bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal), fontSize,
                Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display);

            if (formattedText.Width > lineWidth)
                renderText += Environment.NewLine + word;
            else
                renderText += word;
            return renderText;
        }

        private static string AllocateStringInPen(string font, int fontSize, bool bold, bool italic, int lineWidth,
                                                  string words)
            => words.Aggregate("", (c, n) => Allocate(font, fontSize, bold, italic, lineWidth, c, n.ToString()));

        private static string AllocateStringInPen(string font, int fontSize, bool bold, bool italic, int lineWidth,
                                                  List<string> words)
            => words.Aggregate("", (c, n) => Allocate(font, fontSize, bold, italic, lineWidth, c, n));

        private static void ForwardChars(ref string text, ref MeCabNode node, ref string surface, ref int currentPos,
                                         bool skipBreaks)
        {
            var addPos = -1;
            // TODO: こ　れ　は　ひ　ど　い
            for (int pos;
                 text.Length > (pos = currentPos + ++addPos) // 値更新 + 語Length超過判定
                 && node.Surface.Length > addPos
                 || node.Next == null
                 || (node.Next != null
                     && text.Length > pos // 語間のスペースの判定(語Lengthを超過 && 次の語の先頭と一致しない → スペースがあると判定)
                     && node.Next.Surface[0] != text[pos]);
                 surface += pos < text.Length ? text[pos].ToString() : "")
                ; /*
                if (new[] {' ', '　', '\r', '\n', '\t'}.Contains(text[pos]))
                    if (!skipBreaks && !new[] {'\r', '\n'}.Contains(text[pos]))
                        surface += text[pos];*/
            currentPos += addPos;
        }
    }
}
