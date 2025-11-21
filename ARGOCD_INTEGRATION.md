# ArgoCD Integration Guide

This guide shows how to integrate your Activation Code API with ArgoCD for automatic GitOps deployments.

## Overview

With the automated version bumping in the GitHub Actions pipeline, every code push triggers:
1. Version auto-increment (1.2.3 → 1.2.4)
2. Docker image build and push
3. K8s manifest update
4. Git commit with new version
5. ArgoCD detects change and deploys

## Workflow Diagram

```
Developer Push
     ↓
GitHub Actions
     ├─ Bump version (1.2.3 → 1.2.4)
     ├─ Build Docker image
     ├─ Push to Docker Hub
     ├─ Update k8s/kustomization.yaml
     └─ Commit back to Git [skip ci]
          ↓
     ArgoCD detects change
          ↓
     ArgoCD syncs to cluster
          ↓
     Kubernetes pulls new image
          ↓
     Application deployed!
```

## Setup ArgoCD Application

### Option 1: Using kubectl

Create a file `argocd-application.yaml`:

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: activationcode-api
  namespace: argocd
spec:
  project: default
  
  # Source repository
  source:
    repoURL: https://github.com/yourusername/yourrepo.git
    targetRevision: main
    path: ActivationCodeApi/k8s
  
  # Destination cluster
  destination:
    server: https://kubernetes.default.svc
    namespace: activationcode
  
  # Sync policy
  syncPolicy:
    automated:
      prune: true        # Delete resources that are no longer in Git
      selfHeal: true     # Automatically sync when cluster state drifts
      allowEmpty: false  # Don't sync if the directory is empty
    syncOptions:
      - CreateNamespace=true  # Auto-create namespace if it doesn't exist
    retry:
      limit: 5
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 3m
```

Apply it:
```bash
kubectl apply -f argocd-application.yaml
```

### Option 2: Using ArgoCD CLI

```bash
argocd app create activationcode-api \
  --repo https://github.com/yourusername/yourrepo.git \
  --path ActivationCodeApi/k8s \
  --dest-server https://kubernetes.default.svc \
  --dest-namespace activationcode \
  --sync-policy automated \
  --auto-prune \
  --self-heal \
  --sync-option CreateNamespace=true
```

### Option 3: Using ArgoCD UI

1. Open ArgoCD UI
2. Click **+ NEW APP**
3. Fill in the form:
   - **Application Name**: `activationcode-api`
   - **Project**: `default`
   - **Sync Policy**: `Automatic`
   - **Repository URL**: `https://github.com/yourusername/yourrepo.git`
   - **Revision**: `main`
   - **Path**: `ActivationCodeApi/k8s`
   - **Cluster URL**: `https://kubernetes.default.svc`
   - **Namespace**: `activationcode`
4. Enable:
   - ✅ Auto-Create Namespace
   - ✅ Prune Resources
   - ✅ Self Heal
5. Click **CREATE**

## Private Repository Setup

If your repository is private, configure Git credentials:

### Using SSH Key

```bash
# Generate SSH key
ssh-keygen -t ed25519 -C "argocd@yourcompany.com" -f ~/.ssh/argocd

# Add public key to GitHub
# Settings → Deploy keys → Add deploy key
cat ~/.ssh/argocd.pub

# Add private key to ArgoCD
argocd repo add git@github.com:yourusername/yourrepo.git \
  --ssh-private-key-path ~/.ssh/argocd
```

### Using Personal Access Token

```bash
argocd repo add https://github.com/yourusername/yourrepo.git \
  --username yourusername \
  --password ghp_yourpersonalaccesstoken
```

## Verify ArgoCD Setup

### Check Application Status

```bash
# Using CLI
argocd app get activationcode-api

# Using kubectl
kubectl get application activationcode-api -n argocd -o yaml
```

### Watch Sync Status

```bash
argocd app sync activationcode-api --watch
```

### View Application in UI

```bash
# Port forward ArgoCD UI
kubectl port-forward svc/argocd-server -n argocd 8080:443

# Open browser
open https://localhost:8080
```

## Testing the Full Workflow

### 1. Make a Code Change

```bash
# Edit some code
vim ActivationCodeApi/Controllers/HealthController.cs

# Commit and push
git add .
git commit -m "feat: improve health check response"
git push origin main
```

### 2. Watch GitHub Actions

```bash
# View workflow in browser
open https://github.com/yourusername/yourrepo/actions

# Or check status via CLI
gh run list --limit 1
gh run watch
```

### 3. Watch ArgoCD Sync

```bash
# Watch application status
argocd app get activationcode-api --watch

# Or view in UI
open https://localhost:8080/applications/activationcode-api
```

### 4. Verify Deployment

```bash
# Check pod status
kubectl get pods -n activationcode

# Check image version
kubectl get deployment activationcode-api -n activationcode -o jsonpath='{.spec.template.spec.containers[0].image}'

# View logs
kubectl logs -f deployment/activationcode-api -n activationcode
```

## ArgoCD Sync Waves

