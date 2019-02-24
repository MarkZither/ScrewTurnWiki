
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.SearchEngine.Tests {

	[TestFixture]
	public class RelevanceTests {

		[Test]
		public void Constructor() {
			Relevance rel = new Relevance();
			Assert.AreEqual(0, rel.Value, "Wrong value");
			Assert.IsFalse(rel.IsFinalized, "Value should not be finalized");
		}

		[Test]
		public void Constructor_WithValue() {
			Relevance rel = new Relevance(5);
			Assert.AreEqual(5, rel.Value, "Wrong value");
			Assert.IsFalse(rel.IsFinalized, "Value should not be finalized");
		}

		[Test]
		public void Constructor_InvalidValue() {
            Assert.Throws<ArgumentOutOfRangeException>(() => { Relevance rel = new Relevance(-1); });
		}

		[Test]
		public void SetValue() {
			Relevance rel = new Relevance();
			rel.SetValue(8);
			Assert.AreEqual(8, rel.Value, "Wrong value");
			Assert.IsFalse(rel.IsFinalized, "Value should not be finalized");
			rel.SetValue(14);
			Assert.AreEqual(14, rel.Value, "Wrong value");
			Assert.IsFalse(rel.IsFinalized, "Value should not be finalized");
		}

		[Test]
		public void SetValue_InvalidValue() {
			Relevance rel = new Relevance();
            Assert.Throws<ArgumentOutOfRangeException>(() => { rel.SetValue(-1); });
		}

		[Test]
		// Underscore to avoid interference with Destructor
		public void Finalize_() {
			Relevance rel = new Relevance();
			rel.SetValue(12);
			Assert.AreEqual(12, rel.Value, "Wrong value");
			rel.Finalize(24);
			Assert.AreEqual(50, rel.Value, "Wrong finalized value");
			Assert.IsTrue(rel.IsFinalized, "Value should be finalized");
		}

		[Test]
		public void Finalize_InvalidFactor() {
			Relevance rel = new Relevance();
			rel.SetValue(8);
            Assert.Throws<ArgumentOutOfRangeException>(() => { rel.Finalize(-1); });
		}

		[Test]
		public void Finalize_AlreadyFinalized() {
			Relevance rel = new Relevance();
			rel.SetValue(8);
			rel.Finalize(0.5F);
            Assert.Throws<InvalidOperationException>(() => { rel.Finalize(1); });
		}

		[Test]
		public void Finalize_InvalidTotal() {
			Relevance rel = new Relevance(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => { rel.Finalize(-1); });
		}

		[Test]
		public void SetValue_AfterFinalize() {
			Relevance rel = new Relevance();
			rel.SetValue(5);
			rel.Finalize(12);
            Assert.Throws<InvalidOperationException>(() => { rel.SetValue(8); });
		}

		[Test]
		public void NormalizeAfterFinalization() {
			Relevance rel = new Relevance(8);
			rel.Finalize(16);
			rel.NormalizeAfterFinalization(0.5F);
			Assert.AreEqual(25, rel.Value, 0.1, "Wrong value");
		}

		[Test]
		public void NormalizeAfterFinalization_InvalidFactor() {
			Relevance rel = new Relevance(8);
			rel.Finalize(16);
            Assert.Throws<ArgumentOutOfRangeException>(() => { rel.NormalizeAfterFinalization(-0.5F); });
		}

		[Test]
		public void NormalizeAfterFinalization_BeforeFinalize() {
			Relevance rel = new Relevance(8);
            Assert.Throws<InvalidOperationException>(() => { rel.NormalizeAfterFinalization(0.5F); });
        }
	}
}
