
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.SearchEngine.Tests {
	
	public class TestsBase {

		/// <summary>
		/// A general purpose mock repository, initalized dusing test fixture setup.
		/// </summary>
		private MockRepository looseMocks;

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent = "This is some content.";

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent2 = "Dummy text used for testing purposes.";

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent3 = "Todo.";

		/// <summary>
		/// Demo content for a plain-text document.
		/// </summary>
		protected const string PlainTextDocumentContent4 = "Content with repeated content.";

		[SetUp]
		public void SetUp() {
			looseMocks = new MockRepository();
		}
	}
}
