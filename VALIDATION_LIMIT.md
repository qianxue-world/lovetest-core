# 激活码验证次数限制

本文档说明激活码的验证次数限制机制。

## 概述

为了防止激活码被滥用，系统对每个激活码实施验证次数限制：

- **最大验证次数:** 3次
- **计数规则:** 每次调用验证API都会增加计数
- **失效条件:** 超过3次验证后，激活码永久失效

---

## 工作原理

### 验证流程

```
用户请求验证
    ↓
增加验证计数 (ValidationCount++)
    ↓
检查计数 > 3?
    ├─ 是 → 返回失效错误
    └─ 否 → 继续验证逻辑
        ↓
    检查激活码状态
        ├─ 未激活 → 激活并返回成功
        ├─ 已激活且未过期 → 返回有效
        └─ 已激活但过期 → 返回过期错误
```

### 数据库字段

| 字段 | 类型 | 描述 |
|------|------|------|
| ValidationCount | int | 验证次数计数器 |
| LastValidatedAt | datetime | 最后验证时间 |

---

## 验证次数示例

### 场景1: 正常使用

```
第1次验证: 激活成功
  - ValidationCount: 1
  - RemainingValidations: 2
  - 状态: 有效

第2次验证: 仍然有效
  - ValidationCount: 2
  - RemainingValidations: 1
  - 状态: 有效

第3次验证: 仍然有效
  - ValidationCount: 3
  - RemainingValidations: 0
  - 状态: 有效（最后一次）

第4次验证: 失效
  - ValidationCount: 4
  - RemainingValidations: 0
  - 状态: 失效（超过限制）
```

### 场景2: 重复验证未激活的码

```
第1次验证: 激活成功
  - ValidationCount: 1
  - 状态: 已激活

第2次验证: 查询状态
  - ValidationCount: 2
  - 状态: 有效

第3次验证: 查询状态
  - ValidationCount: 3
  - 状态: 有效

第4次验证: 失效
  - ValidationCount: 4
  - 状态: 失效
```

### 场景3: 验证不存在的码

```
第1次验证: 激活码不存在
  - 不增加计数（因为码不存在）
  - 返回: 404 Not Found
```

---

## API响应示例

### 第1次验证（激活）

**请求:**
```bash
curl -X POST http://api.lovetest.com.cn/api/activation/validate \
  -H "Content-Type: application/json" \
  -d '{"code":"TEST-CODE-001"}'
```

**响应:**
```json
{
  "isValid": true,
  "message": "Activation code successfully activated",
  "expiresAt": "2025-11-25T10:30:00Z",
  "validationCount": 1,
  "remainingValidations": 2
}
```

### 第2次验证（查询状态）

**响应:**
```json
{
  "isValid": true,
  "message": "Activation code is valid",
  "expiresAt": "2025-11-25T10:30:00Z",
  "validationCount": 2,
  "remainingValidations": 1
}
```

### 第3次验证（最后一次）

**响应:**
```json
{
  "isValid": true,
  "message": "Activation code is valid",
  "expiresAt": "2025-11-25T10:30:00Z",
  "validationCount": 3,
  "remainingValidations": 0
}
```

### 第4次验证（超限失效）

**响应:**
```json
{
  "isValid": false,
  "message": "Activation code has been invalidated due to excessive validation attempts",
  "expiresAt": null,
  "validationCount": 4,
  "remainingValidations": 0
}
```

---

## 客户端最佳实践

### 1. 缓存验证结果

**不推荐:**
```javascript
// 每次都调用API验证
async function checkActivation() {
  const response = await fetch('/api/activation/validate', {
    method: 'POST',
    body: JSON.stringify({ code: 'TEST-001' })
  });
  return response.json();
}

// 多次调用会快速耗尽验证次数
await checkActivation(); // 第1次
await checkActivation(); // 第2次
await checkActivation(); // 第3次
await checkActivation(); // 第4次 - 失效！
```

**推荐:**
```javascript
// 缓存验证结果
let cachedValidation = null;

async function checkActivation(code) {
  // 检查缓存
  if (cachedValidation && cachedValidation.code === code) {
    return cachedValidation.result;
  }
  
  // 调用API
  const response = await fetch('/api/activation/validate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code })
  });
  
  const result = await response.json();
  
  // 缓存结果
  cachedValidation = { code, result };
  
  return result;
}
```

### 2. 本地存储验证状态

