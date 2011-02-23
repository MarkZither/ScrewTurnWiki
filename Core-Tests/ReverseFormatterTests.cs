using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.Tests {
	
	[TestFixture]
	public class ReverseFormatterTests {

		[Test]
		[TestCase("<b>text</b>", "'''text'''")]
		[TestCase("<strong>text</strong>", "'''text'''")]
		[TestCase("<i>text</i>", "''text''")]
		[TestCase("<em>text</em>", "''text''")]
		[TestCase("<u>text</u>", "__text__")]
		[TestCase("<s>text</s>", "--text--")]
		[TestCase("<h1>text</h1>", "==text==")]
		[TestCase("<h2>text</h2>", "===text===")]
		[TestCase("<h3>text</h3>", "====text====")]
		[TestCase("<h4>text</s>", "=====text=====")]
		[TestCase("<sup>text</sup>", "<sup>text</sup>")]
		[TestCase("<sub>text</sub>", "<sub>text</sub>")]
		[TestCase("<pre><b>text</b></pre>", "@@text@@")]
		[TestCase("<code><b>text</b></code>", "{{'''text'''}}")]
		[TestCase("<div class=\"box\">text</div>","(((text))){br}")]
		[TestCase("<div>text</div>", "text{br}")]
		[TestCase("<ol><li>1</li><li>2</li></ol>","{br}# 1{br}# 2{br}{br}")]
		[TestCase("<ul><li>1</li><li>2</li></ul>", "{br}* 1{br}* 2{br}{br}")]
		[TestCase("<a id=\"Init\" />I'm an anchor", "[anchor|#init]I'm an anchor")]
		[TestCase("<a class=\"internallink\" href=\"#init\" title=\"This recall an anchor\">This recall an anchor</a>", "[#init|This recall an anchor]")]
		[TestCase("<a class=\"externallink\" href=\"google.com\" title=\"BIG TITLE\" target=\"_blank\">BIG TITLE</a>","[^google.com|BIG TITLE]")]
		[TestCase("<esc>try to esc tag</esc>","<esc>try to esc tag</esc>")]
		public void PlainTest(string input, string output) {
			Assert.AreEqual(output, ReverseFormatter.ReverseFormat(input));
		}
	}
}