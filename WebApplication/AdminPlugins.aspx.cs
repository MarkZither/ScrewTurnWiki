
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Net;

namespace ScrewTurn.Wiki {

	public partial class AdminPlugins : BasePage {

		private string currentWiki = null;

		protected void Page_Load(object sender, EventArgs e) {
			currentWiki = DetectWiki();

			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageProviders(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames(currentWiki))) UrlTools.Redirect("AccessDenied.aspx");

			if(!Page.IsPostBack) {
				// Load providers and related data
				rptProviders.DataBind();
			}
		}

		#region Providers List

		protected void rdo_CheckedChanged(object sender, EventArgs e) {
			ResetEditor();
			rptProviders.DataBind();
		}

		/// <summary>
		/// Resets the editor.
		/// </summary>
		private void ResetEditor() {
			pnlProviderDetails.Visible = false;
			btnAutoUpdateProviders.Visible = true;
			txtCurrentProvider.Value = "";
			lblResult.CssClass = "";
			lblResult.Text = "";
		}

		protected void rptProviders_DataBinding(object sender, EventArgs e) {
			List<IProviderV30> providers = new List<IProviderV30>(5);

			int enabledCount = 0;

			if(rdoFormatter.Checked) {
				IFormatterProviderV30[] formatterProviders = Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(currentWiki);
				enabledCount = formatterProviders.Length;
				providers.AddRange(formatterProviders);
			}

			List<ProviderRow> result = new List<ProviderRow>(providers.Count);

			for(int i = 0; i < providers.Count; i++) {
				IProviderV30 prov = providers[i];
				result.Add(new ProviderRow(prov.Information,
					prov.GetType().FullName,
					GetUpdateStatus(prov.Information),
					i > enabledCount - 1,
					txtCurrentProvider.Value == prov.GetType().FullName));
			}

			rptProviders.DataSource = result;
		}

