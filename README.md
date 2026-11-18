## Build setup and gotchas

### First-time setup
1. Install the .NET SDK. Any recent version works; the mods target `net6.0` regardless.
2. Copy `GameDir.props.user.example` to `GameDir.props.user` and set `GameDir` to the
   absolute path of the game's install folder (Steam: right-click game > Manage >
   Browse local files). The file is git-ignored.
   Alternatively, set the `KNIGHTFALL_DIR` environment variable.
3. Launch the game once with MelonLoader installed so it generates
   `MelonLoader/Il2CppAssemblies/`.
4. Run `dotnet build`. Output DLLs are copied to `<GameDir>/Mods/`.

### How the build is configured
Shared settings (target framework, language version, game references, and the copy-to-Mods
step) live in `Directory.Build.props`. Individual `.csproj` files should contain only what is
specific to that mod.

### Gotchas

- **New projects override the shared settings.** `dotnet new classlib` generates a csproj with
  its own `TargetFramework` (e.g. `net10.0`), `ImplicitUsings`, and `Nullable`. Values in the
  csproj are evaluated after `Directory.Build.props` and win, which would silently build the
  mod for the wrong runtime. After creating a project, delete those lines so only the empty
  `<Project Sdk="Microsoft.NET.Sdk">` shell remains. Override a setting in a csproj only on
  purpose.
- **Every mod references every game DLL.** The wildcard references in `Directory.Build.props`
  apply to all projects. If ambiguous-type or conflicting-type errors appear, narrow the list
  there (for example `MelonLoader.dll`, `0Harmony.dll`, `Il2CppInterop.Runtime.dll`,
  `Assembly-CSharp.dll`, the `UnityEngine.*Module` DLLs, and the Photon assemblies).
- **Wildcard mistakes show up late.** A bad glob only surfaces as missing-type errors once
  code uses those types, not as a build error on the reference itself.
- **Shared code needs to be deployed too.** If mods share helper code via a common project
  (`<ProjectReference>`), that DLL must also be present at runtime: copy it into `Mods/`
  alongside the others or place it in MelonLoader's `UserLibs/` folder.
- **Clear errors on bad paths.** The build fails early with a readable message if `GameDir`
  is unset or has no `MelonLoader` folder. Check the path, and confirm the game has been run
  once with MelonLoader installed.
- **Nullable is disabled** because the generated Il2Cpp assemblies aren't annotated for
  nullability and enabling it mostly produces warning noise.