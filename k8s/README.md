# Kubernetes 部署文档

本目录包含在Kubernetes集群中部署Lovetest API的所有资源文件。

## 资源概览

所有资源都部署在 `lovetest` namespace 下。

| 文件 | 资源类型 | 描述 |
|------|---------|------|
| namespace.yaml | Namespace | lovetest命名空间 |
| deployment.yaml | Deployment | API应用部署 |
| service.yaml | Service | ClusterIP服务 |
| pvc.yaml | PersistentVolumeClaim | 数据持久化存储 |
| secret.yaml | Secret | JWT密钥等敏感信息 |
| ingress.yaml | Ingress | 外部访问入口 |
| storageclass.yaml | StorageClass | 存储类定义 |
| kustomization.yaml | Kustomization | Kustomize配置 |

## 快速部署

### 方法1: 使用 kubectl

```bash
# 部署所有资源
kubectl apply -f k8s/

# 或者按顺序部署
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/storageclass.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/pvc.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml
```

### 方法2: 使用 Kustomize

```bash
# 预览将要部署的资源
kubectl kustomize k8s/

# 部署
kubectl apply -k k8s/
```

### 方法3: 使用 ArgoCD

如果使用ArgoCD，创建Application：

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: lovetest-api
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/your-repo/lovetest-core.git
    targetRevision: main
    path: ActivationCodeApi/k8s
  destination:
    server: https://kubernetes.default.svc
    namespace: lovetest
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
    - CreateNamespace=true
```

## 配置说明

### 1. Namespace (namespace.yaml)

创建 `lovetest` 命名空间，所有资源都部署在此命名空间下。

### 2. Secret (secret.yaml)

**重要**: 部署前必须修改JWT密钥！

```bash
# 生成新的JWT密钥
openssl rand -hex 32

# 编辑 secret.yaml，替换 jwt-secret-key 的值
```

或使用kubectl创建：

```bash
kubectl create secret generic lovetest-secrets \
  --from-literal=jwt-secret-key=$(openssl rand -hex 32) \
  -n lovetest
```

### 3. PersistentVolumeClaim (pvc.yaml)

数据库文件存储，默认5Gi。根据需要调整：

```yaml
resources:
  requests:
    storage: 10Gi  # 修改为所需大小
```

### 4. Deployment (deployment.yaml)

主要配置项：

**镜像:**
```yaml
image: omaticaya/lovetest-core:latest
```

**副本数:**
```yaml
replicas: 1  # 根据负载调整
```

**资源限制:**
```yaml
resources:
  requests:
    memory: "128Mi"
    cpu: "100m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

**环境变量:**
- `ASPNETCORE_ENVIRONMENT`: 运行环境
- `ConnectionStrings__DefaultConnection`: 数据库连接字符串
- `JwtSettings__SecretKey`: JWT密钥（从Secret读取）
- `JwtSettings__Issuer`: JWT签发者
- `JwtSettings__Audience`: JWT受众

### 5. Service (service.yaml)

ClusterIP服务，在集群内部暴露API：

```yaml
type: ClusterIP
ports:
- port: 80
  targetPort: 8080
```

### 6. Ingress (ingress.yaml)

外部访问配置，使用NGINX Ingress Controller和Let's Encrypt证书。

**修改域名:**
```yaml
spec:
  tls:
  - hosts:
    - api.lovetest.com.cn  # 修改为你的域名
  rules:
  - host: api.lovetest.com.cn  # 修改为你的域名
```

**前置条件:**
- 已安装NGINX Ingress Controller
- 已安装cert-manager
- 已配置Let's Encrypt ClusterIssuer

## 验证部署

### 1. 检查所有资源

```bash
# 查看namespace
kubectl get namespace lovetest

# 查看所有资源
kubectl get all -n lovetest

# 查看详细信息
kubectl get pods,svc,ingress,pvc -n lovetest
```

### 2. 检查Pod状态

```bash
# 查看Pod
kubectl get pods -n lovetest

# 查看Pod日志
kubectl logs -f deployment/lovetest-api -n lovetest

# 查看Pod详情
kubectl describe pod <pod-name> -n lovetest
```

### 3. 检查服务

```bash
# 查看Service
kubectl get svc -n lovetest

# 测试服务（从集群内部）
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -n lovetest -- \
  curl http://lovetest-api/api/admin/stats
```

### 4. 检查Ingress

```bash
# 查看Ingress
kubectl get ingress -n lovetest

# 查看Ingress详情
kubectl describe ingress lovetest-api -n lovetest

# 测试外部访问
curl https://api.lovetest.com.cn/api/admin/stats
```

### 5. 检查存储

```bash
# 查看PVC
kubectl get pvc -n lovetest

# 查看PV
kubectl get pv
```

## 更新部署

### 更新镜像

