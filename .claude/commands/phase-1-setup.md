---
title: Phase 1 — Project Setup
description: Initialize WordBuddy solution, git, and CLAUDE.md
status: draft
---

## Context

WordBuddy is an English learning web app for children and adults.
Stack: .NET 8, SQL Server, React 18, Docker, AWS EC2.
This phase sets up the project skeleton before any code is written.

## Requirements

- [ ] .NET 8 solution created with 5 projects and correct references
- [ ] git initialized with .gitignore
- [ ] CLAUDE.md written with stack, conventions, and project structure
- [ ] README.md created with project overview
- [ ] First git commit made

## Implementation Plan

### Step 1 — Create solution structure

Run these dotnet CLI commands:
dotnet new sln -n WordBuddy
dotnet new webapi -n WordBuddy.API -o src/WordBuddy.API
dotnet new classlib -n WordBuddy.Application -o src/WordBuddy.Application
dotnet new classlib -n WordBuddy.Domain -o src/WordBuddy.Domain
dotnet new classlib -n WordBuddy.Infrastructure -o src/WordBuddy.Infrastructure
dotnet new xunit -n WordBuddy.Tests -o tests/WordBuddy.Tests
dotnet sln add src/WordBuddy.API
dotnet sln add src/WordBuddy.Application
dotnet sln add src/WordBuddy.Domain
dotnet sln add src/WordBuddy.Infrastructure
dotnet sln add tests/WordBuddy.Tests

Wire project references:

- API → Application, Infrastructure
- Application → Domain
- Infrastructure → Application
- Tests → Application, Domain, Infrastructure

### Step 2 — git init

`git init`

Create .gitignore for .NET + Node + VS Code + .env files.

### Step 3 — Write CLAUDE.md

Content must include:

- Project name: WordBuddy
- Purpose: English learning app for children and adults
- Features: vocabulary, grammar, daily phrases, quizzes, media (text/image/audio/video)
- Stack: ASP.NET Core .NET 8, EF Core, SQL Server, JWT, xUnit
- Conventions: clean architecture, Result<T>, async/await, SOLID,
  no var for non-obvious types, XML doc comments on public APIs
- Project structure with folder descriptions
- Domain entities: User, Lesson, VocabularyItem, GrammarRule,
  DailyPhrase, MediaAsset, LearnerProgress

### Step 4 — Write README.md

Include project overview, tech stack badges, folder structure,
getting started steps, and roadmap of all 5 phases.

### Step 5 — First commit

`git add .`
`git commit -m "chore: phase 1 — project setup + CLAUDE.md"`

## Verification

- `dotnet build` outputs: Build succeeded, 0 errors
- All 5 projects visible in solution
- CLAUDE.md exists at root with all required sections
- README.md exists at root
- `git log --oneline` shows the first commit
