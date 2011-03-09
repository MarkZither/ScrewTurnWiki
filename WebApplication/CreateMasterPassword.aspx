<%@ Page Language="C#" MasterPageFile="~/MasterPageClean.master" AutoEventWireup="true" Inherits="ScrewTurn.Wiki.CreateMasterPassword" Title="Create Master Password" Culture="auto" meta:resourcekey="PageResource1" UICulture="auto" EnableSessionState="True" Codebehind="CreateMasterPassword.aspx.cs" %>

<asp:Content ID="CtnCreatePwd" ContentPlaceHolderID="CphMasterClean" Runat="Server">
	<h1 class="pagetitlesystem"><asp:Literal ID="lblChangeMasterPassword" runat="server" Text="Set Master Password" 
		EnableViewState="False"	meta:resourcekey="lblChangeMasterPasswordResource1" />
	</h1>
	<asp:Literal ID="lblDescriptionPwd" runat="server" meta:resourcekey="lblDescriptionPwdResource1"
		Text="This is the first time you access your wiki. You must configure the password for the built-in admin account. Make sure you use very strong password as the built-in admin account can perform any operation in the wiki.">
	</asp:Literal>
	<br /><br />
	<table>
		<tr>
			<td>
				<p style="text-align: right;"><asp:Literal ID="lblNewPwd" runat="server" 
				Text="New Password" EnableViewState="False" meta:resourcekey="lblNewPwdResource1" /></p>
			</td>
			<td>
				<asp:TextBox ID="txtNewPwd" TextMode="Password" runat="server" meta:resourcekey="txtNewPwdResource1"
				 ToolTip="Type here the master password" />
				<asp:RequiredFieldValidator id="RequiredFieldValidator2"  ControlToValidate="txtNewPwd" Text="The password is required" runat="server" />
			</td>
		</tr> 
		<tr>
			<td>
				<p style="text-align: right;"><asp:Literal ID="lblReNewPwd" runat="server" Text="New Password (Repeat)" EnableViewState="False" 
				meta:resourcekey="lblReNewPwdResource1" /><br /></p>
			</td>
			<td>
				<asp:TextBox ID="txtReNewPwd" TextMode="Password" runat="server" meta:resourcekey="txtReNewPasswordResource1" 
				 ToolTip="Repeat the master password" />
				<asp:RequiredFieldValidator id="cvCheckPwd"  ControlToValidate="txtReNewPwd" Text="The password is required" runat="server" />
			</td>
		</tr>
		<tr>
			<td></td>
			<td>
				<asp:CompareValidator id="cvComparePwd" runat="server" ErrorMessage="Passwords are not equal" ControlToValidate="txtReNewPwd"
				 ControlToCompare="txtNewPwd" meta:resourcekey="cvComparePwdResource1">	</asp:CompareValidator>
			</td>
		</tr>
		<tr>
			<td style="height: 24px">&nbsp;</td>
			<td style="height: 24px">
				<asp:Button ID="BtnSave" runat="server" Text="Save Password" OnClick="btnSave_Click" meta:resourcekey="BtnSaveResource1"/>
				<asp:Label ID="lblRes" runat="server" meta:resourcekey="lblResuResource1" />
			</td>
		</tr>
		<tr>
			<td style="height: 24px">&nbsp;</td>
			<td style="height: 24px">
				<asp:HyperLink ID="lnkMainRedirect" runat="server" Visible="false" Text="Go To Main Page" meta:resourcekey="lnkMainRedirectResource1"></asp:HyperLink>
			</td>
		</tr>
	</table>
</asp:Content>
