
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.AclEngine.Tests {
	
	[TestFixture]
	public class AclEntryTests {

		[Test]
		public void Constructor() {
			AclEntry entry = new AclEntry("Res", "Action", "U.User", Value.Grant);

			Assert.AreEqual("Res", entry.Resource, "Wrong resource");
			Assert.AreEqual("Action", entry.Action, "Wrong action");
			Assert.AreEqual("U.User", entry.Subject, "Wrong subject");
			Assert.AreEqual(Value.Grant, entry.Value, "Wrong value");

			entry = new AclEntry("Res", "Action", "G.Group", Value.Deny);

			Assert.AreEqual("Res", entry.Resource, "Wrong resource");
			Assert.AreEqual("Action", entry.Action, "Wrong action");
			Assert.AreEqual("G.Group", entry.Subject, "Wrong subject");
			Assert.AreEqual(Value.Deny, entry.Value, "Wrong value");
		}

		[TestCase(null)]
		public void Constructor_InvalidResource_ShouldThrowArgumentNullException(string r)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				AclEntry entry = new AclEntry(r, "Action", "U.USer", Value.Grant);
			});
		}

		[TestCase("")]
		public void Constructor_InvalidResource_ShouldThrowArgumentException(string r)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				AclEntry entry = new AclEntry(r, "Action", "U.USer", Value.Grant);
			});
		}

		[TestCase(null)]
		public void Constructor_InvalidAction_ShouldThrowArgumentNullException(string a)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				AclEntry entry = new AclEntry("Res", a, "G.Group", Value.Deny);
			});
		}

		[TestCase("")]
		public void Constructor_InvalidAction_ShouldThrowArgumentException(string a)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				AclEntry entry = new AclEntry("Res", a, "G.Group", Value.Deny);
			});
		}

		[TestCase(null)]
		public void Constructor_InvalidSubject_ShouldThrowArgumentNullException(string s)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				AclEntry entry = new AclEntry("Res", "Action", s, Value.Grant);
			});
		}

		[TestCase("")]
		public void Constructor_InvalidSubject_ShouldThrowArgumentException(string s)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				AclEntry entry = new AclEntry("Res", "Action", s, Value.Grant);
			});
		}

		[Test]
		public void Equals() {
			AclEntry entry = new AclEntry("Res", "Action", "U.User", Value.Grant);

			Assert.IsFalse(entry.Equals(null), "Equals should return false (testing null)");
			Assert.IsFalse(entry.Equals("blah"), "Equals should return false (testing a string)");
			Assert.IsFalse(entry.Equals(new AclEntry("Res1", "Action", "U.User", Value.Grant)), "Equals should return false");
			Assert.IsFalse(entry.Equals(new AclEntry("Res", "Action1", "U.User", Value.Grant)), "Equals should return false");
			Assert.IsFalse(entry.Equals(new AclEntry("Res", "Action", "U.User1", Value.Grant)), "Equals should return false");
			Assert.IsTrue(entry.Equals(new AclEntry("Res", "Action", "U.User", Value.Deny)), "Equals should return true");
			Assert.IsTrue(entry.Equals(new AclEntry("Res", "Action", "U.User", Value.Grant)), "Equals should return true");
			Assert.IsTrue(entry.Equals(entry), "Equals should return true");
		}

		[Test]
		public void Static_Equals() {
			AclEntry entry = new AclEntry("Res", "Action", "U.User", Value.Grant);

			Assert.IsFalse(AclEntry.Equals(entry, null), "Equals should return false (testing null)");
			Assert.IsFalse(AclEntry.Equals(entry, "blah"), "Equals should return false (testing a string)");
			Assert.IsFalse(AclEntry.Equals(entry, new AclEntry("Res1", "Action", "U.User", Value.Grant)), "Equals should return false");
			Assert.IsFalse(AclEntry.Equals(entry, new AclEntry("Res", "Action1", "U.User", Value.Grant)), "Equals should return false");
			Assert.IsFalse(AclEntry.Equals(entry, new AclEntry("Res", "Action", "U.User1", Value.Grant)), "Equals should return false");
			Assert.IsTrue(AclEntry.Equals(entry, new AclEntry("Res", "Action", "U.User", Value.Deny)), "Equals should return true");
			Assert.IsTrue(AclEntry.Equals(entry, new AclEntry("Res", "Action", "U.User", Value.Grant)), "Equals should return true");
			Assert.IsTrue(AclEntry.Equals(entry, entry), "Equals should return true");
		}

	}

}
