
using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ScrewTurn.Wiki.PluginFramework;

namespace ScrewTurn.Wiki {

	public partial class FileManager : System.Web.UI.UserControl {

		private IFilesStorageProviderV40 provider = null;
		private string currentWiki = null;

		bool canList = false;
		bool canDownload = false;
		bool canUpload = false;
		bool canCreateDirs = false;
		bool canDeleteFiles = false;
		bool canDeleteDirs = false;
		bool canSetPerms = false;
		bool isAdmin = false;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = Tools.DetectCurrentWiki();

			if(!Page.IsPostBack) {
				permissionsManager.CurrentResourceName = "/";

				// Localized strings for JavaScript
				StringBuilder sb = new StringBuilder();
				sb.Append(@"<script type=""text/javascript"">" + "\n<!--\n");
				sb.Append("var ConfirmMessage = '");
				sb.Append(Properties.Messages.ConfirmOperation);
				sb.Append("';\r\n");
				sb.AppendFormat("var CurrentNamespace = \"{0}\";\r\n", Tools.DetectCurrentNamespace());
				sb.Append("// -->\n</script>\n");
				lblStrings.Text = sb.ToString();

				// Setup upload information (max file size, allowed file types)
				lblUploadFilesInfo.Text = lblUploadFilesInfo.Text.Replace("$1", Tools.BytesToString(GlobalSettings.MaxFileSize * 1024));
				sb = new StringBuilder();
				string[] aft = Settings.GetAllowedFileTypes(currentWiki);
				for(int i = 0; i < aft.Length; i++) {
					sb.Append(aft[i].ToUpper());
					if(i != aft.Length - 1) sb.Append(", ");
				}
				lblUploadFilesInfo.Text = lblUploadFilesInfo.Text.Replace("$2", sb.ToString());

				LoadProviders();

				permissionsManager.CurrentFilesProvider = lstProviders.SelectedValue;

				// See if a dir is specified in query string
				if(Request["Dir"] != null) {
					string currDir = Request["Dir"];
					if(!currDir.StartsWith("/")) currDir = "/" + currDir;
					if(!currDir.EndsWith("/")) currDir += "/";
					CurrentDirectory = currDir;
				}
			}

			// Set provider
			provider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstProviders.SelectedValue, currentWiki);

			// The following actions are verified ***FOR THE CURRENT DIRECTORY***:
			// - List contents
			// - Download files
			// - Upload files
			// - Create directories
			// - Delete/Rename files -> hide/show buttons in repeater
			// - Delete/Rename directories --> hide/show buttons in repeater
			// - Manage Permissions -> avoid setting permissionsManager.CurrentResourceName/CurrentFilesProvider if not authorized
			// - Member of Administrators -> hide/show provider selection
			// ---> recheck everywhere an action is performed

			DetectPermissions();

			if(!Page.IsPostBack) {
				rptItems.DataBind();
			}

			PopulateBreadcrumb();

