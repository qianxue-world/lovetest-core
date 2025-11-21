# GitOps Setup Summary

## What Was Implemented

Your GitHub Actions pipeline now automatically writes back the new image version to your Kubernetes manifests, creating a complete GitOps workflow.

## How It Works

### 1. Make Your Changes
```bash
git add .
git commit -m "feat: add new feature"
git push
```

### 2. GitHub Actions Pipeline
When you push to `main`, the workflow automatically:

1. ✅ **Auto-bumps version** from `ActivationCodeApi/VERSION` (e.g., 1.2.3 → 1.2.4)
2. ✅ **Builds Docker image** with the new version tag
3. ✅ **Pushes to Docker Hub**: `yourusername/lovetest-api:1.2.4`
4. ✅ **Updates K8s manifests**: `ActivationCodeApi/k8s/kustomization.yaml` with new version
5. ✅ **Commits both files** back to your repository
6. ✅ **Uses `[skip ci]`** in commit message to prevent infinite loops

### 3. Automatic Deployment
- If using ArgoCD/Flux: Automatically detects the manifest change and deploys
- If manual: Run `kubectl apply -k ActivationCodeApi/k8s/`

## Key Features

### Prevents CI/CD Loops
The commit message includes `[skip ci]` to prevent the commit from triggering another build:
```
chore: update K8s deployment to version 1.2.3 [skip ci]
```

### Proper Permissions
The workflow has write permissions to commit back to the repository:
```yaml
permissions:
  contents: write
  packages: write
```

### Idempotent Updates
Only commits if there are actual changes to the manifest:
```bash
if git diff --staged --quiet; then
  echo "No changes to commit"
else
  git commit -m "..."
  git push
fi
```

## Setup Requirements

### 1. GitHub Secrets
Set these in your repository settings (Settings → Secrets and variables → Actions):

- `DOCKERHUB_USERNAME`: Your Docker Hub username
- `DOCKERHUB_TOKEN`: Docker Hub access token

### 2. GitHub Variables
Set this in your repository settings (Settings → Secrets and variables → Actions → Variables):

- `DOCKER_IMAGE_NAME`: Your image name (e.g., `lovetest-api`)

### 3. Repository Permissions
The workflow uses `GITHUB_TOKEN` which is automatically provided by GitHub Actions. Ensure your repository allows GitHub Actions to create and approve pull requests:

Settings → Actions → General → Workflow permissions:
- ✅ Read and write permissions

## Example Workflow

```bash
# 1. Make code changes
git add .
git commit -m "feat: add new endpoint"
git push origin main

# 2. GitHub Actions automatically:
#    - Bumps version: 1.2.3 → 1.2.4
#    - Builds: yourusername/lovetest-api:1.2.4
#    - Updates: VERSION and k8s/kustomization.yaml
#    - Commits: "chore: bump version to 1.2.4 and update K8s manifests [skip ci]"

# 3. ArgoCD/Flux automatically deploys (if configured)
# Or manually deploy:
kubectl apply -k ActivationCodeApi/k8s/
```

## Manual Version Control

For major or minor version bumps, manually set the version before pushing:

```bash
# Set specific version (e.g., major release)
echo "2.0.0" > ActivationCodeApi/VERSION
git add ActivationCodeApi/VERSION
git commit -m "chore: bump to version 2.0.0 for major release"
git push

# Pipeline will use 2.0.0 and continue auto-incrementing from there (2.0.1, 2.0.2, etc.)
```

## Verification

### Check GitHub Actions
1. Go to your repository on GitHub
2. Click the **Actions** tab
3. You should see the workflow run
4. Check the "Commit and push version update" step

### Check Git History
```bash
git pull
git log --oneline -5

# You should see:
# abc1234 chore: bump version to 1.2.4 and update K8s manifests [skip ci]
# def5678 feat: add new endpoint
```

### Check Kubernetes Manifest
```bash
cat ActivationCodeApi/k8s/kustomization.yaml

# Should show:
# images:
# - name: activationcode-api
#   newName: yourusername/lovetest-api
#   newTag: 1.2.4
```

### Check VERSION File
```bash
cat ActivationCodeApi/VERSION
# Should show: 1.2.4
```

### Check Docker Hub
```bash
docker pull yourusername/lovetest-api:1.2.3
```

## Rollback

If you need to rollback:

```bash
# Option 1: Revert the version commit
git revert HEAD~1  # Reverts the version bump
git push

# Option 2: Manual version downgrade
echo "1.2.2" > ActivationCodeApi/VERSION
git commit -am "chore: rollback to version 1.2.2"
git push

# Option 3: Kubernetes rollback (quick)
kubectl rollout undo deployment/activationcode-api -n activationcode
```

## Integration with ArgoCD

Create an ArgoCD Application:

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
```

Now every version change flows automatically:
1. Push version change → GitHub
2. GitHub Actions builds → Docker Hub
3. GitHub Actions updates → K8s manifests
4. ArgoCD detects change → Deploys to cluster

## Troubleshooting

### "Permission denied" when pushing
**Solution**: Ensure workflow has write permissions:
```yaml
permissions:
  contents: write
```

### Infinite CI/CD loop
**Solution**: Ensure commit message includes `[skip ci]`:
```bash
git commit -m "chore: update version [skip ci]"
```

### Manifest not updating
**Solution**: Check the "Update Kubernetes deployment" step in GitHub Actions logs

### ArgoCD not syncing
**Solution**: 
- Check ArgoCD is watching the correct repository and path
- Verify `syncPolicy.automated` is enabled
- Check ArgoCD logs: `kubectl logs -n argocd deployment/argocd-application-controller`

## Documentation

For more details, see:
- [GITOPS_WORKFLOW.md](GITOPS_WORKFLOW.md) - Complete GitOps workflow guide
- [GITHUB_ACTIONS_SETUP.md](../GITHUB_ACTIONS_SETUP.md) - GitHub Actions setup
- [VERSIONING.md](VERSIONING.md) - Version management
- [k8s/README.md](k8s/README.md) - Kubernetes deployment

## Summary

You now have a complete GitOps workflow where:
- ✅ Version changes trigger automatic builds
- ✅ Docker images are automatically published
- ✅ Kubernetes manifests are automatically updated
- ✅ Changes are committed back to Git
- ✅ ArgoCD/Flux can automatically deploy
- ✅ Full audit trail in Git history
- ✅ Easy rollbacks

Your infrastructure is now truly "Infrastructure as Code"!
