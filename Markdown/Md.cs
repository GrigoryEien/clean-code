using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;

namespace Markdown
{
    public class Md
    {
        private static readonly Dictionary<string, Tuple<string, string>> SpecialSymbols
            = new Dictionary<string, Tuple<string, string>>()
            {
                {"_", new Tuple<string, string>("<em>", "</em>")},
                {"__", new Tuple<string, string>("<strong>", "</strong>")},
                {"'", new Tuple<string, string>("<code>", "</code>")}
            };


        public string RenderToHtml(string markdown)
        {
            var words = markdown.Split(' ').ToList();
            foreach (var specialSymbol in SpecialSymbols.Keys)
                RenderSymbol(words, specialSymbol);
            WrapParagraphIntoCorrespondingTag(words);
            return string.Join(" ", words);
        }

        private static void RenderSymbol(List<string> words, string symbol)
        {
            var stack = new Stack<int>();

            for (var i = 0; i < words.Count; i++)
            {
                if (stack.Count != 0 && symbol == "_")
                    EcranizeDoubleUnderscores(words, i);


                if (words[i].StartsWith(@"\" + symbol))
                    words[i] = DeecranizeSymbolInTheStart(words[i]);
                else if (StartsOnlyWith(words[i], symbol))
                    stack.Push(i);

                if (words[i].EndsWith(@"\" + symbol))
                    words[i] = DeecranizeSymbolInTheEnd(words[i], symbol);
                else if (EndsOnlyWith(words[i], symbol) && (stack.Count != 0))
                    RenderPairOfSymbols(words, stack, symbol, i);
            }
        }

        private static void EcranizeDoubleUnderscores(List<string> words, int index)
        {
            if (words[index].StartsWith("__"))
                words[index] = @"\" + words[index];
            if (words[index].EndsWith("__"))
                words[index] = words[index].Insert(words[index].Length - 2, @"\");
        }

        private static bool EndsOnlyWith(string str, string suffix)
        {
            if (!str.EndsWith(suffix))
                return false;
            return SpecialSymbols.Keys.Where(key => !suffix.Contains(key)).All(key => !str.EndsWith(key));
        }

        private static string DeecranizeSymbolInTheStart(string word)
        {
            return word.Substring(1);
        }

        private static string DeecranizeSymbolInTheEnd(string word, string symbol)
        {
            return word.Substring(0, word.Length - symbol.Length - 1) + symbol;
        }

        private static bool StartsOnlyWith(string str, string prefix)
        {
            if (!str.StartsWith(prefix))
                return false;
            return SpecialSymbols.Keys.Where(key => !prefix.Contains(key)).All(key => !str.StartsWith(key));
        }

        private static void RenderPairOfSymbols(List<string> words, Stack<int> stack, string specialSymbol, int index)
        {
            words[stack.Peek()] = SpecialSymbols[specialSymbol].Item1
                                  + words[stack.Peek()].Substring(specialSymbol.Length);
            words[index] = words[index].Substring(0, words[index].Length - specialSymbol.Length) +
                           SpecialSymbols[specialSymbol].Item2;
            stack.Pop();
        }

        private static void WrapParagraphIntoCorrespondingTag(List<string> words)
        {
            if (words[0] == "#")
            {
                words[0] = "<h1>";
                words.Add("</h1>");
            }
            else
            {
                words.Insert(0, "<p>");
                words.Add("</p>");
            }
        }
    }


    [TestFixture]
    public class Md_ShouldRender
    {
        private Md mdRenderer;

        [TestCase("_This_ should be emphasized", ExpectedResult = "<p> <em>This</em> should be emphasized </p>")]
        [TestCase("__This__ should be strong", ExpectedResult = "<p> <strong>This</strong> should be strong </p>")]
        [TestCase("Just ignore it_228_1337", ExpectedResult = "<p> Just ignore it_228_1337 </p>")]
        [TestCase("__Ignore unpaired_ symbols", ExpectedResult = "<p> __Ignore unpaired_ symbols </p>")]
        [TestCase("Ignore_ this_ symbols", ExpectedResult = "<p> Ignore_ this_ symbols </p>")]
        [TestCase("Ignore _this _symbols_", ExpectedResult = "<p> Ignore _this <em>symbols</em> </p>")]
        [TestCase("__Singular scores _can be_ inside of double__", ExpectedResult =
            "<p> <strong>Singular scores <em>can be</em> inside of double</strong> </p>")]
        [TestCase("Different _scores __can_ intersect__", ExpectedResult =
            "<p> Different <em>scores <strong>can</em> intersect</strong> </p>")]
        [TestCase(@"\_Backslash\_ is an escape char", ExpectedResult = "<p> _Backslash_ is an escape char </p>")]
        [TestCase("'Apostrophe means code'", ExpectedResult = "<p> <code>Apostrophe means code</code> </p>")]
        [TestCase("_Double __does not work__ inside of singular_", ExpectedResult =
            "<p> <em>Double __does not work__ inside of singular</em> </p>")]
        [TestCase("_Only _last _pair will close_", ExpectedResult = "<p> _Only _last <em>pair will close</em> </p>")]
        [TestCase("# Hash means header", ExpectedResult = "<h1> Hash means header </h1>")]
        [TestCase("#Hash should be separate word", ExpectedResult = "<p> #Hash should be separate word </p>")]
        [TestCase("Hash should be # first word", ExpectedResult = "<p> Hash should be # first word </p>")]
        public string Render(string markdown)
        {
            mdRenderer = new Md();
            return mdRenderer.RenderToHtml(markdown);
        }

        [Timeout(1000)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void RenderNestedPairs(int numberOfPairs)
        {
            mdRenderer = new Md();

            var markdown = string.Concat(Enumerable.Repeat("_This ", numberOfPairs))
                           + string.Concat(Enumerable.Repeat("and that_ ", numberOfPairs));
            var expectedHtml = "<p> " + string.Concat(Enumerable.Repeat("<em>This ", numberOfPairs))
                               + string.Concat(Enumerable.Repeat("and that</em> ", numberOfPairs))
                               + " </p>";
            var actualHtml = mdRenderer.RenderToHtml(markdown);

            actualHtml.Should().Be(expectedHtml);
        }

        [Timeout(1000)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void NotNestedPairs(int numberOfPairs)
        {
            mdRenderer = new Md();

            var markdown = string.Concat(Enumerable.Repeat("_This_ ", numberOfPairs));
            var expectedHtml = "<p> " + string.Concat(Enumerable.Repeat("<em>This</em> ", numberOfPairs)) + " </p>";
            var actualHtml = mdRenderer.RenderToHtml(markdown);

            actualHtml.Should().Be(expectedHtml);
        }
    }
}