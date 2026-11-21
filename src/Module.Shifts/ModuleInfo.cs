using soft.square.net.ModularBlazor.Abstractions;

// Default settings of this module: read by the WebApi's client-modules core module when the file is uploaded.
// Open to every signed-in user: what each one may do is decided by the Shifts API (shifts.* permissions in the user's
// tenant, and for check-out the user's place on the rota), and the pages show its answer. The API is tenant-bound, so
// sign in to a tenant to use it.
[assembly: ClientModule(Name = "Shifts Management", Layout = "default", RoutePrefixes = new[] { "shifts" },
    Dependencies = new[] { "WebApi.Modules.Shifts.Client" }, Styles = new[] { "shifts.css" },
    NavTitle = "Shifts", NavHref = "shifts", NavOrder = 45)]
