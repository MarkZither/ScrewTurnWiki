
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class WordTests : TestsBase {

		[Test]
		public void Constructor_2Params() {
			Word word = new Word(1, "Hello");
			Assert.AreEqual("hello", word.Text, "Wrong word text");
			Assert.AreEqual(0, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(0, word.TotalOccurrences, "Wrong total occurrences count");
		}

		[Test]
		public void Constructor_3Params_NoOccurrences() {
			Word word = new Word(1, "Hello1", new OccurrenceDictionary());
			Assert.AreEqual("hello1", word.Text, "Wrong word text");
			Assert.AreEqual(0, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(0, word.TotalOccurrences, "Wrong total occurrences count");
		}

		[Test]
		public void Constructor_3Params_1Occurrence() {
			OccurrenceDictionary occ = new OccurrenceDictionary();

			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);

			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			set.Add(new BasicWordInfo(0, 0, WordLocation.Content));
			occ.Add(doc, set);

			Word word = new Word(12, "Hello", occ);
			Assert.AreEqual("hello", word.Text, "Wrong word text");
			Assert.AreEqual(1, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(1, word.TotalOccurrences, "Wrong total occurrences count");
		}

		[Test]
		public void Constructor_NullText() {
            Assert.That(() => { Word word = new Word(1, null); }, Throws.ArgumentNullException);
        }

		[Test]
		public void Constructor_InvalidText() {
            Assert.That(() => { Word word = new Word(1, ""); }, Throws.ArgumentException);
        }

		[Test]
		public void Constructor_NullOccurrences() {
            Assert.That(() => { Word word = new Word(1, "hello", null); }, Throws.ArgumentNullException);
		}

		[Test]
		public void Add1Occurrence() {
			Word word = new Word(1, "hello");

			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);

			word.AddOccurrence(doc, 0, 0, WordLocation.Content);
			Assert.AreEqual(1, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(1, word.TotalOccurrences, "Wrong total occurences count");
			Assert.AreEqual(0, word.Occurrences[doc][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc][0].WordIndex, "Wrong word index");
		}

		[Test]
		public void Add2Occurrences_DifferentDocuments() {
			Word word = new Word(1, "hello");

			IDocument doc1 = MockDocument("Doc1", "Doc1", "d", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Doc2", "d", DateTime.Now);
			word.AddOccurrence(doc1, 0, 0, WordLocation.Content);
			word.AddOccurrence(doc2, 10, 1, WordLocation.Content);
			Assert.AreEqual(2, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(2, word.TotalOccurrences, "Wrong total occurences count");
			Assert.AreEqual(0, word.Occurrences[doc1][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc1][0].WordIndex, "Wrong word index");
			Assert.AreEqual(10, word.Occurrences[doc2][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(1, word.Occurrences[doc2][0].WordIndex, "Wrong word index");
		}

		[Test]
		public void Add2Occurrences_SameDocument() {
			Word word = new Word(1, "hello");

			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);
			word.AddOccurrence(doc, 0, 0, WordLocation.Content);
			word.AddOccurrence(doc, 10, 1, WordLocation.Content);
			Assert.AreEqual(1, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(2, word.TotalOccurrences, "Wrong total occurences count");
			Assert.AreEqual(0, word.Occurrences[doc][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc][0].WordIndex, "Wrong word index");
			Assert.AreEqual(10, word.Occurrences[doc][1].FirstCharIndex, "Wrong occurrence");
			Assert.AreEqual(1, word.Occurrences[doc][1].WordIndex, "Wrong word index");
		}

		[Test]
		public void AddOccurrence_NullDocument() {
			Word word = new Word(1, "dummy");
			Assert.That(() => word.AddOccurrence(null, 0, 0, WordLocation.Content), Throws.ArgumentNullException);
		}

		[Test]
		public void RemoveOccurrences() {
			Word word = new Word(1, "hello");

			IDocument doc1 = MockDocument("Doc1", "Doc1", "d", DateTime.Now);
			IDocument doc2 = MockDocument("Doc2", "Doc2", "d", DateTime.Now);
			word.AddOccurrence(doc1, 0, 0, WordLocation.Content);
			word.AddOccurrence(doc1, 10, 1, WordLocation.Content);
			word.AddOccurrence(doc2, 5, 0, WordLocation.Content);
			Assert.AreEqual(2, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(3, word.TotalOccurrences, "Wrong total occurrences count");

			word.RemoveOccurrences(doc1);

			Assert.AreEqual(1, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(1, word.TotalOccurrences, "Wrong total occurrences count");
			Assert.AreEqual(5, word.Occurrences[doc2][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc2][0].WordIndex, "Wrong word index");
		}

		[Test]
		public void RemoveOccurrences_NullDocument() {
			Word word = new Word(1, "hey");
			IDocument doc = null;
			Assert.That(() => word.RemoveOccurrences(doc), Throws.ArgumentNullException);
		}

		[Test]
		public void BulkAddOccurrences_NewDocument() {
			Word word = new Word(1, "hello");
			IDocument doc0 = MockDocument("Doc0", "Doc0", "d", DateTime.Now);
			word.AddOccurrence(doc0, 10, 0, WordLocation.Content);
			Assert.AreEqual(10, word.Occurrences[doc0][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc0][0].WordIndex, "Wrong word index");

			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			set.Add(new BasicWordInfo(10, 0, WordLocation.Content));
			set.Add(new BasicWordInfo(25, 1, WordLocation.Content));
			set.Add(new BasicWordInfo(102, 2, WordLocation.Content));
			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);
			word.BulkAddOccurrences(doc, set);

			Assert.AreEqual(2, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(4, word.TotalOccurrences, "Wrong total occurrences count");
			Assert.AreEqual(10, word.Occurrences[doc0][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc0][0].WordIndex, "Wrong word index");
			Assert.AreEqual(10, word.Occurrences[doc][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc][0].WordIndex, "Wrong word index");
			Assert.AreEqual(25, word.Occurrences[doc][1].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(1, word.Occurrences[doc][1].WordIndex, "Wrong word index");
			Assert.AreEqual(102, word.Occurrences[doc][2].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(2, word.Occurrences[doc][2].WordIndex, "Wrong word index");
		}

		[Test]
		public void BulkAddOccurrences_ExistingDocument() {
			Word word = new Word(1, "hello");
			IDocument doc0 = MockDocument("Doc0", "Doc0", "d", DateTime.Now);
			word.AddOccurrence(doc0, 10, 0, WordLocation.Content);
			Assert.AreEqual(10, word.Occurrences[doc0][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc0][0].WordIndex, "Wrong word index");

			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);
			word.AddOccurrence(doc, 0, 0, WordLocation.Content);
			Assert.AreEqual(2, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(2, word.TotalOccurrences, "Wrong total occurrences count");

			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			set.Add(new BasicWordInfo(10, 0, WordLocation.Content));
			set.Add(new BasicWordInfo(25, 1, WordLocation.Content));
			set.Add(new BasicWordInfo(102, 2, WordLocation.Content));
			word.BulkAddOccurrences(doc, set);

			Assert.AreEqual(2, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(4, word.TotalOccurrences, "Wrong total occurrences count");
			Assert.AreEqual(10, word.Occurrences[doc0][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc0][0].WordIndex, "Wrong word index");
			Assert.AreEqual(10, word.Occurrences[doc][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc][0].WordIndex, "Wrong word index");
			Assert.AreEqual(25, word.Occurrences[doc][1].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(1, word.Occurrences[doc][1].WordIndex, "Wrong word index");
			Assert.AreEqual(102, word.Occurrences[doc][2].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(2, word.Occurrences[doc][2].WordIndex, "Wrong word index");
		}

		[Test]
		public void BulkAddOccurrences_ExistingDocument_EmptyPositionsSet() {
			Word word = new Word(1, "hello");
			IDocument doc0 = MockDocument("Doc0", "Doc0", "d", DateTime.Now);
			word.AddOccurrence(doc0, 10, 0, WordLocation.Content);
			Assert.AreEqual(10, word.Occurrences[doc0][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc0][0].WordIndex, "Wrong word index");

			IDocument doc = MockDocument("Doc", "Doc", "d", DateTime.Now);
			word.AddOccurrence(doc, 0, 0, WordLocation.Content);
			Assert.AreEqual(2, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(2, word.TotalOccurrences, "Wrong total occurrences count");

			SortedBasicWordInfoSet set = new SortedBasicWordInfoSet();
			word.BulkAddOccurrences(doc, set);

			Assert.AreEqual(1, word.Occurrences.Count, "Wrong occurrences count");
			Assert.AreEqual(1, word.TotalOccurrences, "Wrong total occurrences count");
			Assert.AreEqual(10, word.Occurrences[doc0][0].FirstCharIndex, "Wrong start index");
			Assert.AreEqual(0, word.Occurrences[doc0][0].WordIndex, "Wrong word index");
		}

		[Test]
		public void BulkAddOccurrences_NullDocument() {
			Word word = new Word(1, "john");
            Assert.That(() => { word.BulkAddOccurrences(null, new SortedBasicWordInfoSet()); }, Throws.ArgumentNullException);
		}

		[Test]
		public void BulkAddOccurrences_NullPositions() {
			Word word = new Word(1, "john");
            Assert.That(() => { word.BulkAddOccurrences(MockDocument("Doc", "Doc", "d", DateTime.Now), null); }, Throws.ArgumentNullException);
        }
	}
}
