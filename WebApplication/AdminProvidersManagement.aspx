<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminProvidersManagement.aspx.cs" Inherits="ScrewTurn.Wiki.AdminProvidersManagement" culture="auto" meta:resourcekey="PageResource1" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnConfig" ContentPlaceHolderID="cphAdmin" runat="server">
	<div class="leftaligned">
		<h2 class="sectiontitle"><asp:Literal ID="lblProvidersManagementTitle" runat="server" Text="Providers Management" EnableViewState="False" meta:resourcekey="lblProvidersManagementTitleResource1" /></h2>
	
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
		<asp:DropDownList ID="lstFilesDestination" runat="server" meta:resourcekey="lstFilesDestinationResource1"  />
		<asp:Button ID="btnMigrateFiles" runat="server" Text="Migrate" Enabled="False"
			OnClick="btnMigrateFiles_Click" meta:resourcekey="btnMigrateFilesResource1" />
		<asp:Label ID="lblMigrateFilesResult" runat="server" meta:resourcekey="lblMigrateFilesResultResource1" />
		<br /><br />
	
		<h4><asp:Literal ID="lblCopySettings" runat="server" Text="Copy Settings and related data" EnableViewState="False" meta:resourcekey="lblCopySettingsResource1" /></h4>
		<asp:Label ID="lblSettingsSource" runat="server" meta:resourcekey="lblSettingsSourceResource1"  />
		<img src="Images/ArrowRight.png" alt="->" />
		<asp:DropDownList ID="lstSettingsDestination" runat="server"
			OnSelectedIndexChanged="lstSettingsDestination_SelectedIndexChanged" meta:resourcekey="lstSettingsDestinationResource1" />
		<asp:Button ID="btnCopySettings" runat="server" Text="Copy" Enabled="False"
			OnClick="btnCopySettings_Click" meta:resourcekey="btnCopySettingsResource1" />
		<asp:Label ID="lblCopySettingsResult" runat="server" meta:resourcekey="lblCopySettingsResultResource1" /><br />
		<span class="small">
			<asp:Literal ID="lblCopySettingsInfo" runat="server" EnableViewState="False"
				Text="<b>Note</b>: in order to be detected, the destination Provider must be uploaded using the upload tool.<br />Log and recent changes will not be copied." 
				meta:resourcekey="lblCopySettingsInfoResource1" />
		</span>
		<br /><br />
		<div id="CopySettingsConfigDiv">
			<asp:Literal ID="lblCopySettingsDestinationConfig" runat="server" 
				Text="Destination Settings Provider Configuration string (if needed)" 
				EnableViewState="False" 
				meta:resourcekey="lblCopySettingsDestinationConfigResource1" /><br />
			<asp:TextBox ID="txtSettingsDestinationConfig" runat="server" 
				TextMode="MultiLine" CssClass="config" 
				meta:resourcekey="txtSettingsDestinationConfigResource1" />
		</div>
	</div>
	
	<div class="clear"></div>

</asp:Content>
