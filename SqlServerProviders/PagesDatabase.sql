﻿
create table [Namespace] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(100) not null,
	[DefaultPage] nvarchar(200),
	constraint [PK_Namespace] primary key clustered ([Wiki], [Name])
)

create table [Category](
	[Wiki] varchar(100) not null,
	[Name] nvarchar(100) not null,
	[Namespace] nvarchar(100) not null,
	constraint [FK_Category_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name])
		on delete cascade on update cascade,
	constraint [PK_Category] primary key clustered ([Wiki], [Name], [Namespace])
)

create table [Page] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[CreationDateTime] datetime not null,
	constraint [FK_Page_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name])
		on delete cascade on update cascade,
	constraint [PK_Page] primary key clustered ([Wiki], [Name], [Namespace])
)

-- Deleting/Renaming/Moving a page requires manually updating the binding
create table [CategoryBinding] (
	[Wiki] varchar(100) not null,
	[Namespace] nvarchar(100) not null,
	[Category] nvarchar(100) not null,
	[Page] nvarchar(200) not null,
		constraint [FK_CategoryBinding_Namespace] foreign key ([Wiki], [Namespace]) references [Namespace]([Wiki], [Name]),
	constraint [FK_CategoryBinding_Category] foreign key ([Wiki], [Category], [Namespace]) references [Category]([Wiki], [Name], [Namespace])
		on delete cascade on update cascade,
	constraint [FK_CategoryBinding_Page] foreign key ([Wiki], [Page], [Namespace]) references [Page]([Wiki], [Name], [Namespace])
		on delete no action on update no action,
	constraint [PK_CategoryBinding] primary key clustered ([Wiki], [Namespace], [Page], [Category])
)

create table [PageContent] (
	[Wiki] varchar(100) not null,
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Revision] smallint not null,
	[Title] nvarchar(200) not null,
	[User] nvarchar(100) not null,
	[LastModified] datetime not null,
	[Comment] nvarchar(300),
	[Content] nvarchar(max) not null,
	[Description] nvarchar(200),
	constraint [FK_PageContent_Page] foreign key ([Wiki], [Page], [Namespace]) references [Page]([Wiki], [Name], [Namespace])
		on delete cascade on update cascade,
	constraint [PK_PageContent] primary key clustered ([Wiki], [Page], [Namespace], [Revision])
)

create table [PageKeyword] (
	[Wiki] varchar(100) not null,
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Revision] smallint not null,
	[Keyword] nvarchar(50) not null,
	constraint [FK_PageKeyword_PageContent] foreign key ([Wiki], [Page], [Namespace], [Revision]) references [PageContent]([Wiki], [Page], [Namespace], [Revision])
		on delete cascade on update cascade,
	constraint [PK_PageKeyword] primary key clustered ([Wiki], [Page], [Namespace], [Revision], [Keyword])
)

create table [Message] (
	[Wiki] varchar(100) not null,
	[Page] nvarchar(200) not null,
	[Namespace] nvarchar(100) not null,
	[Id] smallint not null,
	[Parent] smallint,
	[Username] nvarchar(100) not null,
	[Subject] nvarchar(200) not null,
	[DateTime] datetime not null,
	[Body] nvarchar(max) not null,
	constraint [FK_Message_Page] foreign key ([Wiki], [Page], [Namespace]) references [Page]([Wiki], [Name], [Namespace])
		on delete cascade on update cascade,
	constraint [PK_Message] primary key clustered ([Wiki], [Page], [Namespace], [Id])
)

create table [NavigationPath] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(100) not null,
	[Namespace] nvarchar(100) not null,
	[Page] nvarchar(200) not null,
	[Number] smallint not null,
	constraint [FK_NavigationPath_Page] foreign key ([Wiki], [Page], [Namespace]) references [Page]([Wiki], [Name], [Namespace])	
		on delete cascade on update cascade,
	constraint [PK_NavigationPath] primary key clustered ([Wiki], [Name], [Namespace], [Page])
)

create table [Snippet] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Content] nvarchar(max) not null,
	constraint [PK_Snippet] primary key clustered ([Wiki], [Name])
)

create table [ContentTemplate] (
	[Wiki] varchar(100) not null,
	[Name] nvarchar(200) not null,
	[Content] nvarchar(max) not null,
	constraint [PK_ContentTemplate] primary key clustered ([Wiki], [Name])
)

create table [IndexDocument] (
	[Wiki] varchar(100) not null,
	[Id] int not null,
	[Name] nvarchar(200) not null
		constraint [UQ_IndexDocument] unique,
	[Title] nvarchar(200) not null,
	[TypeTag] varchar(10) not null,
	[DateTime] datetime not null,
	constraint [PK_IndexDocument] primary key clustered ([Wiki], [Id])
)

create table [IndexWord] (
	[Wiki] varchar(100) not null,
	[Id] int not null,
	[Text] nvarchar(200) not null
		constraint [UQ_IndexWord] unique,
	constraint [PK_IndexWord] primary key clustered ([Wiki], [Id])
)

create table [IndexWordMapping] (
	[Wiki] varchar(100) not null,
	[Word] int not null,
	[Document] int not null,
	[FirstCharIndex] smallint not null,
	[WordIndex] smallint not null,
	[Location] tinyint not null,
	constraint [FK_IndexWordMapping_IndexWord] foreign key ([Wiki], [Word]) references [IndexWord]([Wiki], [Id])
		on delete cascade on update cascade,
	constraint [FK_IndexWordMapping_IndexDocument] foreign key ([Wiki], [Document]) references [IndexDocument]([Wiki], [Id])
		on delete cascade on update cascade,
	constraint [PK_IndexWordMapping] primary key clustered ([Wiki], [Word], [Document], [FirstCharIndex], [WordIndex], [Location])
)

if (select count(*) from sys.tables where [Name] = 'Version') = 0
begin
	create table [Version] (
		[Component] varchar(100) not null,
		[Version] int not null,
		constraint [PK_Version] primary key clustered ([Component])
	)
end

if (select count([Version]) from [Version] where [Component] = 'Pages') = 0
begin
	insert into [Version] ([Component], [Version]) values ('Pages', 3001)
end

if (select count([Name]) from [Namespace] where [Name] = '') = 0
begin
	insert into [Namespace] ([Wiki], [Name], [DefaultPage]) values ('root', '', null)
end
