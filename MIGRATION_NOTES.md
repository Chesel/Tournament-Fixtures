# .NET 8 → .NET Framework 4.8 Migration Notes

## Status: conversion complete

Every controller, view, and model file has been ported. Nothing runs ASP.NET Core
or EF Core anymore. What's left is: open it in Visual Studio, restore NuGet
packages, build, fix whatever the compiler flags (I can't compile `System.Web`
projects in my own environment to pre-verify this), and test against a real
database.

## Why this wasn't a simple retarget

ASP.NET Core stopped supporting .NET Framework after version 2.x (that line loses
support entirely in April 2027). EF Core stopped supporting .NET Framework after
3.1 (EOL Dec 2022). So this moves to what actually runs on .NET Framework 4.8 with
active Microsoft support:

| Was (net8.0) | Now (net48) |
|---|---|
| ASP.NET Core MVC | Classic ASP.NET MVC 5 (`System.Web.Mvc`) |
| Minimal hosting (`Program.cs`) | `Global.asax` / `Application_Start` |
| Built-in DI container | Autofac + `Autofac.Mvc5` |
| Cookie auth middleware | `FormsAuthentication` |
| `HttpContext.Session.SetString/GetString` | `Session["key"]` |
| EF Core 9 | EF6 |
| `IFormFile` | `HttpPostedFileBase` |
| Tag Helpers (`asp-for`, etc.) | HTML Helpers (`Html.TextBoxFor`, etc.) |
| `_ViewImports.cshtml` | `Views/Web.config` (MVC5's actual namespace-import mechanism) |
| `wwwroot/` auto-mapped to `/` | Moved contents to root-level `lib/`, `css/`, `js/`, `images/` to match the `~/lib/...`-style paths already in the views |

## What changed file by file

**Every controller** (`AuthController`, `HomeController`, `AccountController`,
`DistrictsController`, `TournamentController`, `ExcelUploadController`,
`PlayerRegistration`) — `IActionResult`→`ActionResult`, EF Core async methods→EF6
equivalents (mostly identical method names), `IFormFile`→`HttpPostedFileBase`,
`Request.Scheme`/`Request.Host`→`Request.Url.Scheme`/`Request.Url.Authority`,
`BadRequest()`/`NotFound()`/`Ok()`→`HttpStatusCodeResult`/`HttpNotFound()`.

**One real bug fix, not just syntax**: `PlayerRegistration.GetClubsByDistrict` used
to return a plain `List<SelectListItem>` directly from a public action method.
ASP.NET Core auto-serializes that to JSON via content negotiation; MVC5 does not.
The view's AJAX call to `/PlayerRegistration/GetClubsByDistrict` would have quietly
gotten back garbage instead of JSON. Split into a private helper (used internally)
and a `[HttpGet]` action that explicitly returns `Json(..., JsonRequestBehavior.AllowGet)`.

**`ApplicationDbContext`** — ported to EF6. The two `HasNoKey()` entities
(`TblDistLocalClub`, `TblDistrict`) now use their real identity-column keys, since
EF6 doesn't support keyless entities the way EF Core does, and `ClubId`/`DistictId`
were genuinely identity columns anyway.

**Views** — every `asp-for`/`asp-action`/`asp-validation-for`/`asp-items` swapped
for the matching `Html.TextBoxFor`/`Html.BeginForm`/`Html.ValidationMessageFor`/
`Html.DropDownListFor`. The 21 `Nplayer.cshtml` bracket templates in
`Views/ExcelUpload/` needed **zero changes** — they're pure HTML/CSS/Razor with no
ASP.NET Core-specific syntax at all.

## Pre-existing quirks carried forward as-is (not introduced by this conversion)

I ported behavior faithfully rather than fixing unrelated bugs along the way.
Worth knowing about, in rough order of how much they matter:

- **`AuthController` login is `admin`/`password` hardcoded** — this is a real login
  gate for a live app with effectively no authentication. Worth replacing with a
  real credential check.
- **A dead method with a live credential**: `PlayerRegistration.GenerateIdCardPdf`
  is never called (only referenced from a commented-out line) but sends an email
  using the same Gmail password as the rest of the app. Ported syntactically since
  deleting business logic wasn't asked for, but it's a good candidate for outright
  removal.
- `HomeController.EditGender(int id)` returns `View(gender)` but there's no
  `Views/Home/EditGender.cshtml` in the project — this route was already broken
  before conversion.
- `Views/Home/DistrictManagement.cshtml`'s "Add New District" link points at
  `Districts/DistrictManagement`, which doesn't exist (`DistrictsController` only
  has `Register`) — likely meant to be `Districts/Register`.
- `Views/Tournament/Register.cshtml` reuses the `DistictId` field for a "Tournament
  Type" dropdown alongside its real use for district selection.

None of these break the build — they're runtime behavior quirks that predate the
.NET 8 version too.

## ⚠️ Rotate these credentials before deploying

Two real secrets were sitting in plaintext in the source you uploaded:
1. **Azure SQL admin password**, hardcoded in the old `ApplicationDbContext.cs`
2. **Gmail app password**, in `appsettings.json`'s `EmailSettings:FromPassword`,
   and duplicated inline in `PlayerRegistration.GenerateIdCardPdf`

Both are removed/placeholdered in these files. Rotate both in Azure Portal and your
Google Account regardless of anything else here, since they were already exposed
in the zip you sent.

## Build verification

I don't have Windows/IIS/MSBuild available to compile a `System.Web` project, so
none of this has been build-tested. Open it in Visual Studio, restore NuGet
packages, build, and send me the error list — that's the fastest way to close out
anything I got wrong (EF6 Fluent API syntax and the Autofac wiring are the parts
most worth double-checking against a real compiler).

## Deployment checklist (see chat for the full explanation)

1. Build in Visual Studio (or `dotnet build` / `msbuild` if you have the .NET
   Framework 4.8 targeting pack installed) — this regenerates `bin/`/`obj/`.
2. Set the real (rotated) connection string and email password in `Web.config`.
3. Publish (Web Deploy / Zip Deploy / FTP — whatever HostingRaja's panel supports)
   and upload the **published output**, not this source tree, to the host.
