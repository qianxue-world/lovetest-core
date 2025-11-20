# CORS 配置文档

本文档说明API的跨域资源共享(CORS)配置。

## 配置概述

API在两个层面配置了CORS支持：

1. **应用程序层面** - ASP.NET Core CORS中间件
2. **Ingress层面** - NGINX Ingress Controller CORS注解

这种双层配置确保无论通过何种方式访问API，都能正确处理跨域请求。

---

## 应用程序层面配置

### 配置位置
`Program.cs`

### 配置内容

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
```

### 策略说明

| 配置项 | 值 | 说明 |
|--------|-----|------|
| Origin | `*` (任意) | 允许所有来源访问 |
| Methods | `*` (任意) | 允许所有HTTP方法 (GET, POST, PUT, DELETE, OPTIONS等) |
| Headers | `*` (任意) | 允许所有请求头 |

### 中间件顺序

CORS中间件在管道中的位置很重要：

```csharp
app.UseCors("AllowAll");        // 1. CORS (必须在路由之前)
app.UseIpRateLimiting();        // 2. 速率限制
app.UseMiddleware<...>();       // 3. 认证中间件
app.UseAuthorization();         // 4. 授权
app.MapControllers();           // 5. 路由
```

---

## Ingress层面配置

### 配置位置
`k8s/ingress.yaml`

### 配置内容

```yaml
annotations:
  nginx.ingress.kubernetes.io/enable-cors: "true"
  nginx.ingress.kubernetes.io/cors-allow-origin: "*"
  nginx.ingress.kubernetes.io/cors-allow-methods: "GET, POST, PUT, DELETE, OPTIONS"
  nginx.ingress.kubernetes.io/cors-allow-headers: "DNT,Keep-Alive,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range,Authorization"
  nginx.ingress.kubernetes.io/cors-expose-headers: "Content-Length,Content-Range"
  nginx.ingress.kubernetes.io/cors-allow-credentials: "true"
  nginx.ingress.kubernetes.io/cors-max-age: "86400"
```

### 注解说明

| 注解 | 值 | 说明 |
|------|-----|------|
| `enable-cors` | `true` | 启用CORS |
| `cors-allow-origin` | `*` | 允许所有来源 |
| `cors-allow-methods` | `GET, POST, PUT, DELETE, OPTIONS` | 允许的HTTP方法 |
| `cors-allow-headers` | 多个头部 | 允许的请求头 |
| `cors-expose-headers` | `Content-Length,Content-Range` | 暴露给客户端的响应头 |
| `cors-allow-credentials` | `true` | 允许携带凭证 |
| `cors-max-age` | `86400` | 预检请求缓存时间(秒) |

---

## CORS响应头

当客户端发起跨域请求时，API会返回以下响应头：

### 简单请求响应头

```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Credentials: true
Access-Control-Expose-Headers: Content-Length, Content-Range
```

### 预检请求(OPTIONS)响应头

```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: DNT, Keep-Alive, User-Agent, X-Requested-With, If-Modified-Since, Cache-Control, Content-Type, Range, Authorization
Access-Control-Allow-Credentials: true
Access-Control-Max-Age: 86400
```

---

## 测试CORS

### 1. 使用cURL测试

#### 简单请求
```bash
curl -i -X GET https://api.lovetest.com.cn/api/admin/stats \
  -H "Origin: https://example.com"
```

#### 预检请求
```bash
curl -i -X OPTIONS https://api.lovetest.com.cn/api/admin/login \
  -H "Origin: https://example.com" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type, Authorization"
```

### 2. 使用JavaScript测试

```javascript
// 简单GET请求
fetch('https://api.lovetest.com.cn/api/admin/stats')
  .then(response => response.json())
  .then(data => console.log(data))
  .catch(error => console.error('Error:', error));

// 带认证的POST请求
fetch('https://api.lovetest.com.cn/api/admin/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    username: 'admin',
    password: 'admin'
  })
})
  .then(response => response.json())
  .then(data => console.log(data))
  .catch(error => console.error('Error:', error));

