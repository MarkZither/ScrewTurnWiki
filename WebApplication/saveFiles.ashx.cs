using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki
{
	/// <summary>
	/// Summary description for saveFiles
	/// </summary>
	public class saveFiles : IHttpHandler
	{

		public void ProcessRequest(HttpContext context)
		{
			string targetFolder = HttpContext.Current.Server.MapPath("uploadfiles");
			if(!Directory.Exists(targetFolder))
			{
				Directory.CreateDirectory(targetFolder);
			}
			HttpRequest request = context.Request;
			HttpFileCollection uploadedFiles = context.Request.Files;
			if(uploadedFiles != null && uploadedFiles.Count > 0)
			{
				for(int i = 0; i < uploadedFiles.Count; i++)
				{
					string fileName = uploadedFiles[i].FileName;
					int indx = fileName.LastIndexOf("\\");
					if(indx > -1)
					{
						fileName = fileName.Substring(indx + 1);
					}
					uploadedFiles[i].SaveAs(targetFolder + "\\" + fileName);

					//string file = upDll.FileName;

					string ext = System.IO.Path.GetExtension(fileName);
					if(ext != null)
					{
						ext = ext.ToLowerInvariant();
					}

					if(ext != ".dll")
					{
						//lblUploadResult.CssClass = "resulterror";
						//lblUploadResult.Text = Properties.Messages.VoidOrInvalidFile;
						return;
					}

					Log.LogEntry("Provider DLL upload requested " + fileName, EntryType.General, SessionFacade.CurrentUsername);

					string[] asms = Settings.Provider.ListPluginAssemblies();
					if(Array.Find<string>(asms, delegate (string v)
					{
						if(v.Equals(fileName))
						{
							return true;
						}
						else
						{
							return false;
						}
					}) != null)
					{
						// DLL already exists
						//lblUploadResult.CssClass = "resulterror";
						//lblUploadResult.Text = Properties.Messages.DllAlreadyExists;
						return;
					}
					else
					{
						BinaryReader b = new BinaryReader(uploadedFiles[i].InputStream);
						byte[] binData = b.ReadBytes(uploadedFiles[i].ContentLength);
						Stream stream = uploadedFiles[i].InputStream;
						stream.Position = 0;
						byte[] fileData = null;
						using(var binaryReader = new BinaryReader(uploadedFiles[i].InputStream))
						{
							fileData = binaryReader.ReadBytes(uploadedFiles[i].ContentLength);
							Settings.Provider.StorePluginAssembly(fileName, fileData);
						}
						

						int count = ProviderLoader.LoadFromAuto(fileName);

						//lblUploadResult.CssClass = "resultok";
						//lblUploadResult.Text = Properties.Messages.LoadedProviders.Replace("###", count.ToString());
						//upDll.Attributes.Add("value", "");

						//PerformPostProviderChangeActions();
						Content.InvalidateAllPages();
						Content.ClearPseudoCache();

						//LoadDlls(); //populate the dropdownlist
						//ResetEditor(); // reset the upload control
						//rptProviders.DataBind(); 
						//LoadSourceProviders();
					}
				}
			}
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}
	}
}