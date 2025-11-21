# Kubernetes版本管理

本文档说明如何管理Kubernetes部署中的镜像版本。

## 自动版本更新

### Git Pre-commit Hook

每次提交代码时，pre-commit hook会自动：

1. 递增版本号（PATCH +0.0.1）
2. 更新 `ActivationCodeApi/VERSION` 文件
3. 更新 `k8s/deployment.yaml` 中的镜像标签
4. 更新 `k8s/kustomization.yaml` 中的 newTag
5. 将所有更改添加到提交中

### 示例

```bash
# 当前版本: 1.0.0
git add .
git commit -m "feat: Add new feature"

# 自动更新:
# - VERSION: 1.0.0 → 1.0.1
# - deployment.yaml: image: omaticaya/lovetest-core:1.0.1
# - kustomization.yaml: newTag: 1.0.1
```

## 手动版本更新

### 更新到特定版本

```bash
# 1. 更新VERSION文件
echo "1.2.0" > ActivationCodeApi/VERSION

# 2. 运行更新脚本
./scripts/update-k8s-version.sh

# 3. 提交更改
git add ActivationCodeApi/VERSION ActivationCodeApi/k8s/
git commit -m "chore: Bump version to 1.2.0"
```

### 使用更新脚本

```bash
# 更新k8s清单到当前VERSION
./scripts/update-k8s-version.sh
```

脚本会自动：
- 读取 `ActivationCodeApi/VERSION`
- 更新 `deployment.yaml` 中的镜像标签
- 更新 `kustomization.yaml` 中的 newTag

## 部署特定版本

### 方法1: 使用Kustomize（推荐）

```bash
# 部署清单中指定的版本
kubectl apply -k ActivationCodeApi/k8s/
```

### 方法2: 直接应用

```bash
# 部署所有资源
kubectl apply -f ActivationCodeApi/k8s/
```

### 方法3: 运行时覆盖版本

```bash
# 临时更改镜像版本
kubectl set image deployment/lovetest-api \
  api=omaticaya/lovetest-core:1.0.5 \
  -n lovetest
```

## 版本策略

### 开发环境

使用 `latest` 标签：

```yaml
spec:
  containers:
  - name: api
    image: omaticaya/lovetest-core:latest
    imagePullPolicy: Always
```

### 生产环境

使用固定版本（推荐）：

```yaml
spec:
  containers:
  - name: api
    image: omaticaya/lovetest-core:1.0.5
    imagePullPolicy: IfNotPresent
```

## 当前配置

### deployment.yaml

```yaml
containers:
- name: api
  image: omaticaya/lovetest-core:1.0.0
  imagePullPolicy: IfNotPresent
```

### kustomization.yaml

```yaml
images:
- name: omaticaya/lovetest-core
  newTag: 1.0.0
```

## 版本回滚

### 查看部署历史

```bash
kubectl rollout history deployment/lovetest-api -n lovetest
```

### 回滚到上一个版本

```bash
kubectl rollout undo deployment/lovetest-api -n lovetest
```

### 回滚到特定版本

```bash
# 回滚到指定修订版本
kubectl rollout undo deployment/lovetest-api \
  --to-revision=2 \
  -n lovetest

# 或直接设置镜像版本
kubectl set image deployment/lovetest-api \
  api=omaticaya/lovetest-core:1.0.3 \
  -n lovetest
```

## 验证部署版本

### 检查当前镜像

```bash
# 查看deployment使用的镜像
kubectl get deployment lovetest-api -n lovetest \
  -o jsonpath='{.spec.template.spec.containers[0].image}'

# 查看运行中的Pod使用的镜像
kubectl get pods -n lovetest \
  -o jsonpath='{.items[*].spec.containers[0].image}'
```

### 检查镜像标签

```bash
# 查看Docker Hub上的可用标签
curl -s https://hub.docker.com/v2/repositories/omaticaya/lovetest-core/tags/ | jq '.results[].name'
```

### 检查应用版本

```bash
# 通过健康检查端点
kubectl port-forward -n lovetest deployment/lovetest-api 8080:8080
curl http://localhost:8080/api/health | jq '.checks.version'
```

## 版本同步

确保以下文件中的版本一致：

1. `ActivationCodeApi/VERSION` - 源版本文件
2. `ActivationCodeApi/k8s/deployment.yaml` - 镜像标签
3. `ActivationCodeApi/k8s/kustomization.yaml` - Kustomize newTag
4. Docker Hub - 已发布的镜像标签

### 检查版本一致性

```bash
# 检查VERSION文件
cat ActivationCodeApi/VERSION

# 检查deployment.yaml
grep "image: omaticaya/lovetest-core:" ActivationCodeApi/k8s/deployment.yaml

# 检查kustomization.yaml
grep "newTag:" ActivationCodeApi/k8s/kustomization.yaml

# 检查已部署的版本
kubectl get deployment lovetest-api -n lovetest -o jsonpath='{.spec.template.spec.containers[0].image}'
```

## CI/CD集成

GitHub Actions自动：

1. 读取 `ActivationCodeApi/VERSION`
2. 构建Docker镜像
3. 标记镜像为版本号
4. 推送到Docker Hub

部署流程：

```bash
# 1. 提交代码（版本自动递增）
git commit -m "feat: New feature"

# 2. 推送到GitHub
git push origin main

# 3. CI/CD自动构建并推送镜像

# 4. 部署到Kubernetes
kubectl apply -k ActivationCodeApi/k8s/
```

## ArgoCD集成

如果使用ArgoCD，配置自动同步：

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: lovetest-api
spec:
  source:
    repoURL: https://github.com/your-repo/lovetest-core.git
    targetRevision: main
    path: ActivationCodeApi/k8s
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
```

ArgoCD会自动检测版本变更并部署。

## 故障排查

### 镜像拉取失败

```bash
# 检查镜像是否存在
docker pull omaticaya/lovetest-core:1.0.0

# 查看Pod事件
kubectl describe pod <pod-name> -n lovetest

# 检查镜像拉取策略
kubectl get deployment lovetest-api -n lovetest \
  -o jsonpath='{.spec.template.spec.containers[0].imagePullPolicy}'
```

### 版本不匹配

```bash
# 同步所有版本
VERSION=$(cat ActivationCodeApi/VERSION)
./scripts/update-k8s-version.sh

# 验证更新
git diff ActivationCodeApi/k8s/
```

### Pod未更新

```bash
# 强制重启Pod
kubectl rollout restart deployment/lovetest-api -n lovetest

# 或删除Pod让其重建
kubectl delete pod -l app=lovetest-api -n lovetest
```

## 最佳实践

1. **使用固定版本** - 生产环境避免使用 `latest`
2. **版本一致性** - 确保所有文件版本同步
3. **测试后部署** - 先在开发环境测试新版本
4. **保留历史** - 不要删除旧版本镜像
5. **文档化变更** - 在CHANGELOG.md记录重要版本
6. **自动化部署** - 使用CI/CD和GitOps工具

## 相关文件

- `ActivationCodeApi/VERSION` - 版本号文件
- `ActivationCodeApi/k8s/deployment.yaml` - Deployment清单
- `ActivationCodeApi/k8s/kustomization.yaml` - Kustomize配置
- `scripts/update-k8s-version.sh` - 版本更新脚本
- `.git/hooks/pre-commit` - Git钩子
- `VERSIONING.md` - 版本管理文档

## 支持

如遇问题：
1. 检查VERSION文件是否存在
2. 验证git hooks已安装
3. 确认镜像已推送到Docker Hub
4. 查看Kubernetes事件日志