For more complex deployments, use sync waves to control deployment order:

```yaml
# In your K8s manifests, add annotations:
apiVersion: v1
kind: Namespace
metadata:
  name: activationcode
  annotations:
    argocd.argoproj.io/sync-wave: "0"  # Deploy first
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: activationcode-data
  annotations:
    argocd.argoproj.io/sync-wave: "1"  # Deploy second
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: activationcode-api
  annotations:
    argocd.argoproj.io/sync-wave: "2"  # Deploy last
```

## Health Checks

ArgoCD uses Kubernetes health checks to determine application health. Your API already has:

```yaml
# In deployment.yaml
livenessProbe:
  httpGet:
    path: /api/health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

ArgoCD will show the application as "Healthy" when these probes pass.

## Rollback Strategies

### Automatic Rollback (ArgoCD)

ArgoCD can automatically rollback failed deployments:

```yaml
syncPolicy:
  automated:
    selfHeal: true
  retry:
    limit: 5
    backoff:
      duration: 5s
      factor: 2
      maxDuration: 3m
```

### Manual Rollback (Git)

```bash
# Revert to previous version
git revert HEAD
git push

# Or set specific version
echo "1.2.3" > ActivationCodeApi/VERSION
git commit -am "chore: rollback to v1.2.3"
git push

# ArgoCD will automatically sync the rollback
```

### Manual Rollback (ArgoCD)

```bash
# Rollback to previous sync
argocd app rollback activationcode-api

# Rollback to specific revision
argocd app rollback activationcode-api 5
```

## Monitoring

### ArgoCD Notifications

Configure notifications for deployment events:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: argocd-notifications-cm
  namespace: argocd
data:
  service.slack: |
    token: $slack-token
  trigger.on-deployed: |
    - when: app.status.operationState.phase in ['Succeeded']
      send: [app-deployed]
  template.app-deployed: |
    message: |
      Application {{.app.metadata.name}} is now running version {{.app.status.sync.revision}}.
```

### Prometheus Metrics

ArgoCD exposes Prometheus metrics:

```bash
# Port forward Prometheus
kubectl port-forward svc/argocd-metrics -n argocd 8082:8082

# View metrics
curl http://localhost:8082/metrics
```

## Troubleshooting

### Application Stuck in "Progressing"

```bash
# Check sync status
argocd app get activationcode-api

# View detailed sync info
kubectl describe application activationcode-api -n argocd

# Check pod events
kubectl get events -n activationcode --sort-by='.lastTimestamp'
```

### Application "OutOfSync"

```bash
# Force sync
argocd app sync activationcode-api

# Sync with prune
argocd app sync activationcode-api --prune

# Hard refresh
argocd app get activationcode-api --hard-refresh
```

### Image Pull Errors

```bash
# Check image exists
docker pull yourusername/lovetest-api:1.2.4

# Check image pull secrets
kubectl get secrets -n activationcode

# View pod logs
kubectl describe pod <pod-name> -n activationcode
```

## Best Practices

1. **Use Separate Branches**: Consider using `main` for production and `develop` for staging
2. **Enable Notifications**: Get alerted when deployments succeed or fail
3. **Monitor Sync Status**: Set up dashboards to track ArgoCD sync health
4. **Use Sync Waves**: Control deployment order for complex applications
5. **Test Rollbacks**: Regularly test your rollback procedures
6. **Review Diffs**: Check ArgoCD UI to review changes before syncing
7. **Use Projects**: Organize applications into ArgoCD projects for better access control

## Complete Example

Here's a complete workflow from code change to deployment:

```bash
# 1. Developer makes changes
vim ActivationCodeApi/Controllers/ActivationController.cs
git add .
git commit -m "feat: add validation caching"
git push origin main

# 2. GitHub Actions (automatic)
# - Bumps version: 1.2.3 → 1.2.4
# - Builds image: yourusername/lovetest-api:1.2.4
# - Updates k8s/kustomization.yaml: newTag: 1.2.4
# - Commits: "chore: bump version to 1.2.4 and update K8s manifests [skip ci]"

# 3. ArgoCD (automatic)
# - Detects Git change
# - Syncs new manifests
# - Kubernetes pulls new image
# - Runs health checks
# - Marks as "Healthy"

# 4. Verify deployment
kubectl get pods -n activationcode
# NAME                                  READY   STATUS    RESTARTS   AGE
# activationcode-api-7d9f8b5c4d-x7k2m   1/1     Running   0          30s

kubectl get deployment activationcode-api -n activationcode -o jsonpath='{.spec.template.spec.containers[0].image}'
# yourusername/lovetest-api:1.2.4
```

## Summary

With this setup:
- ✅ Every code push automatically creates a new version
- ✅ Docker images are built and published automatically
- ✅ Kubernetes manifests are updated in Git
- ✅ ArgoCD automatically deploys to your cluster
- ✅ Full audit trail in Git history
- ✅ Easy rollbacks via Git or ArgoCD
- ✅ Zero manual intervention required

You now have a complete GitOps workflow! 🚀
