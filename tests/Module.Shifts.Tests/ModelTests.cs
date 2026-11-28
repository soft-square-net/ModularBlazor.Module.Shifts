namespace soft.square.net.Module.Shifts.Tests;

public class UnitTreeTests
{
    private static readonly Guid Root = Guid.NewGuid();
    private static readonly Guid News = Guid.NewGuid();
    private static readonly Guid Politics = Guid.NewGuid();
    private static readonly Guid Art = Guid.NewGuid();

    private static UnitDto Unit(Guid id, Guid? parent, string name, string path, string? zone = null, string nameAr = "") =>
        new() { Id = id, OrgId = Root, ParentId = parent, NameEn = name, NameAr = nameAr, Path = path, TypeKey = "department", TimeZone = zone };

    private static UnitTree Tree() => UnitTree.Build(
    [
        Unit(Politics, News, "Politics", "/o/n/p/"),
        Unit(News, Root, "News", "/o/n/", "Africa/Cairo", "الأخبار"),
        Unit(Art, Root, "Art", "/o/a/"),
        Unit(Root, null, "Org", "/o/", "Europe/Berlin"),
    ]);

    [Fact]
    public void Units_come_in_tree_order_with_siblings_by_name()
    {
        Tree().All().Select(n => (n.Unit.NameEn, n.Level)).ShouldBe([("Org", 0), ("Art", 1), ("News", 1), ("Politics", 2)]);
    }

    [Fact]
    public void A_unit_whose_parent_is_not_cached_is_a_root()
    {
        var tree = UnitTree.Build([Unit(Politics, News, "Politics", "/o/n/p/")]);

        tree.Roots.Count.ShouldBe(1);
        tree.SelfAndBelow(News).ShouldBe([News]);
    }

    [Fact]
    public void Self_and_below_walks_the_subtree()
    {
        Tree().SelfAndBelow(News).ShouldBe([News, Politics], ignoreOrder: true);
        Tree().SelfAndBelow(Root).Count.ShouldBe(4);
    }

    [Fact]
    public void The_time_zone_is_the_units_or_the_nearest_ancestors()
    {
        Tree().TimeZoneIdOf(Politics).ShouldBe("Africa/Cairo");
        Tree().TimeZoneIdOf(Art).ShouldBe("Europe/Berlin");
        Tree().TimeZoneIdOf(null).ShouldBeNull();
    }

    [Fact]
    public void Names_follow_the_language_with_english_as_the_fallback()
    {
        var tree = Tree();

        UnitTree.NameOf(tree.Find(News)!, arabic: true).ShouldBe("الأخبار");
        UnitTree.NameOf(tree.Find(Art)!, arabic: true).ShouldBe("Art");
        UnitTree.NameOf(tree.Find(News)!, arabic: false).ShouldBe("News");
    }
}

public class ZoneTimeTests
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private static DateTimeOffset Utc(int y, int m, int d, int h, int min = 0) => new(y, m, d, h, min, 0, TimeSpan.Zero);

    [Fact]
    public void A_local_time_is_the_units_wall_clock()
    {
        ZoneTime.ToUtc(new DateTime(2026, 7, 1, 9, 0, 0), Berlin).ShouldBe(Utc(2026, 7, 1, 7));
        ZoneTime.ToUtc(new DateTime(2026, 12, 1, 9, 0, 0), Berlin).ShouldBe(Utc(2026, 12, 1, 8));
    }

    [Fact]
    public void A_skipped_time_moves_forward_and_a_repeated_one_is_the_first()
    {
        ZoneTime.ToUtc(new DateTime(2026, 3, 29, 2, 30, 0), Berlin).ShouldBe(Utc(2026, 3, 29, 1, 30)); // 03:30 summer time
        ZoneTime.ToUtc(new DateTime(2026, 10, 25, 2, 30, 0), Berlin).ShouldBe(Utc(2026, 10, 25, 0, 30)); // still summer time
    }

    [Fact]
    public void A_week_runs_from_local_midnight_to_local_midnight()
    {
        var (from, to) = ZoneTime.Week(new DateOnly(2026, 3, 23), Berlin); // the clocks change inside it

        from.ShouldBe(Utc(2026, 3, 22, 23));
        to.ShouldBe(Utc(2026, 3, 29, 22));
    }

    [Theory]
    [InlineData(DayOfWeek.Saturday, "2026-11-21")]
    [InlineData(DayOfWeek.Sunday, "2026-11-22")]
    [InlineData(DayOfWeek.Monday, "2026-11-23")]
    public void The_week_starts_on_the_cultures_first_day(DayOfWeek first, string expected)
    {
        ZoneTime.Date(ZoneTime.WeekStart(new DateOnly(2026, 11, 27), first)).ShouldBe(expected);
    }

    [Fact]
    public void An_unknown_zone_falls_back_and_says_so()
    {
        ZoneTime.Find("Europe/Berlin").Known.ShouldBeTrue();
        ZoneTime.Find("Nowhere/Never").ShouldBe((TimeZoneInfo.Utc, false));
        ZoneTime.Find(null).ShouldBe((TimeZoneInfo.Utc, false));
    }

    [Fact]
    public void A_range_names_the_end_date_only_when_it_is_another_day()
    {
        ZoneTime.Range(Utc(2026, 12, 1, 8), Utc(2026, 12, 1, 16), Berlin).ShouldBe("09:00 – 17:00");
        ZoneTime.Range(Utc(2026, 12, 1, 21), Utc(2026, 12, 2, 5), Berlin).ShouldBe("22:00 – 12-02 06:00");
    }
}

