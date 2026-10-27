// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Nalix.Dashboard;
using Nalix.Dashboard.Abstractions.Connection;
using Nalix.Dashboard.Abstractions.Settings;
using Nalix.Dashboard.Infrastructure.Browser;
using Nalix.Dashboard.State;
using Nalix.Dashboard.Transport;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

_ = builder.Services.AddSingleton<IObservabilityClient, NalixObservabilityClient>();
_ = builder.Services.AddSingleton<ISettingsStore, BrowserSettingsStore>();
_ = builder.Services.AddSingleton<IFileSaver, BrowserFileSaver>();
_ = builder.Services.AddSingleton<DashboardStore>();

await builder.Build().RunAsync();
