# Deployment Guide

## Docker Deployment

### Prerequisites
- Docker installed on your VM
- Docker Compose (optional, but recommended)

### Option 1: Using Docker Compose (Recommended)

1. **Clone or upload the project to your VM**

2. **Update environment variables in `docker-compose.yml`:**
   ```yaml
   environment:
     - JwtSettings__SecretKey=your-secure-random-key-here
   ```

3. **Build and run:**
   ```bash
   cd ActivationCodeApi
   docker-compose up -d
   ```

4. **View logs:**
   ```bash
   docker-compose logs -f
   ```

5. **Stop the service:**
   ```bash
   docker-compose down
   ```

### Option 2: Using Docker CLI

1. **Build the image:**
   ```bash
   cd ActivationCodeApi
   docker build -t activationcode-api .
   ```

2. **Create data directory:**
   ```bash
   mkdir -p ./data
   ```

3. **Run the container:**
   ```bash
   docker run -d \
     --name activationcode-api \
     -p 8080:8080 \
     -v $(pwd)/data:/app/data \
     -e ConnectionStrings__DefaultConnection="Data Source=/app/data/activationcodes.db" \
     -e JwtSettings__SecretKey="your-secure-random-key-min-32-chars" \
     --restart unless-stopped \
     activationcode-api
   ```

4. **View logs:**
   ```bash
   docker logs -f activationcode-api
   ```

5. **Stop the container:**
   ```bash
   docker stop activationcode-api
   docker rm activationcode-api
   ```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Production/Development) | Production |
| `ConnectionStrings__DefaultConnection` | SQLite database path | Data Source=/app/data/activationcodes.db |
| `JwtSettings__SecretKey` | JWT signing key (min 32 chars) | (must be set) |
| `JwtSettings__Issuer` | JWT issuer | ActivationCodeApi |
| `JwtSettings__Audience` | JWT audience | ActivationCodeApi |

### Persistent Data

The database is stored in a volume mounted at `/app/data`. This ensures:
- Data persists across container restarts
- Easy backup by copying the `./data` directory
- Database accessible from host for maintenance

## First Run

On first startup, the API will:
1. Create the SQLite database at `/app/data/activationcodes.db`
2. Initialize tables
3. Create admin account with credentials: `admin` / `admin`
4. Seed test activation codes

**Important:** Change the admin password immediately after first login!

## Accessing the API

Once running, the API will be available at:
- **Local:** `http://localhost:8080`
- **Remote:** `http://your-vm-ip:8080`

### Test the API:
```bash
# Health check
curl http://localhost:8080/api/admin/stats

# Login
curl -X POST http://localhost:8080/api/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

## Production Considerations

### Security
1. **Change JWT Secret Key:** Set a strong random key in environment variables
2. **Change Admin Password:** Use the change-password endpoint immediately
3. **Use HTTPS:** Put the API behind a reverse proxy (nginx/traefik) with SSL
4. **Firewall:** Restrict access to port 8080 if needed

### Reverse Proxy Example (nginx)

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Backup

Backup the database regularly:
```bash
# Create backup
cp ./data/activationcodes.db ./backups/activationcodes-$(date +%Y%m%d).db

# Restore backup
docker-compose down
cp ./backups/activationcodes-20231117.db ./data/activationcodes.db
docker-compose up -d
```

## Monitoring

### View logs:
```bash
docker-compose logs -f activationcode-api
```

### Check container status:
```bash
docker-compose ps
```

### Container stats:
```bash
docker stats activationcode-api
```

## Troubleshooting

### Container won't start
```bash
# Check logs
docker-compose logs

# Check if port is already in use
netstat -tulpn | grep 8080
```

### Database permission issues
```bash
# Fix permissions on data directory
chmod -R 777 ./data
```

### Reset everything
```bash
docker-compose down
rm -rf ./data
docker-compose up -d
```
