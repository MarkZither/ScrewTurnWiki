﻿
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Security.Cryptography;

namespace ScrewTurn.Wiki {

	public partial class AdminProvidersManagement : BasePage {

		protected void Page_Load(object sender, EventArgs e) {

			AdminMaster.RedirectToLoginIfNeeded();

			/*if(!AdminMaster.CanManageGlobalConfiguration(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames()))
			{
				UrlTools.Redirect("AccessDenied.aspx");
			}*/

			if(!Page.IsPostBack) {
				LoadDlls();

				// Load providers and related data
				rptProviders.DataBind();
			}
		}

		protected void rptProviders_DataBinding(object sender, EventArgs e) {
			/*IFormatterProviderV30[] plugins = Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(currentWiki);

			btnAutoUpdateProviders.Visible = true;
			btnAutoUpdateProviders.Enabled = plugins.Length > 0;

			List<ProviderRow> result = new List<ProviderRow>(plugins.Length);

			for(int i = 0; i < plugins.Length; i++) {
				result.Add(new ProviderRow(plugins[i].Information,
					plugins[i].GetType().FullName,
					GetUpdateStatus(plugins[i].Information),
					false,
					false));
			}

			rptProviders.DataSource = result;*/
		}

		/// <summary>
		/// Gets the update status of a provider.
		/// </summary>
		/// <param name="info">The component information.</param>
		/// <returns>The update status.</returns>
		private string GetUpdateStatus(ComponentInformation info) {
			if(!Settings.DisableAutomaticVersionCheck) {
				if(string.IsNullOrEmpty(info.UpdateUrl))
				{
					return "n/a";
				}
				else {
					string newVersion = null;
					string newAssemblyUrl = null;
					UpdateStatus status = Tools.GetUpdateStatus(info.UpdateUrl, info.Version, out newVersion, out newAssemblyUrl);

					if(status == UpdateStatus.Error) {
						return "<span class=\"resulterror\">" + Properties.Messages.Error + "</span>";
					}
					else if(status == UpdateStatus.NewVersionFound) {
						return "<span class=\"resulterror\">" + Properties.Messages.NewVersion + " <b>" + newVersion + "</b>" +
							(string.IsNullOrEmpty(newAssemblyUrl) ? "" : " (" + Properties.Messages.AutoUpdateAvailable + ")") + "</span>";
					}
					else if(status == UpdateStatus.UpToDate) {
						return "<span class=\"resultok\">" + Properties.Messages.UpToDate + "</span>";
					}
					else
					{
						throw new NotSupportedException();
					}
				}
			}
			else
			{
				return "n/a";
			}
		}

		protected void btnAutoUpdateProviders_Click(object sender, EventArgs e) {
			lblAutoUpdateResult.CssClass = "";
			lblAutoUpdateResult.Text = "";

			Log.LogEntry("Providers auto-update requested", EntryType.General, SessionFacade.CurrentUsername);

			ProviderUpdater updater = new ProviderUpdater(Settings.Provider, Collectors.FileNames,
				Collectors.FormatterProviderCollector.AllProviders);

			int count = updater.UpdateAll();

			lblAutoUpdateResult.CssClass = "resultok";
			if(count > 0)
			{
				lblAutoUpdateResult.Text = Properties.Messages.ProvidersUpdated;
			}
			else
			{
				lblAutoUpdateResult.Text = Properties.Messages.NoProvidersToUpdate;
			}

			rptProviders.DataBind();
		}

		/// <summary>
		/// Loads all the providers' DLLs.
		/// </summary>
		private void LoadDlls() {
			string[] files = Settings.Provider.ListPluginAssemblies();
			lstDlls.Items.Clear();
			lstDlls.Items.Add(new ListItem(Properties.Messages.SelectAndDelete, ""));
			for(int i = 0; i < files.Length; i++) {
				lstDlls.Items.Add(new ListItem(files[i], files[i]));
			}
		}

		protected void lstDlls_SelectedIndexChanged(object sender, EventArgs e) {
			btnDeleteDll.Enabled = lstDlls.SelectedIndex >= 0 && !string.IsNullOrEmpty(lstDlls.SelectedValue);
		}

		protected void btnDeleteDll_Click(object sender, EventArgs e) {
			if(Settings.Provider.DeletePluginAssembly(lstDlls.SelectedValue)) {
				LoadDlls();
				lstDlls_SelectedIndexChanged(sender, e);
				lblDllResult.CssClass = "resultok";
				lblDllResult.Text = Properties.Messages.DllDeleted;
			}
			else {
				lblDllResult.CssClass = "resulterror";
				lblDllResult.Text = Properties.Messages.CouldNotDeleteDll;
			}
		}

		protected void btnUpload_Click(object sender, EventArgs e) {
			string file = upDll.FileName;

			string ext = System.IO.Path.GetExtension(file);
			if(ext != null)
			{
				ext = ext.ToLowerInvariant();
			}

			if(ext != ".dll") {
				lblUploadResult.CssClass = "resulterror";
				lblUploadResult.Text = Properties.Messages.VoidOrInvalidFile;
				return;
			}

			Log.LogEntry("Provider DLL upload requested " + upDll.FileName, EntryType.General, SessionFacade.CurrentUsername);

			string[] asms = Settings.Provider.ListPluginAssemblies();
			if(Array.Find<string>(asms, delegate(string v) {
				if(v.Equals(file))
				{
					return true;
				}
				else
				{
					return false;
				}
			}) != null) {
				// DLL already exists
				lblUploadResult.CssClass = "resulterror";
				lblUploadResult.Text = Properties.Messages.DllAlreadyExists;
				return;
			}
			else {
				Settings.Provider.StorePluginAssembly(file, upDll.FileBytes);
				throw new NotImplementedException();
				//int count = ProviderLoader.LoadFormatterProvidersFromAuto(file);

				//lblUploadResult.CssClass = "resultok";
				//lblUploadResult.Text = Properties.Messages.LoadedProviders.Replace("###", count.ToString());
				//upDll.Attributes.Add("value", "");

				//LoadDlls();
				//rptProviders.DataBind();
			}
		}

		/// <summary>
		/// Detects whether a users storage provider fully supports writing to all managed data.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// <returns><c>true</c> if the provider fully supports writing all managed data, <c>false</c> otherwise.</returns>
		private static bool IsUsersProviderFullWriteEnabled(IUsersStorageProviderV30 provider) {
			return
				!provider.UserAccountsReadOnly &&
				!provider.UserGroupsReadOnly &&
				!provider.GroupMembershipReadOnly &&
				!provider.UsersDataReadOnly;
		}

	}

}