		/// <summary>
		/// Gets the update status of a provider.
		/// </summary>
		/// <param name="info">The component information.</param>
		/// <returns>The update status.</returns>
		private string GetUpdateStatus(ComponentInformation info) {
			if(!GlobalSettings.DisableAutomaticVersionCheck) {
				if(string.IsNullOrEmpty(info.UpdateUrl)) return "n/a";
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
					else throw new NotSupportedException();
				}
			}
			else return "n/a";
		}

		/// <summary>
		/// Gets the currently selected provider.
		/// </summary>
		/// <returns>The provider.</returns>
		/// <param name="enabled">A value indicating whether the returned provider is enabled.</param>
		/// <param name="canDisable">A value indicating whether the returned provider can be disabled.</param>
		private IProviderV30 GetCurrentProvider(out bool enabled, out bool canDisable) {
			enabled = true;
			canDisable = false;

			return Collectors.FindProvider(currentWiki, txtCurrentProvider.Value, out enabled, out canDisable);
		}

		protected void rptProviders_ItemCommand(object sender, CommandEventArgs e) {
			txtCurrentProvider.Value = e.CommandArgument as string;

			if(e.CommandName == "Select") {
				bool enabled;
				bool canDisable;
				IProviderV30 provider = GetCurrentProvider(out enabled, out canDisable);

				// Cannot disable the provider that handles the default page of the root namespace
				if(Pages.FindPage(currentWiki, Settings.GetDefaultPage(currentWiki)).Provider == provider) canDisable = false;

				pnlProviderDetails.Visible = true;
				lblProviderName.Text = provider.Information.Name + " (" + provider.Information.Version + ")";
				string dll = provider.GetType().Assembly.FullName;
				lblProviderDll.Text = dll.Substring(0, dll.IndexOf(",")) + ".dll";
				txtConfigurationString.Text = ProviderLoader.LoadPluginConfiguration(currentWiki, provider.GetType().FullName);
				if(provider.ConfigHelpHtml != null) {
					lblProviderConfigHelp.Text = provider.ConfigHelpHtml;
				}
				else {
					lblProviderConfigHelp.Text = Properties.Messages.NoConfigurationRequired;
				}

				btnEnable.Visible = !enabled;
				btnAutoUpdateProviders.Visible = false;
				btnDisable.Visible = enabled;
				btnDisable.Enabled = canDisable;
				lblCannotDisable.Visible = !canDisable;
				btnUnload.Enabled = !enabled;

				rptProviders.DataBind();
			}
		}

		/// <summary>
		/// Performs all the actions that are needed after a provider status is changed.
		/// </summary>
		private void PerformPostProviderChangeActions() {
			Content.InvalidateAllPages();
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Configuration change requested for Provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			string error;
			if(ProviderLoader.TryChangePluginConfiguration(txtCurrentProvider.Value, txtConfigurationString.Text, currentWiki, out error)) {
				PerformPostProviderChangeActions();

				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.ProviderConfigurationSaved;

				ResetEditor();
				rptProviders.DataBind();
			}
			else {
				lblResult.CssClass = "resulterror";
				lblResult.Text = Properties.Messages.ProviderRejectedConfiguration +
					(string.IsNullOrEmpty(error) ? "" : (": " + error));
			}
		}

		protected void btnDisable_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Deactivation requested for Provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			ProviderLoader.SavePluginStatus(currentWiki, txtCurrentProvider.Value, false);

			PerformPostProviderChangeActions();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ProviderDisabled;

			ResetEditor();
			rptProviders.DataBind();
		}

		protected void btnEnable_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Activation requested for provider provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);
			
			ProviderLoader.SavePluginStatus(currentWiki, txtCurrentProvider.Value, true);

			PerformPostProviderChangeActions();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ProviderEnabled;

			ResetEditor();
			rptProviders.DataBind();
		}

		protected void btnUnload_Click(object sender, EventArgs e) {
			bool enabled, canDisable;
			IProviderV30 prov = GetCurrentProvider(out enabled, out canDisable);
			Log.LogEntry("Unloading requested for provider provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			ProviderLoader.UnloadPlugin(txtCurrentProvider.Value);
			PerformPostProviderChangeActions();

			lblResult.CssClass = "resultok";
			lblResult.Text = Properties.Messages.ProviderUnloaded;

			ResetEditor();
			rptProviders.DataBind();
		}

		protected void btnCancel_Click(object sender, EventArgs e) {
			ResetEditor();
			rptProviders.DataBind();
		}

		protected void btnAutoUpdateProviders_Click(object sender, EventArgs e) {
			lblAutoUpdateResult.CssClass = "";
			lblAutoUpdateResult.Text = "";

			Log.LogEntry("Providers auto-update requested", EntryType.General, SessionFacade.CurrentUsername);

			ProviderUpdater updater = new ProviderUpdater(GlobalSettings.Provider,
				Collectors.FileNames,
				Collectors.CollectorsBox.PagesProviderCollector.GetAllProviders(currentWiki),
				Collectors.CollectorsBox.UsersProviderCollector.GetAllProviders(currentWiki),
				Collectors.CollectorsBox.FilesProviderCollector.GetAllProviders(currentWiki),
				Collectors.CollectorsBox.FormatterProviderCollector.GetAllProviders(currentWiki));

			int count = updater.UpdateAll();

			lblAutoUpdateResult.CssClass = "resultok";
			if(count > 0) lblAutoUpdateResult.Text = Properties.Messages.ProvidersUpdated;
			else lblAutoUpdateResult.Text = Properties.Messages.NoProvidersToUpdate;

			rptProviders.DataBind();
		}

		#endregion

	}

	/// <summary>
	/// Represents a provider for display purposes.
	/// </summary>
	public class ProviderRow {

		private string name, typeName, version, author, authorUrl, updateStatus, additionalClass;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProviderRow" /> class.
		/// </summary>
		/// <param name="info">The original component information.</param>
		/// <param name="typeName">The type name.</param>
		/// <param name="disabled">A value indicating whether the provider is disabled.</param>
		/// <param name="selected">A value indicating whether the provider is selected.</param>
		public ProviderRow(ComponentInformation info, string typeName, string updateStatus, bool disabled, bool selected) {
			name = info.Name;
			this.typeName = typeName;
			version = info.Version;
			author = info.Author;
			authorUrl = info.Url;
			this.updateStatus = updateStatus;
			additionalClass = disabled ? " disabled" : "";
			additionalClass += selected ? " selected" : "";
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the type name.
		/// </summary>
		public string TypeName {
			get { return typeName; }
		}

		/// <summary>
		/// Gets the version.
		/// </summary>
		public string Version {
			get { return version; }
		}

		/// <summary>
		/// Gets the author.
		/// </summary>
		public string Author {
			get { return author; }
		}

		/// <summary>
		/// Gets the author URL.
		/// </summary>
		public string AuthorUrl {
			get { return authorUrl; }
		}

		/// <summary>
		/// Gets the provider update status.
		/// </summary>
		public string UpdateStatus {
			get { return updateStatus; }
		}

		/// <summary>
		/// Gets the additional CSS class.
		/// </summary>
		public string AdditionalClass {
			get { return additionalClass; }
		}

	}

}
