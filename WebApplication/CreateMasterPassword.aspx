<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CreateMasterPassword.aspx.cs" Inherits="ScrewTurn.Wiki.CreateMasterPassword" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<title></title>
</head>
<body>
	<form id="form1" runat="server">
		<div>
			<h3 class="separator">
				<asp:Literal ID="lblChangeMasterPassword" runat="server" 
					Text="Master Password" EnableViewState="False"
					meta:resourcekey="lblChangeMasterPasswordResource1" />
			</h3>
		</div>	

		<div>
				<asp:Literal ID="lblNewPwd" runat="server" 
					Text="New Password" EnableViewState="False" 
					meta:resourcekey="lblNewPwdResource1" /><br />
				<asp:TextBox ID="txtNewPwd" TextMode="Password" runat="server"
					meta:resourcekey="txtNewPasswordResource1" />
				<asp:RequiredFieldValidator id="RequiredFieldValidator1"  
					 ControlToValidate="txtNewPwd" Text="The password is required" runat="server" />
		</div>

		<div>
				<asp:Literal ID="lblReNewPwd" runat="server" 
					Text="New Password (Repeat)" EnableViewState="False" 
					meta:resourcekey="lblReNewPwdResource1" /><br />
				<asp:TextBox ID="txtReNewPwd" TextMode="Password" runat="server" 
					meta:resourcekey="txtReNewPasswordResource1" />
				<asp:CompareValidator id="cvComparePwd" runat="server" ErrorMessage="Passwords are not equal"
					ControlToValidate="txtReNewPwd" ControlToCompare="txtNewPwd" meta:resourcekey="cvComparePwdResource1">
				</asp:CompareValidator>
		</div>
		<div>
			<asp:Button ID="BtnSave" runat="server" Text="Save Password" OnClick="btnSave_Click"/>
			<asp:Label ID="lblRes" runat="server" meta:resourcekey="lblResultResource1" />
			<asp:HyperLink ID="lnkMainRedirect" runat="server" Visible="false" Text="Main Page" meta:resourcekey="lnkMainRedirectResource1"></asp:HyperLink>
		</div>
	</form>
</body>
</html>
