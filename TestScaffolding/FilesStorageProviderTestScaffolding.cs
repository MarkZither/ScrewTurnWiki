
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

		protected IHostV30 MockHost() {
			if(!Directory.Exists(testDir))
			{
				Directory.CreateDirectory(testDir);
			}

			IHostV30 host = mocks.DynamicMock<IHostV30>();
			Expect.Call(host.GetSettingValue(SettingName.PublicDirectory)).Return(testDir).Repeat.AtLeastOnce();

			mocks.Replay(host);

			return host;
		}

		protected IPagesStorageProviderV30 MockPagesProvider() {
			IPagesStorageProviderV30 prov = mocks.DynamicMock<IPagesStorageProviderV30>();

			mocks.Replay(prov);

			return prov;
		}

		public abstract IFilesStorageProviderV30 GetProvider();

		[Test]
		public void Init_NullHost() {
			IFilesStorageProviderV30 prov = GetProvider();
			Assert.That(() => prov.Init(null, ""), Throws.ArgumentNullException);
		}

		[Test]
		public void Init_NullConfig() {
			IFilesStorageProviderV30 prov = GetProvider();
			Assert.That(() => prov.Init(MockHost(), null), Throws.ArgumentNullException);
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
			IFilesStorageProviderV30 prov = GetProvider();

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
			IFilesStorageProviderV30 prov = GetProvider();

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

		[TestCase("")]
		public void StoreFile_InvalidFullName(string fn) {
			IFilesStorageProviderV30 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				Assert.That(() => prov.StoreFile(fn, s, false), Throws.ArgumentException);
			}
		}

		[TestCase(null)]
		public void StoreFile_NullFullName(string fn)
		{
			IFilesStorageProviderV30 prov = GetProvider();

			using(Stream s = FillStream("Blah"))
			{
				Assert.That(() => prov.StoreFile(fn, s, false), Throws.ArgumentNullException);
			}
		}

		[Test]
		public void StoreFile_NullStream() {
			IFilesStorageProviderV30 prov = GetProvider();

			Assert.That(() => prov.StoreFile("/Blah.txt", null, false), Throws.ArgumentNullException);
		}

		[Test]
		public void StoreFile_ClosedStream()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				Stream s = FillStream("Blah");
				s.Close();

				prov.StoreFile("Blah.txt", s, false);
			});
		}

		[Test]
		public void StoreFile_Overwrite_RetrieveFile() {
			IFilesStorageProviderV30 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StoreFile("/File.txt", s, false), "StoreFile should return true");
			}

			using(Stream s = FillStream("Blah222")) {
				Assert.IsFalse(prov.StoreFile("/File.txt", s, false), "StoreFile should return false");
			}

			MemoryStream ms = new MemoryStream();
			prov.RetrieveFile("/File.txt", ms, false);
			ms.Seek(0, SeekOrigin.Begin);
			string c = Encoding.UTF8.GetString(ms.ToArray());
			Assert.AreEqual("Blah", c, "Wrong content (seems modified");

			using(Stream s = FillStream("Blah222")) {
				Assert.IsTrue(prov.StoreFile("/File.txt", s, true), "StoreFile should return true");
			}

			ms = new MemoryStream();
			prov.RetrieveFile("/File.txt", ms, false);
			ms.Seek(0, SeekOrigin.Begin);
			c = Encoding.UTF8.GetString(ms.ToArray());
			Assert.AreEqual("Blah222", c, "Wrong content (seems modified");
		}

		[Test]
		public void ListFiles_NullOrEmptyDirectory() {
			IFilesStorageProviderV30 prov = GetProvider();

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
		public void ListFiles_InexistentDirectory() {
			IFilesStorageProviderV30 prov = GetProvider();
			Assert.That(() => prov.ListFiles("/dir/that/does/not/exist"), Throws.ArgumentException);
		}

		[TestCase("")]
		public void RetrieveFile_InvalidFile(string f) {
			IFilesStorageProviderV30 prov = GetProvider();

			using(MemoryStream s = new MemoryStream()) {
				Assert.That(() => prov.RetrieveFile(f, s, false), Throws.ArgumentException);
			}
		}

		[TestCase(null)]
		public void RetrieveFile_NullFile(string f)
		{
			IFilesStorageProviderV30 prov = GetProvider();

			using(MemoryStream s = new MemoryStream())
			{
				Assert.That(() => prov.RetrieveFile(f, s, false), Throws.ArgumentNullException);
			}
		}

		[Test]
		public void RetrieveFile_InexistentFile() {
			IFilesStorageProviderV30 prov = GetProvider();

			using(MemoryStream s = new MemoryStream()) {
				Assert.That(() => prov.RetrieveFile("/Inexistent.txt", s, false), Throws.ArgumentException);
			}
		}

		[Test]
		public void RetrieveFile_NullStream() {
			IFilesStorageProviderV30 prov = GetProvider();

			using(Stream s = FillStream("Blah")) {
				prov.StoreFile("/File.txt", s, false);
			}

			Assert.That(() => prov.RetrieveFile("/File.txt", null, false), Throws.ArgumentNullException);
		}

		[Test]
		public void RetrieveFile_ClosedStream()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				using(Stream s = FillStream("Blah"))
				{
					prov.StoreFile("/File.txt", s, false);
				}

				MemoryStream s2 = new MemoryStream();
				s2.Close();
				prov.RetrieveFile("/File.txt", s2, false);
			});
		}

		[Test]
		public void GetFileDetails_SetFileRetrievalCount() {
			IFilesStorageProviderV30 prov = GetProvider();

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/File.txt", s, false);
			}
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/File2.txt", s, false);
			}

			FileDetails details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s, false);
			}
			details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File2.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s, true);
			}
			details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(2, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			prov.SetFileRetrievalCount("/File2.txt", 0);

			details = prov.GetFileDetails("/File.txt");
			Assert.AreEqual(2, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			prov.DeleteFile("/File.txt");

			Assert.IsNull(prov.GetFileDetails("/File.txt"), "GetFileDetails should return null");
			
			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetFileDetails_RenameFile() {
			IFilesStorageProviderV30 prov = GetProvider();

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StoreFile("/File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/File.txt", s, true);
			}

			prov.RenameFile("/File.txt", "/File2.txt");

			FileDetails details = prov.GetFileDetails("/File.txt");
			Assert.IsNull(details, "GetFileDetails should return null");

			details = prov.GetFileDetails("/File2.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetFileDetails_RenameDirectory() {
			IFilesStorageProviderV30 prov = GetProvider();

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
				prov.RetrieveFile("/Dir/File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir/Sub/File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir100/File.txt", s, true);
			}

			prov.RenameDirectory("/Dir/", "/Dir2/");

			FileDetails details;

			details = prov.GetFileDetails("/Dir100/File.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			Assert.IsNull(prov.GetFileDetails("/Dir/File.txt"), "GetFileDetails should return null");

			details = prov.GetFileDetails("/Dir2/File.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			Assert.IsNull(prov.GetFileDetails("/Dir/Sub/File.txt"), "GetFileDetails should return null");

			details = prov.GetFileDetails("/Dir2/Sub/File.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetFileDetails_DeleteDirectory() {
			IFilesStorageProviderV30 prov = GetProvider();

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
				prov.RetrieveFile("/Dir/File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir2/File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrieveFile("/Dir/Sub/File.txt", s, true);
			}

			prov.DeleteDirectory("/Dir/");

			FileDetails details = prov.GetFileDetails("/Dir2/File.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong file retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong file size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			Assert.IsNull(prov.GetFileDetails("/Dir/File.txt"), "GetFileDetails should return null");
			Assert.IsNull(prov.GetFileDetails("/Dir/Sub/File.txt"), "GetFileDetails should return null");
		}

		[TestCase(null)]
		public void GetFileDetails_InvalidFile_ShouldThrowArgumentNullException(string f)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.GetFileDetails(f);
			});
		}

		[TestCase("")]
		public void GetFileDetails_InvalidFile_ShouldThrowArgumentException(string f)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.GetFileDetails(f);
			});
		}

		[Test]
		public void GetFileDetails_InexistentFile() {
			IFilesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.GetFileDetails("/Inexistent.txt"), "GetFileDetails should return null");
		}

		[TestCase(null)]
		public void SetFileRetrievalCount_InvalidFile_ShouldThrowArgumentNullException(string f)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.SetFileRetrievalCount(f, 10);
			});
		}

		[TestCase("")]
		public void SetFileRetrievalCount_InvalidFile_ShouldThrowArgumentException(string f)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.SetFileRetrievalCount(f, 10);
			});
		}

		[Test]
		public void SetFileRetrievalCount_NegativeCount()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.SetFileRetrievalCount("/File.txt", -1);
			});
		}

		[Test]
		public void DeleteFile() {
			IFilesStorageProviderV30 prov = GetProvider();

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

		[TestCase(null)]
		public void DeleteFile_InvalidFile_ShouldThrowArgumentNullException(string f)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.DeleteFile(f);
			});
		}

		[TestCase("")]
		public void DeleteFile_InvalidFile_ShouldThrowArgumentException(string f)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.DeleteFile(f);
			});
		}

		[Test]
		public void DeleteFile_InexistentFile()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.DeleteFile("/File.txt");
			});
		}

		[Test]
		public void RenameFile() {
			IFilesStorageProviderV30 prov = GetProvider();

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

		[TestCase(null)]
		public void RenameFile_InvalidFile_ShouldThrowArgumentNullException(string f)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.RenameFile(f, "/Blah.txt");
			});
		}

		[TestCase("")]
		public void RenameFile_InvalidFile_ShouldThrowArgumentException(string f)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.RenameFile(f, "/Blah.txt");
			});
		}

		[Test]
		public void RenameFile_InexistentFile()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.RenameFile("/Blah.txt", "/Blah2.txt");
			});
		}

		[TestCase(null)]
		public void RenameFile_InvalidName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				using(Stream s = FillStream("Blah"))
				{
					prov.StoreFile("/File.txt", s, false);
				}

				prov.RenameFile("/File.txt", n);
			});
		}

		[TestCase("")]
		public void RenameFile_InvalidName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				using(Stream s = FillStream("Blah"))
				{
					prov.StoreFile("/File.txt", s, false);
				}

				prov.RenameFile("/File.txt", n);
			});
		}

		[Test]
		public void RenameFile_ExistentName()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				using(Stream s = FillStream("Blah"))
				{
					prov.StoreFile("/File.txt", s, false);
					prov.StoreFile("/File2.txt", s, false);
				}

				prov.RenameFile("/File.txt", "/File2.txt");
			});
		}

		[Test]
		public void CreateDirectory_ListDirectories() {
			IFilesStorageProviderV30 prov = GetProvider();

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
		public void CreateDirectory_NullDirectory()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.CreateDirectory(null, "Dir");
			});
		}

		[Test]
		public void CreateDirectory_InexistentDirectory()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.CreateDirectory("/Inexistent/Dir/", "Sub");
			});
		}

		[TestCase(null)]
		public void CreateDirectory_InvalidName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.CreateDirectory("/", n);
			});
		}

		[TestCase("")]
		public void CreateDirectory_InvalidName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.CreateDirectory("/", n);
			});
		}

		[Test]
		public void CreateDirectory_ExistentName()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				Assert.IsTrue(prov.CreateDirectory("/", "Dir"), "CreateDirectory should return true");
				prov.CreateDirectory("/", "Dir");
			});
		}

		[Test]
		public void ListDirectories_NullOrEmptyDirectory() {
			IFilesStorageProviderV30 prov = GetProvider();

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
		public void ListDirectories_InexistentDirectory()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.ListDirectories("/Inexistent/");
			});
		}

		[Test]
		public void DeleteDirectory() {
			IFilesStorageProviderV30 prov = GetProvider();

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

		[TestCase(null)]
		public void DeleteDirectory_InvalidDirectory_ShouldThrowArgumentNullException(string d)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.DeleteDirectory(d);
			});
		}

		[TestCase("")]
		[TestCase("/")]
		public void DeleteDirectory_InvalidDirectory_ShouldThrowArgumentException(string d)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.DeleteDirectory(d);
			});
		}

		[Test]
		public void DeleteDirectory_InexistentDirectory()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.DeleteDirectory("/Inexistent/");
			});
		}

		[Test]
		public void RenameDirectory() {
			IFilesStorageProviderV30 prov = GetProvider();

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

		[TestCase(null)]
		public void RenameDirectory_InvalidDirectory_ShouldThrowArgumentNullException(string d)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.RenameDirectory(d, "/Dir/");
			});
		}

		[TestCase("")]
		[TestCase("/")]
		public void RenameDirectory_InvalidDirectory_ShouldThrowArgumentException(string d)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.RenameDirectory(d, "/Dir/");
			});
		}

		[Test]
		public void RenameDirectory_InexistentDirectory()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.RenameDirectory("/Inexistent/", "/Inexistent2/");
			});
		}

		[TestCase(null)]
		public void RenameDirectory_InvalidNewDir_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.CreateDirectory("/", "Dir");

				prov.RenameDirectory("/Dir/", n);
			});
		}

		[TestCase("")]
		[TestCase("/")]
		public void RenameDirectory_InvalidNewDir_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.CreateDirectory("/", "Dir");

				prov.RenameDirectory("/Dir/", n);
			});
		}

		[Test]
		public void RenameDirectory_ExistentNewDir()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.CreateDirectory("/", "Dir");
				prov.CreateDirectory("/", "Dir2");

				prov.RenameDirectory("/Dir/", "/Dir2/");
			});
		}

		[Test]
		public void StorePageAttachment_ListPageAttachments_GetPagesWithAttachments() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo pi1 = new PageInfo("MainPage", null, DateTime.Now);
			PageInfo pi2 = new PageInfo("Page2", null, DateTime.Now);

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
			Assert.AreEqual(pi1.FullName, pages[0], "Wrong page");
			Assert.AreEqual(pi2.FullName, pages[1], "Wrong page");
		}

		[Test]
		public void ListPageAttachments_NullPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.ListPageAttachments(null);
			});
		}

		[Test]
		public void ListPageAttachments_InexistentPage() {
			IFilesStorageProviderV30 prov = GetProvider();

			Assert.AreEqual(0, prov.ListPageAttachments(new PageInfo("Page", MockPagesProvider(), DateTime.Now)).Length, "Wrong attachment count");
		}

		[Test]
		public void StorePageAttachment_NullPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(null, "File.txt", s, false);
				}
			});
		}

		[TestCase(null)]
		public void StorePageAttachment_InvalidName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, n, s, false);
				}
			});
		}

		[TestCase("")]
		public void StorePageAttachment_InvalidName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, n, s, false);
				}
			});
		}

		[Test]
		public void StorePageAttachment_ExistentName() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StorePageAttachment(pi, "File.txt", s, false), "StorePageAttachment should return true");
				Assert.IsFalse(prov.StorePageAttachment(pi, "File.txt", s, false), "StorePageAttachment should return false");
			}
		}

		[Test]
		public void StorePageAttachment_Overwrite_RetrievePageAttachment() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StorePageAttachment(pi, "File.txt", s, false), "StorePageAttachment should return true");
			}

			using(Stream s = FillStream("Blah222")) {
				Assert.IsTrue(prov.StorePageAttachment(pi, "File.txt", s, true), "StorePageAttachment should return true");
			}

			MemoryStream ms = new MemoryStream();
			Assert.IsTrue(prov.RetrievePageAttachment(pi, "File.txt", ms, false), "RetrievePageAttachment should return true");
			ms.Seek(0, SeekOrigin.Begin);

			Assert.AreEqual("Blah222", Encoding.UTF8.GetString(ms.ToArray()), "Wrong attachment content");
		}

		[Test]
		public void StorePageAttachment_NullStream()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.StorePageAttachment(pi, "File.txt", null, false);
			});
		}

		[Test]
		public void StorePageAttachment_ClosedStream()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				MemoryStream ms = new MemoryStream();
				ms.Close();
				prov.StorePageAttachment(pi, "File.txt", ms, false);
			});
		}

		[Test]
		public void RetrievePageAttachment_NullPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, "File.txt", s, false);
				}

				using(MemoryStream ms = new MemoryStream())
				{
					prov.RetrievePageAttachment(null, "File.txt", ms, false);
				}
			});
		}

		[Test]
		public void RetrievePageAttachment_InexistentPage()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, "File.txt", s, false);
				}

				pi = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);

				using(MemoryStream ms = new MemoryStream())
				{
					prov.RetrievePageAttachment(pi, "File.txt", ms, false);
				}
			});
		}

		[TestCase(null)]
		public void RetrievePageAttachment_InvalidName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(MemoryStream ms = new MemoryStream())
				{
					prov.RetrievePageAttachment(pi, n, ms, false);
				}
			});
		}

		[TestCase("")]
		public void RetrievePageAttachment_InvalidName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(MemoryStream ms = new MemoryStream())
				{
					prov.RetrievePageAttachment(pi, n, ms, false);
				}
			});
		}

		[Test]
		public void RetrievePageAttachment_InexistentName()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(MemoryStream ms = new MemoryStream())
				{
					prov.RetrievePageAttachment(pi, "File.txt", ms, false);
				}
			});
		}

		[Test]
		public void RetrievePageAttachment_NullStream()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, "File.txt", s, false);
				}

				prov.RetrievePageAttachment(pi, "File.txt", null, false);
			});
		}

		[Test]
		public void RetrievePageAttachment_ClosedStream()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, "File.txt", s, false);
				}

				MemoryStream ms = new MemoryStream();
				ms.Close();
				prov.RetrievePageAttachment(pi, "File.txt", ms, false);
			});
		}

		[Test]
		public void GetPageAttachmentDetails_SetPageAttachmentRetrievalCount() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo page1 = new PageInfo("Page1", null, DateTime.Now);
			PageInfo page2 = new PageInfo("Page2", null, DateTime.Now);

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
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page1, "File.txt", s, false);
			}
			details = prov.GetPageAttachmentDetails(page1, "File.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page1, "File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page1, "File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page2, "File.txt", s, true);
			}

			details = prov.GetPageAttachmentDetails(page1, "File.txt");
			Assert.AreEqual(2, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			prov.SetPageAttachmentRetrievalCount(page2, "File.txt", 0);

			details = prov.GetPageAttachmentDetails(page1, "File.txt");
			Assert.AreEqual(2, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			prov.DeletePageAttachment(page1, "File.txt");

			Assert.IsNull(prov.GetPageAttachmentDetails(page1, "File.txt"), "GetPageAttachmentDetails should return null");

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(page1, "File2.txt");
			Assert.AreEqual(0, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetPageAttachmentDetails_NotifyPageRenaming() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo page = new PageInfo("Page", null, DateTime.Now);
			PageInfo page2 = new PageInfo("Page2", null, DateTime.Now);
			PageInfo newPage = new PageInfo("newPage", null, DateTime.Now);

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page, "File.txt", s, false);
			}
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page2, "File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page, "File.txt", s, true);
			}
			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page2, "File.txt", s, true);
			}

			prov.NotifyPageRenaming(page, newPage);

			FileDetails details;

			Assert.IsNull(prov.GetPageAttachmentDetails(page, "File.txt"), "GetPageAttachmentDetails should return null");

			details = prov.GetPageAttachmentDetails(page2, "File.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);

			details = prov.GetPageAttachmentDetails(newPage, "File.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetPageAttachmentDetails_RenamePageAttachment() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo page = new PageInfo("Page", null, DateTime.Now);

			DateTime now = DateTime.Now;
			using(Stream s = FillStream("Content")) {
				prov.StorePageAttachment(page, "File.txt", s, false);
			}

			using(Stream s = new MemoryStream()) {
				prov.RetrievePageAttachment(page, "File.txt", s, true);
			}

			prov.RenamePageAttachment(page, "File.txt", "File2.txt");

			FileDetails details;

			Assert.IsNull(prov.GetPageAttachmentDetails(page, "File.txt"), "GetPageAttachmentDetails should return null");

			details = prov.GetPageAttachmentDetails(page, "File2.txt");
			Assert.AreEqual(1, details.RetrievalCount, "Wrong attachment retrieval count");
			Assert.AreEqual(7, details.Size, "Wrong attachment size");
			Tools.AssertDateTimesAreEqual(now, details.LastModified, true);
		}

		[Test]
		public void GetPageAttachmentDetails_InexistentAttachment() {
			IFilesStorageProviderV30 prov = GetProvider();

			Assert.IsNull(prov.GetPageAttachmentDetails(new PageInfo("Inexistent", null, DateTime.Now), "File.txt"), "GetPageAttachmentDetails should retur null");
		}

		[Test]
		public void GetPageAttachmentDetails_NullPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.GetPageAttachmentDetails(null, "File.txt");
			});
		}

		[TestCase(null)]
		public void GetPageAttachmentDetails_InvalidName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.GetPageAttachmentDetails(new PageInfo("Page", null, DateTime.Now), n);
			});
		}

		[TestCase("")]
		public void GetPageAttachmentDetails_InvalidName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.GetPageAttachmentDetails(new PageInfo("Page", null, DateTime.Now), n);
			});
		}

		[Test]
		public void SetPageAttachmentRetrievalCount_NullPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.SetPageAttachmentRetrievalCount(null, "File.txt", 10);
			});
		}

		[TestCase(null)]
		public void SetPageAttachmentRetrievalCount_InvalidFile_ShouldThrowArgumentNullException(string f)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.SetPageAttachmentRetrievalCount(new PageInfo("Page", null, DateTime.Now), f, 10);
			});
		}

		[TestCase("")]
		public void SetPageAttachmentRetrievalCount_InvalidFile_ShouldThrowArgumentException(string f)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.SetPageAttachmentRetrievalCount(new PageInfo("Page", null, DateTime.Now), f, 10);
			});
		}

		[Test]
		public void SetPageAttachmentRetrievalCount_NegativeCount()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.SetPageAttachmentRetrievalCount(new PageInfo("Page", null, DateTime.Now), "File.txt", -1);
			});
		}

		[Test]
		public void DeletePageAttachment() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			Assert.IsTrue(prov.DeletePageAttachment(pi, "File.txt"), "DeletePageAttachment should return true");

			Assert.IsNull(prov.GetPageAttachmentDetails(pi, "File.txt"), "GetPageAttachmentDetails should return null");
			Assert.AreEqual(0, prov.ListPageAttachments(pi).Length, "Wrong attachment count");
		}

		[Test]
		public void DeletePageAttachment_NullPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.DeletePageAttachment(null, "File.txt");
			});
		}

		[Test]
		public void DeletePageAttachment_InexistentPage()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.DeletePageAttachment(pi, "File.txt");
			});
		}

		[TestCase(null)]
		public void DeletePageAttachment_InvalidName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.DeletePageAttachment(pi, n);
			});
		}

		[TestCase("")]
		public void DeletePageAttachment_InvalidName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.DeletePageAttachment(pi, n);
			});
		}

		[Test]
		public void DeletePageAttachment_InexistentName()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, "File.txt", s, false);
				}

				prov.DeletePageAttachment(pi, "File222.txt");
			});
		}

		[Test]
		public void RenamePageAttachment() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

			using(Stream s = FillStream("Blah")) {
				prov.StorePageAttachment(pi, "File.txt", s, false);
			}

			Assert.IsTrue(prov.RenamePageAttachment(pi, "File.txt", "File2.txt"), "RenamePageAttachment should return true");

			string[] attachs = prov.ListPageAttachments(pi);
			Assert.AreEqual(1, attachs.Length, "Wrong attachment count");
			Assert.AreEqual("File2.txt", attachs[0], "Wrong attachment");
		}

		[Test]
		public void RenamePageAttachment_NullPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				prov.RenamePageAttachment(null, "File.txt", "File2.txt");
			});
		}

		[Test]
		public void RenamePageAttachment_InexistentPage()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.RenamePageAttachment(pi, "File.txt", "File2.txt");
			});
		}

		[TestCase(null)]
		public void RenamePageAttachment_InvalidName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.RenamePageAttachment(pi, n, "File2.txt");
			});
		}

		[TestCase("")]
		public void RenamePageAttachment_InvalidName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.RenamePageAttachment(pi, n, "File2.txt");
			});
		}

		[Test]
		public void RenamePageAttachment_InexistentName()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, "File.txt", s, false);
				}

				prov.RenamePageAttachment(pi, "File1.txt", "File2.txt");
			});
		}

		[TestCase(null)]
		public void RenamePageAttachment_InvalidNewName_ShouldThrowArgumentNullException(string n)
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.RenamePageAttachment(pi, "File.txt", n);
			});
		}

		[TestCase("")]
		public void RenamePageAttachment_InvalidNewName_ShouldThrowArgumentException(string n)
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				prov.RenamePageAttachment(pi, "File.txt", n);
			});
		}

		[Test]
		public void RenamePageAttachment_ExistentNewName()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo pi = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(pi, "File.txt", s, false);
					prov.StorePageAttachment(pi, "File2.txt", s, false);
				}

				prov.RenamePageAttachment(pi, "File.txt", "File2.txt");
			});
		}

		[Test]
		public void NotifyPageRenaming() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);

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
		public void NotifyPageRenaming_NullOldPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);

				prov.NotifyPageRenaming(null, p2);
			});
		}

		[Test]
		public void NotifyPageRenaming_InexistentOldPage() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
			PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);

			prov.NotifyPageRenaming(p1, p2);

			// Nothing specific to verify
		}

		[Test]
		public void NotifyPageRenaming_NullNewPage()
		{
			Assert.Throws<ArgumentNullException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);

				prov.NotifyPageRenaming(p1, null);
			});
		}

		[Test]
		public void NotifyPageRenaming_ExistentNewPage()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				IFilesStorageProviderV30 prov = GetProvider();

				PageInfo p1 = new PageInfo("Page1", MockPagesProvider(), DateTime.Now);
				PageInfo p2 = new PageInfo("Page2", MockPagesProvider(), DateTime.Now);

				using(Stream s = FillStream("Blah"))
				{
					prov.StorePageAttachment(p1, "File1.txt", s, false);
					prov.StorePageAttachment(p2, "File2.txt", s, false);
				}

				prov.NotifyPageRenaming(p1, p2);
			});
		}

		[Test]
		public void CompleteTestsForCaseInsensitivity_Files() {
			IFilesStorageProviderV30 prov = GetProvider();

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
			Assert.IsTrue(prov.RetrieveFile("/FILE.tXt", ms, false), "RetrieveFile should return true");
			ms = new MemoryStream();
			Assert.IsTrue(prov.RetrieveFile("/SuB/FILe.tXt", ms, false), "RetrieveFile should return true");

			Assert.IsTrue(prov.RenameFile("/FILE.TXT", "/NEWfile.txt"), "RenameFile should return true");
			Assert.IsTrue(prov.RenameFile("/SUB/FILE.TXT", "/sub/NEWfile.txt"), "RenameFile should return true");

			Assert.IsTrue(prov.DeleteFile("/newfile.txt"), "DeleteFile should return true");
			Assert.IsTrue(prov.DeleteFile("/sub/newfile.txt"), "DeleteFile should return true");
		}

		[Test]
		public void CompleteTestsForCaseInsensitivity_Attachments() {
			IFilesStorageProviderV30 prov = GetProvider();

			PageInfo page = new PageInfo("Page", MockPagesProvider(), DateTime.Now);

			Host.Instance = new Host();

			Collectors.IndexDirectoryProvider = new DummyIndexDirectoryProvider();
			ProviderLoader.SetUp<IIndexDirectoryProviderV30>(typeof(DummyIndexDirectoryProvider), "");

			using(Stream s = FillStream("Blah")) {
				Assert.IsTrue(prov.StorePageAttachment(page, "Attachment.TXT", s, false), "StorePageAttachment should return true");
				Assert.IsFalse(prov.StorePageAttachment(page, "ATTACHMENT.txt", s, false), "StorePageAttachment should return false");
			}

			Assert.IsNotNull(prov.GetPageAttachmentDetails(page, "attachment.txt"), "GetPageAttachmentDetails should return a value");

			MemoryStream ms = new MemoryStream();
			Assert.IsTrue(prov.RetrievePageAttachment(page, "Attachment.txt", ms, false), "RetrievePageAttachment should return true");

			Assert.IsTrue(prov.RenamePageAttachment(page, "Attachment.txt", "NEWATT.txt"), "RenamePageAttachment should return true");

			Assert.IsTrue(prov.DeletePageAttachment(page, "newatt.TXT"), "DeletePageAttachment should return true");
		}

		private class DummyIndexDirectoryProvider : IIndexDirectoryProviderV30
		{

			#region IIndexDirectoryProviderV30 Members

			private static Lucene.Net.Store.Directory directory;

			public Lucene.Net.Store.Directory GetDirectory()
			{
				return directory;
			}

			#endregion

			#region IProviderV30 Members

			public string CurrentWiki
			{
				get
				{
					return "wiki1";
				}
			}

			public void Init(IHostV30 host, string config)
			{
				// Nothing to do.
			}

			public void SetUp(IHostV30 host, string config)
			{
				directory = new Lucene.Net.Store.RAMDirectory();
			}

			public ComponentInformation Information
			{
				get
				{
					return new ComponentInformation("DummyIndexDirectoryProvider", "Threeplicate", "1.0", "", "");
				}
			}

			public string ConfigHelpHtml
			{
				get
				{
					return "";
				}
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				// Nothing to do.
			}

			public void Shutdown()
			{
				// Nothing to do.
			}

			#endregion
		}
	}
}