public class ShiftStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);

    private static ShiftDto Shift(bool published = true, int startsIn = -1, int endsIn = 7, params RotaMemberDto[] rota) => new()
    {
        Published = published,
        StartsAt = Now.AddHours(startsIn),
        EndsAt = Now.AddHours(endsIn),
        CheckInOpensAt = Now.AddHours(startsIn).AddMinutes(-15),
        Rota = rota,
    };

    private static RotaMemberDto Member(string id, bool @in = false, bool @out = false) => new()
    {
        UserId = id,
        RoleKey = "desk",
        Status = "expected",
        CheckedInAt = @in ? Now : null,
        CheckedOutAt = @out ? Now : null,
    };

    [Fact]
    public void Every_status_is_listed()
    {
        string[] seen =
        [
            ShiftState.Status(Shift(false, 1, 9), Now),
            ShiftState.Status(Shift(true, 1, 9), Now),
            ShiftState.Status(Shift(), Now),
            ShiftState.Status(Shift(false), Now),
            ShiftState.Status(Shift(true, -9, -1), Now),
            ShiftState.Status(Shift(false, -9, -1), Now),
        ];

        seen.ShouldBe(["Planned", "Published", "Running", "Running, not published", "Overdue", "Ended, not published"]);
        seen.ShouldAllBe(s => ShiftState.Statuses.Contains(s));
    }

    [Fact]
    public void Check_in_is_open_from_the_early_threshold_to_the_end_of_a_published_shift()
    {
        ShiftState.CanCheckIn(Shift(true, 0, 8, Member("me")), "me", Now).ShouldBeTrue();
        ShiftState.CanCheckIn(Shift(true, 1, 8, Member("me")), "me", Now).ShouldBeFalse(); // opens in 45 minutes
        ShiftState.CanCheckIn(Shift(false, 0, 8, Member("me")), "me", Now).ShouldBeFalse();
        ShiftState.CanCheckIn(Shift(true, -9, -1, Member("me")), "me", Now).ShouldBeFalse();
        ShiftState.CanCheckIn(Shift(true, 0, 8, Member("me", @in: true)), "me", Now).ShouldBeFalse();
        ShiftState.CanCheckIn(Shift(true, 0, 8, Member("me", @in: true, @out: true)), "me", Now).ShouldBeTrue();
        ShiftState.CanCheckIn(Shift(true, 0, 8, Member("other")), "me", Now).ShouldBeFalse();
    }

    [Fact]
    public void Presence_counts_people_not_rota_rows()
    {
        ShiftState.Presence(Shift(true, 0, 8, Member("a", @in: true), Member("a", @in: true), Member("b"))).ShouldBe((1, 2));
    }
}

public class RoleAndPermissionTests
{
    private static readonly Func<string, string> Same = key => key;

    [Fact]
    public void Without_definitions_the_seeded_roles_are_offered()
    {
        RoleChoices.For([], null, arabic: false, Same).Select(c => c.Key).ShouldBe(["manager", "desk", "editor"]);
    }

    [Fact]
    public void A_role_a_member_already_has_stays_among_the_choices()
    {
        RoleDefinitionDto[] roles = [new() { Key = "desk", NameEn = "Desk", NameAr = "المكتب" }];

        RoleChoices.For(roles, "night", arabic: false, Same).Select(c => c.Key).ShouldBe(["desk", "night"]);
        RoleChoices.Name(roles, "desk", arabic: true, Same).ShouldBe("المكتب");
    }

    [Fact]
    public void Only_a_refused_change_of_the_lead_flag_is_read_as_one()
    {
        RoleConflicts.IsLeadRefusal(409, 4, isLead: true, wasLead: false).ShouldBeTrue();
        RoleConflicts.IsLeadRefusal(409, 4, isLead: true, wasLead: true).ShouldBeFalse();
        RoleConflicts.IsLeadRefusal(409, null, isLead: true, wasLead: false).ShouldBeFalse(); // a new role: the key is taken
        RoleConflicts.IsLeadRefusal(400, 4, isLead: true, wasLead: false).ShouldBeFalse();
    }

    [Fact]
    public void A_tenant_wide_heir_keeps_the_key_and_the_lead_flag()
    {
        var unitRole = new RoleDefinitionDto { Id = 7, Key = "night", NameEn = "Night", NameAr = "ليل", IsLead = true, Permissions = ["shifts.view"], UnitId = Guid.NewGuid() };

        var heir = RoleForm.HeirOf(unitRole);

        heir.Id.ShouldBeNull();
        var request = heir.ToRequest();
        (request.Key, request.IsLead, request.UnitId).ShouldBe(("night", true, null));
        request.Permissions.ShouldBe(["shifts.view"]);
    }

    [Fact]
    public void The_catalog_is_sorted_and_empty_groups_are_left_out()
    {
        var catalog = PermissionCatalog.Parse("""
            [ { "moduleId": "z", "moduleName": "Zeta", "permissions": [ { "name": "z.b" }, { "name": "z.a", "description": "A" } ] },
              { "moduleId": "empty", "moduleName": "Empty", "permissions": [] },
              { "moduleId": "a", "permissions": [ { "name": "a.view" } ] } ]
            """);

        catalog.Select(g => g.ModuleName).ShouldBe(["a", "Zeta"]);
        catalog[1].Permissions.Select(p => p.Name).ShouldBe(["z.a", "z.b"]);
    }

    [Theory]
    [InlineData("shifts.view", true)]
    [InlineData("content.posts.publish", true)]
    [InlineData("Shifts.View", false)]
    [InlineData("shifts view", false)]
    [InlineData(".view", false)]
    [InlineData("", false)]
    public void Typed_permission_names_are_checked(string name, bool valid) => PermissionCatalog.IsValidName(name).ShouldBe(valid);
}
