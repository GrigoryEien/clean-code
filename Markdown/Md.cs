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
			var words = markdown.Split(' ');

			foreach (var specialSymbol in SpecialSymbols.Keys) {
				var stack = new Stack<int>();

				for (var i = 0; i < words.Length; i++)
				{

					if (stack.Count != 0 && specialSymbol == "_") {
						if (words[i].StartsWith("__"))
							words[i] = @"\" + words[i];
						if (words[i].EndsWith("__"))
							words[i] = words[i].Insert(words[i].Length - 2, @"\");
					}

					if (words[i].StartsWith(@"\" + specialSymbol))
					{
						words[i] = words[i].Substring(1);
					}
					else if (StartsOnlyWith(words[i],specialSymbol))
					{
						stack.Push(i);
					}

					if (words[i].EndsWith(@"\" + specialSymbol))
					{
						words[i] = words[i].Substring(0, words[i].Length - specialSymbol.Length - 1) + specialSymbol;
					}
					else if (EndsOnlyWith(words[i], specialSymbol) && (stack.Count != 0))
					{
						words[stack.Peek()] = SpecialSymbols[specialSymbol].Item1
												           + words[stack.Peek()].Substring(specialSymbol.Length);
						words[i] = words[i].Substring(0, words[i].Length - specialSymbol.Length) +
						           SpecialSymbols[specialSymbol].Item2;
						stack.Pop();
					}

				}
			}
			return string.Join(" ", words);
		}

		private static bool EndsOnlyWith(string str, string suffix) {
			if (!str.EndsWith(suffix))
				return false;
			return SpecialSymbols.Keys.Where(key => !suffix.Contains(key)).All(key => !str.EndsWith(key));
		}


		private static bool StartsOnlyWith(string str, string prefix)
		{
			if (!str.StartsWith(prefix))
				return false;
			return SpecialSymbols.Keys.Where(key => !prefix.Contains(key)).All(key => !str.StartsWith(key));
		}
		
	}

	

	[TestFixture]
	public class Md_ShouldRender
	{
		private Md mdRenderer;

		[TestCase("_This_ should be emphasized", ExpectedResult = "<em>This</em> should be emphasized")]
		[TestCase("__This__ should be strong", ExpectedResult = "<strong>This</strong> should be strong")]
		[TestCase("Just ignore it_228_1337", ExpectedResult = "Just ignore it_228_1337")]
		[TestCase("__Ignore unpaired_ symbols", ExpectedResult = "__Ignore unpaired_ symbols")]
		[TestCase("Ignore_ this_ symbols", ExpectedResult = "Ignore_ this_ symbols")]
		[TestCase("Ignore _this _symbols_", ExpectedResult = "Ignore _this <em>symbols</em>")]
		[TestCase("__Singular scores _can be_ inside of double__", ExpectedResult = "<strong>Singular scores <em>can be</em> inside of double</strong>")]
		[TestCase("Different _scores __can_ intersect__", ExpectedResult = "Different <em>scores <strong>can</em> intersect</strong>")]
		[TestCase(@"\_Backslash\_ is an escape char", ExpectedResult = "_Backslash_ is an escape char")]
		[TestCase("'Apostrophe means code'", ExpectedResult = "<code>Apostrophe means code</code>")]
		[TestCase("_Double __does not work__ inside of singular_", ExpectedResult = "<em>Double __does not work__ inside of singular</em>")]
		[TestCase("_Only _last _pair will close_", ExpectedResult = "_Only _last <em>pair will close</em>")]

		public string Render(string markdown)
		{
			mdRenderer = new Md();
			return mdRenderer.RenderToHtml(markdown);
		}

	}
}