```bash
# 方法1: 直接更新
kubectl set image deployment/lovetest-api \
  api=omaticaya/lovetest-core:v1.0.0 \
  -n lovetest

# 方法2: 编辑deployment
kubectl edit deployment lovetest-api -n lovetest

# 方法3: 重新应用配置
kubectl apply -f k8s/deployment.yaml
```

### 滚动重启

```bash
kubectl rollout restart deployment/lovetest-api -n lovetest
```

### 查看更新状态

```bash
kubectl rollout status deployment/lovetest-api -n lovetest
```

### 回滚

```bash
# 查看历史版本
kubectl rollout history deployment/lovetest-api -n lovetest

# 回滚到上一个版本
kubectl rollout undo deployment/lovetest-api -n lovetest

# 回滚到指定版本
kubectl rollout undo deployment/lovetest-api --to-revision=2 -n lovetest
```

## 扩缩容

```bash
# 手动扩容
kubectl scale deployment/lovetest-api --replicas=3 -n lovetest

# 查看副本状态
kubectl get pods -n lovetest -l app=lovetest-api
```

## 故障排查

### Pod无法启动

```bash
# 查看Pod事件
kubectl describe pod <pod-name> -n lovetest

# 查看日志
kubectl logs <pod-name> -n lovetest

# 查看上一次容器日志（如果容器重启了）
kubectl logs <pod-name> -n lovetest --previous
```

### 存储问题

```bash
# 查看PVC状态
kubectl describe pvc lovetest-data -n lovetest

# 查看PV
kubectl get pv

# 检查StorageClass
kubectl get storageclass
```

### 网络问题

```bash
# 测试Service连接
kubectl run -it --rm debug --image=nicolaka/netshoot --restart=Never -n lovetest -- bash
# 在容器内执行
curl http://lovetest-api/api/admin/stats

# 查看Ingress
kubectl describe ingress lovetest-api -n lovetest

# 查看Ingress Controller日志
kubectl logs -n ingress-nginx deployment/ingress-nginx-controller
```

### 证书问题

```bash
# 查看证书
kubectl get certificate -n lovetest

# 查看证书详情
kubectl describe certificate lovetest-api-tls -n lovetest

# 查看cert-manager日志
kubectl logs -n cert-manager deployment/cert-manager
```

## 清理资源

### 删除所有资源

```bash
# 删除整个namespace（会删除所有资源）
kubectl delete namespace lovetest

# 或者单独删除资源
kubectl delete -f k8s/

# 使用Kustomize删除
kubectl delete -k k8s/
```

### 保留数据删除应用

```bash
# 删除除PVC外的所有资源
kubectl delete deployment,service,ingress,secret -n lovetest -l app=lovetest-api
```

## 备份与恢复

### 备份数据库

```bash
# 进入Pod
kubectl exec -it deployment/lovetest-api -n lovetest -- bash

# 在Pod内备份
cp /app/data/lovetests.db /app/data/lovetests-backup-$(date +%Y%m%d).db

# 从Pod复制到本地
kubectl cp lovetest/<pod-name>:/app/data/lovetests.db ./lovetests-backup.db
```

### 恢复数据库

```bash
# 从本地复制到Pod
kubectl cp ./lovetests-backup.db lovetest/<pod-name>:/app/data/lovetests.db

# 重启Pod使其生效
kubectl rollout restart deployment/lovetest-api -n lovetest
```

## 监控

### 查看资源使用

```bash
# 查看Pod资源使用
kubectl top pods -n lovetest

# 查看节点资源使用
kubectl top nodes
```

### 查看事件

```bash
# 查看namespace事件
kubectl get events -n lovetest --sort-by='.lastTimestamp'

# 持续监控事件
kubectl get events -n lovetest --watch
```

## 最佳实践

1. **安全性**
   - 修改默认JWT密钥
   - 使用HTTPS（Ingress TLS）
   - 定期更新镜像
   - 使用Secret管理敏感信息

2. **可靠性**
   - 配置健康检查（liveness/readiness probe）
   - 设置资源限制
   - 使用PVC持久化数据
   - 定期备份数据库

3. **可维护性**
   - 使用标签组织资源
   - 使用Kustomize管理配置
   - 记录配置变更
   - 使用GitOps（ArgoCD）

4. **性能**
   - 根据负载调整副本数
   - 优化资源请求和限制
   - 使用HPA自动扩缩容
   - 监控资源使用情况

## 版本管理

Kubernetes清单中的镜像版本会自动更新。详见 [版本管理文档](VERSION_MANAGEMENT.md)。

### 快速命令

```bash
# 查看当前版本
cat ../VERSION

# 手动更新k8s清单到当前版本
../../scripts/update-k8s-version.sh

# 部署特定版本
kubectl set image deployment/lovetest-api \
  api=omaticaya/lovetest-core:1.0.5 \
  -n lovetest
```

## 相关文档

- [版本管理](VERSION_MANAGEMENT.md)
- [API文档](../API_DOCUMENTATION.md)
- [部署文档](../DEPLOYMENT.md)
- [版本控制](../VERSIONING.md)
- [README](../README.md)
