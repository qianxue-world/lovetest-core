# 故障排查指南

本文档包含常见问题和解决方案。

## 目录

- [启动问题](#启动问题)
- [数据库问题](#数据库问题)
- [认证问题](#认证问题)
- [CORS问题](#cors问题)
- [速率限制问题](#速率限制问题)
- [Docker问题](#docker问题)
- [Kubernetes问题](#kubernetes问题)

---

## 启动问题

### 错误: Unable to resolve service for type 'AspNetCoreRateLimit.IProcessingStrategy'

**症状:**
```
System.InvalidOperationException: Unable to resolve service for type 'AspNetCoreRateLimit.IProcessingStrategy' 
while attempting to activate 'AspNetCoreRateLimit.IpRateLimitMiddleware'.
```

**原因:**
速率限制服务注册不完整。

**解决方案:**
确保 `Program.cs` 中包含完整的速率限制配置：

```csharp
// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
```

并确保 `appsettings.json` 包含配置：

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 3
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 10
      }
    ]
  }
}
```

### 错误: Database file not found

**症状:**
```
Unable to open the database file
```

**解决方案:**
1. 确保应用有写入权限
2. 检查连接字符串路径
3. 手动创建数据目录：
```bash
mkdir -p data
```

### 错误: Port already in use

**症状:**
```
Failed to bind to address http://0.0.0.0:8080: address already in use
```

**解决方案:**
1. 查找占用端口的进程：
```bash
# macOS/Linux
lsof -i :8080

# Windows
netstat -ano | findstr :8080
```

2. 停止占用端口的进程或更改端口：
```bash
# 使用环境变量更改端口
export ASPNETCORE_URLS="http://+:8081"
dotnet run
```

---

## 数据库问题

### 数据库锁定

**症状:**
```
SQLite Error: database is locked
```

**解决方案:**
1. 确保只有一个应用实例在运行
2. 检查是否有其他进程打开了数据库文件
3. 重启应用

### 数据库损坏

**症状:**
```
SQLite Error: database disk image is malformed
```

**解决方案:**
1. 从备份恢复：
```bash
cp backups/activationcodes-backup.db activationcodes.db
```

2. 如果没有备份，删除数据库重新初始化：
```bash
rm activationcodes.db
# 重启应用会自动创建新数据库
```

### 迁移失败

**症状:**
```
Unable to apply migrations
```

**解决方案:**
1. 删除现有数据库
2. 重新运行应用让它自动创建

---

## 认证问题

### JWT令牌无效

**症状:**
```
401 Unauthorized: Invalid or expired token
```

**解决方案:**
1. 检查令牌是否过期（30分钟有效期）
2. 重新登录获取新令牌
3. 确保请求头格式正确：
```
Authorization: Bearer <token>
```

### 无法登录

**症状:**
```
401 Unauthorized: Invalid username or password
```

**解决方案:**
1. 确认使用默认凭据：`admin` / `admin`
2. 如果已修改密码但忘记，需要重置数据库
3. 检查数据库中的AdminUsers表

### 密码修改失败

**症状:**
```
400 Bad Request: Invalid old password
```

**解决方案:**
1. 确认当前密码正确
2. 新密码至少6个字符
3. 检查请求格式是否正确

---

## CORS问题

### 跨域请求被拒绝

**症状:**
浏览器控制台显示：
```
Access to fetch at 'https://api.lovetest.com.cn' from origin 'https://example.com' 
has been blocked by CORS policy
```

**解决方案:**
1. 检查 `Program.cs` 中CORS配置：
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

2. 检查Ingress注解（Kubernetes部署）：
```yaml
nginx.ingress.kubernetes.io/enable-cors: "true"
nginx.ingress.kubernetes.io/cors-allow-origin: "*"
```

3. 确保CORS中间件在认证中间件之前

### OPTIONS请求返回401

**症状:**
预检请求(OPTIONS)返回401 Unauthorized

**解决方案:**
确保中间件顺序正确：
```csharp
app.UseCors("AllowAll");        // 1. CORS
app.UseIpRateLimiting();        // 2. 速率限制
app.UseMiddleware<...>();       // 3. 认证
app.UseAuthorization();         // 4. 授权
```

---

## 速率限制问题

### 频繁触发速率限制

**症状:**
```
429 Too Many Requests
```

**解决方案:**
1. 调整 `appsettings.json` 中的限制：
```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 10  // 增加限制
      }
    ]
  }
}
```

2. 为特定端点设置不同限制：
```json
{
  "GeneralRules": [
    {
      "Endpoint": "*/api/activation/validate",
      "Period": "1m",
      "Limit": 100
    }
  ]
}
```

### 速率限制不生效

**症状:**
可以无限制发送请求

**解决方案:**
1. 检查配置是否正确加载
2. 确认中间件已添加：`app.UseIpRateLimiting();`
3. 检查日志确认速率限制服务已启动

---

## Docker问题

### 容器无法启动

**症状:**
```
docker: Error response from daemon: driver failed programming external connectivity
```

**解决方案:**
1. 检查端口是否被占用
2. 更改端口映射：
```bash
docker run -p 8081:8080 ...
```

### 数据丢失

**症状:**
容器重启后数据消失

**解决方案:**
确保使用卷挂载：
```bash
docker run -v $(pwd)/data:/app/data ...
```

### 权限问题

**症状:**
```
Permission denied: /app/data/activationcodes.db
```

**解决方案:**
```bash
# 修改数据目录权限
chmod -R 777 ./data
```

---

## Kubernetes问题

### Pod无法启动

**症状:**
```
CrashLoopBackOff
```

**解决方案:**
1. 查看Pod日志：
```bash
kubectl logs -f pod/<pod-name> -n lovetest
```

2. 查看Pod事件：
```bash
kubectl describe pod/<pod-name> -n lovetest
```

3. 常见原因：
   - 镜像拉取失败
   - 配置错误
   - 资源不足
   - 健康检查失败

### PVC绑定失败

**症状:**
```
PersistentVolumeClaim is not bound
```

**解决方案:**
1. 检查StorageClass：
```bash
kubectl get storageclass
```

2. 检查PV可用性：
```bash
kubectl get pv
```

3. 查看PVC详情：
```bash
kubectl describe pvc lovetest-data -n lovetest
```

### Ingress无法访问

**症状:**
无法通过域名访问API

**解决方案:**
1. 检查Ingress状态：
```bash
kubectl get ingress -n lovetest
kubectl describe ingress lovetest-api -n lovetest
```

2. 检查DNS解析：
```bash
nslookup api.lovetest.com.cn
```

3. 检查Ingress Controller：
```bash
kubectl get pods -n ingress-nginx
```

4. 查看Ingress Controller日志：
```bash
kubectl logs -n ingress-nginx deployment/ingress-nginx-controller
```

### 证书问题

**症状:**
HTTPS证书无效或未生成

**解决方案:**
1. 检查cert-manager：
```bash
kubectl get pods -n cert-manager
```

2. 检查证书状态：
```bash
kubectl get certificate -n lovetest
kubectl describe certificate lovetest-api-tls -n lovetest
```

3. 查看cert-manager日志：
```bash
kubectl logs -n cert-manager deployment/cert-manager
```

4. 手动触发证书生成：
```bash
kubectl delete certificate lovetest-api-tls -n lovetest
kubectl apply -f k8s/ingress.yaml
```

---

## 性能问题

### 响应缓慢

**症状:**
API响应时间过长

**解决方案:**
1. 检查数据库大小和索引
2. 增加资源限制（Kubernetes）：
```yaml
resources:
  limits:
    memory: "1Gi"
    cpu: "1000m"
