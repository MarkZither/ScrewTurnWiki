
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class DumpedWordTests {

		[Test]
		public void Constructor_WithParameters() {
			DumpedWord w = new DumpedWord(12, "word");
			Assert.AreEqual(12, w.ID, "Wrong ID");
			Assert.AreEqual("word", w.Text, "Wrong text");
		}

		[TestCase("")]
		public void Constructor_WithParameters_InvalidText(string text) {
            Assert.That(() => new DumpedWord(5, text), Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_WithParameters_NullText(string text)
        {
            Assert.That(() => new DumpedWord(5, text), Throws.ArgumentNullException);
        }

        [Test]
		public void Constructor_Word() {
			DumpedWord w = new DumpedWord(new Word(23, "text"));
			Assert.AreEqual(23, w.ID, "Wrong ID");
			Assert.AreEqual("text", w.Text, "Wrong text");
		}

		[Test]
		public void Constructor_Word_NullWord() {
            Assert.That(() => new DumpedWord(null), Throws.ArgumentNullException);
        }

	}

}
