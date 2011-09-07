
create table [Directory] (
	[Wiki] varchar(100) not null,
	[FullPath] nvarchar(250) not null,
	[Parent] nvarchar(250),
	constraint [PK_Directory] primary key clustered ([Wiki], [FullPath])
)

create table [File] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Directory] nvarchar(250) not null,
	[Size] bigint not null,
	[LastModified] datetime not null,
	[Data] varbinary(max) not null,
	constraint [FK_File_Directory] foreign key ([Wiki], [Directory]) references [Directory]([Wiki], [FullPath])
		on delete cascade on update cascade,
	constraint [PK_File] primary key clustered ([Wiki], [Name], [Directory])
)

create table [Attachment] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Page] nvarchar(200) not null,
	[Size] bigint not null,
	[LastModified] datetime not null,
	[Data] varbinary(max) not null,
	constraint[PK_Attachment] primary key clustered ([Wiki], [Name], [Page])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Files') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Files', 4000)
end

if (select count([FullPath]) from [Directory] where [FullPath] = '/') = 0
begin
	insert into [Directory] ([Wiki], [FullPath], [Parent]) values ('root', '/', NULL)
end
