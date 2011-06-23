
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki.Tests {

	[TestFixture]
	public abstract class FilesStorageProviderTestScaffolding {

		private MockRepository mocks = new MockRepository();
		private string testDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());

		[TearDown]
		public void TearDown() {
			try {
				Directory.Delete(testDir, true);
			}
			catch {
				//Console.WriteLine("Test: could not delete temp directory");
			}
		}

		protected IHostV40 MockHost() {
			if(!Directory.Exists(testDir)) Directory.CreateDirectory(testDir);

			IHostV40 host = mocks.DynamicMock<IHostV40>();
			Expect.Call(host.GetGlobalSettingValue(GlobalSettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			mocks.Replay(host);

			return host;
		}

		protected IPagesStorageProviderV40 MockPagesProvider() {
			IPagesStorageProviderV40 prov = mocks.DynamicMock<IPagesStorageProviderV40>();

			mocks.Replay(prov);

			return prov;
		}

		public abstract IFilesStorageProviderV40 GetProvider();

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullHost() {
			IFilesStorageProviderV40 prov = GetProvider();
			prov.Init(null, "", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Init_NullConfig() {
			IFilesStorageProviderV40 prov = GetProvider();
			prov.Init(MockHost(), null, null);
		}

		private Stream FillStream(string content) {
			MemoryStream ms = new MemoryStream();
			byte[] buff = Encoding.UTF8.GetBytes(content);
			ms.Write(buff, 0, buff.Length);
			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		[Test]
		public void StoreFile_ListFiles() {
			IFilesStorageProviderV40 prov = GetProvider();

			Assert.AreEqual(0, prov.ListFiles("/").Length, "Wrong file count");

			using(Stream s = FillStream("File1")) {
				Assert.IsTrue(prov.StoreFile("/File1.txt", s, false), "StoreFile should return true");
			}
			using(Stream s = FillStream("File2")) {
				Assert.IsTrue(prov.StoreFile("/File2.txt", s, true), "StoreFile should return true");
			}

			string[] files = prov.ListFiles("/");
			Assert.AreEqual(2, files.Length, "Wrong file count");
			Assert.AreEqual("/File1.txt", files[0], "Wrong file");
			Assert.AreEqual("/File2.txt", files[1], "Wrong file");
		}

		[Test]
		public void StoreFile_SubDir() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", "Test");

			Assert.AreEqual(0, prov.ListFiles("/Test").Length, "Wrong file count");

			using(Stream s = FillStream("File1")) {
				Assert.IsTrue(prov.StoreFile("/Test/File1.txt", s, false), "StoreFile should return true");
			}
			using(Stream s = FillStream("File2")) {
				Assert.IsTrue(prov.StoreFile("/Test/File2.txt", s, true), "StoreFile should return true");
			}

			string[] files = prov.ListFiles("/Test");
			Assert.AreEqual(2, files.Length, "Wrong file count");
			Assert.AreEqual("/Test/File1.txt", files[0], "Wrong file");
			Assert.AreEqual("/Test/File2.txt", files[1], "Wrong file");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StoreFile_InvalidFullName(string fn) {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile(fn, s, false);
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StoreFile_NullStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.StoreFile("/Blah.txt", null, false);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void StoreFile_ClosedStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			Stream s = FillStream("Blah");
			s.Close();

			prov.StoreFile("Blah.txt", s, false);
		}

		[Test]
		public void StoreFile_Overwrite_RetrieveFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StoreFile("/File.txt", s, false), "StoreFile should return true");
			}

			using(Stream s = FillStream("Blah222")) {
				Assert.IsFalse(prov.StoreFile("/File.txt", s, false), "StoreFile should return false");
			}

			MemoryStream ms = new MemoryStream();
			prov.RetrieveFile("/File.txt", ms);
			ms.Seek(0, SeekOrigin.Begin);
			string c = Encoding.UTF8.GetString(ms.ToArray());
			Assert.AreEqual("Blah", c, "Wrong content (seems modified");

			using(Stream s = FillStream("Blah222")) {
				Assert.IsTrue(prov.StoreFile("/File.txt", s, true), "StoreFile should return true");
			}

			ms = new MemoryStream();
			prov.RetrieveFile("/File.txt", ms);
			ms.Seek(0, SeekOrigin.Begin);
			c = Encoding.UTF8.GetString(ms.ToArray());
			Assert.AreEqual("Blah222", c, "Wrong content (seems modified");
		}

		[Test]
		public void ListFiles_NullOrEmptyDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StoreFile("/File.txt", s, false), "StoreFile should return true");
			}

			string[] files = prov.ListFiles(null);
			Assert.AreEqual(1, files.Length, "Wrong file count");
			Assert.AreEqual("/File.txt", files[0], "Wrong file");

			files = prov.ListFiles("");
			Assert.AreEqual(1, files.Length, "Wrong file count");
			Assert.AreEqual("/File.txt", files[0], "Wrong file");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void ListFiles_InexistentDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();
			prov.ListFiles("/dir/that/does/not/exist");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrieveFile_InvalidFile(string f) {
			IFilesStorageProviderV40 prov = GetProvider();

			using(MemoryStream s = new MemoryStream()) {
				prov.RetrieveFile(f, s);
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RetrieveFile_InexistentFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(MemoryStream s = new MemoryStream()) {
				prov.RetrieveFile("/Inexistent.txt", s);
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrieveFile_NullStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/File.txt", s, false);
			}

			prov.RetrieveFile("/File.txt", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RetrieveFile_ClosedStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/File.txt", s, false);
			}

			MemoryStream s2 = new MemoryStream();
			s2.Close();
			prov.RetrieveFile("/File.txt", s2);
		}

		[Test]
		public void GetFileDetails_SetFileRetrievalCount() {
			IFilesStorageProviderV40 prov = GetProvider();

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/File.txt", s, false);
			}
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/File2.txt", s, false);
			}

			FileDetails details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s);
			}
			details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File2.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s);
			}
			details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			prov.DeleteFile("/File.txt");

			Assert.IsNull(prov.GetFileDetails("/File.txt"), "GetFileDetails should return null");
			
			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetFileDetails_RenameFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s);
			}

			prov.RenameFile("/File.txt", "/File2.txt");

			FileDetails details = prov.GetFileDetails("/File.txt");
			Assert.IsNull(details, "GetFileDetails should return null");

			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetFileDetails_RenameDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			DateTime now = DateTime.Now;
			prov.CreateDirectory("/", "Dir");
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/Dir/File.txt", s, false);
			}
			prov.CreateDirectory("/Dir/", "Sub");
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/Dir/Sub/File.txt", s, false);
			}
			prov.CreateDirectory("/", "Dir100");
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/Dir100/File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir/File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir/Sub/File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir100/File.txt", s);
			}

			prov.RenameDirectory("/Dir/", "/Dir2/");

			FileDetails details;

			details = prov.GetFileDetails("/Dir100/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			Assert.IsNull(prov.GetFileDetails("/Dir/File.txt"), "GetFileDetails should return null");

			details = prov.GetFileDetails("/Dir2/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			Assert.IsNull(prov.GetFileDetails("/Dir/Sub/File.txt"), "GetFileDetails should return null");

			details = prov.GetFileDetails("/Dir2/Sub/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetFileDetails_DeleteDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			DateTime now = DateTime.Now;
			prov.CreateDirectory("/", "Dir");
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/Dir/File.txt", s, false);
			}
			prov.CreateDirectory("/Dir/", "Sub");
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/Dir/Sub/File.txt", s, false);
			}
			prov.CreateDirectory("/", "Dir2");
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/Dir2/File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir/File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir2/File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir/Sub/File.txt", s);
			}

			prov.DeleteDirectory("/Dir/");

			FileDetails details = prov.GetFileDetails("/Dir2/File.txt");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			Assert.IsNull(prov.GetFileDetails("/Dir/File.txt"), "GetFileDetails should return null");
			Assert.IsNull(prov.GetFileDetails("/Dir/Sub/File.txt"), "GetFileDetails should return null");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetFileDetails_InvalidFile(string f) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.GetFileDetails(f);
		}

		[Test]
		public void GetFileDetails_InexistentFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			Assert.IsNull(prov.GetFileDetails("/Inexistent.txt"), "GetFileDetails should return null");
		}

		[Test]
		public void DeleteFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", "Sub");

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/File.txt", s, false);
				prov.StoreFile("/Sub/File.txt", s, false);
			}

			Assert.IsTrue(prov.DeleteFile("/File.txt"), "DeleteFile should return true");
			Assert.IsTrue(prov.DeleteFile("/Sub/File.txt"), "DeleteFile should return true");
			Assert.AreEqual(0, prov.ListFiles("/").Length, "Wrong file count");
			Assert.AreEqual(0, prov.ListFiles("/Sub/").Length, "Wrong file count");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeleteFile_InvalidFile(string f) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.DeleteFile(f);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void DeleteFile_InexistentFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.DeleteFile("/File.txt");
		}

		[Test]
		public void RenameFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", "Sub");

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/File.txt", s, false);
				prov.StoreFile("/Sub/File.txt", s, false);
			}

			Assert.IsTrue(prov.RenameFile("/File.txt", "/File2.txt"), "RenameFile should return true");
			Assert.IsTrue(prov.RenameFile("/Sub/File.txt", "/Sub/File2.txt"), "RenameFile should return true");

			string[] files = prov.ListFiles("/");
			Assert.AreEqual(1, files.Length, "Wrong file count");
			Assert.AreEqual("/File2.txt", files[0], "Wrong file");

			files = prov.ListFiles("/Sub/");
			Assert.AreEqual(1, files.Length, "Wrong file count");
			Assert.AreEqual("/Sub/File2.txt", files[0], "Wrong file");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenameFile_InvalidFile(string f) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.RenameFile(f, "/Blah.txt");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RenameFile_InexistentFile() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.RenameFile("/Blah.txt", "/Blah2.txt");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenameFile_InvalidName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/File.txt", s, false);
			}

			prov.RenameFile("/File.txt", n);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RenameFile_ExistentName() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/File.txt", s, false);
				prov.StoreFile("/File2.txt", s, false);
			}

			prov.RenameFile("/File.txt", "/File2.txt");
		}

		[Test]
		public void CreateDirectory_ListDirectories() {
			IFilesStorageProviderV40 prov = GetProvider();

			Assert.IsTrue(prov.CreateDirectory("/", "Dir1"), "CreateDirectory should return true");
			Assert.IsTrue(prov.CreateDirectory("/", "Dir2"), "CreateDirectory should return true");
			Assert.IsTrue(prov.CreateDirectory("/Dir1", "Sub"), "CreateDirectory should return true");

			string[] dirs = prov.ListDirectories("/");
			Assert.AreEqual(2, dirs.Length, "Wrong dir count");
			Assert.AreEqual("/Dir1/", dirs[0], "Wrong dir");
			Assert.AreEqual("/Dir2/", dirs[1], "Wrong dir");

			dirs = prov.ListDirectories("/Dir1/");
			Assert.AreEqual(1, dirs.Length, "Wrong dir count");
			Assert.AreEqual("/Dir1/Sub/", dirs[0], "Wrong dir");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CreateDirectory_NullDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory(null, "Dir");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateDirectory_InexistentDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/Inexistent/Dir/", "Sub");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void CreateDirectory_InvalidName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", n);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateDirectory_ExistentName() {
			IFilesStorageProviderV40 prov = GetProvider();

			Assert.IsTrue(prov.CreateDirectory("/", "Dir"), "CreateDirectory should return true");
			prov.CreateDirectory("/", "Dir");
		}

		[Test]
		public void ListDirectories_NullOrEmptyDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			string[] dirs = prov.ListDirectories(null);
			Assert.AreEqual(0, dirs.Length, "Wrong dir count");

			prov.CreateDirectory("/", "Dir");

			dirs = prov.ListDirectories(null);
			Assert.AreEqual(1, dirs.Length, "Wrong dir count");
			Assert.AreEqual("/Dir/", dirs[0], "Wrong dir");

			dirs = prov.ListDirectories("");
			Assert.AreEqual(1, dirs.Length, "Wrong dir count");
			Assert.AreEqual("/Dir/", dirs[0], "Wrong dir");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void ListDirectories_InexistentDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.ListDirectories("/Inexistent/");
		}

		[Test]
		public void DeleteDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", "Dir");
			prov.CreateDirectory("/", "Dir2");
			prov.CreateDirectory("/Dir/", "Sub");

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/Dir/File.txt", s, false);
			}
			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/Dir/Sub/File.txt", s, false);
			}

			Assert.IsTrue(prov.DeleteDirectory("/Dir"), "DeleteDirectory should return true");
			Assert.AreEqual("/Dir2/", prov.ListDirectories("/")[0], "Wrong directory");
			Assert.IsTrue(prov.DeleteDirectory("/Dir2"), "DeleteDirectory should return true");
			Assert.AreEqual(0, prov.ListDirectories("/").Length, "Wrong dir count");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("/", ExpectedException = typeof(ArgumentException))]
		public void DeleteDirectory_InvalidDirectory(string d) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.DeleteDirectory(d);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void DeleteDirectory_InexistentDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.DeleteDirectory("/Inexistent/");
		}

		[Test]
		public void RenameDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", "Dir");
			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/Dir/File.txt", s, false);
			}

			Assert.IsTrue(prov.RenameDirectory("/Dir/", "/Dir2/"), "RenameDirectory should return true");

			Assert.AreEqual("/Dir2/", prov.ListDirectories("/")[0], "Wrong directory");

			bool thisHouldBeTrue = false;
			try {
				Assert.AreEqual(0, prov.ListFiles("/Dir/").Length, "Wrong file count");
			}
			catch(ArgumentException) {
				thisHouldBeTrue = true;
			}
			Assert.IsTrue(thisHouldBeTrue, "ListFiles did not throw an exception");
			Assert.AreEqual("/Dir2/File.txt", prov.ListFiles("/Dir2/")[0], "Wrong file");

			prov.CreateDirectory("/Dir2/", "Sub");
			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/Dir2/Sub/File.txt", s, false);
			}

			Assert.IsTrue(prov.RenameDirectory("/Dir2/Sub/", "/Dir2/Sub2/"), "RenameDirectory should return true");

			Assert.AreEqual("/Dir2/Sub2/", prov.ListDirectories("/Dir2/")[0], "Wrong dir");

			thisHouldBeTrue = false;
			try {
				Assert.AreEqual(0, prov.ListFiles("/Dir/Sub/").Length, "Wrong file count");
			}
			catch(ArgumentException) {
				thisHouldBeTrue = true;
			}
			Assert.IsTrue(thisHouldBeTrue, "ListFiles did not throw an exception");
			Assert.AreEqual("/Dir2/Sub2/File.txt", prov.ListFiles("/Dir2/Sub2/")[0], "Wrong file");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("/", ExpectedException = typeof(ArgumentException))]
		public void RenameDirectory_InvalidDirectory(string d) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.RenameDirectory(d, "/Dir/");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RenameDirectory_InexistentDirectory() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.RenameDirectory("/Inexistent/", "/Inexistent2/");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		[TestCase("/", ExpectedException = typeof(ArgumentException))]
		public void RenameDirectory_InvalidNewDir(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", "Dir");

			prov.RenameDirectory("/Dir/", n);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RenameDirectory_ExistentNewDir() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.CreateDirectory("/", "Dir");
			prov.CreateDirectory("/", "Dir2");

			prov.RenameDirectory("/Dir/", "/Dir2/");
		}

		[Test]
		public void StorePageAttachment_ListPageAttachments_GetPagesWithAttachments() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi1 = "MainPage";
			string pi2 = "Page2";

			Assert.AreEqual(0, prov.ListPageAttachments(pi1).Length, "Wrong attachment count");

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StorePageAttachment(pi1, "File.txt", s, false), "StorePageAttachment should return true");
				Assert.IsTrue(prov.StorePageAttachment(pi1, "File2.txt", s, false), "StorePageAttachment should return true");
				Assert.IsTrue(prov.StorePageAttachment(pi2, "File.txt", s, false), "StorePageAttachment should return true");
			}

			string[] attachs = prov.ListPageAttachments(pi1);
			Assert.AreEqual(2, attachs.Length, "Wrong attachment count");
			Assert.AreEqual("File.txt", attachs[0], "Wrong attachment");
			Assert.AreEqual("File2.txt", attachs[1], "Wrong attachment");

			attachs = prov.ListPageAttachments(pi2);
			Assert.AreEqual(1, attachs.Length, "Wrong attachment count");
			Assert.AreEqual("File.txt", attachs[0], "Wrong attachment");

			string[] pages = prov.GetPagesWithAttachments();
			Assert.AreEqual(2, pages.Length, "Wrong page count");
			Assert.AreEqual(pi1, pages[0], "Wrong page");
			Assert.AreEqual(pi2, pages[1], "Wrong page");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ListPageAttachments_NullPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.ListPageAttachments(null);
		}

		[Test]
		public void ListPageAttachments_InexistentPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			Assert.AreEqual(0, prov.ListPageAttachments("Page").Length, "Wrong attachment count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StorePageAttachment_NullPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(null, "File.txt", s, false);
			}
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void StorePageAttachment_InvalidName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, n, s, false);
			}
		}

		[Test]
		public void StorePageAttachment_ExistentName() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StorePageAttachment(pi, "File.txt", s, false), "StorePageAttachment should return true");
				Assert.IsFalse(prov.StorePageAttachment(pi, "File.txt", s, false), "StorePageAttachment should return false");
			}
		}

		[Test]
		public void StorePageAttachment_Overwrite_RetrievePageAttachment() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StorePageAttachment(pi, "File.txt", s, false), "StorePageAttachment should return true");
			}

			using(Stream s = FillStream("Blah222")) {
				Assert.IsTrue(prov.StorePageAttachment(pi, "File.txt", s, true), "StorePageAttachment should return true");
			}

			MemoryStream ms = new MemoryStream();
			Assert.IsTrue(prov.RetrievePageAttachment(pi, "File.txt", ms), "RetrievePageAttachment should return true");
			ms.Seek(0, SeekOrigin.Begin);

			Assert.AreEqual("Blah222", Encoding.UTF8.GetString(ms.ToArray()), "Wrong attachment content");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void StorePageAttachment_NullStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			prov.StorePageAttachment(pi, "File.txt", null, false);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void StorePageAttachment_ClosedStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			MemoryStream ms = new MemoryStream();
			ms.Close();
			prov.StorePageAttachment(pi, "File.txt", ms, false);
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrievePageAttachment_NullPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			using(MemoryStream ms = new MemoryStream()) {
				prov.RetrievePageAttachment(null, "File.txt", ms);
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RetrievePageAttachment_InexistentPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			pi = "Page2";

			using(MemoryStream ms = new MemoryStream()) {
				prov.RetrievePageAttachment(pi, "File.txt", ms);
			}
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RetrievePageAttachment_InvalidName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(MemoryStream ms = new MemoryStream()) {
				prov.RetrievePageAttachment(pi, n, ms);
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RetrievePageAttachment_InexistentName() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(MemoryStream ms = new MemoryStream()) {
				prov.RetrievePageAttachment(pi, "File.txt", ms);
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RetrievePageAttachment_NullStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			prov.RetrievePageAttachment(pi, "File.txt", null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RetrievePageAttachment_ClosedStream() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			MemoryStream ms = new MemoryStream();
			ms.Close();
			prov.RetrievePageAttachment(pi, "File.txt", ms);
		}

		[Test]
		public void GetPageAttachmentDetails_SetPageAttachmentRetrievalCount() {
			IFilesStorageProviderV40 prov = GetProvider();

			string page1 = "Page1";
			string page2 = "Page2";

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page1, "File.txt", s, false);
			}
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page2, "File.txt", s, false);
			}
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page1, "File2.txt", s, false);
			}

			FileDetails details;

			details = prov.GetPageAttachmentDetails(page1, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page1, "File.txt", s);
			}
			details = prov.GetPageAttachmentDetails(page1, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page1, "File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page1, "File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page2, "File.txt", s);
			}

			details = prov.GetPageAttachmentDetails(page1, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			prov.DeletePageAttachment(page1, "File.txt");

			Assert.IsNull(prov.GetPageAttachmentDetails(page1, "File.txt"), "GetPageAttachmentDetails should return null");

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetPageAttachmentDetails_NotifyPageRenaming() {
			IFilesStorageProviderV40 prov = GetProvider();

			string page = "Page";
			string page2 ="Page2";
			string newPage = "newPage";

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page, "File.txt", s, false);
			}
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page2, "File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page, "File.txt", s);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page2, "File.txt", s);
			}

			prov.NotifyPageRenaming(page, newPage);

			FileDetails details;

			Assert.IsNull(prov.GetPageAttachmentDetails(page, "File.txt"), "GetPageAttachmentDetails should return null");

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(newPage, "File.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetPageAttachmentDetails_RenamePageAttachment() {
			IFilesStorageProviderV40 prov = GetProvider();

			string page = "Page";

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page, "File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page, "File.txt", s);
			}

			prov.RenamePageAttachment(page, "File.txt", "File2.txt");

			FileDetails details;

			Assert.IsNull(prov.GetPageAttachmentDetails(page, "File.txt"), "GetPageAttachmentDetails should return null");

			details = prov.GetPageAttachmentDetails(page, "File2.txt");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetPageAttachmentDetails_InexistentAttachment() {
			IFilesStorageProviderV40 prov = GetProvider();

			Assert.IsNull(prov.GetPageAttachmentDetails("Inexistent", "File.txt"), "GetPageAttachmentDetails should retur null");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetPageAttachmentDetails_NullPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.GetPageAttachmentDetails(null, "File.txt");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void GetPageAttachmentDetails_InvalidName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.GetPageAttachmentDetails("Page", n);
		}

		[Test]
		public void DeletePageAttachment() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			Assert.IsTrue(prov.DeletePageAttachment(pi, "File.txt"), "DeletePageAttachment should return true");

			Assert.IsNull(prov.GetPageAttachmentDetails(pi, "File.txt"), "GetPageAttachmentDetails should return null");
			Assert.AreEqual(0, prov.ListPageAttachments(pi).Length, "Wrong attachment count");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void DeletePageAttachment_NullPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.DeletePageAttachment(null, "File.txt");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void DeletePageAttachment_InexistentPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			prov.DeletePageAttachment(pi, "File.txt");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void DeletePageAttachment_InvalidName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			prov.DeletePageAttachment(pi, n);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void DeletePageAttachment_InexistentName() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			prov.DeletePageAttachment(pi, "File222.txt");
		}

		[Test]
		public void RenamePageAttachment() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			Assert.IsTrue(prov.RenamePageAttachment(pi, "File.txt", "File2.txt"), "RenamePageAttachment should return true");

			string[] attachs = prov.ListPageAttachments(pi);
			Assert.AreEqual(1, attachs.Length, "Wrong attachment count");
			Assert.AreEqual("File2.txt", attachs[0], "Wrong attachment");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RenamePageAttachment_NullPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			prov.RenamePageAttachment(null, "File.txt", "File2.txt");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RenamePageAttachment_InexistentPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			prov.RenamePageAttachment(pi, "File.txt", "File2.txt");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenamePageAttachment_InvalidName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			prov.RenamePageAttachment(pi, n, "File2.txt");
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RenamePageAttachment_InexistentName() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			prov.RenamePageAttachment(pi, "File1.txt", "File2.txt");
		}

		[TestCase(null, ExpectedException = typeof(ArgumentNullException))]
		[TestCase("", ExpectedException = typeof(ArgumentException))]
		public void RenamePageAttachment_InvalidNewName(string n) {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			prov.RenamePageAttachment(pi, "File.txt", n);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void RenamePageAttachment_ExistentNewName() {
			IFilesStorageProviderV40 prov = GetProvider();

			string pi = "Page";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
				prov.StorePageAttachment(pi, "File2.txt", s, false);
			}

			prov.RenamePageAttachment(pi, "File.txt", "File2.txt");
		}

		[Test]
		public void NotifyPageRenaming() {
			IFilesStorageProviderV40 prov = GetProvider();

			string p1 = "Page1";
			string p2 = "Page2";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(p1, "File1.txt", s, false);
				prov.StorePageAttachment(p1, "File2.txt", s, false);
			}

			prov.NotifyPageRenaming(p1, p2);

			Assert.AreEqual(0, prov.ListPageAttachments(p1).Length, "Wrong attachment count");

			string[] attachs = prov.ListPageAttachments(p2);
			Assert.AreEqual(2, attachs.Length, "Wrong attachment count");
			Assert.AreEqual("File1.txt", attachs[0], "Wrong attachment");
			Assert.AreEqual("File2.txt", attachs[1], "Wrong attachment");
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NotifyPageRenaming_NullOldPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string p2 = "Page2";

			prov.NotifyPageRenaming(null, p2);
		}

		[Test]
		public void NotifyPageRenaming_InexistentOldPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string p1 = "Page1";
			string p2 = "Page2";

			prov.NotifyPageRenaming(p1, p2);

			// Nothing specific to verify
		}

		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void NotifyPageRenaming_NullNewPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string p1 = "Page1";

			prov.NotifyPageRenaming(p1, null);
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void NotifyPageRenaming_ExistentNewPage() {
			IFilesStorageProviderV40 prov = GetProvider();

			string p1 ="Page1";
			string p2 ="Page2";

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(p1, "File1.txt", s, false);
				prov.StorePageAttachment(p2, "File2.txt", s, false);
			}

			prov.NotifyPageRenaming(p1, p2);
		}

		[Test]
		public void CompleteTestsForCaseInsensitivity_Files() {
			IFilesStorageProviderV40 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StoreFile("/File.TXT", s, false), "StoreFile should return true");
				Assert.IsFalse(prov.StoreFile("/file.txt", s, false), "StoreFile should return false");
				prov.CreateDirectory("/", "Sub");
				Assert.IsTrue(prov.StoreFile("/Sub/File.TXT", s, false), "StoreFile should return true");
				Assert.IsFalse(prov.StoreFile("/SUB/File.TXT", s, false), "StoreFile should return false");
			}

			Assert.IsNotNull(prov.GetFileDetails("/file.TXT"), "GetFileDetails should return something");
			Assert.IsNotNull(prov.GetFileDetails("/suB/fILe.TXT"), "GetFileDetails should return something");

			MemoryStream ms = new MemoryStream();
			Assert.IsTrue(prov.RetrieveFile("/FILE.tXt", ms), "RetrieveFile should return true");
			ms = new MemoryStream();
			Assert.IsTrue(prov.RetrieveFile("/SuB/FILe.tXt", ms), "RetrieveFile should return true");

			Assert.IsTrue(prov.RenameFile("/FILE.TXT", "/NEWfile.txt"), "RenameFile should return true");
			Assert.IsTrue(prov.RenameFile("/SUB/FILE.TXT", "/sub/NEWfile.txt"), "RenameFile should return true");

			Assert.IsTrue(prov.DeleteFile("/newfile.txt"), "DeleteFile should return true");
			Assert.IsTrue(prov.DeleteFile("/sub/newfile.txt"), "DeleteFile should return true");
		}

		[Test]
		public void CompleteTestsForCaseInsensitivity_Attachments() {
			IFilesStorageProviderV40 prov = GetProvider();

			string page = "Page";

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StorePageAttachment(page, "Attachment.TXT", s, false), "StorePageAttachment should return true");
				Assert.IsFalse(prov.StorePageAttachment(page, "ATTACHMENT.txt", s, false), "StorePageAttachment should return false");
			}

			Assert.IsNotNull(prov.GetPageAttachmentDetails(page, "attachment.txt"), "GetPageAttachmentDetails should return a value");

			MemoryStream ms = new MemoryStream();
			Assert.IsTrue(prov.RetrievePageAttachment(page, "Attachment.txt", ms), "RetrievePageAttachment should return true");

			Assert.IsTrue(prov.RenamePageAttachment(page, "Attachment.txt", "NEWATT.txt"), "RenamePageAttachment should return true");

			Assert.IsTrue(prov.DeletePageAttachment(page, "newatt.TXT"), "DeletePageAttachment should return true");
		}

	}

}
