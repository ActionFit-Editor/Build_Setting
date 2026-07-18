# AI Guide - Build Setting

This file is shipped inside the UPM package so an AI assistant in a consuming Unity project can understand the package without access to the source project's `Docs/AI` folder.

## Package Identity

- Package ID: `com.actionfit.buildsetting`
- Display name: Build Setting
- Repository: `https://github.com/ActionFit-Editor/Build_Setting.git`
- Current package version at generation time: `1.1.12`
- Unity version: `6000.2`

## Purpose

Build Setting manages generic Android/iOS build/player settings for Unity projects. Use `README.md`, `package.json`, package source files, and `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` together to understand the user-facing workflow and catalog metadata.

## Agent Skills

- `Skills~/manifest.json` registers schema v2 read-only `build-settings-help` for Codex and Claude.
- Help reads the generated `PACKAGE_SKILLS.md` inventory before explaining `BuildSettingsSO`, platform settings, company profiles, Addressables prebuild, optional integrations, credential safety, and Build Automation ownership.
- Help must not open Unity windows, create or modify settings assets, apply `PlayerSettings`, start builds, read credential values, edit manifests or `.gitignore`, publish, tag, or update the package catalog.

## Project Router Registration

This package should be listed in `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md`.

Requested router entry:

- `Packages/com.actionfit.buildsetting/AI_GUIDE.md` - Build Setting manages generic Android/iOS build/player settings. Read when changing Android/iOS build setup, versioning, signing, Addressables prebuild, or build menu behavior.

If the router file is not already included in the AI assistant's default reading sequence, the router file is responsible for asking the user to link it from `Docs/AI/PROJECT.md` when available, or otherwise from `AGENTS.md`, `CLAUDE.md`, or another primary AI markdown entry point.

Read this file when:

- changing files under `Packages/com.actionfit.buildsetting/`
- diagnosing `Build Setting` behavior in a consuming project
- preparing a release for `com.actionfit.buildsetting`
- editing package metadata, README, AI guide, package version, or release notes

## Required Reading For AI

- Read this `AI_GUIDE.md` before changing, diagnosing, or explaining this package.
- Read `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md` when deciding which installed ActionFit package `AI_GUIDE.md` applies to a task.
- Read `README.md` for human-facing setup and usage.
- Read `package.json` for package ID, version, Unity version, and dependencies.
- Read `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` for catalog metadata, repository name, owner, status, description, release note, and dependency override.

## Editing Rules

- Keep changes scoped to this package unless the user explicitly asks for cross-package edits.
- Do not change package IDs, repository names, public menu paths, serialized field names, or package assembly names casually; these can affect installed projects.
- Preserve Unity `.meta` files when adding, moving, or renaming files inside the package.
- When behavior changes, update this `AI_GUIDE.md` in the same package before publishing so consuming projects receive the latest AI context.
- Keep `README.md` focused on human usage. Keep this file focused on AI-facing architecture, constraints, migration notes, and package-specific editing rules.

## Menu And Behavior Notes

