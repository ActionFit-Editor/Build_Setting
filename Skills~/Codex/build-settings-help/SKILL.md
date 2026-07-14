---
name: build-settings-help
description: Explain ActionFit Build Setting, its installed skills, BuildSettingsSO configuration, Android and iOS PlayerSettings application, company profiles, Addressables prebuild, optional integrations, credential safety, and release boundaries. Use when a user asks how the Build Setting package works or which package skill applies.
---

# ActionFit Build Setting Help

Answer in the user's language. Explain the package without opening Unity windows, creating or changing settings assets, applying `PlayerSettings`, starting a build, reading credential values, or running a package audit unless the user separately requests that operation.

1. When `PACKAGE_SKILLS.md` is present, read it first. Treat its generated package identity, complete related-skill table, `$skill-name` invocations, descriptions, and access boundaries as authoritative. In a source checkout before installation, it may not exist; read `Skills~/manifest.json` only to identify the packaged skills, explain that the inventory is generated during installation, and do not create the inventory yourself.
2. Read `Packages/com.actionfit.buildsetting/README.md` and `Packages/com.actionfit.buildsetting/AI_GUIDE.md` when embedded. If downloaded, resolve `Library/PackageCache/com.actionfit.buildsetting@*` without editing it.
3. Explain `BuildSettingsSO`, the `Tools > Package > Build Setting` menus, Android/iOS `PlayerSettings` application, build paths and versioning, Addressables prebuild, `BuildCompanySettingsSO`, and optional Custom Symbols or GPGS integration only to the depth the user needs.
4. Keep company-specific defaults in bootstrap packages and keep remote BuildCommit requests, workflow templates, runner setup, signing distribution, and deployment in `com.actionfit.buildautomation`.
5. Treat Google Play service account JSON, App Store Connect key IDs, issuer IDs, P8 contents, passwords, certificates, profiles, and signing values as sensitive. Explain where they belong without reading, printing, copying, or changing their values.
6. State that the help skill must not create `BuildSettingsSO`, change `PlayerSettings`, modify manifests or `.gitignore`, invoke build or post-process code, publish, tag, or update the package catalog. Mention package contract validation and isolated Unity compilation only as follow-up checks; do not run them from this skill.
