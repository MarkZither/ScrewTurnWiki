
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class DumpedDocumentTests : TestsBase {

		[Test]
		public void Constructor_WithDocument() {
			IDocument doc = MockDocument("name", "Title", "doc", DateTime.Now);
			DumpedDocument ddoc = new DumpedDocument(doc);

			Assert.AreEqual(doc.ID, ddoc.ID, "Wrong ID");
			Assert.AreEqual("name", ddoc.Name, "Wrong name");
			Assert.AreEqual("Title", ddoc.Title, "Wrong title");
			Assert.AreEqual(doc.DateTime, ddoc.DateTime, "Wrong date/time");
		}

		[Test]
		public void Constructor_WithDocument_NullDocument() {
            Assert.That(() => new DumpedDocument(null), Throws.ArgumentNullException);
		}

		[Test]
		public void Constructor_WithParameters() {
			IDocument doc = MockDocument("name", "Title", "doc", DateTime.Now);
			DumpedDocument ddoc = new DumpedDocument(doc.ID, doc.Name, doc.Title, doc.TypeTag, doc.DateTime);

			Assert.AreEqual(doc.ID, ddoc.ID, "Wrong ID");
			Assert.AreEqual("name", ddoc.Name, "Wrong name");
			Assert.AreEqual("Title", ddoc.Title, "Wrong title");
			Assert.AreEqual(doc.DateTime, ddoc.DateTime, "Wrong date/time");
		}

		[TestCase("")]
		public void Constructor_WithParameters_InvalidName(string name) {
            Assert.That(() => new DumpedDocument(10, name, "Title", "doc", DateTime.Now), Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_WithParameters_NullName(string name)
        {
            Assert.That(() => new DumpedDocument(10, name, "Title", "doc", DateTime.Now), Throws.ArgumentNullException);
        }

		[TestCase("")]
		public void Constructor_WithParameters_InvalidTitle(string title) {
            Assert.That(() => new DumpedDocument(1, "name", title, "doc", DateTime.Now), Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_WithParameters_NullTitle(string title)
        {
            Assert.That(() => new DumpedDocument(1, "name", title, "doc", DateTime.Now), Throws.ArgumentNullException);
        }

		[TestCase("")]
		public void Constructor_WithParameters_InvalidTypeTag(string typeTag) {
            Assert.That(() => new DumpedDocument(1, "name", "Title", typeTag, DateTime.Now), Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_WithParameters_NullTypeTag(string typeTag)
        {
            Assert.That(() => new DumpedDocument(1, "name", "Title", typeTag, DateTime.Now), Throws.ArgumentNullException);
        }
    }

}
