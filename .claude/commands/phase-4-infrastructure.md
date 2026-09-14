---
title: Phase 4 — Infrastructure
description: Docker, local media storage, AWS EC2 provisioning
status: draft
---

## Context

Cloud: AWS (region: ap-southeast-1, Singapore).
Media storage: local filesystem (EBS volume), migrate to S3 later.
Architecture: EC2 t3.small running Docker Compose with 3 services:
wordbuddy-api, wordbuddy-ui (Nginx), sqlserver.

## Requirements

- [ ] Docker Desktop installed and running
- [ ] Backend Dockerfile (multi-stage, non-root user, port 8080)
- [ ] Frontend Dockerfile (multi-stage, Nginx, SPA routing, /api proxy)
- [ ] nginx.conf: SPA fallback, /api proxy, gzip, cache headers
- [ ] IFileStorageService interface + LocalFileStorageService implementation
- [ ] MediaController: POST /api/media/upload, GET /api/media/{id}
- [ ] docker-compose.yml: sqlserver, wordbuddy-api, wordbuddy-ui
- [ ] .env.example committed, .env in .gitignore
- [ ] Makefile: up, down, logs, migrate, clean
- [ ] AWS CLI configured (ap-southeast-1)
- [ ] EC2 security group: SSH (my IP), HTTP 80, HTTPS 443
- [ ] EC2 t3.small created with Ubuntu 22.04 + Docker user data
- [ ] Key pair saved to ~/.ssh/wordbuddy-key.pem
- [ ] Deploy script: SCP files → SSH → docker compose up
- [ ] Both CLAUDE.md + README.md updated

## Implementation Plan

### Step 1 — Backend Dockerfile

At solution root, create Dockerfile:
Stage 1 (sdk:8.0): restore + publish Release → /app/publish.
Stage 2 (aspnet:8.0): copy publish, create appuser, EXPOSE 8080,
ASPNETCORE_URLS=http://+:8080, VOLUME /app/media.
Create .dockerignore: **/bin,**/obj, **/.vs, .git,**/*.user.

### Step 2 — Frontend Dockerfile

At WordBuddy.UI root, create Dockerfile:
Stage 1 (node:20-alpine): npm ci + npm run build.
Stage 2 (nginx:alpine): copy dist + nginx.conf, EXPOSE 80.
nginx.conf: try_files for SPA, proxy_pass /api → wordbuddy-api:8080,
gzip on, cache 1yr for hashed assets, no-cache for index.html.
Create .dockerignore: node_modules, dist, .env*, .git.

### Step 3 — Local media storage

In WordBuddy.Application, create IFileStorageService:

- SaveAsync(Stream, fileName, contentType, ct) → returns relative path
- GetAsync(filePath, ct) → returns Stream
- DeleteAsync(filePath, ct)

In WordBuddy.Infrastructure, create LocalFileStorageService:

- Reads FileStorage:BasePath from config
- SaveAsync: generates Guid filename, saves to BasePath/{type}/{year}/{month}/
- Inject ILogger<LocalFileStorageService>

In WordBuddy.API, create MediaController:

- POST /api/media/upload: validate IFormFile (max 50MB),
  allowed types: image/jpeg, image/png, audio/mpeg, audio/wav, video/mp4,
  save via IFileStorageService, create MediaAsset record, return DTO
- GET /api/media/{id}: stream file with FileStreamResult + Content-Type
- [Authorize] on upload, anonymous on GET

appsettings.Development.json: FileStorage:BasePath = C:\\WordBuddyMedia.

### Step 4 — Docker Compose

docker-compose.yml at solution root:

- sqlserver: mcr.microsoft.com/mssql/server:2022-latest,
  volume sqlserver-data:/var/opt/mssql, health check, port 1433
- wordbuddy-api: build from root, depends_on sqlserver (healthy),
  env from .env, port 8080:8080, volume media-data:/app/media
- wordbuddy-ui: build from WordBuddy.UI path, depends_on wordbuddy-api,
  port 3000:80
.env.example with SA_PASSWORD, JWT_SECRET, JWT_ISSUER,
JWT_EXPIRY_MINUTES, CONNECTION_STRING.
Add .env to .gitignore.
Makefile: up, down, logs, migrate, clean targets.

### Step 5 — AWS setup

Install AWS CLI v2 for Windows (AWSCLIV2.msi).
`aws configure`: access key, secret, region ap-southeast-1, output json.

aws/03-create-key-pair.ps1:

- Create key pair wordbuddy-key
- Save to ~/.ssh/wordbuddy-key.pem with correct icacls permissions

aws/01-create-security-group.ps1:

- Create wordbuddy-sg, inbound: SSH from my IP, HTTP 80, HTTPS 443

aws/02-create-ec2.ps1:

- AMI: ami-0df7a207adb9748c7 (Ubuntu 22.04 ap-southeast-1)
- Type: t3.small, key: wordbuddy-key, sg: wordbuddy-sg, EBS 20GB gp3
- User data: apt update, install docker + compose plugin,
  add ubuntu to docker group, mkdir /app/wordbuddy, enable docker on boot
- Tag: Name=wordbuddy-prod, output public IP

Run order: 03 → 01 → 02. Save EC2 IP to aws/config.env.

### Step 6 — Deploy script

aws/04-deploy.ps1:

- Read EC2_IP from aws/config.env
- SCP docker-compose.yml, .env, Dockerfile, src/, UI path to EC2
- SSH: docker compose up -d --build
- SSH: docker compose exec wordbuddy-api dotnet ef database update
- SSH: docker compose ps
- Print app URL: http://$EC2_IP

### Step 7 — Docs

Update C:\Projects\WordBuddy CLAUDE.md + README.md:
architecture diagram, docker section, AWS deploy steps,
env vars table, media upload docs.
Update WordBuddy.UI CLAUDE.md + README.md:
Docker/Nginx section, production build notes.

### Step 8 — Commit

Backend

```batch
git add .
git commit -m "feat: phase 4 — docker, media storage, aws ec2 scripts"
```

Frontend (WordBuddy.UI)

```batch
git add .
git commit -m "feat: phase 4 — dockerfile + nginx config"
```

## Verification

- `docker compose up` → all 3 services healthy
- localhost:3000 loads React app
- localhost:8080/swagger responds
- POST /api/media/upload accepts an image, returns MediaAsset
- GET /api/media/{id} streams the file back
- SSH to EC2 works: `ssh -i ~/.ssh/wordbuddy-key.pem ubuntu@{EC2_IP}`
- deploy script puts app live at http://{EC2_IP}
