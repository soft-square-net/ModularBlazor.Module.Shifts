using soft.square.net.ModularBlazor.Abstractions;

// Default settings of this module: read by the WebApi's client-modules core module when the file is uploaded.
// Open to every signed-in user: what each one may do is decided by the Shifts API (shifts.* permissions in the user's
// tenant, and for check-out the user's place on the rota), and the pages show its answer. The API is tenant-bound, so
// sign in to a tenant to use it.
[assembly: ClientModule(Name = "Shifts Management", Layout = "default", RoutePrefixes = new[] { "shifts" },
    Dependencies = new[] { "WebApi.Modules.Shifts.Client" }, Styles = new[] { "shifts.css" },
    NavTitle = "Shifts", NavHref = "shifts", NavOrder = 45)]

// The menu (it replaces the NavTitle entry): one group with a link per page.
[assembly: MenuGroup("shifts", "Shifts", Icon = "calendar-clock", Order = 45)]
[assembly: MenuLink("planner", "Planner", "shifts", Parent = "shifts", Icon = "calendar-days", Order = 10)]
[assembly: MenuLink("mine", "My shifts", "shifts/mine", Parent = "shifts", Icon = "user-check", Order = 20)]
[assembly: MenuLink("templates", "Templates", "shifts/templates", Parent = "shifts", Icon = "copy", Order = 30)]
[assembly: MenuLink("roles", "Roles", "shifts/roles", Parent = "shifts", Icon = "badge-check", Order = 40)]
[assembly: MenuLink("attendance", "Attendance rules", "shifts/attendance", Parent = "shifts", Icon = "timer", Order = 50)]
[assembly: MenuLink("sections", "Moved sections", "shifts/sections", Parent = "shifts", Icon = "archive", Order = 60)]
