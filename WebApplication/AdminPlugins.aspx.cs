
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScrewTurn.Wiki.PluginFramework;
using System.Net;
using System.Linq;
using System.IO;

namespace ScrewTurn.Wiki {

	public partial class AdminPlugins : BasePage {

		protected void Page_Load(object sender, EventArgs e) {

			AdminMaster.RedirectToLoginIfNeeded();

			if(!AdminMaster.CanManageProviders(SessionFacade.GetCurrentUsername(), SessionFacade.GetCurrentGroupNames()))
			{
				UrlTools.Redirect("AccessDenied.aspx");
			}

			if(!Page.IsPostBack) {
				lblResult.CssClass = "";
				lblResult.Text = "";
				// Load providers and related data
				rptProviders.DataBind();
			}
		}

		#region Providers List

		/// <summary>
		/// Resets the editor.
		/// </summary>
		private void ResetEditor() {
			pnlProviderDetails.Visible = false;
			txtCurrentProvider.Value = "";
		}

		protected void rptProviders_DataBinding(object sender, EventArgs e) {
			IFormatterProviderV30[] plugins = Collectors.FormatterProviderCollector.AllProviders;

			List<ProviderRow> result = new List<ProviderRow>(plugins.Length);

			for(int i = 0; i < plugins.Length; i++) {
				result.Add(new ProviderRow(plugins[i].Information,
					plugins[i].GetType().FullName,
					GetUpdateStatus(plugins[i].Information),
					!Settings.Provider.GetPluginStatus(plugins[i].GetType().FullName),
					txtCurrentProvider.Value == plugins[i].GetType().FullName));
			}

			rptProviders.DataSource = result;
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

		/// <summary>
		/// Gets the currently selected provider.
		/// </summary>
		/// <returns>The provider.</returns>
		/// <param name="enabled">A value indicating whether the returned provider is enabled.</param>
		/// <param name="canDisable">A value indicating whether the returned provider can be disabled.</param>
		private IProviderV30 GetCurrentProvider(out bool enabled) {
			enabled = true;
			bool canDisable;
			return Collectors.FindProvider(txtCurrentProvider.Value, out enabled, out canDisable);
		}

		protected void rptProviders_ItemCommand(object sender, CommandEventArgs e) {
			txtCurrentProvider.Value = e.CommandArgument as string;

			if(e.CommandName == "Select") {
				bool enabled;
				IProviderV30 provider = GetCurrentProvider(out enabled);

				pnlProviderDetails.Visible = true;
				lblProviderName.Text = provider.Information.Name + " (" + provider.Information.Version + ")";
				string dll = provider.GetType().Assembly.FullName;
				lblProviderDll.Text = dll.Substring(0, dll.IndexOf(",")) + ".dll";
				txtConfigurationString.Text = ProviderLoader.LoadConfiguration(provider.GetType().FullName);
				if(provider.ConfigHelpHtml != null) {
					lblProviderConfigHelp.Text = provider.ConfigHelpHtml;
				}
				else {
					lblProviderConfigHelp.Text = Properties.Messages.NoConfigurationRequired;
				}

				lblResult.CssClass = "";
				lblResult.Text = "";
				rptProviders.DataBind();
			}
			else if(e.CommandName == "Disable") {
				bool enabled;
				IProviderV30 prov = GetCurrentProvider(out enabled);
				Log.LogEntry("Deactivation requested for Provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

				ProviderLoader.SaveStatus(txtCurrentProvider.Value, false);

				lblResult.CssClass = "resultok";
				lblResult.Text = Properties.Messages.ProviderDisabled;

				ResetEditor();
				rptProviders.DataBind();
			}
			else if(e.CommandName == "Enable") {
				bool enabled;
				IProviderV30 prov = GetCurrentProvider(out enabled);
				Log.LogEntry("Activation requested for provider provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

				try {
					//ProviderLoader.SetUp<IFormatterProviderV30>(prov.GetType(), Settings.Provider.GetPluginConfiguration(prov.GetType().FullName));
					ProviderLoader.SaveStatus(txtCurrentProvider.Value, true);
					lblResult.CssClass = "resultok";
					lblResult.Text = Properties.Messages.ProviderEnabled;
				}
				catch(InvalidConfigurationException) {
					ProviderLoader.SaveStatus(txtCurrentProvider.Value, false);
					lblResult.CssClass = "resulterror";
					lblResult.Text = Properties.Messages.ProviderRejectedConfiguration;
				}

				ResetEditor();
				rptProviders.DataBind();
			}
		}

		protected void btnSave_Click(object sender, EventArgs e) {
			bool enabled;
			IProviderV30 prov = GetCurrentProvider(out enabled);
			Log.LogEntry("Configuration change requested for Provider " + prov.Information.Name, EntryType.General, SessionFacade.CurrentUsername);

			string error;
			if(ProviderLoader.TryChangeConfiguration(prov.GetType().Name, txtConfigurationString.Text, out error)) {
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
		
		protected void btnCancel_Click(object sender, EventArgs e) {
			ResetEditor();
			rptProviders.DataBind();
		}

		#endregion

		private string GetMimeType(string ext) {
			string mime = "";
			if(MimeTypes.Types.TryGetValue(ext, out mime))
			{
				return mime;
			}
			else
			{
				return "application/octet-stream";
			}
		}

	}

	/// <summary>
	/// Represents a provider for display purposes.
	/// </summary>
	public class ProviderRow4 {

		private string name, typeName, version, author, authorUrl, updateStatus, additionalClass;
		private bool disabled;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProviderRow" /> class.
		/// </summary>
		/// <param name="info">The original component information.</param>
		/// <param name="typeName">The type name.</param>
		/// <param name="disabled">A value indicating whether the provider is disabled.</param>
		/// <param name="selected">A value indicating whether the provider is selected.</param>
		public ProviderRow4(ComponentInformation info, string typeName, string updateStatus, bool disabled, bool selected) {
			name = info.Name;
			this.typeName = typeName;
			version = info.Version;
			author = info.Author;
			authorUrl = info.Url;
			this.updateStatus = updateStatus;
			additionalClass = disabled ? " disabled" : "";
			additionalClass += selected ? " selected" : "";
			this.disabled = disabled;
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

		/// <summary>
		/// <c>true</c> if the plugin is enabled, <c>false</c> otherwise.
		/// </summary>
		public bool Enabled {
			get { return !disabled; }
		}

		/// <summary>
		/// <c>true</c> if the plugin is disabled, <c>false</c> otherwise.
		/// </summary>
		public bool Disabled {
			get { return disabled; }
		}
	}

}
