namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

/// <summary>Column lengths shared by several mappings.</summary>
public static class DataSchemaConstants
{
    /// <summary>Section names.</summary>
    public const int NameLength = 200;
    /// <summary>Category ids of the content module (slugs).</summary>
    public const int SlugLength = 64;
    /// <summary>User ids (the token's <c>sub</c>).</summary>
    public const int UserIdLength = 128;
    /// <summary>Rota roles, stored by name.</summary>
    public const int RoleLength = 16;
    /// <summary>Section kinds, stored by name.</summary>
    public const int KindLength = 16;
}
