
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public class AuthToolsTests {

		[Test]
		public void Static_PrepareUsername() {
			Assert.AreEqual("U.User", AuthTools.PrepareUsername("User"), "Wrong result");
			Assert.AreEqual("U.U.User", AuthTools.PrepareUsername("U.User"), "Wrong result");
		}

		[TestCase(null)]
		public void Static_PrepareUsername_InvalidUsername_ShouldThrowArgumentNullException(string s)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				AuthTools.PrepareUsername(s);
			});
		}

		[TestCase("")]
		public void Static_PrepareUsername_InvalidUsername_ShouldThrowArgumentException(string s)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				AuthTools.PrepareUsername(s);
			});
		}

		[Test]
		public void Static_PrepareGroups() {
			Assert.AreEqual(0, AuthTools.PrepareGroups(new string[0]).Length, "Wrong result length");

			string[] input = new string[] { "Group", "G.Group" };
			string[] output = AuthTools.PrepareGroups(input);

			Assert.AreEqual(input.Length, output.Length, "Wrong result length");
			for(int i = 0; i < input.Length; i++) {
				Assert.AreEqual("G." + input[i], output[i], "Wrong value");
			}
		}

		[Test]
		public void Static_PrepareGroups_NullGroups()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				AuthTools.PrepareGroups(null);
			});
		}

		[TestCase(null)]
		public void Static_PrepareGroups_InvalidElement_ShouldThrowArgumentNullException(string e)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				AuthTools.PrepareGroups(new string[] { e });
			});
		}

		[TestCase("")]
		public void Static_PrepareGroups_InvalidElement_ShouldThrowArgumentException(string e)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				AuthTools.PrepareGroups(new string[] { e });
			});
		}

		[TestCase("G.", true)]
		[TestCase("g.", true)]
		[TestCase("G.Blah", true)]
		[TestCase("g.Blah", true)]
		[TestCase("U.", false)]
		[TestCase("u.", false)]
		[TestCase("U.Blah", false)]
		[TestCase("u.Blah", false)]
		public void Static_IsGroup(string subject, bool result) {
			Assert.AreEqual(result, AuthTools.IsGroup(subject), "Wrong result");
		}

		[TestCase(null, false)]
		public void Static_IsGroup_ShouldThrowArgumentNullException(string subject, bool result)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				Assert.AreEqual(result, AuthTools.IsGroup(subject), "Wrong result");
			});
		}

		[TestCase("", false)]
		[TestCase("G", false)]
		public void Static_IsGroup_ShouldThrowArgumentException(string subject, bool result)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				Assert.AreEqual(result, AuthTools.IsGroup(subject), "Wrong result");
			});
		}

	}

}
