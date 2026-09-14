# Instruction

Good choices for a first deployment. AWS + local storage keeps it straightforward — we can migrate media to S3 later with minimal changes. Here's the full phase:

---

## Phase 4 — Infrastructure

**What we're building:**

```
Your Windows machine (dev)
│
├── docker-compose.yml        → runs everything locally in containers
│   ├── wordbuddy-api         → .NET 8 API container
│   ├── wordbuddy-ui          → React/Nginx container
│   └── sqlserver             → SQL Server container (replaces localdb)
│
AWS (production)
├── EC2 instance              → runs Docker + both containers
├── SQL Server                → runs in Docker on same EC2 (cost-effective)
└── EBS volume                → persists media files + database data
```

---

### Step 1: Install Docker Desktop on Windows

Download and install from [docker.com](https://www.docker.com/products/docker-desktop). After install:

```powershell
# Verify
docker --version
docker compose version
```

Make sure Docker Desktop is running (whale icon in system tray) before continuing.

---

### Step 2: Dockerize the backend

```powershell
cd C:\Projects\WordBuddy
claude
```

Paste:

```
Create a production-grade Dockerfile for WordBuddy.API (.NET 8) at the 
solution root. Requirements:

1. Use multi-stage build:
   Stage 1 — build:
   - Base image: mcr.microsoft.com/dotnet/sdk:8.0
   - Copy solution + all project files
   - dotnet restore
   - dotnet publish WordBuddy.API in Release mode to /app/publish

   Stage 2 — runtime:
   - Base image: mcr.microsoft.com/dotnet/aspnet:8.0
   - Copy published output from build stage
   - Create a non-root user (appuser) and run as that user
   - Expose port 8080
   - Set ASPNETCORE_URLS=http://+:8080
   - Entrypoint: dotnet WordBuddy.API.dll

2. Create a .dockerignore at solution root excluding:
   - **/bin, **/obj
   - **/.vs, **/.vscode
   - **/node_modules
   - **/*.user
   - .git

Show complete files with no placeholders.
```

Test it:

```powershell
docker build -t wordbuddy-api .
docker run -p 8080:8080 wordbuddy-api
# Should start — will fail on DB connection, that's expected for now
```

---

### Step 3: Dockerize the frontend

```powershell
cd D:\MyProgramming\microservices\WordBuddy\sources\WordBuddy.UI
claude
```

Paste:

```
Create a production-grade Dockerfile for WordBuddy.UI (React + Vite) 
in the project root. Requirements:

1. Use multi-stage build:
   Stage 1 — build:
   - Base image: node:20-alpine
   - Set WORKDIR /app
   - Copy package.json + package-lock.json first (layer caching)
   - npm ci
   - Copy rest of source
   - npm run build (outputs to /app/dist)

   Stage 2 — runtime:
   - Base image: nginx:alpine
   - Copy dist/ from build stage to /usr/share/nginx/html
   - Copy a custom nginx.conf (see below)
   - Expose port 80

2. Create nginx.conf that:
   - Serves the React SPA (all routes → index.html for React Router)
   - Proxies /api requests to http://wordbuddy-api:8080
     (Docker internal network — no CORS needed in production)
   - Enables gzip compression for JS/CSS/HTML
   - Sets cache headers: 1 year for hashed assets, no-cache for index.html

3. Create a .dockerignore excluding:
   - node_modules
   - dist
   - .env*
   - .git

Show complete files with no placeholders.
```

---

### Step 4: Local media file storage

Go back to the backend:

```powershell
cd C:\Projects\WordBuddy
claude
```

Paste:

```
Add local file storage support to WordBuddy for media assets 
(images, audio, video). Requirements:

1. In WordBuddy.Infrastructure, create a LocalFileStorageService:
   - Interface IFileStorageService in WordBuddy.Application:
     * Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct)
     * Task<Stream> GetAsync(string filePath, CancellationToken ct)
     * Task DeleteAsync(string filePath, CancellationToken ct)
   - Implementation LocalFileStorageService reads base path from config:
     FileStorage:BasePath (e.g. /app/media in container, C:\WordBuddyMedia in dev)
   - On SaveAsync: generate a unique filename (Guid + original extension),
     save to BasePath/{type}/{year}/{month}/filename, return the relative path
   - Log all operations using ILogger<LocalFileStorageService>

2. In WordBuddy.API, create MediaController:
   - POST /api/media/upload
     * Accepts IFormFile (max 50MB)
     * Validates allowed MIME types: image/jpeg, image/png, audio/mpeg, 
       audio/wav, video/mp4
     * Calls IFileStorageService.SaveAsync
     * Creates a MediaAsset record in DB via a new UploadMediaCommand
     * Returns the MediaAsset DTO with StorageUrl
   - GET /api/media/{id}
     * Looks up MediaAsset by id
     * Streams the file back with correct Content-Type header
     * Uses FileStreamResult for efficient streaming
   - Requires [Authorize] on upload, allow anonymous on GET

3. In appsettings.Development.json add:
   "FileStorage": { "BasePath": "C:\\WordBuddyMedia" }

4. In Dockerfile (production), the BasePath will be /app/media —
   we'll mount a Docker volume there. Update the Dockerfile VOLUME 
   instruction to declare /app/media.

Show all files complete with no placeholders.
```

---

### Step 5: Docker Compose for local full-stack dev

```powershell
cd C:\Projects\WordBuddy
claude
```

Paste:

```
Create a docker-compose.yml at the solution root that runs the full 
WordBuddy stack locally. Requirements:

Services:
1. sqlserver:
   - Image: mcr.microsoft.com/mssql/server:2022-latest
   - Environment: ACCEPT_EULA=Y, SA_PASSWORD from .env file
   - Port: 1433:1433
   - Volume: sqlserver-data:/var/opt/mssql (persist data between restarts)
   - Health check: /opt/mssql-tools/bin/sqlcmd ping every 10s

2. wordbuddy-api:
   - Build: from solution root Dockerfile
   - Depends on sqlserver (with condition: service_healthy)
   - Environment variables read from .env:
     * ConnectionStrings__Default (points to sqlserver container)
     * Jwt__Secret, Jwt__Issuer, Jwt__ExpiryMinutes
     * FileStorage__BasePath=/app/media
     * ASPNETCORE_ENVIRONMENT=Development
   - Port: 8080:8080
   - Volume: media-data:/app/media (persist uploaded files)
   - Restart: unless-stopped

3. wordbuddy-ui:
   - Build: from WordBuddy.UI directory
     context: D:/MyProgramming/microservices/WordBuddy/sources/WordBuddy.UI
   - Depends on wordbuddy-api
   - Port: 3000:80
   - Restart: unless-stopped

Volumes:
  sqlserver-data:
  media-data:

Also create:
1. .env.example (committed to git) with all required variables and placeholder values:
   SA_PASSWORD=YourStrong@Password123
   JWT_SECRET=your-256-bit-secret-here
   JWT_ISSUER=WordBuddy
   JWT_EXPIRY_MINUTES=60
   CONNECTION_STRING=Server=sqlserver;Database=WordBuddy;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=true

2. .env (NOT committed — add to .gitignore) with real local values filled in.

3. A Makefile with shortcuts (works on Windows via Git Bash):
   make up     → docker compose up --build -d
   make down   → docker compose down
   make logs   → docker compose logs -f
   make migrate → run EF Core migrations inside the API container
   make clean  → docker compose down -v (removes volumes too)

Show all files complete with no placeholders.
```

Test locally:

```powershell
# Copy .env.example to .env and fill in values
copy .env.example .env

# Start everything
docker compose up --build -d

# Watch logs
docker compose logs -f

# Verify
# API:      http://localhost:8080/swagger
# Frontend: http://localhost:3000
```

---

### Step 6: AWS setup

#### 6a. Install AWS CLI

```powershell
# Install AWS CLI v2 for Windows
# Download from: https://awscli.amazonaws.com/AWSCLIV2.msi
# Run the installer, then verify:
aws --version
```

#### 6b. Create IAM user

1. Go to [AWS Console](https://console.aws.amazon.com) → IAM → Users → Create user
2. Name it `wordbuddy-deploy`
3. Attach policy: `AmazonEC2FullAccess` (we'll tighten this later)
4. Create access key → download CSV

```powershell
aws configure
# AWS Access Key ID: (paste from CSV)
# AWS Secret Access Key: (paste from CSV)
# Default region: ap-southeast-1  (Singapore — closest to Vietnam)
# Default output format: json
```

#### 6c. Provision EC2 with Claude

```powershell
cd C:\Projects\WordBuddy
claude
```

Paste:

```
Create AWS infrastructure setup scripts for WordBuddy on EC2. 
Use AWS CLI commands that run on Windows PowerShell. Requirements:

1. Create a setup script aws/01-create-security-group.ps1:
   - Create a security group "wordbuddy-sg" in default VPC
   - Inbound rules:
     * Port 22 (SSH) from my IP only — use checkip.amazonaws.com to get it
     * Port 80 (HTTP) from anywhere (0.0.0.0/0)
     * Port 443 (HTTPS) from anywhere
   - Output the security group ID

2. Create aws/02-create-ec2.ps1:
   - AMI: Ubuntu 22.04 LTS (ami-0df7a207adb9748c7 for ap-southeast-1)
   - Instance type: t3.small (2 vCPU, 2GB RAM — enough for WordBuddy)
   - Key pair name: wordbuddy-key (we create this separately)
   - Security group: from step 1
   - Storage: 20GB gp3 EBS root volume
   - Tag: Name=wordbuddy-prod
   - User data script that:
     * Updates Ubuntu packages
     * Installs Docker + Docker Compose plugin
     * Adds ubuntu user to docker group
     * Creates /app/wordbuddy directory
     * Enables Docker to start on boot
   - Output the public IP

3. Create aws/03-create-key-pair.ps1:
   - Creates key pair "wordbuddy-key"
   - Saves private key to ~/.ssh/wordbuddy-key.pem
   - Sets correct permissions (icacls for Windows)

Show all scripts complete. Add comments explaining each AWS CLI command.
```

Run in order:

```powershell
.\aws\03-create-key-pair.ps1   # create SSH key first
.\aws\01-create-security-group.ps1
.\aws\02-create-ec2.ps1
```

---

### Step 7: Deploy to EC2

```powershell
cd C:\Projects\WordBuddy
claude
```

Paste:

```
Create a deploy script aws/04-deploy.ps1 for Windows PowerShell that:

1. Reads EC2_IP from an aws/config.env file (we fill this after EC2 is created)

2. Copies these files to the EC2 instance via SCP:
   - docker-compose.yml
   - .env (production values)
   - Dockerfile
   - All src/ project files
   - WordBuddy.UI Dockerfile + nginx.conf

3. SSHs into the EC2 instance and runs:
   - docker compose pull (or build)
   - docker compose up -d --build
   - docker compose exec wordbuddy-api dotnet ef database update
     (runs migrations on first deploy)
   - docker compose ps (verify all services are running)

4. Prints the app URL at the end: http://$EC2_IP

Also create aws/config.env.example:
   EC2_IP=
   KEY_PATH=~/.ssh/wordbuddy-key.pem

Use ssh with -o StrictHostKeyChecking=no -i for the key.
Show the complete script with error handling.
```

---

### Step 8: Update both CLAUDE.md files + README files

**Backend:**

```
Update CLAUDE.md and README.md in C:\Projects\WordBuddy to document Phase 4:

CLAUDE.md additions:
- Infrastructure section: Docker multi-stage build, docker-compose services
- Local media storage: IFileStorageService, LocalFileStorageService, 
  basepath config, /api/media endpoints
- Environment variables: full list of all required env vars with descriptions

README.md additions:
- Infrastructure section with architecture diagram (text-based)
- Docker section: how to run locally with docker compose
- AWS deployment section: prerequisites, step-by-step deploy instructions
- Environment variables table: all vars, where they go, example values
- Media upload section: supported formats, size limits, how storage works
```

**Frontend:**

```
Update CLAUDE.md and README.md in WordBuddy.UI to document Phase 4:

CLAUDE.md additions:
- Docker/Nginx section: how nginx.conf handles SPA routing + API proxy
- Production build notes

README.md additions:
- Docker section: how the UI container works
- Nginx proxy note: /api → wordbuddy-api:8080 in production
```

---

### Commit everything

```powershell
# Backend
cd C:\Projects\WordBuddy
git add .
git commit -m "feat: phase 4 — docker, local media storage, aws ec2 scripts"

# Frontend
cd D:\MyProgramming\microservices\WordBuddy\sources\WordBuddy.UI
git add .
git commit -m "feat: phase 4 — dockerfile + nginx config"
```

---

### ✅ Phase 4 checklist

| | Item |
|---|---|
| ☐ | Docker Desktop installed and running |
| ☐ | Backend Dockerfile builds successfully |
| ☐ | Frontend Dockerfile builds successfully |
| ☐ | `docker compose up` runs all 3 services |
| ☐ | Frontend at `localhost:3000` talks to API at `localhost:8080` |
| ☐ | Media upload endpoint accepts files + saves locally |
| ☐ | AWS CLI configured with `wordbuddy-deploy` IAM user |
| ☐ | EC2 instance created and SSH accessible |
| ☐ | Deploy script pushes and starts containers on EC2 |
| ☐ | App accessible via EC2 public IP |
| ☐ | Both CLAUDE.md + README.md updated |
| ☐ | Git committed on both repos |

---

Ready for **Phase 5 — Deployment Pipeline** (GitHub Actions CI/CD → auto deploy to EC2 on every push to `main`) when you're done here. Let me know if you hit anything.
