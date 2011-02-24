using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.Tests {
	
	[TestFixture]
	public class ReverseFormatterTests {

		[Test]
		//[TestCase("<b>text</b>", "'''text'''")]
		//[TestCase("<strong>text</strong>", "'''text'''")]
		//[TestCase("<i>text</i>", "''text''")]
		//[TestCase("<em>text</em>", "''text''")]
		//[TestCase("<u>text</u>", "__text__")]
		//[TestCase("<s>text</s>", "--text--")]
		//[TestCase("<h1>text</h1>", "==text==")]
		//[TestCase("<h2>text</h2>", "===text===")]
		//[TestCase("<h3>text</h3>", "====text====")]
		//[TestCase("<h4>text</s>", "=====text=====")]
		//[TestCase("<sup>text</sup>", "<sup>text</sup>")]
		//[TestCase("<sub>text</sub>", "<sub>text</sub>")]
		//[TestCase("<pre><b>text</b></pre>", "@@text@@")]
		//[TestCase("<code><b>text</b></code>", "{{'''text'''}}")]
		//[TestCase("<div class=\"box\">text</div>", "(((text))){br}")]
		//[TestCase("<div>text</div>", "text{br}")]
		[TestCase("<ol><li>1</li><li>2</li></ol>", "# 1{br}# 2{br}{br}")]
		[TestCase("<ul><li>1</li><li>2</li></ul>", "* 1{br}* 2{br}{br}")]
		[TestCase("<ul><li>it 1<ul><li>1.1</li><li>1.2</li></ul></li><li>it2</li></ul>", "* it 1{br}** 1.1{br}** 1.2{br}* it2{br}{br}")]
		[TestCase("<ul><li>it 1<ol><li>1.1</li><li>1.2</li></ol></li><li>it2</li></ul>", "* it 1{br}*# 1.1{br}*# 1.2{br}* it2{br}{br}")]
		//[TestCase("<html><a id=\"Init\" />I'm an anchor</html>", "[anchor|#init]I'm an anchor")]
		//[TestCase("<html><a class=\"internallink\" href=\"#init\" title=\"This recall an anchor\">This recall an anchor</a></html>", "[#init|This recall an anchor]")]
		//[TestCase("<html><a class=\"externallink\" href=\"google.com\" title=\"BIG TITLE\" target=\"_blank\">BIG TITLE</a></html>", "[^google.com|BIG TITLE]")]
		//[TestCase("<esc>try to esc tag</esc>", "<esc>try to esc tag</esc>")]
		//[TestCase("<div class=\"imageleft\"><a target=\"_blank\" href=\"www.link.com\" title=\"left Align\"><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"left Align\" /></a><p class=\"imagedescription\">leftalign</p></div>", "[imageleft|leftalign|{UP(MainPage)}image.png|^www.link.com]{br}")]
		//[TestCase("<img src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"inlineimage\" />", "[image|inlineimage|{UP(MainPage)}image.png]{br}")]
		//[TestCase("<a target=\"_blank\" href=\"www.google.it\" title=\"description\"><img src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"description\" /></a>", "[image|description|{UP(MainPage)}image.png|^www.google.it]{br}")]
		//[TestCase("<table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"autoalign\" /><p class=\"imagedescription\">autoalign</p></td></tr></tbody></table>", "[imageauto|autoalign|{UP(MainPage)}image.png]{br}")]
		//[TestCase("<table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><a href=\"www.link.com\" title=\"Auto align\"><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"Auto align\" /></a><p class=\"imagedescription\">Auto align</p></td></tr></tbody></table>", "[imageauto|Auto align|{UP(MainPage)}image.png|www.link.com]{br}")]
		public void PlainTest(string input, string output) {
			Assert.AreEqual(output, ReverseFormatter.ReverseFormat(input));
		}
	}
}