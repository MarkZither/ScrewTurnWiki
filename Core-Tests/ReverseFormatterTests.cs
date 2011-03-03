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
		[TestCase("<ul><li>prova <b>pippo</b></li><li>riga2</li></ul>", "* prova '''pippo'''\r\n* riga2\r\n")]
		[TestCase("<em>text</em>", "''text''")]
		[TestCase("<u>text</u>", "__text__")]
		[TestCase("<s>text</s>", "--text--")]
		[TestCase("<html><table border=\"1\" bgcolor=\"LightBlue\"><thead><tr><th>Cells x.1</th><th>Cells x.2</th></tr></thead><tbody><tr><td>Cell 1.1</td><td>Cell 1.2</td></tr><tr><td>Cell 2.1</td><td>Cell 2.2</td></tr></tbody></table></html>", "{| border=\"1\" bgcolor=\"LightBlue\" \r\n|- \r\n! Cells x.1\r\n! Cells x.2\r\n|- \r\n| Cell 1.1\r\n| Cell 1.2\r\n|- \r\n| Cell 2.1\r\n| Cell 2.2\r\n|}\r\n")]
		[TestCase("<ol><li><a class=\"internallink\" target=\"_blank\" href=\"www.try.com\" title=\"try\">try</a></li><li><a class=\"internallink\" target=\"_blank\" href=\"www.secondtry.com\" title=\"www.secondtry.com\">www.secondtry.com</a><br></li></ol>","# [^www.try.com|try]\r\n# [^www.secondtry.com|www.secondtry.com]\r\n")]
		[TestCase("<table><tbody><tr><td bgcolor=\"Blue\">Styled Cell</td><td>Normal cell</td></tr><tr><td>Normal cell</td><td bgcolor=\"Yellow\">Styled cell</td></tr></tbody></table>", "{| \r\n|- \r\n|  bgcolor=\"Blue\"  | Styled Cell\r\n| Normal cell\r\n|- \r\n| Normal cell\r\n|  bgcolor=\"Yellow\"  | Styled cell\r\n|}\r\n")]
		[TestCase("<h1>text</h1>", "\r\n==text==\r\n")]
		[TestCase("<h2>text</h2>", "\r\n===text===\r\n")]
		[TestCase("<h3>text</h3>", "\r\n====text====\r\n")]
		[TestCase("<h4>text</s>", "\r\n=====text=====\r\n")]
		[TestCase("<sup>text</sup>", "<sup>text</sup>")]
		[TestCase("<sub>text</sub>", "<sub>text</sub>")]
		[TestCase("<pre><b>text</b></pre>", "@@text@@")]
		[TestCase("<code><b>text</b></code>", "{{'''text'''}}")]
		[TestCase("<div class=\"box\">text</div>", "\r\n(((text)))\r\n")]
		[TestCase("<div>text</div>", "\r\ntext\r\n")]
		[TestCase("<html>riga1\r\n<b>riga2</b>\r\nriga3</html>", "riga1\r\n'''riga2'''\r\nriga3")]
		[TestCase("<html><ol><li>1</li><li>2</li><li>3<ol><li>3.1</li><li>3.2<ol><li>3.2.1</li></ol></li><li>3.3</li></ol></li><li>4 ciao</li></ol><br /></html>", "# 1\r\n# 2\r\n# 3\r\n## 3.1\r\n## 3.2\r\n### 3.2.1\r\n## 3.3\r\n# 4 ciao\r\n\r\n")]
		[TestCase("<ol><li>1</li><li>2</li></ol>", "# 1\r\n# 2\r\n")]
		[TestCase("<ul><li><img src=\"GetFile.aspx?File=/AmanuensMicro.png\" alt=\"Image\"></li><li><div class=\"imageleft\"><img class=\"image\" src=\"GetFile.aspx?File=/DownloadButton.png\" alt=\"Image\"></div></li><li><div class=\"imageright\"><a target=\"_blank\" href=\"www.tututu.tu\" title=\"guihojk\"><img class=\"image\" src=\"GetFile.aspx?File=/Checked.png\" alt=\"guihojk\"></a><p class=\"imagedescription\">guihojk</p></div></li><li><table class=\"imageauto\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><img class=\"image\" src=\"GetFile.aspx?File=/Alert.png\" alt=\"auto\"><p class=\"imagedescription\">auto</p></td></tr></tbody></table><br></li></ul>", "* [image|Image|{UP}/AmanuensMicro.png]\r\n* [imageleft||{UP}/DownloadButton.png]\r\n* [imageright|guihojk|{UP}/Checked.png|^www.tututu.tu]\r\n* [imageauto|auto|{UP}/Alert.png]\r\n\r\n")]
		[TestCase("<ul><li>1</li><li>2</li></ul>", "* 1\r\n* 2\r\n")]
		[TestCase("<div class=\"imageright\"><img class=\"image\" src=\"GetFile.aspx?File=/Help/Desktop/image.png\"><p class=\"imagedescription\">description</p></div>", "[imageright|description|{UP}/Help/Desktop/image.png]\r\n")]
		[TestCase("<html><ul><li>Punto 1</li><li>Punto 2</li><li>Punto 3</li><li>Punto 4</li><li>Punto 5</li></ul></html>", "* Punto 1\r\n* Punto 2\r\n* Punto 3\r\n* Punto 4\r\n* Punto 5\r\n")]
		[TestCase("<ul><li>it 1<ul><li>1.1</li><li>1.2</li></ul></li><li>it2</li></ul>", "* it 1\r\n** 1.1\r\n** 1.2\r\n* it2\r\n")]
		[TestCase("<ul><li>it 1<ol><li>1.1</li><li>1.2</li></ol></li><li>it2</li></ul>", "* it 1\r\n*# 1.1\r\n*# 1.2\r\n* it2\r\n")]
		[TestCase("<ul><li><b>1</b></li><li>2</li></ul>", "* '''1'''\r\n* 2\r\n")]
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