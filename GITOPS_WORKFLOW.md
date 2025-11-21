# GitOps Workflow

This project implements a complete GitOps workflow where version changes automatically propagate through the entire deployment pipeline.

## Overview

The workflow follows these principles:
1. **Single Source of Truth**: The `VERSION` file is the authoritative version
2. **Automated Propagation**: Version changes automatically update all deployment artifacts
3. **Git as Source of Truth**: All deployment configurations are stored in Git
4. **Declarative Configuration**: Kubernetes manifests declare the desired state

## Workflow Steps

### 1. Make Your Changes

Just commit and push your code changes:

```bash
git add .
git commit -m "feat: add new feature"
git push
```

### 2. Automated Version Bump & Deploy (GitHub Actions)

When you push to `main`, the GitHub Actions workflow automatically:

1. **Auto-Bumps Version**
   ```bash
   # Reads current version: 1.2.3
   # Increments patch: 1.2.4
   # Writes back to VERSION file
   ```

2. **Builds Docker Image**
   ```bash
   docker build -t yourusername/lovetest-api:1.2.3 .
   ```

3. **Pushes to Docker Hub**
   ```bash
   docker push yourusername/lovetest-api:1.2.3
   docker push yourusername/lovetest-api:latest
   ```

4. **Updates Kubernetes Manifests**
   ```bash
   # Updates k8s/kustomization.yaml
   sed -i "s|newTag:.*|newTag: 1.2.3|g" k8s/kustomization.yaml
   ```

5. **Commits Back to Repository**
   ```bash
   # Commits both VERSION file and kustomization.yaml
   git commit -m "chore: bump version to 1.2.4 and update K8s manifests [skip ci]"
   git push
   ```

### 3. Deployment (ArgoCD/Flux or Manual)

#### Option A: ArgoCD (Recommended)

ArgoCD automatically detects the Git change and deploys:

```bash
# ArgoCD watches your repository
# When k8s/kustomization.yaml changes, it automatically:
# 1. Pulls the new manifest
# 2. Applies it to the cluster
# 3. Kubernetes pulls the new Docker image
```

#### Option B: Manual Deployment

```bash
cd ActivationCodeApi/k8s
kubectl apply -k .
```

## File Structure

```
ActivationCodeApi/
├── VERSION                          # Single source of truth
├── Dockerfile                       # Uses VERSION during build
├── k8s/
│   ├── kustomization.yaml          # Auto-updated by CI/CD
│   ├── deployment.yaml             # References image
│   └── ...
└── .github/
    └── workflows/
        └── docker-publish.yml      # Orchestrates the workflow
```

## Key Features

### Prevents CI/CD Loops

The commit message includes `[skip ci]` to prevent infinite loops:
```bash
git commit -m "chore: update K8s deployment to version 1.2.3 [skip ci]"
```

### Atomic Updates

All version updates happen in a single workflow run:
- Docker image is built and pushed
- K8s manifests are updated
- Changes are committed back

### Audit Trail

Every version change is tracked in Git:
```bash
git log --oneline
abc1234 chore: update K8s deployment to version 1.2.3 [skip ci]
def5678 chore: bump version to 1.2.3
```

## Version Tagging Strategy

The workflow creates multiple Docker tags:

| Tag | Purpose | Example |
|-----|---------|---------|
| `latest` | Always points to the newest version | `lovetest-api:latest` |
| `{version}` | Specific version from VERSION file | `lovetest-api:1.2.3` |
| `{branch}` | Branch-specific builds | `lovetest-api:main` |
| `{branch}-{sha}` | Commit-specific builds | `lovetest-api:main-abc1234` |

## Rollback Procedure

### Quick Rollback (Kubernetes)

```bash
# Rollback to previous deployment
kubectl rollout undo deployment/activationcode-api -n activationcode

# Rollback to specific revision
kubectl rollout undo deployment/activationcode-api -n activationcode --to-revision=2
```

### Full Rollback (Git)

```bash
# Revert the version change
git revert HEAD
git push

# Or manually update VERSION file
echo "1.2.2" > ActivationCodeApi/VERSION
git add ActivationCodeApi/VERSION
git commit -m "chore: rollback to version 1.2.2"
git push
```

## Monitoring the Workflow

### Check GitHub Actions

```bash
# View workflow status
https://github.com/yourusername/yourrepo/actions
```

### Check Docker Hub

```bash
# Verify image was pushed
docker pull yourusername/lovetest-api:1.2.3
```

### Check Kubernetes

```bash
# Verify deployment
kubectl get deployment activationcode-api -n activationcode -o yaml | grep image:

# Check rollout status
kubectl rollout status deployment/activationcode-api -n activationcode
```

## Troubleshooting

### Workflow Fails to Commit

**Problem**: Permission denied when pushing to repository

**Solution**: Ensure the workflow has write permissions:
```yaml
permissions:
  contents: write
  packages: write
```

### Infinite CI/CD Loop

**Problem**: Each commit triggers another build

**Solution**: Ensure commit message includes `[skip ci]`:
```bash
git commit -m "chore: update version [skip ci]"
```

### Image Not Updating in Kubernetes

**Problem**: Kubernetes doesn't pull the new image

**Solution**: Check image pull policy:
```yaml
imagePullPolicy: Always  # Forces pull on every deployment
```

Or use specific version tags instead of `latest`:
```yaml
image: yourusername/lovetest-api:1.2.3
```

## Best Practices

1. **Semantic Versioning**: Use semver format (MAJOR.MINOR.PATCH)
   ```
   1.0.0 → Initial release
   1.0.1 → Bug fix
   1.1.0 → New feature
   2.0.0 → Breaking change
   ```

2. **Meaningful Commits**: Use conventional commits
   ```bash
   feat: add new endpoint
   fix: resolve authentication bug
   chore: bump version to 1.2.3
   ```

3. **Test Before Versioning**: Ensure tests pass before bumping version
   ```bash
   dotnet test
   echo "1.2.3" > VERSION
   git commit -m "chore: bump version to 1.2.3"
   ```

4. **Tag Releases**: Create Git tags for major releases
   ```bash
   git tag -a v1.2.3 -m "Release version 1.2.3"
   git push origin v1.2.3
   ```

## Integration with ArgoCD

### Setup ArgoCD Application

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: activationcode-api
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/yourusername/yourrepo.git
    targetRevision: main
    path: ActivationCodeApi/k8s
  destination:
    server: https://kubernetes.default.svc
    namespace: activationcode
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
      allowEmpty: false
    syncOptions:
    - CreateNamespace=true
```

### How It Works

1. You push version change to Git
2. GitHub Actions builds and updates K8s manifests
3. ArgoCD detects the manifest change
4. ArgoCD automatically syncs to your cluster
5. Kubernetes pulls the new Docker image

## Summary

This GitOps workflow provides:
- ✅ Automated version propagation
- ✅ Single source of truth (VERSION file)
- ✅ Full audit trail in Git
- ✅ Easy rollbacks
- ✅ No manual manifest updates
- ✅ Integration with ArgoCD/Flux
- ✅ Prevents CI/CD loops

Every version change flows automatically from code → Docker → Kubernetes!
