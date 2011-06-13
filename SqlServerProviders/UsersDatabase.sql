
create table [User] (
	[Wiki] varchar(100) not null,
	[Username] nvarchar(100) not null,
	[PasswordHash] varchar(100) not null,
	[DisplayName] nvarchar(150),
	[Email] varchar(100) not null,
	[Active] bit not null,
	[DateTime] datetime not null,
	constraint [PK_User] primary key clustered ([Wiki], [Username])
)

create table [UserGroup] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(100) not null,
	[Description] nvarchar(150),
	constraint [PK_UserGroup] primary key clustered ([Wiki], [Name])
)

create table [UserGroupMembership] (
	[Wiki] varchar(100) not null,
	[User] nvarchar(100) not null,
	[UserGroup] nvarchar(100) not null,
	constraint [FK_UserGroupMembership_User] foreign key ([Wiki], [User]) references [User]([Wiki], [UserName])
		on delete cascade on update cascade,
	constraint [FK_UserGroupMembership_UserGroup] foreign key ([Wiki], [UserGroup]) references [UserGroup]([Wiki], [Name])
		on delete cascade on update cascade,
	constraint [PK_UserGroupMembership] primary key clustered ([Wiki], [User], [UserGroup])
)

create table [UserData] (
	[Wiki] varchar(100) not null,
	[User] nvarchar(100) not null,
	[Key] nvarchar(100) not null,
	[Data] nvarchar(4000) not null,
	constraint [FK_UserData_User] foreign key ([Wiki], [User]) references [User]([Wiki], [UserName])
		on delete cascade on update cascade,
	constraint [PK_UserData] primary key clustered ([Wiki], [User], [Key])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Users') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Users', 3000)
end
