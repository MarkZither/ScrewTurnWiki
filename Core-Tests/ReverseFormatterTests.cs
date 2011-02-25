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
		[TestCase("<div class=\"box\">text</div>", "(((text)))\r\n")]
		[TestCase("<div>text</div>", "text\r\n")]
		[TestCase("<html><ol><li>1</li><li>2</li><li>3<ol><li>3.1</li><li>3.2<ol><li>3.2.1</li></ol></li><li>3.3</li></ol></li><li>4<br /></li></ol><br /></html>", "# 1\r\n# 2\r\n# 3\r\n## 3.1\r\n## 3.2\r\n### 3.2.1\r\n## 3.3\r\n# 4\r\n# \r\n\r\n\r\n\r\n")]
		[TestCase("<ol><li>1</li><li>2</li></ol>", "# 1\r\n# 2\r\n\r\n")]
		[TestCase("<ul><li>1</li><li>2</li></ul>", "* 1\r\n* 2\r\n\r\n")]
		[TestCase("<html><ul><li>Punto 1</li><li>Punto 2</li><li>Punto 3</li><li>Punto 4</li><li>Punto 5</li></ul></html>", "* Punto 1\r\n* Punto 2\r\n* Punto 3\r\n* Punto 4\r\n* Punto 5\r\n\r\n")]
		[TestCase("<ul><li>it 1<ul><li>1.1</li><li>1.2</li></ul></li><li>it2</li></ul>", "* it 1\r\n** 1.1\r\n** 1.2\r\n* it2\r\n\r\n")]
		[TestCase("<ul><li>it 1<ol><li>1.1</li><li>1.2</li></ol></li><li>it2</li></ul>", "* it 1\r\n*# 1.1\r\n*# 1.2\r\n* it2\r\n\r\n")]
		[TestCase("<ul><li><b>1</b></li><li>2</li></ul>", "* '''1'''\r\n* 2\r\n\r\n")]
		[TestCase("<html><a id=\"Init\" />I'm an anchor</html>", "[anchor|#init]I'm an anchor")]
		[TestCase("<html><a class=\"internallink\" href=\"#init\" title=\"This recall an anchor\">This recall an anchor</a></html>", "[#init|This recall an anchor]")]
		[TestCase("<html><a class=\"externallink\" href=\"google.com\" title=\"BIG TITLE\" target=\"_blank\">BIG TITLE</a></html>", "[^google.com|BIG TITLE]")]
		[TestCase("<esc>try to esc tag</esc>", "<esc>try to esc tag</esc>")]
		[TestCase("<div class=\"imageleft\"><a target=\"_blank\" href=\"www.link.com\" title=\"left Align\"><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"left Align\" /></a><p class=\"imagedescription\">leftalign</p></div>", "[imageleft|leftalign|{UP(MainPage)}image.png|^www.link.com]\r\n")]
		[TestCase("<img src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"inlineimage\" />", "[image|inlineimage|{UP(MainPage)}image.png]\r\n")]
		[TestCase("<a target=\"_blank\" href=\"www.google.it\" title=\"description\"><img src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"description\" /></a>", "[image|description|{UP(MainPage)}image.png|^www.google.it]\r\n")]
		[TestCase("<table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"autoalign\" /><p class=\"imagedescription\">autoalign</p></td></tr></tbody></table>", "[imageauto|autoalign|{UP(MainPage)}image.png]\r\n")]
		[TestCase("<table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><a href=\"www.link.com\" title=\"Auto align\"><img class=\"image\" src=\"GetFile.aspx?Page=MainPage&File=image.png\" alt=\"Auto align\" /></a><p class=\"imagedescription\">Auto align</p></td></tr></tbody></table>", "[imageauto|Auto align|{UP(MainPage)}image.png|www.link.com]\r\n")]
		public void PlainTest(string input, string output) {
			Assert.AreEqual(output, ReverseFormatter.ReverseFormat(input));
		}
	}
}