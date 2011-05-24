<%@ Page Title="" Language="C#" MasterPageFile="~/Admin.master" AutoEventWireup="true" CodeBehind="AdminGlobalConfig.aspx.cs" Inherits="ScrewTurn.Wiki.AdminGlobalConfig" culture="auto" meta:resourcekey="PageResource2" uiculture="auto" %>

<asp:Content ID="ctnHead" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="ctnConfig" ContentPlaceHolderID="cphAdmin" runat="server">
	<h2 class="sectiontitle"><asp:Literal ID="lblConfig" runat="server" Text="Configuration" meta:resourcekey="lblConfigResource1" /></h2>
	
	<div id="ConfigGeneralDiv">
		<div class="featurecontainer">
			<h3 class="separator"><asp:Literal ID="lblGeneralConfig" runat="server" Text="General Configuration" EnableViewState="False" meta:resourcekey="lblGeneralConfigResource1" /></h3>
		</div>

		<div class="featurecontainer">
			<asp:Literal ID="lblContactEmail" runat="server" Text="Contact email" 
				EnableViewState="False" meta:resourcekey="lblContactEmailResource1" /><br />
			<asp:TextBox ID="txtContactEmail" runat="server" CssClass="configmedium" 
				meta:resourcekey="txtContactEmailResource1" />
			
			<asp:RequiredFieldValidator ID="rfvContactEmail" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtContactEmail" ErrorMessage="Contact Email is required" 
				meta:resourcekey="rfvContactEmailResource1" />
			<asp:RegularExpressionValidator ID="revContactEmail" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtContactEmail" ErrorMessage="Invalid email address" 
				meta:resourcekey="revContactEmailResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblSenderEmail" runat="server" Text="Sender email" 
				EnableViewState="False" meta:resourcekey="lblSenderEmailResource1" /><br />
			<asp:TextBox ID="txtSenderEmail" runat="server" CssClass="configmedium" 
				meta:resourcekey="txtSenderEmailResource1" />
			
			<asp:RequiredFieldValidator ID="rfvSenderEmail" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtSenderEmail" ErrorMessage="Sender Email is required" 
				meta:resourcekey="rfvSenderEmailResource1" />
			<asp:RegularExpressionValidator ID="revSenderEmail" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtSenderEmail" ErrorMessage="Invalid email address" 
				meta:resourcekey="revSenderEmailResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblErrorsEmails" runat="server" 
				Text="Email addresses to notify in case of errors (separate with commas)" 
				EnableViewState="False" meta:resourcekey="lblErrorsEmailsResource1" /><br />
			<asp:TextBox ID="txtErrorsEmails" runat="server" CssClass="configlarge" 
				meta:resourcekey="txtErrorsEmailsResource1" />
			
			<asp:CustomValidator ID="cvErrorsEmails" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtErrorsEmails" ErrorMessage="Invalid email address" OnServerValidate="cvErrorsEmails_ServerValidate" 
				meta:resourcekey="cvErrorsEmailsResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblSmtpServer" runat="server" 
				Text="SMTP server address and port" EnableViewState="False" 
				meta:resourcekey="lblSmtpServerResource1" /><br />
			<asp:TextBox ID="txtSmtpServer" runat="server" CssClass="configmedium" 
				meta:resourcekey="txtSmtpServerResource1" />
			<asp:TextBox ID="txtSmtpPort" runat="server" CssClass="configsmallest" 
				meta:resourcekey="txtSmtpPortResource1" />
			
			<asp:RequiredFieldValidator ID="rfvSmtpServer" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtSmtpServer" ErrorMessage="SMTP Server is required" 
				meta:resourcekey="rfvSmtpServerResource1" />
			<asp:RegularExpressionValidator ID="revSmtpServer" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtSmtpServer" ErrorMessage="Invalid SMTP Server address" 
				meta:resourcekey="revSmtpServerResource1" />
			<asp:RangeValidator ID="rvSmtpPort" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtSmtpPort" ErrorMessage="Invalid Port (min. 1, max. 65535)"
				Type="Integer" MinimumValue="1" MaximumValue="65535" 
				meta:resourcekey="rvSmtpPortResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblSmtpAuthentication" runat="server" 
				Text="SMTP username and password" EnableViewState="False" 
				meta:resourcekey="lblSmtpAuthenticationResource1" /><br />
			<asp:TextBox ID="txtUsername" runat="server" CssClass="configsmall" 
				meta:resourcekey="txtUsernameResource1" />
			<asp:TextBox ID="txtPassword" runat="server" TextMode="Password" 
				CssClass="configsmall" meta:resourcekey="txtPasswordResource1" />
			<asp:CheckBox ID="chkEnableSslForSmtp" runat="server" Text="Enable SSL" 
				meta:resourcekey="chkEnableSslForSmtpResource1" />
			
			<asp:CustomValidator ID="cvUsername" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtUsername" ErrorMessage="Password is required"
				OnServerValidate="cvUsername_ServerValidate" ValidateEmptyText="True" 
				meta:resourcekey="cvUsernameResource1" />
			<asp:CustomValidator ID="cvPassword" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtUsername" ErrorMessage="Username is required"
				OnServerValidate="cvPassword_ServerValidate" ValidateEmptyText="True" 
				meta:resourcekey="cvPasswordResource1" />
		</div>
	</div>
	
	<div id="ConfigSecurityDiv">
		<div class="featurecontainer">
			<h3 class="separator"><asp:Literal ID="lblSecurityConfig" runat="server" 
					Text="Security Configuration" EnableViewState="False" 
					meta:resourcekey="lblSecurityConfigResource1" /></h3>
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblMaxFileSize" runat="server" 
				Text="Max file size allowed for upload" EnableViewState="False" 
				meta:resourcekey="lblMaxFileSizeResource1" /><br />
			<asp:TextBox ID="txtMaxFileSize" runat="server" CssClass="configsmallest" 
				meta:resourcekey="txtMaxFileSizeResource1" /> KB
			
			<asp:RequiredFieldValidator ID="rfvMaxFileSize" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtMaxFileSize" ErrorMessage="Max File Size is required" 
				meta:resourcekey="rfvMaxFileSizeResource1" />
			<asp:RangeValidator ID="rvMaxFileSize" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtMaxFileSize" ErrorMessage="Invalid File Size (min. 256, max. 102400)"
				Type="Integer" MinimumValue="256" MaximumValue="102400" 
				meta:resourcekey="rvMaxFileSizeResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblLoggingLevel" runat="server" Text="Logging level" 
				EnableViewState="False" meta:resourcekey="lblLoggingLevelResource1" /><br />
			<asp:RadioButton ID="rdoAllMessages" runat="server" Text="All Messages" 
				GroupName="log" meta:resourcekey="rdoAllMessagesResource1" />
			<asp:RadioButton ID="rdoWarningsAndErrors" runat="server" 
				Text="Warnings and Errors" GroupName="log" 
				meta:resourcekey="rdoWarningsAndErrorsResource1" />
			<asp:RadioButton ID="rdoErrorsOnly" runat="server" Text="Errors Only" 
				GroupName="log" meta:resourcekey="rdoErrorsOnlyResource1" />
			<asp:RadioButton ID="rdoDisableLog" runat="server" Text="Disable Log" 
				GroupName="log" meta:resourcekey="rdoDisableLogResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblMaxLogSize" runat="server" Text="Max log size" 
				EnableViewState="False" meta:resourcekey="lblMaxLogSizeResource1" /><br />
			<asp:TextBox ID="txtMaxLogSize" runat="server" CssClass="configsmallest" 
				meta:resourcekey="txtMaxLogSizeResource1" /> KB
			
			<asp:RequiredFieldValidator ID="rfvMaxLogSize" runat="server" 
				Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtMaxLogSize" ErrorMessage="Max Log Size is required" 
				meta:resourcekey="rfvMaxLogSizeResource1" />
			<asp:RangeValidator ID="rvMaxLogSize" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtMaxLogSize" ErrorMessage="Invalid Log Size (min. 16, max. 10240)"
				Type="Integer" MinimumValue="16" MaximumValue="10240" 
				meta:resourcekey="rvMaxLogSizeResource1" />
		</div>
	</div>
	
	<div id="ConfigAdvancedDiv">	
		<div class="featurecontainer">
			<h3 class="separator"><asp:Literal ID="lblAdvancedConfig" runat="server" 
					Text="Advanced Configuration" EnableViewState="False" 
					meta:resourcekey="lblAdvancedConfigResource1" /></h3>
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblUsernameRegEx" Text="Regular Expression for validating usernames and group names" runat="server" EnableViewState="false"
			meta:resourcekey="lblUsernameRegExResource2" /><br />
			<asp:TextBox ID="txtUsernameRegEx" runat="server" CssClass="configlarge" 
					meta:resourcekey="txtUsernameRegExResource1" />
					<asp:CustomValidator ID="cvUsernameRegEx" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtUsernameRegEx" ErrorMessage="Invalid Regular Expression"
				OnServerValidate="cvUsernameRegEx_ServerValidate" 
				meta:resourcekey="cvUsernameRegExResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:Literal ID="lblPasswordRegEx" Text="Regular Expression for validating passwords" runat="server" EnableViewState="false"
			meta:resourcekey="lblPasswordRegExResource2" /><br />
			<asp:TextBox ID="txtPasswordRegEx" runat="server" CssClass="configlarge" 
					meta:resourcekey="txtPasswordRegExResource1" />
			<asp:CustomValidator ID="cvPasswordRegEx" runat="server" Display="Dynamic" CssClass="resulterror"
				ControlToValidate="txtPasswordRegEx" ErrorMessage="Invalid Regular Expression"
				OnServerValidate="cvPasswordRegEx_ServerValidate" 
				meta:resourcekey="cvPasswordRegExResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:CheckBox ID="chkEnableAutomaticUpdateChecks" runat="server" 
				Text="Enable automatic update checks (system and providers)" 
				meta:resourcekey="chkEnableAutomaticUpdateChecksResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:CheckBox ID="chkEnableViewStateCompression" runat="server" 
				Text="Enable ViewState compression" 
				meta:resourcekey="chkEnableViewStateCompressionResource1" />
		</div>
		
		<div class="featurecontainer">
			<asp:CheckBox ID="chkEnableHttpCompression" runat="server" 
				Text="Enable HTTP compression" 
				meta:resourcekey="chkEnableHttpCompressionResource1" />
		</div>
	</div>
	
	<div id="ButtonsDiv">
		<asp:Button ID="btnSave" runat="server" Text="Save Configuration" 
			OnClick="btnSave_Click" meta:resourcekey="btnSaveResource1" />
		<asp:Label ID="lblResult" runat="server" 
			meta:resourcekey="lblResultResource1" />
	</div>
	
	<div style="clear: both;"></div>

	<h2 class="separator"><asp:Literal ID="lblUploadProvidersTitle" runat="server" Text="Providers DLLs Management" EnableViewState="False" meta:resourcekey="lblUploadProvidersTitleResource1" /></h2>
	
	<h4><asp:Literal ID="lblUploadNewDll" runat="server" Text="Upload new DLL" EnableViewState="False" meta:resourcekey="lblUploadNewDllResource1" /></h4>
	<asp:FileUpload ID="upDll" runat="server" meta:resourcekey="upDllResource1" />
	<asp:Button ID="btnUpload" runat="server" Text="Upload" OnClick="btnUpload_Click"
		meta:resourcekey="btnUploadResource1" />
	<span id="UploadProgressSpan" style="display: none;"><img src="Images/Wait.gif" alt="Uploading..." /></span><br />
	<asp:Label ID="lblUploadResult" runat="server" meta:resourcekey="lblUploadResultResource1" />
	
	<div id="DllsListContainerDiv">
		<asp:DropDownList ID="lstDlls" runat="server" OnSelectedIndexChanged="lstDlls_SelectedIndexChanged" meta:resourcekey="lstDllsResource1" AutoPostBack="true" />
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
	
	<div style="clear: both;"></div>
	
	<br />
	<h2 class="separator"><asp:Literal ID="lblDataMigration" runat="server" Text="Data Migration" EnableViewState="False" meta:resourcekey="lblDataMigrationResource1" /></h2>
	<asp:Literal ID="lblMigrationInfo" runat="server" EnableViewState="False"		
		Text="<b>Note 1</b>: always perform a full backup of all your data before performing a migration.<br /><b>Note 2</b>: migrations usually take several minutes to complete: during this time, do not perform any other activity in the wiki, and do not close this page.<br /><b>Note 3</b>: the destination provider should be completely empty: if it contains any data, it might cause consistency issues. Refer to the target provider's documentation for details.<br /><b>Timeouts</b>: it is strongly suggested that you increase the executionTimeout parameter in web.config before migrating data." 
		meta:resourcekey="lblMigrationInfoResource2" />
	<br /><br />
	
	<h4><asp:Literal ID="lblMigratePages" runat="server" Text="Migrate Pages and related data" EnableViewState="False" meta:resourcekey="lblMigratePagesResource1" /></h4>
	<asp:DropDownList ID="lstPagesSource" runat="server" 
		OnSelectedIndexChanged="lstPagesSource_SelectedIndexChanged" AutoPostBack="true" meta:resourcekey="lstPagesSourceResource1" />
	<img src="Images/ArrowRight.png" alt="->" />
	<asp:DropDownList ID="lstPagesDestination" runat="server" meta:resourcekey="lstPagesDestinationResource1"  />
	<asp:Button ID="btnMigratePages" runat="server" Text="Migrate" Enabled="False" 
		OnClick="btnMigratePages_Click" meta:resourcekey="btnMigratePagesResource1" />
	<asp:Label ID="lblMigratePagesResult" runat="server" meta:resourcekey="lblMigratePagesResultResource1" />
	<br />
	<br />
	
	<h4><asp:Literal ID="lblMigrateUsers" runat="server" Text="Migrate Users and related data" EnableViewState="False" meta:resourcekey="lblMigrateUsersResource1" /></h4>
	<asp:DropDownList ID="lstUsersSource" runat="server" 
		OnSelectedIndexChanged="lstUsersSource_SelectedIndexChanged" AutoPostBack="true" meta:resourcekey="lstUsersSourceResource1" />
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
		OnSelectedIndexChanged="lstFilesSource_SelectedIndexChanged" AutoPostBack="true" meta:resourcekey="lstFilesSourceResource1" />
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
		<asp:Literal ID="lblCopySettingsDestinationConfig" runat="server" Text="Destination Settings Provider Configuration string (if needed)" EnableViewState="false" /><br />
		<asp:TextBox ID="txtSettingsDestinationConfig" runat="server" TextMode="MultiLine" CssClass="config" />
	</div>
	
	<div style="clear: both;"></div>
</asp:Content>
