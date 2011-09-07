
create table [GlobalSetting] (
	[Name] varchar(100) not null,
	[Value] nvarchar(4000) not null,
	constraint [PK_GlobalSetting] primary key clustered ([Name])
)

create table [Log] (
	[Id] int not null identity,
	[DateTime] datetime not null,
	[EntryType] char not null,
	[User] nvarchar(100) not null,
	[Message] nvarchar(4000) not null,
	[Wiki] nvarchar(100),
	constraint [PK_Log] primary key clustered ([Id])
)

create table [PluginAssembly] (
	[Name] varchar(100) not null,
	[Assembly] varbinary(max) not null,
	constraint [PK_PluginAssembly] primary key clustered ([Name])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'GlobalSettings') = 0
begin
	insert into [Version] ([Component], [Version]) values ('GlobalSettings', 4000)
end
