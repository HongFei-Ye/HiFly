// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HiFly.Table.DataSources;
using Sandbox.UI.WasmHost;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// v5.5 §7.7 P3 — WASM Render 模式：HttpTItemDataSource 是唯一的合法 TItemTable 数据源
builder.Services.AddScoped<HttpTItemDataSource<DummyDto>>();
builder.Services.AddScoped<ITItemDataSource<DummyDto>>(sp => sp.GetRequiredService<HttpTItemDataSource<DummyDto>>());

await builder.Build().RunAsync();