```javascript
// 使用localStorage缓存
function validateCode(code) {
  // 检查本地缓存
  const cached = localStorage.getItem(`activation_${code}`);
  if (cached) {
    const data = JSON.parse(cached);
    // 检查缓存是否过期（例如1小时）
    if (Date.now() - data.timestamp < 3600000) {
      return Promise.resolve(data.result);
    }
  }
  
  // 调用API
  return fetch('/api/activation/validate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code })
  })
  .then(res => res.json())
  .then(result => {
    // 保存到本地
    localStorage.setItem(`activation_${code}`, JSON.stringify({
      result,
      timestamp: Date.now()
    }));
    return result;
  });
}
```

### 3. 显示剩余验证次数

```javascript
async function validateAndDisplay(code) {
  const result = await validateCode(code);
  
  if (result.isValid) {
    console.log(`✓ 激活码有效`);
    console.log(`剩余验证次数: ${result.remainingValidations}`);
    
    // 警告用户
    if (result.remainingValidations === 0) {
      alert('警告: 这是最后一次验证机会！');
    }
  } else {
    console.log(`✗ 激活码无效: ${result.message}`);
  }
}
```

---

## 管理员操作

### 查看激活码验证次数

管理员可以通过查询API查看激活码的验证次数：

```bash
TOKEN="your-jwt-token"

curl -X GET "http://api.lovetest.com.cn/api/admin/codes?pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

**响应包含验证次数:**
```json
{
  "codes": [
    {
      "id": 1,
      "code": "TEST-CODE-001",
      "isUsed": true,
      "activatedAt": "2025-11-18T10:00:00Z",
      "expiresAt": "2025-11-25T10:00:00Z",
      "validationCount": 2,
      "lastValidatedAt": "2025-11-18T11:30:00Z"
    }
  ]
}
```

### 重置验证次数

目前系统不支持重置验证次数。如果需要，可以：

1. **删除旧激活码**
2. **生成新激活码**

```bash
# 删除旧码
curl -X DELETE http://api.lovetest.com.cn/api/admin/codes/TEST-CODE-001 \
  -H "Authorization: Bearer $TOKEN"

# 生成新码
curl -X POST http://api.lovetest.com.cn/api/admin/generate-codes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"count":1,"prefix":"TEST"}'
```

---

## 监控和统计

### 查看高验证次数的激活码

管理员可以通过查询找出验证次数较高的激活码：

```bash
# 获取所有激活码
curl -X GET "http://api.lovetest.com.cn/api/admin/codes?pageSize=1000" \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.codes[] | select(.validationCount >= 3)'
```

### 日志监控

系统会记录以下日志：

**正常验证:**
```
INFO: Activation code activated: TEST-CODE-001, expires at: 2025-11-25, validation count: 1
```

**超限警告:**
```
WARNING: Activation code validation limit exceeded: TEST-CODE-001, count: 4
```

---

## 常见问题

### Q1: 为什么限制验证次数？

**A:** 防止以下滥用行为：
- 暴力破解激活码
- 频繁查询激活码状态
- 恶意消耗系统资源

### Q2: 验证失败也计数吗？

**A:** 是的。只要调用验证API，无论成功或失败都会增加计数。但如果激活码不存在（404），则不会计数。

### Q3: 可以重置验证次数吗？

**A:** 目前不支持。建议删除旧激活码并生成新的。

### Q4: 3次够用吗？

**A:** 对于正常使用场景：
- 第1次：用户激活
- 第2次：应用启动时验证
- 第3次：备用验证

如果客户端正确缓存结果，3次足够使用。

### Q5: 超限后能恢复吗？

**A:** 不能。激活码一旦超限就永久失效，需要生成新的激活码。

### Q6: 如何避免快速耗尽验证次数？

**A:** 
1. 客户端缓存验证结果
2. 使用localStorage或数据库存储状态
3. 避免在循环或定时器中调用验证API
4. 只在必要时验证（如应用启动）

---

## 技术实现

### 数据库模型

```csharp
public class ActivationCode
{
    public int Id { get; set; }
    public string Code { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ValidationCount { get; set; } = 0;      // 新增
    public DateTime? LastValidatedAt { get; set; }     // 新增
}
```

### 验证逻辑

```csharp
// 增加验证次数
activationCode.ValidationCount++;
activationCode.LastValidatedAt = DateTime.UtcNow;

// 检查是否超限
if (activationCode.ValidationCount > 3)
{
    return BadRequest(new ValidateCodeResponse
    {
        IsValid = false,
        Message = "Activation code has been invalidated due to excessive validation attempts",
        ValidationCount = activationCode.ValidationCount,
        RemainingValidations = 0
    });
}
```

---

## 相关文档

- [API文档](API_DOCUMENTATION.md)
- [故障排查](TROUBLESHOOTING.md)
- [部署文档](DEPLOYMENT.md)