- Setting window menu: `Tools/Package/Build Setting/Setting Window`.
- SO focus menu: `Tools/Package/Build Setting/Setting SO`.
- This package stores Android/iOS build settings in `BuildSettingsSO`.
- `BuildCompanySettingsSO` stores optional company profiles for `BuildSettingsSO`. It is created at `Assets/_Data/_BuildSetting/BuildCompanySettingsSO.asset`; the legacy `Assets/_Data/_BuildSetting/ActionFitBuildSetting_SO.asset` path is still read for migration.
- `BuildSettingsSO.companySettings` is a drag-and-drop reference to that company profile SO. The serialized field uses `[FormerlySerializedAs("actionFitBuildSetting")]` so existing assets migrate. The SettingWindow exposes a `Company Profile` popup with `Custom / Add Company`, `Custom / Manual`, and stored company profiles. `Add Company` opens a small editor window that saves a new company name / Development Team ID pair into `BuildCompanySettingsSO`. Stored profile selection synchronizes `companyName` with `developmentTeamId` and hides manual company fields. `BuildSettingsSO.useManualCompanyProfile` stores whether `Custom / Manual` is selected; manual mode disables profile auto-sync so both company name and Development Team ID remain editable.
- This public base package must not hardcode company-specific profiles. Company-specific defaults belong in separate private bootstrap packages.
- `BuildSettingsSO.iosTargetOSVersion` stores the iOS Deployment Target. The SettingWindow exposes it as `Target iOS Version`, applies it to `PlayerSettings.iOS.targetOSVersionString`, and iOS Xcode post-processing writes it to `IPHONEOS_DEPLOYMENT_TARGET`. The default remains `13.0` for existing behavior.
- `BuildSettingsSO.associatedDomains` stores iOS Associated Domains entitlement entries such as `applinks:actionfit.sng.link`. The SettingWindow exposes it as a string list under iOS Capabilities, and iOS Xcode post-processing writes non-empty, trimmed, de-duplicated entries to the generated entitlements file. The Apple App ID and provisioning profile must also have the Associated Domains capability enabled.
- `BuildSettingsSO.developmentBuild` is an independent, default-off build option. The SettingWindow always exposes it, including when Custom Symbols is installed. `AOSBuildProcess` and `iOSBuildProcess` OR `BuildOptions.Development` into the existing options so `AutoRunPlayer` and `AcceptExternalModificationsToPlayer` are preserved. Do not conflate it with the legacy `isDevMode` field or the `DEV` scripting define.
- `BuildSettingsSO.FindOrCreateSettingsAsset()` should find the last-used or first project asset, and create `Assets/_Data/_BuildSetting/BuildSettingsSO.asset` only when none exists.
- Do not add `Assets/_Data/_BuildSetting/BuildSettingsSO.asset` or its `.meta` file to a consuming project's `.gitignore`. BuildCommit CI starts from a clean checkout and requires this asset to exist, so both files must remain tracked in Git.
- `BuildSettingsSO` can hold temporary BuildCommit credential overrides, but that is not a reason to ignore the entire asset. Keep credential override fields empty in committed assets and provide secrets through the Build Automation request or CI secret environment instead.
- When diagnosing `BuildSettingsSO not found` in CI, first remove any matching `.gitignore` rules, generate or locate the settings asset through `FindOrCreateSettingsAsset()`, and commit both the asset and `.meta` before rerunning BuildCommit.
- The first auto-created `BuildSettingsSO` should initialize user-editable identity/version fields from current `PlayerSettings` values without overwriting existing assets.
- `BuildSettingsSO` also stores temporary BuildCommit request override fields for Google Play service account JSON and App Store Connect API key id, issuer id, and P8.
- It applies settings to Unity `PlayerSettings` and build execution.
- Firebase config file lookup should search inside `Assets`.
- Build Setting does not create automatic build requests by itself. BuildCommit, request JSON, Git tag CI triggers, workflow templates, and runner guidance live in `com.actionfit.buildautomation`.
- Treat build tools as release workflow tools. Do not run build actions during normal content validation unless the task explicitly targets release/build behavior.

## Package Tools Menu

- Unity menu root: `Tools/Package/Build Setting/`.
- Keep package commands under this package root.
- Lower separated entries:
- `Setting SO`: focuses this package's settings ScriptableObject.
- `README`: opens this package README.
- Do not add README or Setting SO access back to Custom Package Manager package rows or Project Files.

## Release Note Rules

- `ActionFitPackageInfo_SO.ReleaseNote` must contain only the single version being prepared.
- Do not copy older changelog entries into the newest release note.
- Version history and update-range summaries are composed by Custom Package Manager from separate catalog version rows.
- Do not add headings such as `## 1.0.0` inside ReleaseNote unless a specific package UI requires it; the catalog row already carries the version.
## Publish Notes

- Publishing is manual through Custom Package Manager.
- Before reusing a version, check the remote Git tags. Published tags are immutable.
- If this package is modified after a version was tagged, bump to the next unused patch version before publishing.
- The package repository should include this `AI_GUIDE.md` so other projects can load the AI package context after installing the package.