			SetupControlsForPermissions();
		}

		/// <summary>
		/// Loads the providers.
		/// </summary>
		private void LoadProviders() {
			lstProviders.Items.Clear();
			foreach(IFilesStorageProviderV40 prov in Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki)) {
				ListItem item = new ListItem(prov.Information.Name, prov.GetType().FullName);
				if(item.Value == GlobalSettings.DefaultFilesProvider) {
					item.Selected = true;
				}
				lstProviders.Items.Add(item);
			}
		}

		/// <summary>
		/// Detects the permissions of the current user for the current directory.
		/// </summary>
		private void DetectPermissions() {
			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames(currentWiki);

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			canList = authChecker.CheckActionForDirectory(provider, CurrentDirectory, Actions.ForDirectories.List, currentUser, currentGroups);
			canDownload = authChecker.CheckActionForDirectory(provider, CurrentDirectory, Actions.ForDirectories.DownloadFiles, currentUser, currentGroups);
			canUpload = authChecker.CheckActionForDirectory(provider, CurrentDirectory, Actions.ForDirectories.UploadFiles, currentUser, currentGroups);
			canCreateDirs = authChecker.CheckActionForDirectory(provider, CurrentDirectory, Actions.ForDirectories.CreateDirectories, currentUser, currentGroups);
			canDeleteFiles = authChecker.CheckActionForDirectory(provider, CurrentDirectory, Actions.ForDirectories.DeleteFiles, currentUser, currentGroups);
			canDeleteDirs = authChecker.CheckActionForDirectory(provider, CurrentDirectory, Actions.ForDirectories.DeleteDirectories, currentUser, currentGroups);
			canSetPerms = authChecker.CheckActionForGlobals(Actions.ForGlobals.ManagePermissions, currentUser, currentGroups);
			isAdmin = Array.Find(currentGroups, delegate(string g) { return g == Settings.GetAdministratorsGroup(currentWiki); }) != null;
		}

		/// <summary>
		/// Returns to the root.
		/// </summary>
		public void GoToRoot() {
			CurrentDirectory = "/";

			DetectPermissions();
			SetupControlsForPermissions();

			rptItems.DataBind();
			PopulateBreadcrumb();
		}

		protected void rptItems_DataBinding(object sender, EventArgs e) {
			permissionsManager.CurrentResourceName = CurrentDirectory;
			permissionsManager.CurrentFilesProvider = lstProviders.SelectedValue;

			// Build a DataTable containing the proper information
			DataTable table = new DataTable("Items");

			table.Columns.Add("Type");
			table.Columns.Add("Name");
			table.Columns.Add("Size");
			table.Columns.Add("WikiMarkupLink");
			table.Columns.Add("Link");
			table.Columns.Add("Editable", typeof(bool));
			table.Columns.Add("FullPath");
			table.Columns.Add("CanDelete", typeof(bool));
			table.Columns.Add("CanDownload", typeof(bool));

			if(!canList) {
				lblNoList.Visible = true;
				rptItems.DataSource = table; // This is empty
				return;
			}
			lblNoList.Visible = false;

			string currDir = CurrentDirectory;

			string[] dirs = provider.ListDirectories(currDir);

			string currentUser = SessionFacade.GetCurrentUsername();
			string[] currentGroups = SessionFacade.GetCurrentGroupNames(currentWiki);

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			foreach(string s in dirs) {
				bool canListThisSubDir = authChecker.CheckActionForDirectory(provider, s, Actions.ForDirectories.List, currentUser, currentGroups);

				DataRow row = table.NewRow();
				row["Type"] = "D";
				row["Name"] = GetItemName(s)/* + "/"*/;
				row["Size"] = "(" + ((int)(provider.ListFiles(s).Length + provider.ListDirectories(s).Length)).ToString() + ")";
				row["WikiMarkupLink"] = "&nbsp;";
				row["Link"] = "";
				row["Editable"] = false;
				row["FullPath"] = s;
				row["CanDelete"] = canDeleteDirs;
				row["CanDownload"] = canListThisSubDir;
				table.Rows.Add(row);
			}

			string[] files = provider.ListFiles(currDir);
			foreach(string s in files) {
				FileDetails details = provider.GetFileDetails(s);

				DataRow row = table.NewRow();
				string ext = Path.GetExtension(s).ToLowerInvariant();
				row["Type"] = "F";
				row["Name"] = GetItemName(s);
				row["Size"] = Tools.BytesToString(details.Size);
				row["WikiMarkupLink"] = "{UP}" + s;
				if(canDownload) {
					row["Link"] = "GetFile.aspx?File=" + Tools.UrlEncode(s).Replace("'", "&#39;") + "&amp;AsStreamAttachment=1&amp;Provider=" +
						provider.GetType().FullName + "&amp;NoHit=1";
				}
				else {
					row["Link"] = "";
				}
				row["Editable"] = canUpload && canDeleteFiles && (ext == ".jpg" || ext == ".jpeg" || ext == ".png");
				row["FullPath"] = s;
				row["CanDelete"] = canDeleteFiles;
				row["CanDownload"] = canDownload;
				table.Rows.Add(row);
			}

			rptItems.DataSource = table;
		}

		protected void rptItems_ItemCommand(object sender, RepeaterCommandEventArgs e) {
			string item = (string)e.CommandArgument;

			switch(e.CommandName) {
				case "Dir":
					EnterDirectory(GetItemName(item));
					break;
				case "Rename":
					// Hide all directory-specific controls
					// Permissions are verified in btnRename_Click
					pnlRename.Visible = true;
					pnlNewDirectory.Visible = false;
					pnlUpload.Visible = false;
					pnlPermissions.Visible = false;
					lstProviders.Visible = false;
					lblItem.Text = GetItemName(item) + (item.EndsWith("/") ? "/" : "");
					txtNewName.Text = GetItemName(item);
					rptItems.Visible = false;
					break;
				case "Delete":
					if(item.EndsWith("/")) {
						if(canDeleteDirs) FilesAndAttachments.DeleteDirectory(provider, item);
					}
					else {
						if(canDeleteFiles) FilesAndAttachments.DeleteFile(provider, item);
					}
					rptItems.DataBind();
					break;
			}
		}

		/// <summary>
		/// Tries to enter a directory.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <param name="directory">The full path of the directory.</param>
		public void TryEnterDirectory(string provider, string directory) {
			if(string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(provider)) return;

			if(!directory.StartsWith("/")) directory = "/" + directory;
			if(!directory.EndsWith("/")) directory += "/";
			directory = directory.Replace("//", "/");

			LoadProviders();

			IFilesStorageProviderV40 realProvider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(provider, currentWiki);
			if(realProvider == null) return;
			this.provider = realProvider;

			// Detect existence
			try {
				realProvider.ListDirectories(directory);
			}
			catch(ArgumentException) {
				return;
			}

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			bool canListThisSubDir = authChecker.CheckActionForDirectory(realProvider, directory, Actions.ForDirectories.List,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki));
			if(!canListThisSubDir) {
				return;
			}

			lstProviders.SelectedIndex = -1;
			foreach(ListItem item in lstProviders.Items) {
				if(item.Value == provider) {
					item.Selected = true;
					break;
				}
			}
			//lstProviders_SelectedIndexChanged(this, null);

			string parent = "/";
			string trimmedDirectory = directory.TrimEnd('/');
			if(trimmedDirectory.Length > 0) {
				int lastSlash = trimmedDirectory.LastIndexOf("/");
				if(lastSlash != -1) {
					parent = "/" + trimmedDirectory.Substring(0, lastSlash) + "/";
				}
			}

			if(parent != directory) {
				CurrentDirectory = parent;
				EnterDirectory(Tools.ExtractDirectoryName(directory));
			}
		}

		/// <summary>
		/// Enters a sub-directory of the current directory.
		/// </summary>
		/// <param name="name">The name of the current directory.</param>
		private void EnterDirectory(string name) {
			string newDirectory = CurrentDirectory + name + "/";

			AuthChecker authChecker = new AuthChecker(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));

			bool canListThisSubDir = authChecker.CheckActionForDirectory(provider, newDirectory, Actions.ForDirectories.List,
				SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki));
			if(!canListThisSubDir) {
				return;
			}

			CurrentDirectory += name + "/";

			DetectPermissions();
			SetupControlsForPermissions();

			rptItems.DataBind();
			PopulateBreadcrumb();
		}

		/// <summary>
		/// Gets the name of an item.
		/// </summary>
		/// <param name="path">The full path of the item (file or directory).</param>
		/// <returns>The name of the item.</returns>
		private string GetItemName(string path) {
			path = path.Trim('/');
			if(path.Contains("/")) {
				path = path.Substring(path.LastIndexOf("/") + 1);
			}
			return path;
		}

		/// <summary>
		/// Gets or sets the current directory.
		/// </summary>
		private string CurrentDirectory {
			get {
				if(ViewState["CurrDir"] != null) return ((string)ViewState["CurrDir"]).Replace("//", "/");
				else return "/";
			}
			set { ViewState["CurrDir"] = value.Replace("//", "/"); }
		}

		protected void lnkRoot_Click(object sender, EventArgs e) {
			GoToRoot();
		}

		private void linkButton_Click(object sender, EventArgs e) {
			System.Web.UI.WebControls.LinkButton lnk = sender as System.Web.UI.WebControls.LinkButton;
			if(lnk != null) {
				CurrentDirectory = lnk.CommandArgument;

				DetectPermissions();
				SetupControlsForPermissions();

				rptItems.DataBind();
				PopulateBreadcrumb();
			}
		}

		/// <summary>
		/// Populates the directory breadcrumb placeholder. See also methods linkButton_Click e lnkRoot_Click.
		/// </summary>
		private void PopulateBreadcrumb() {
			plhDirectory.Controls.Clear();
			// Get all directories by splitting the content of txtCurrentDirectory
			string[] dirs = CurrentDirectory.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

			// Add a LinkButton and a "/" label for each directory
			string current = "/";
			for(int i = 0; i < dirs.Length; i++) {
				System.Web.UI.WebControls.LinkButton lnk = new System.Web.UI.WebControls.LinkButton();
				lnk.ID = "lnkDir" + i.ToString();
				lnk.Text = dirs[i];
				current += dirs[i] + "/";
				lnk.CommandArgument = current;
				lnk.ToolTip = current;
				lnk.Click += new EventHandler(linkButton_Click);
				plhDirectory.Controls.Add(lnk);

				System.Web.UI.WebControls.Label lbl = new System.Web.UI.WebControls.Label();
				lbl.ID = "lblDir" + i.ToString();
				lbl.Text = " / ";
				plhDirectory.Controls.Add(lbl);
			}
		}

		/// <summary>
		/// Sets the controls depending on the current permissions.
		/// </summary>
		private void SetupControlsForPermissions() {
			// Setup buttons and controls visibility
			chkOverwrite.Enabled = canDeleteFiles;
			lstProviders.Visible = isAdmin;

			pnlUpload.Visible = canUpload;
			pnlNewDirectory.Visible = canCreateDirs;
			pnlPermissions.Visible = canSetPerms;
			permissionsManager.Visible = canSetPerms;
			if(!canSetPerms) {
				permissionsManager.CurrentResourceName = "";
				permissionsManager.CurrentFilesProvider = "";
			}
		}

		protected void btnRename_Click(object sender, EventArgs e) {
			lblRenameResult.Text = "";
			bool done = false;

			txtNewName.Text = txtNewName.Text.Trim();

			if(lblItem.Text.EndsWith("/")) {
				if(canDeleteDirs) done = FilesAndAttachments.RenameDirectory(provider, CurrentDirectory + lblItem.Text, txtNewName.Text);
			}
			else {
				if(canDeleteFiles) {
					// Ensure that the extension is not changed (security)
					string previousExtension = Path.GetExtension(lblItem.Text);
					string newExtension = Path.GetExtension(txtNewName.Text);
					if(string.IsNullOrEmpty(newExtension)) {
						newExtension = previousExtension;
						txtNewName.Text += previousExtension;
					}

					if(newExtension.ToLowerInvariant() != previousExtension.ToLowerInvariant()) {
						txtNewName.Text += previousExtension;
					}

					txtNewName.Text = txtNewName.Text.Trim();

					done = FilesAndAttachments.RenameFile(provider, CurrentDirectory + lblItem.Text, txtNewName.Text);
				}
			}
			if(done) {
				pnlRename.Visible = false;
				rptItems.Visible = true;
				rptItems.DataBind();
				SetupControlsForPermissions();
			}
			else {
				lblRenameResult.Text = Properties.Messages.CannotRenameItem;
				lblRenameResult.CssClass = "resulterror";
			}
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			pnlRename.Visible = false;
			rptItems.Visible = true;
			SetupControlsForPermissions();
			lblRenameResult.Text = "";
		}

		protected void btnNewDirectory_Click(object sender, EventArgs e) {
			if(canCreateDirs) {
				txtNewDirectoryName.Text = txtNewDirectoryName.Text.Trim();

				lblNewDirectoryResult.Text = "";
				txtNewDirectoryName.Text = txtNewDirectoryName.Text.Trim('/');
				AuthWriter authWriter = new AuthWriter(Collectors.CollectorsBox.GetSettingsProvider(currentWiki));
				authWriter.ClearEntriesForDirectory(provider, CurrentDirectory + txtNewDirectoryName.Text + "/");
				
				bool done = FilesAndAttachments.CreateDirectory(provider, CurrentDirectory, txtNewDirectoryName.Text);
				if(!done) {
					lblNewDirectoryResult.CssClass = "resulterror";
					lblNewDirectoryResult.Text = Properties.Messages.CannotCreateNewDirectory;
				}
				else {
					txtNewDirectoryName.Text = "";
				}
				rptItems.DataBind();
			}
		}

		protected void btnUpload_Click(object sender, EventArgs e) {
			if(canUpload && (chkOverwrite.Checked && canDeleteFiles || !chkOverwrite.Checked)) {
				lblUploadResult.Text = "";
				if(fileUpload.HasFile) {
					if(fileUpload.FileBytes.Length > GlobalSettings.MaxFileSize * 1024) {
						lblUploadResult.Text = Properties.Messages.FileTooBig;
						lblUploadResult.CssClass = "resulterror";
					}
					else {
						// Check file extension
						string[] aft = Settings.GetAllowedFileTypes(currentWiki);
						bool allowed = false;

						if(aft.Length > 0 && aft[0] == "*") allowed = true;
						else {
							string ext = Path.GetExtension(fileUpload.FileName);
							if(ext == null) ext = "";
							if(ext.StartsWith(".")) ext = ext.Substring(1).ToLowerInvariant();
							foreach(string ft in aft) {
								if(ft == ext) {
									allowed = true;
									break;
								}
							}
						}

						if(!allowed) {
							lblUploadResult.Text = Properties.Messages.InvalidFileType;
							lblUploadResult.CssClass = "resulterror";
						}
						else {
							bool done = FilesAndAttachments.StoreFile(provider, CurrentDirectory + fileUpload.FileName, fileUpload.FileContent, chkOverwrite.Checked);

							if(!done) {
								lblUploadResult.Text = Properties.Messages.CannotStoreFile;
								lblUploadResult.CssClass = "resulterror";
							}
							rptItems.DataBind();
						}
					}
				}
				else {
					lblUploadResult.Text = Properties.Messages.FileVoid;
					lblUploadResult.CssClass = "resulterror";
				}
			}
		}

		protected void lstProviders_SelectedIndexChanged(object sender, EventArgs e) {
			provider = Collectors.CollectorsBox.FilesProviderCollector.GetProvider(lstProviders.SelectedValue, currentWiki);
			GoToRoot();
		}

	}

}
