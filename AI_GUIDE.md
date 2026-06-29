# AI Guide - Build Setting

This file is shipped inside the UPM package so an AI assistant in a consuming Unity project can understand the package without access to the source project's `Docs/AI` folder.

## Package Identity

- Package ID: `com.actionfit.buildsetting`
- Display name: Build Setting
- Repository: `https://github.com/ActionFit-Editor/Build_Setting.git`
- Current package version at generation time: `1.1.0`
- Unity version: `6000.2`

## Purpose

Build Setting manages ActionFit build/player settings. Use `README.md`, `package.json`, package source files, and `Editor/PackageInfo/ActionFitPackageInfo_SO.asset` together to understand the user-facing workflow and catalog metadata.

## Project Router Registration

This package should be listed in `Packages/com.actionfit.custompackagemanager/PACKAGE_AI_GUIDE_ROUTER.md`.

Requested router entry:

- `Packages/com.actionfit.buildsetting/AI_GUIDE.md` - Build Setting manages ActionFit build/player settings. Read when changing Android/iOS build setup, versioning, signing, Addressables prebuild, or build menu behavior.

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

- Main menu: `Tools/ActionFit/Build Setting`.
- This package stores Android/iOS build settings in `BuildSettingsSO`.
- It applies settings to Unity `PlayerSettings` and build execution.
- Firebase config file lookup should search inside `Assets`.
- Build Setting does not create automatic build requests by itself. BuildCommit, request JSON, Git tag CI triggers, workflow templates, and runner guidance live in `com.actionfit.buildautomation`.
- Treat build tools as release workflow tools. Do not run build actions during normal content validation unless the task explicitly targets release/build behavior.

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
