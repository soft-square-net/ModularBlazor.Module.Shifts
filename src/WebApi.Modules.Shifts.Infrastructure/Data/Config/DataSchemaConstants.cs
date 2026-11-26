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
    /// <summary>Organization unit paths (32 characters and a slash per level).</summary>
    public const int PathLength = 1000;
    /// <summary>Organization unit type keys.</summary>
    public const int TypeKeyLength = 64;
    /// <summary>IANA time zone ids.</summary>
    public const int TimeZoneLength = 64;
    /// <summary>A unit's tags or categories, as a JSON array of strings.</summary>
    public const int ListLength = 2000;
}
