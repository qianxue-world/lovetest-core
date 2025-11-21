# Versioning Guide

This project uses semantic versioning with automatic version bumping on every commit.

## Version Format

Versions follow the format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (manual update required)
- **MINOR**: New features, backwards compatible (manual update required)
- **PATCH**: Bug fixes, small changes (auto-incremented)

## Automatic Version Bumping

### Setup

Install the git pre-commit hook:

```bash
./scripts/install-hooks.sh
```

Or manually:

```bash
chmod +x scripts/install-hooks.sh
./scripts/install-hooks.sh
```

### How It Works

1. Every time you commit, the pre-commit hook runs
2. The hook reads `ActivationCodeApi/VERSION`
3. It increments the PATCH version by 0.0.1
4. The updated VERSION file is added to your commit
5. Kubernetes manifests are automatically updated with the new version
6. Docker images are tagged with this version

### Example

```bash
# Current version: 1.0.0
git add .
git commit -m "Fix bug"
# Version automatically bumped to 1.0.1

git commit -m "Another fix"
# Version automatically bumped to 1.0.2
```

## Manual Version Updates

### Increment Minor Version (New Feature)

```bash
echo "1.1.0" > ActivationCodeApi/VERSION
git add ActivationCodeApi/VERSION
git commit -m "feat: Add new feature"
# Next commit will be 1.1.1
```

### Increment Major Version (Breaking Change)

```bash
echo "2.0.0" > ActivationCodeApi/VERSION
git add ActivationCodeApi/VERSION
git commit -m "BREAKING: Major API changes"
# Next commit will be 2.0.1
```

## Docker Image Tags

When you push to GitHub, the CI/CD pipeline creates multiple Docker tags:

| Tag Format | Example | Description |
|------------|---------|-------------|
| `latest` | `latest` | Latest build from main branch |
| `{version}` | `1.0.5` | Specific version from VERSION file |
| `{branch}` | `main`, `develop` | Latest build from branch |
| `{branch}-{sha}` | `main-abc1234` | Specific commit |

### Example Docker Tags

After committing version 1.0.5 to main branch:

```
omaticaya/lovetest-api:latest
omaticaya/lovetest-api:1.0.5
omaticaya/lovetest-api:main
omaticaya/lovetest-api:main-abc1234
```

## Pulling Specific Versions

```bash
# Pull latest version
docker pull omaticaya/lovetest-api:latest

# Pull specific version
docker pull omaticaya/lovetest-api:1.0.5

# Pull from specific branch
docker pull omaticaya/lovetest-api:develop
```

## Kubernetes Deployment

The Kubernetes manifests are automatically updated with each version bump.

### Automatic Updates

When you commit, the pre-commit hook updates:
- `ActivationCodeApi/k8s/deployment.yaml` - Image tag
- `ActivationCodeApi/k8s/kustomization.yaml` - newTag value

### Manual Version Update

If you need to manually update k8s manifests:

```bash
./scripts/update-k8s-version.sh
```

### Deploy Specific Version

```bash
# Deploy the version from manifests
kubectl apply -k ActivationCodeApi/k8s/

# Or override with specific version
kubectl set image deployment/lovetest-api \
  api=omaticaya/lovetest-core:1.0.5 \
  -n lovetest
```

## Version History

Check version history:

```bash
# View VERSION file history
git log --oneline ActivationCodeApi/VERSION

# View all versions
git log --all --oneline --grep="Version bumped"
```

## Checking Current Version

### In Code

The version is embedded in the Docker image:

```bash
# Check Docker image version
docker inspect omaticaya/lovetest-api:latest | grep version

# Check running container
docker exec <container-id> cat VERSION
```

### In Kubernetes

```bash
# Check deployed version
kubectl get deployment lovetest-api -n lovetest -o jsonpath='{.spec.template.spec.containers[0].image}'
```

### Via API

The version is available through the health check endpoint:

```bash
curl http://api.lovetest.com.cn/api/health
```

Response includes version:
```json
{
  "status": "healthy",
  "checks": {
    "version": {
      "status": "healthy",
      "version": "1.0.5"
    }
  }
}
```

## Troubleshooting

### Hook Not Running

If the version isn't incrementing:

```bash
# Check if hook is installed
ls -la .git/hooks/pre-commit

# Reinstall hook
./scripts/install-hooks.sh

# Verify hook is executable
chmod +x .git/hooks/pre-commit
```

### Skip Version Bump

To commit without bumping version (not recommended):

```bash
git commit --no-verify -m "docs: Update README"
```

### Reset Version

To reset to a specific version:

```bash
echo "1.0.0" > ActivationCodeApi/VERSION
git add ActivationCodeApi/VERSION
git commit -m "chore: Reset version to 1.0.0"
```

## Best Practices

1. **Let patch auto-increment** - Don't manually change patch version
2. **Update minor for features** - Manually bump minor version for new features
3. **Update major for breaking changes** - Manually bump major version for breaking changes
4. **Tag releases** - Create git tags for important releases:
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

5. **Pin versions in production** - Use specific version tags in production deployments
6. **Use latest in development** - Use `latest` tag for development environments

## Conventional Commits

Consider using conventional commit messages:

```bash
# Patch (auto-incremented)
git commit -m "fix: Resolve validation bug"
git commit -m "chore: Update dependencies"

# Minor (manual bump to 1.1.0 first)
git commit -m "feat: Add batch delete API"

# Major (manual bump to 2.0.0 first)
git commit -m "BREAKING: Change API response format"
```

## CI/CD Integration

The GitHub Actions workflow automatically:

1. Reads version from `ActivationCodeApi/VERSION`
2. Builds Docker image with version label
3. Tags image with version number
4. Pushes to Docker Hub

No manual intervention needed!

## Related Files

- `ActivationCodeApi/VERSION` - Version number file
- `.git/hooks/pre-commit` - Git hook script
- `scripts/install-hooks.sh` - Hook installation script
- `ActivationCodeApi/Dockerfile` - Docker build with version label
- `.github/workflows/docker-publish.yml` - CI/CD pipeline

## Support

If you encounter issues with versioning:

1. Check the VERSION file exists: `cat ActivationCodeApi/VERSION`
2. Verify hook is installed: `ls -la .git/hooks/pre-commit`
3. Test hook manually: `.git/hooks/pre-commit`
4. Check CI/CD logs in GitHub Actions
