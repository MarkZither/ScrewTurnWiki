
using System;
using System.Collections.Generic;
using System.Text;
using ScrewTurn.Wiki.AclEngine;
using NUnit.Framework;
using Rhino.Mocks;

namespace ScrewTurn.Wiki.Plugins.SqlCommon.Tests {
	
	[TestFixture]
	public class SqlAclStorerTests {

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullLoadData() {
			MockRepository mocks = new MockRepository();

			SqlAclStorer storer = new SqlAclStorer(mocks.StrictMock<IAclManager>(), null,
				new DeleteEntries(e => { }), new StoreEntries(e => { }));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullDeleteEntries() {
			MockRepository mocks = new MockRepository();

			SqlAclStorer storer = new SqlAclStorer(mocks.StrictMock<IAclManager>(), new LoadData(() => { return null; }),
				null, new StoreEntries(e => { }));
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Constructor_NullStoreEntries() {
			MockRepository mocks = new MockRepository();

			SqlAclStorer storer = new SqlAclStorer(mocks.StrictMock<IAclManager>(), new LoadData(() => { return null; }),
				new DeleteEntries(e => { }), null);
		}

	}

}
