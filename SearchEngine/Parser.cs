using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ScrewTurn.Wiki.SearchEngine {

	/// <summary>
	/// Summary description for Parser.
	/// </summary>
	public class Parser {
		[DllImport("ole32.dll", CharSet = CharSet.Unicode)]
		private static extern void CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease,
														 [Out] out System.Runtime.InteropServices.ComTypes.IStream pStream);

		[DllImport("query.dll", CharSet = CharSet.Unicode)]
		private static extern int LoadIFilter(string pwcsPath, [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter,
											  ref IFilter ppIUnk);

		[DllImport("query.dll", CharSet = CharSet.Unicode)]
		private static extern int BindIFilterFromStream(System.Runtime.InteropServices.ComTypes.IStream pStm,
														[MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter,
														[Out] out IFilter ppIUnk);


		private static IFilter loadIFilter(Stream stream) {
			object iunk = null;
			IFilter filter = null;

			// copy stream to byte array
			var b = new byte[stream.Length];
			stream.Read(b, 0, b.Length);
			// allocate space on the native heap
			IntPtr nativePtr = Marshal.AllocHGlobal(b.Length);
			// copy byte array to native heap
			Marshal.Copy(b, 0, nativePtr, b.Length);
			// Create a UCOMIStream from the allocated memory
			System.Runtime.InteropServices.ComTypes.IStream comStream;
			CreateStreamOnHGlobal(nativePtr, true, out comStream);

			// Try to load the corresponding IFilter 
			int resultLoad = BindIFilterFromStream(comStream, iunk, out filter);
			if(resultLoad == (int)IFilterReturnCodes.S_OK) {
				return filter;
			}
			else {
				Marshal.ThrowExceptionForHR(resultLoad);
			}
			return null;
		}

		private static IFilter loadIFilter(string filename) {
			object iunk = null;
			IFilter filter = null;

			// Try to load the corresponding IFilter 
			int resultLoad = LoadIFilter(filename, iunk, ref filter);
			if((resultLoad != (int)IFilterReturnCodes.S_OK) || (filter == null)) {
				throw new COMException("Could not load IFilter for file: " + filename, resultLoad);
			}
			return filter;
		}

		/*
				private static IFilter loadIFilterOffice(string filename)
				{
					IFilter filter = (IFilter)(new CFilter());
					System.Runtime.InteropServices.UCOMIPersistFile ipf = (System.Runtime.InteropServices.UCOMIPersistFile)(filter);
					ipf.Load(filename, 0);

					return filter;
				}
		*/

		public static bool IsParseable(string filename) {
			return loadIFilter(filename) != null;
		}

		public static string Parse(string filename) {
			IFilter filter = null;

			try {
				filter = loadIFilter(filename);

				return ExtractText(filter);
			}
			catch { throw; }
			finally {
				if(filter != null)
					Marshal.ReleaseComObject(filter);
			}
		}

		public static string Parse(Stream stream) {
			IFilter filter = null;

			try {
				filter = loadIFilter(stream);

				return ExtractText(filter);
			}
			finally {
				if(filter != null)
					Marshal.ReleaseComObject(filter);
			}
		}

		private static string ExtractText(IFilter filter) {
			var plainTextResult = new StringBuilder();
			var ps = new STAT_CHUNK();
			IFILTER_INIT mFlags = 0;

			uint i = 0;
			filter.Init(mFlags, 0, null, ref i);

			int resultChunk = 0;

			resultChunk = filter.GetChunk(out ps);
			while(resultChunk == 0) {
				if(ps.flags == CHUNKSTATE.CHUNK_TEXT) {
					uint sizeBuffer = 60000;
					var resultText = 0;
					while(resultText == Constants.FILTER_S_LAST_TEXT || resultText == 0) {
						sizeBuffer = 60000;
						var sbBuffer = new StringBuilder((int)sizeBuffer);
						resultText = filter.GetText(ref sizeBuffer, sbBuffer);

						if(sizeBuffer > 0 && sbBuffer.Length > 0) {
							string chunk = sbBuffer.ToString(0, (int)sizeBuffer);
							plainTextResult.Append(chunk);
						}
					}
				}
				resultChunk = filter.GetChunk(out ps);
			}
			return plainTextResult.ToString();
		}

		#region Nested type: IUnknown

		[ComImport, Guid("00000000-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IUnknown {
			[PreserveSig]
			IntPtr QueryInterface(ref Guid riid, out IntPtr pVoid);

			[PreserveSig]
			IntPtr AddRef();

			[PreserveSig]
			IntPtr Release();
		}

		#endregion
	}
}