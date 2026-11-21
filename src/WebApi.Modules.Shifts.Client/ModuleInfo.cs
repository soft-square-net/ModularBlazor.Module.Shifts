using soft.square.net.ModularBlazor.Abstractions;

// The Shifts API client as a hidden client module (catalog id "WebApi.Modules.Shifts.Client"): no pages, no menu
// entry; it is loaded when a module that lists it in Dependencies is, and those modules resolve its types from it.
[assembly: ClientModule(Name = "Shifts API client", Hidden = true)]
