
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class SearchParametersTests {

		[Test]
		public void Constructor_QueryOnly() {
			SearchParameters par = new SearchParameters("query");
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.AtLeastOneWord, par.Options);
		}

		[TestCase("")]
		public void Constructor_QueryOnly_InvalidQuery(string q) {
            Assert.That(() => { SearchParameters par = new SearchParameters(q); }, Throws.ArgumentException);
		}

        [TestCase(null)]
        public void Constructor_QueryOnly_NullQuery(string q)
        {
            Assert.That(() => { SearchParameters par = new SearchParameters(q); }, Throws.ArgumentNullException);
        }

        [Test]
		public void Constructor_QueryDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", "blah", "doc");
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.AreEqual(2, par.DocumentTypeTags.Length, "Wrong DocumentTypeTag count");
			Assert.AreEqual("blah", par.DocumentTypeTags[0], "Wrong type tag");
			Assert.AreEqual("doc", par.DocumentTypeTags[1], "Wrong type tag");
			Assert.AreEqual(SearchOptions.AtLeastOneWord, par.Options);
		}

		[TestCase("")]
		public void Constructor_QueryDocumentTypeTags_InvalidQuery(string q) {
            Assert.That(() => { SearchParameters par = new SearchParameters(q, "blah", "doc"); }, Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_QueryDocumentTypeTags_NullQuery(string q)
        {
            Assert.That(() => { SearchParameters par = new SearchParameters(q, "blah", "doc"); }, Throws.ArgumentNullException);
        }

        [Test]
		public void Constructor_QueryDocumentTypeTags_NullDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", null);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.AtLeastOneWord, par.Options);
		}

		[Test]
		public void Constructor_QueryDocumentTypeTags_EmptyDocumentTypeTags() {
            Assert.That(() => { SearchParameters par = new SearchParameters("query", new string[0]); }, Throws.ArgumentException);
		}

		[TestCase("")]
		public void Constructor_QueryDocumentTypeTags_InvalidDocumentTypeTagsElement(string e) {
            Assert.That(() => { SearchParameters par = new SearchParameters("query", new string[] { "blah", e }); }, Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_QueryDocumentTypeTags_NullDocumentTypeTagsElement(string e)
        {
            Assert.That(() => { SearchParameters par = new SearchParameters("query", new string[] { "blah", e }); }, Throws.ArgumentNullException);
        }

        [Test]
		public void Constructor_QueryOptions() {
			SearchParameters par = new SearchParameters("query", SearchOptions.ExactPhrase);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.ExactPhrase, par.Options);
		}

		[TestCase("")]
		public void Constructor_QueryOptions_InvalidQuery(string q) {
            Assert.That(() => { SearchParameters par = new SearchParameters(q, SearchOptions.ExactPhrase); }, Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_QueryOptions_NullQuery(string q)
        {
            Assert.That(() => { SearchParameters par = new SearchParameters(q, SearchOptions.ExactPhrase); }, Throws.ArgumentNullException);
        }

        [Test]
		public void Constructor_Full() {
			SearchParameters par = new SearchParameters("query", new string[] { "blah", "doc" }, SearchOptions.AllWords);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.AreEqual(2, par.DocumentTypeTags.Length, "Wrong DocumentTypeTag count");
			Assert.AreEqual("blah", par.DocumentTypeTags[0], "Wrong type tag");
			Assert.AreEqual("doc", par.DocumentTypeTags[1], "Wrong type tag");
			Assert.AreEqual(SearchOptions.AllWords, par.Options);
		}

		[TestCase("")]
		public void Constructor_Full_InvalidQuery(string q) {
            Assert.That(() => { SearchParameters par = new SearchParameters(q, new string[] { "blah", "doc" }, SearchOptions.AllWords); }, Throws.ArgumentException);
        }

        [TestCase(null)]
        public void Constructor_Full_NullQuery(string q)
        {
            Assert.That(() => { SearchParameters par = new SearchParameters(q, new string[] { "blah", "doc" }, SearchOptions.AllWords); }, Throws.ArgumentNullException);
        }

        [Test]
		public void Constructor_Full_NullDocumentTypeTags() {
			SearchParameters par = new SearchParameters("query", null, SearchOptions.AllWords);
			Assert.AreEqual("query", par.Query, "Wrong query");
			Assert.IsNull(par.DocumentTypeTags, "DocumentTypeTags should be null");
			Assert.AreEqual(SearchOptions.AllWords, par.Options);
		}

		[Test]
		public void Constructor_Full_EmptyDocumentTypeTags() {
            Assert.That(() => { SearchParameters par = new SearchParameters("query", new string[0], SearchOptions.AllWords); }, Throws.ArgumentException);
        }

		[TestCase("")]
		public void Constructor_Full_InvalidDocumentTypeTagsElement(string e) {
            Assert.That(() => { SearchParameters par = new SearchParameters("query", new string[] { "blah", e }, SearchOptions.ExactPhrase); }, Throws.ArgumentException);
		}

        [TestCase(null)]
        public void Constructor_Full_NullDocumentTypeTagsElement(string e)
        {
            Assert.That(() => { SearchParameters par = new SearchParameters("query", new string[] { "blah", e }, SearchOptions.ExactPhrase); }, Throws.ArgumentNullException);
        }

    }

}
