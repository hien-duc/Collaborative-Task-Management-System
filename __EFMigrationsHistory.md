classDiagram
direction BT
class AspNetRoleClaims {
   nvarchar(450) RoleId
   nvarchar(max) ClaimType
   nvarchar(max) ClaimValue
   int Id
}
class AspNetRoles {
   nvarchar(256) Name
   nvarchar(256) NormalizedName
   nvarchar(max) ConcurrencyStamp
   nvarchar(450) Id
}
class AspNetUserClaims {
   nvarchar(450) UserId
   nvarchar(max) ClaimType
   nvarchar(max) ClaimValue
   int Id
}
class AspNetUserLogins {
   nvarchar(max) ProviderDisplayName
   nvarchar(450) UserId
   nvarchar(450) LoginProvider
   nvarchar(450) ProviderKey
}
class AspNetUserRoles {
   nvarchar(450) UserId
   nvarchar(450) RoleId
}
class AspNetUserTokens {
   nvarchar(max) Value
   nvarchar(450) UserId
   nvarchar(450) LoginProvider
   nvarchar(450) Name
}
class AspNetUsers {
   nvarchar(100) FullName
   nvarchar(256) UserName
   nvarchar(256) NormalizedUserName
   nvarchar(256) Email
   nvarchar(256) NormalizedEmail
   bit EmailConfirmed
   nvarchar(max) PasswordHash
   nvarchar(max) SecurityStamp
   nvarchar(max) ConcurrencyStamp
   nvarchar(max) PhoneNumber
   bit PhoneNumberConfirmed
   bit TwoFactorEnabled
   datetimeoffset LockoutEnd
   bit LockoutEnabled
   int AccessFailedCount
   nvarchar(450) Id
}
class AuditLogs {
   nvarchar(450) UserId
   nvarchar(100) Action
   nvarchar(max) Details
   nvarchar(max) IpAddress
   nvarchar(max) UserAgent
   datetime2 Timestamp
   int Id
}
class Comments {
   nvarchar(1000) Text
   datetime2 CreatedAt
   int TaskId
   nvarchar(450) UserId
   int Id
}
class FileAttachments {
   int TaskId
   nvarchar(255) FileName
   nvarchar(255) FilePath
   nvarchar(450) UploadedById
   datetime2 UploadedAt
   bigint FileSize
   nvarchar(100) ContentType
   int Id
}
class Notifications {
   nvarchar(200) Title
   nvarchar(1000) Message
   datetime2 CreatedAt
   bit IsRead
   int Type
   nvarchar(450) UserId
   int TaskId
   int ProjectId
   int Id
}
class ProjectTeamMembers {
   int ProjectId
   nvarchar(450) TeamMembersId
}
class Projects {
   nvarchar(100) Title
   nvarchar(100) Name
   nvarchar(max) Description
   datetime2 Deadline
   nvarchar(450) CreatedById
   nvarchar(450) OwnerId
   int Status
   nvarchar(max) Priority
   datetime2 CreatedAt
   datetime2 UpdatedAt
   int Id
}
class Tasks {
   nvarchar(100) Title
   nvarchar(max) Description
   datetime2 DueDate
   nvarchar(max) Priority
   int Status
   int ProjectId
   nvarchar(450) AssignedUserId
   nvarchar(max) AssignedToId
   nvarchar(max) AssigneeId
   nvarchar(450) CreatedById
   datetime2 CreatedAt
   int Id
}
class __EFMigrationsHistory {
   nvarchar(32) ProductVersion
   nvarchar(150) MigrationId
}

AspNetRoleClaims  -->  AspNetRoles : RoleId:Id
AspNetUserClaims  -->  AspNetUsers : UserId:Id
AspNetUserLogins  -->  AspNetUsers : UserId:Id
AspNetUserRoles  -->  AspNetRoles : RoleId:Id
AspNetUserRoles  -->  AspNetUsers : UserId:Id
AspNetUserTokens  -->  AspNetUsers : UserId:Id
AuditLogs  -->  AspNetUsers : UserId:Id
Comments  -->  AspNetUsers : UserId:Id
Comments  -->  Tasks : TaskId:Id
FileAttachments  -->  AspNetUsers : UploadedById:Id
FileAttachments  -->  Tasks : TaskId:Id
Notifications  -->  AspNetUsers : UserId:Id
Notifications  -->  Projects : ProjectId:Id
Notifications  -->  Tasks : TaskId:Id
ProjectTeamMembers  -->  AspNetUsers : TeamMembersId:Id
ProjectTeamMembers  -->  Projects : ProjectId:Id
Projects  -->  AspNetUsers : OwnerId:Id
Projects  -->  AspNetUsers : CreatedById:Id
Tasks  -->  AspNetUsers : AssignedUserId:Id
Tasks  -->  AspNetUsers : CreatedById:Id
Tasks  -->  Projects : ProjectId:Id