// 带Token的请求
const token = 'your-jwt-token';
fetch('https://api.lovetest.com.cn/api/admin/codes', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
})
  .then(response => response.json())
  .then(data => console.log(data))
  .catch(error => console.error('Error:', error));
```

### 3. 使用浏览器开发者工具

1. 打开浏览器开发者工具 (F12)
2. 切换到 **Network** 标签
3. 发起跨域请求
4. 查看请求和响应头中的CORS相关字段

---

## 常见问题

### Q1: 为什么需要双层CORS配置？

**A:** 
- **应用层配置**: 确保直接访问API时(如开发环境、Docker部署)支持CORS
- **Ingress层配置**: 在Kubernetes环境中，Ingress作为入口点处理CORS，性能更好

### Q2: 允许所有来源安全吗？

**A:** 
- 对于公共API，允许所有来源是常见做法
- API已通过JWT令牌保护敏感端点
- 速率限制防止滥用
- 如需限制特定域名，修改配置：

**应用层:**
```csharp
policy.WithOrigins("https://example.com", "https://app.example.com")
      .AllowAnyMethod()
      .AllowAnyHeader();
```

**Ingress层:**
```yaml
nginx.ingress.kubernetes.io/cors-allow-origin: "https://example.com, https://app.example.com"
```

### Q3: 预检请求是什么？

**A:** 
浏览器在发送某些跨域请求前，会先发送OPTIONS请求询问服务器是否允许。这称为"预检请求"。

触发预检的条件：
- 使用PUT、DELETE等方法
- 使用自定义请求头(如Authorization)
- Content-Type为application/json

### Q4: 为什么OPTIONS请求返回401？

**A:** 
如果OPTIONS请求返回401，说明认证中间件拦截了预检请求。确保：

1. CORS中间件在认证中间件之前
2. 认证中间件跳过OPTIONS请求

当前配置已正确处理此问题。

### Q5: 如何验证CORS配置生效？

**A:** 
检查响应头中是否包含：
```
Access-Control-Allow-Origin: *
```

如果没有，检查：
1. CORS中间件是否正确配置
2. 中间件顺序是否正确
3. Ingress注解是否正确应用

---

## 限制特定域名(可选)

如果需要限制只允许特定域名访问：

### 应用层配置

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "https://lovetest.com.cn",
                "https://www.lovetest.com.cn",
                "https://app.lovetest.com.cn"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 使用策略
app.UseCors("AllowSpecificOrigins");
```

### Ingress层配置

```yaml
annotations:
  nginx.ingress.kubernetes.io/cors-allow-origin: "https://lovetest.com.cn, https://www.lovetest.com.cn, https://app.lovetest.com.cn"
```

---

## 禁用CORS(不推荐)

如果需要禁用CORS：

### 应用层
注释掉或删除：
```csharp
// builder.Services.AddCors(...);
// app.UseCors("AllowAll");
```

### Ingress层
删除或设置为false：
```yaml
nginx.ingress.kubernetes.io/enable-cors: "false"
```

---

## 监控和日志

### 查看CORS相关日志

```bash
# 查看应用日志
kubectl logs -f deployment/lovetest-api -n lovetest

# 查看Ingress Controller日志
kubectl logs -f -n ingress-nginx deployment/ingress-nginx-controller
```

### 常见日志信息

**成功的CORS请求:**
```
INFO: CORS request from origin: https://example.com
INFO: CORS headers added to response
```

**被拒绝的CORS请求:**
```
WARN: CORS request rejected: origin not allowed
```

---

## 性能考虑

1. **预检请求缓存**: `max-age: 86400` (24小时)
   - 减少重复的OPTIONS请求
   - 提高客户端性能

2. **Ingress层处理**: 
   - NGINX在网关层处理CORS
   - 减轻应用服务器负担
   - 更快的响应时间

3. **简单请求优化**:
   - 尽量使用简单请求(GET, POST with simple headers)
   - 避免触发预检请求

---

## 相关文档

- [API文档](API_DOCUMENTATION.md)
- [部署文档](DEPLOYMENT.md)
- [Kubernetes部署](k8s/README.md)
- [MDN CORS文档](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
