<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminProvidersManagement.aspx.cs" Inherits="ScrewTurn.Wiki.AdminProvidersManagement" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnConfig" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<h2 class="sectiontitle"><asp:Literal ID="lblProvidersManagementTitle" runat="server" Text="Providers Management" EnableViewState="False" meta:resourcekey="lblProvidersManagementTitleResource1" /></h2>
	
		<asp:Literal ID="lblDisplay" runat="server" Text="Display" EnableViewState="False" meta:resourcekey="lblDisplayResource1" />:
		<asp:RadioButton ID="rdoStorageProviders" runat="server" Text="Storage Providers" GroupName="type" Checked="True" OnCheckedChanged="rdo_CheckedChanged" meta:resourcekey="rdoStorageProvidersResource1" AutoPostBack="true" />
		<asp:RadioButton ID="rdoFormatter" runat="server" Text="Formatter Providers" GroupName="type" OnCheckedChanged="rdo_CheckedChanged" meta:resourcekey="rdoFormatterResource1" AutoPostBack="true" />
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
				OnClick="btnAutoUpdateProviders_Click" meta:resourcekey="btnAutoUpdateProvidersResource1" />
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

		<h2 class="separator"><asp:Literal ID="lblDataMigration" runat="server" Text="Data Migration" EnableViewState="False" meta:resourcekey="lblDataMigrationResource1" /></h2>
		<asp:Literal ID="lblMigrationInfo" runat="server" EnableViewState="False"		
			Text="<b>Note 1</b>: always perform a full backup of all your data before performing a migration.<br /><b>Note 2</b>: migrations usually take several minutes to complete: during this time, do not perform any other activity in the wiki, and do not close this page.<br /><b>Note 3</b>: the destination provider should be completely empty: if it contains any data, it might cause consistency issues. Refer to the target provider's documentation for details.<br /><b>Timeouts</b>: it is strongly suggested that you increase the executionTimeout parameter in web.config before migrating data." 
			meta:resourcekey="lblMigrationInfoResource2" />
		<br /><br />
	
		<h4><asp:Literal ID="lblMigratePages" runat="server" Text="Migrate Pages and related data" EnableViewState="False" meta:resourcekey="lblMigratePagesResource1" /></h4>
		<asp:DropDownList ID="lstPagesSource" runat="server" 
			OnSelectedIndexChanged="lstPagesSource_SelectedIndexChanged" AutoPostBack="True" 
			meta:resourcekey="lstPagesSourceResource1" />
		<img src="Images/ArrowRight.png" alt="->" />
		<asp:DropDownList ID="lstPagesDestination" runat="server" meta:resourcekey="lstPagesDestinationResource1"  />
		<asp:Button ID="btnMigratePages" runat="server" Text="Migrate" Enabled="False" 
			OnClick="btnMigratePages_Click" meta:resourcekey="btnMigratePagesResource1" />
		<asp:Label ID="lblMigratePagesResult" runat="server" meta:resourcekey="lblMigratePagesResultResource1" />
		<br />
		<br />
	
		<h4><asp:Literal ID="lblMigrateUsers" runat="server" Text="Migrate Users and related data" EnableViewState="False" meta:resourcekey="lblMigrateUsersResource1" /></h4>
		<asp:DropDownList ID="lstUsersSource" runat="server" 
			OnSelectedIndexChanged="lstUsersSource_SelectedIndexChanged" AutoPostBack="True" 
			meta:resourcekey="lstUsersSourceResource1" />
		<img src="Images/ArrowRight.png" alt="->" />
		<asp:DropDownList ID="lstUsersDestination" runat="server" meta:resourcekey="lstUsersDestinationResource1" />
		<asp:Button ID="btnMigrateUsers" runat="server" Text="Migrate" Enabled="False"
			OnClick="btnMigrateUsers_Click" meta:resourcekey="btnMigrateUsersResource1" />
		<asp:Label ID="lblMigrateUsersResult" runat="server" meta:resourcekey="lblMigrateUsersResultResource1" />
		<br />
		<span class="small">
			<asp:Literal ID="lblMigrateUsersInfo" runat="server" 
				Text="<b>Note</b>: migrating user accounts will reset all their passwords (an email notice will be sent to all users)." 
				EnableViewState="False" meta:resourcekey="lblMigrateUsersInfoResource1" />
		</span>	
		<br /><br />
	
		<h4><asp:Literal ID="lblMigrateFiles" runat="server" Text="Migrate Files and related data" EnableViewState="False" meta:resourcekey="lblMigrateFilesResource1" /></h4>
		<asp:DropDownList ID="lstFilesSource" runat="server" 
			OnSelectedIndexChanged="lstFilesSource_SelectedIndexChanged" AutoPostBack="True" 
			meta:resourcekey="lstFilesSourceResource1" />
		<img src="Images/ArrowRight.png" alt="->" />
		<asp:DropDownList ID="lstFilesDestination" runat="server" meta:resourcekey="lstFilesDestinationResource1" />
		<asp:Button ID="btnMigrateFiles" runat="server" Text="Migrate" Enabled="False"
			OnClick="btnMigrateFiles_Click" meta:resourcekey="btnMigrateFilesResource1" />
		<asp:Label ID="lblMigrateFilesResult" runat="server" meta:resourcekey="lblMigrateFilesResultResource1" />
		
		<br /><br /><br />
	
		<h2 class="separator"><asp:Literal ID="lblExportImportSettings" runat="server" Text="Export/Import Settings" EnableViewState="False" meta:resourcekey="lblExportImportSettingsResource1" /></h2>
		<h4><asp:Literal ID="lblExportSettings" runat="server" Text="Export Settings and related data" EnableViewState="False" meta:resourcekey="lblExportSettingsResource1" /></h4>
		<asp:Label ID="lblSettingsSource" runat="server" meta:resourcekey="lblSettingsSourceResource1"  />
		<asp:DropDownList ID="lstWiki" runat="server"
			OnSelectedIndexChanged="lstWiki_SelectedIndexChanged" AutoPostBack="true"
			meta:resourcekey="lstWikiResource1" />
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
			OnSelectedIndexChanged="lstDestinationWiki_SelectedIndexChanged" AutoPostBack="true" Enabled="false"/>
		<asp:FileUpload ID="upSettings" runat="server" meta:resourcekey="upSettingsResource1" />
		<asp:Button ID="btnImportSettings" runat="server" Text="Import" OnClick="btnImportSettings_Click" meta:resourcekey="btnImportSettingsResource1" />
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