```

3. 增加副本数：
```bash
kubectl scale deployment/lovetest-api --replicas=3 -n lovetest
```

4. 启用数据库连接池
5. 添加缓存层

### 内存泄漏

**症状:**
内存使用持续增长

**解决方案:**
1. 监控内存使用：
```bash
kubectl top pods -n lovetest
```

2. 定期重启Pod：
```bash
kubectl rollout restart deployment/lovetest-api -n lovetest
```

3. 检查代码中的资源释放

---

## 日志和调试

### 启用详细日志

修改 `appsettings.json`：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### 查看应用日志

**本地运行:**
```bash
dotnet run
```

**Docker:**
```bash
docker logs -f <container-name>
```

**Kubernetes:**
```bash
kubectl logs -f deployment/lovetest-api -n lovetest
```

### 调试模式运行

```bash
# 设置环境变量
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

---

## 获取帮助

如果以上方案都无法解决问题：

1. 查看相关文档：
   - [API文档](API_DOCUMENTATION.md)
   - [部署文档](DEPLOYMENT.md)
   - [CORS配置](CORS_CONFIGURATION.md)
   - [K8s部署](k8s/README.md)

2. 收集以下信息：
   - 错误消息完整内容
   - 应用日志
   - 环境信息（OS、.NET版本、Docker版本等）
   - 配置文件内容

3. 检查GitHub Issues或创建新Issue

4. 确保使用最新版本的代码和依赖
