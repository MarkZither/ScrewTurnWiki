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
</asp:Content>
