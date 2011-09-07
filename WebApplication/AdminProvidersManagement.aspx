<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminProvidersManagement.aspx.cs" Inherits="ScrewTurn.Wiki.AdminProvidersManagement" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnConfig" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<h2 class="sectiontitle"><asp:Literal ID="lblPluginsManagementTitle" runat="server" Text="Providers Management" EnableViewState="False" meta:resourcekey="lblPluginsManagementTitleResource1" /></h2>
	
		<asp:Literal ID="lblPlugins" runat="server" Text="Plugins (Formatter Providers)" EnableViewState="False" meta:resourcekey="lblPluginsResource1" />:
		<br />
		
		<div id="ProvidersListContainerDiv">
			<asp:Repeater ID="rptProviders" runat="server" OnDataBinding="rptProviders_DataBinding" >
				<HeaderTemplate>
					<table cellpadding="0" cellspacing="0" class="generic">
						<thead>
						<tr class="tableheader">
							<th><asp:Literal ID="lblName" runat="server" EnableViewState="False" meta:resourcekey="lblNameResource1" Text="Name" /></th>
							<th><asp:Literal ID="lblVersion" runat="server" EnableViewState="False" meta:resourcekey="lblVersionResource1" Text="Ver." /></th>
							<th><asp:Literal ID="lblAuthor" runat="server" EnableViewState="False" meta:resourcekey="lblAuthorResource1" Text="Author" /></th>
							<th><asp:Literal ID="lblUpdateStatus" runat="server" EnableViewState="false" meta:resourcekey="lblUpdateStatusResource1" Text="Update Status" /></th>
						</tr>
						</thead>
						<tbody>
				</HeaderTemplate>
				<ItemTemplate>
					<tr class='tablerow<%# Eval("AdditionalClass") %>'>
						<td><%# Eval("Name") %></td>
						<td><%# Eval("Version") %></td>
						<td><a href='<%# Eval("AuthorUrl") %>' target="_blank"><%# Eval("Author") %></a></td>
						<td><%# Eval("UpdateStatus") %></td>
					</tr>
				</ItemTemplate>
				<AlternatingItemTemplate>
					<tr class='tablerowalternate<%# Eval("AdditionalClass") %>'>
						<td><%# Eval("Name") %></td>
						<td><%# Eval("Version") %></td>
						<td><a href='<%# Eval("AuthorUrl") %>' target="_blank"><%# Eval("Author") %></a></td>
						<td><%# Eval("UpdateStatus") %></td>
					</tr>
				</AlternatingItemTemplate>
				<FooterTemplate>
					</tbody>
					</table>
				</FooterTemplate>
			</asp:Repeater>
		</div>

		<div id="ProvidersUpdateDiv">
		
			<script type="text/javascript">
			<!--
				function __ShowUpdateProgress() {
					if(RequestConfirm()) {
						document.getElementById("ProvidersUpdateProgress").style["display"] = "";
						return true;
					}
					else return false;
				}

				function __HideUpdateProgress() {
					document.getElementById("ProvidersUpdateProgress").style["display"] = "none";
				}
			// -->
			</script>
			
			<asp:Button ID="btnAutoUpdateProviders" runat="server" Text="Auto-update Providers" ToolTip="Automatically update all installed providers, of all types"
				OnClick="btnAutoUpdateProviders_Click" meta:resourcekey="btnAutoUpdateProvidersResource1" Visible="false" Enabled="true" />
			<span id="ProvidersUpdateProgress" style="display: none;">
				<img src="Images/Wait.gif" alt="..." />
			</span>
			<asp:Label ID="lblAutoUpdateResult" runat="server" />
			
		</div>
		<br /><br />

		<h2 class="separator"><asp:Literal ID="lblUploadProviders" runat="server" Text="Providers DLLs Management" EnableViewState="False" meta:resourcekey="lblUploadProvidersResource1" /></h2>
		<div id="DllUploadContainerDiv">
			<h4><asp:Literal ID="lblUploadNewDll" runat="server" Text="Upload new DLL" EnableViewState="False" meta:resourcekey="lblUploadNewDllResource1" /></h4>
			<asp:FileUpload ID="upDll" runat="server" meta:resourcekey="upDllResource1" />
			<asp:Button ID="btnUpload" runat="server" Text="Upload" OnClick="btnUpload_Click"
				meta:resourcekey="btnUploadResource1" />
			<span id="UploadProgressSpan" style="display: none;"><img src="Images/Wait.gif" alt="Uploading..." /></span><br />
			<asp:Label ID="lblUploadResult" runat="server" meta:resourcekey="lblUploadResultResource1" />
		</div>
		<div id="DllsListContainerDiv">
			<h4><asp:Literal ID="lblRemoveDll" runat="server" Text="Remove DLL" EnableViewState="False" meta:resourcekey="lblRemoveDllResource1" /></h4>
			<asp:DropDownList ID="lstDlls" runat="server" 
				OnSelectedIndexChanged="lstDlls_SelectedIndexChanged" 
				meta:resourcekey="lstDllsResource1" AutoPostBack="True" />
			<asp:Button ID="btnDeleteDll" runat="server" Text="Delete" OnClick="btnDeleteDll_Click" Enabled="False" meta:resourcekey="btnDeleteDllResource1" />
			<br />
			<asp:Label ID="lblDllResult" runat="server" meta:resourcekey="lblDllResultResource1" />
		</div>
	
		<div id="DllNoticeDiv">
			<small>
				<asp:Literal ID="lblUploadInfo" runat="server" EnableViewState="False"
					Text="<b>Note</b>: removing a DLL won't disable the Providers it contains until the next wiki restart,<br />but uploading a new DLL will automatically load the Providers it contains." 
					meta:resourcekey="lblUploadInfoResource1" />
			</small>
		</div>
	
		<div class="clear"></div>
	
		<br /><br /><br />

		<h2 class="separator"><asp:Literal ID="lblExportImportSettings" runat="server" Text="Export/Import Settings" EnableViewState="False" meta:resourcekey="lblExportImportSettingsResource1" /></h2>
		<h4><asp:Literal ID="lblExportSettings" runat="server" Text="Export Settings and related data" EnableViewState="False" meta:resourcekey="lblExportSettingsResource1" /></h4>
		<asp:Label ID="lblSettingsSource" runat="server" meta:resourcekey="lblSettingsSourceResource1"  />
		<asp:DropDownList ID="lstWiki" runat="server" OnSelectedIndexChanged="lstWiki_SelectedIndexChanged" AutoPostBack="true" meta:resourcekey="lstWikiResource1" />
		<asp:Button ID="btnExportSettings" runat="server" Text="Export" Enabled="false" OnClick="btnExportSettings_Click" meta:resourcekey="btnExportSettingsResource1" />
		<asp:Label ID="lblExportSettingsResult" runat="server" meta:resourcekey="lblExportSettingsResultResource1" />
		
		<br /><br />
		<h4><asp:Literal ID="lblExportGlobalSettings" runat="server" Text="Export Global Settings and related data" EnableViewState="False" meta:resourcekey="lblExportGlobalSettingsResource1" /></h4>
		<asp:Label ID="lblGlobalSettingsSource" runat="server" meta:resourcekey="lblGlobalSettingsSourceResource1"  />
		<asp:Button ID="btnExportGlobalSettings" runat="server" Text="Export" OnClick="btnExportGlobalSettings_Click" meta:resourcekey="btnExportGlobalSettingsResource1" />
		<asp:Label ID="lblExportGlobalSettingsResult" runat="server" meta:resourcekey="lblExportGlobalSettingsResultResource1" />

		<br /><br />
		<h4><asp:Literal ID="lblImportSettings" runat="server" Text="Import Settings" EnableViewState="False" meta:resourcekey="lblImportSettingsResource1" /></h4>
		<asp:DropDownList ID="lstDestinationWiki" runat="server" meta:resourcekey="lstDestinationWikiResource1"
			OnSelectedIndexChanged="lstDestinationWiki_SelectedIndexChanged" AutoPostBack="true"/>
		<asp:FileUpload ID="upSettings" runat="server" meta:resourcekey="upSettingsResource1" />
		<asp:Button ID="btnImportSettings" runat="server" Text="Import" OnClick="btnImportSettings_Click" meta:resourcekey="btnImportSettingsResource1" Enabled="false" />
		<br />
		<asp:Label ID="lblImportSettingsResult" runat="server" meta:resourcekey="lblImportSettingsResultResource1" />

		<br /><br />
		<h4><asp:Literal ID="lblImportGlobalSettings" runat="server" Text="Import Global Settings" EnableViewState="False" meta:resourcekey="lblImportGlobalSettingsResource1" /></h4>
		<asp:FileUpload ID="upGlobalSettings" runat="server" meta:resourcekey="upGlobalSettingsResource1" />
		<asp:Button ID="btnImportGlobalSettings" runat="server" Text="Import" OnClick="btnImportGlobalSettings_Click" meta:resourcekey="btnImportGlobalSettingsResource1" />
		<br />
		<asp:Label ID="lblImportGlobalSettingsResult" runat="server" meta:resourcekey="lblImportGlobalSettingsResultResource1" />
	</div>
	
	<div class="clear"></div>

</asp:Content>
