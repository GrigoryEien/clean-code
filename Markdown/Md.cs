using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using FluentAssertions;
using static System.ValueTuple;

namespace Markdown
{
    public class Md
    {
        private readonly Dictionary<string, Tuple<string, string>> specialSymbols;
        private readonly string escapeString;
        private readonly Dictionary<string, List<string>> ecranizedSymbols;
        private readonly (string open, string closed) defaultWrapper;

        public Md(Dictionary<string, Tuple<string, string>> specialSymbols, string escapeString,
            Dictionary<string, List<string>> ecranizedSymbols,
            (string open, string closed) defaultWrapper)
        {
            this.specialSymbols = specialSymbols;
            this.escapeString = escapeString;
            this.ecranizedSymbols = ecranizedSymbols;
            this.defaultWrapper = defaultWrapper;
        }

        public string RenderToHtml(string markdown)
        {
            var words = markdown.Split(' ').ToList();
            foreach (var specialSymbol in specialSymbols.Keys)
                RenderSymbol(words, specialSymbol);
            WrapParagraphIntoCorrespondingTag(words);
            return string.Join(" ", words);
        }

        private void RenderSymbol(List<string> words, string symbol)
        {
            var stack = new Stack<int>();
            for (var i = 0; i < words.Count; i++)
            {
                if (stack.Count != 0 && ecranizedSymbols.ContainsKey(symbol))
                    Ecranize(words, i, symbol);

                if (StartsOnlyWith(words[i], escapeString + symbol))
                    words[i] = DeecranizeSymbolInTheStart(words[i]);
                else if (StartsOnlyWith(words[i], symbol))
                    stack.Push(i);

                if (EndsOnlyWith(words[i], escapeString + symbol))
                    words[i] = DeecranizeSymbolInTheEnd(words[i], symbol);
                else if (EndsOnlyWith(words[i], symbol) && (stack.Count != 0))
                    RenderPairOfSymbols(words, stack, symbol, i);
            }
        }

        private void Ecranize(List<string> words, int index, string symbol)
        {
            foreach (var toBeEcranized in ecranizedSymbols[symbol])
            {
                if (StartsOnlyWith(words[index], toBeEcranized))
                    words[index] = escapeString + words[index];
                if (EndsOnlyWith(words[index], toBeEcranized))
                    words[index] = words[index].Insert(words[index].Length - 2, escapeString);
            }
        }

        private bool EndsOnlyWith(string str, string suffix)
        {
            if (!str.EndsWith(suffix))
                return false;
            return specialSymbols.Keys.Where(key => !suffix.Contains(key)).All(key => !str.EndsWith(key));
        }

        private static string DeecranizeSymbolInTheStart(string word)
        {
            return word.Substring(1);
        }

        private static string DeecranizeSymbolInTheEnd(string word, string symbol)
        {
            return word.Substring(0, word.Length - symbol.Length - 1) + symbol;
        }

        private bool StartsOnlyWith(string str, string prefix)
        {
            if (!str.StartsWith(prefix))
                return false;
            return specialSymbols.Keys.Where(key => !prefix.Contains(key)).All(key => !str.StartsWith(key));
        }

        private void RenderPairOfSymbols(List<string> words, Stack<int> stack, string specialSymbol, int index)
        {
            words[stack.Peek()] = specialSymbols[specialSymbol].Item1
                                  + words[stack.Peek()].Substring(specialSymbol.Length);
            words[index] = words[index].Substring(0, words[index].Length - specialSymbol.Length) +
                           specialSymbols[specialSymbol].Item2;
            stack.Pop();
        }

        private void WrapParagraphIntoCorrespondingTag(List<string> words)
        {
            words.Insert(0, defaultWrapper.open);
            words.Add(defaultWrapper.closed);
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
        public string Render(string markdown)
        {
            mdRenderer = CreateMd();
            return mdRenderer.RenderToHtml(markdown);
        }

        [Timeout(1000)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void RenderNestedPairs(int numberOfPairs)
        {
            mdRenderer = CreateMd();
            
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
            mdRenderer = CreateMd();

            var markdown = string.Concat(Enumerable.Repeat("_This_ ", numberOfPairs));
            var expectedHtml = "<p> " + string.Concat(Enumerable.Repeat("<em>This</em> ", numberOfPairs)) + " </p>";
            var actualHtml = mdRenderer.RenderToHtml(markdown);

            actualHtml.Should().Be(expectedHtml);
        }

        private Md CreateMd()
        {
            var specialSymbols
                = new Dictionary<string, Tuple<string, string>>()
                {
                    {"_", new Tuple<string, string>("<em>", "</em>")},
                    {"__", new Tuple<string, string>("<strong>", "</strong>")},
                    {"'", new Tuple<string, string>("<code>", "</code>")}
                };

            var escapeString = @"\";

            var ecranizedSymbols = new Dictionary<string, List<string>>()
            {
                {"_", new List<string>() {"__"}}
            };

            var defaultWrapper = (open: "<p>", closed: "</p>");

            return new Md(specialSymbols, escapeString, ecranizedSymbols, defaultWrapper);
        }
    }
}