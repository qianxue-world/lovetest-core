# 批量删除激活码 - 使用示例

本文档提供批量删除API的详细使用示例和最佳实践。

## 端点信息

**端点:** `POST /api/admin/codes/batch-delete`

**认证:** 需要 Bearer Token

**请求体:**
```json
{
  "pattern": "正则表达式",
  "dryRun": true/false
}
```

---

## 基本使用流程

### 1. 先试运行，查看匹配结果

```bash
TOKEN="your-jwt-token"

curl -X POST http://api.lovetest.com.cn/api/admin/codes/batch-delete \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pattern": "^TEST-.*",
    "dryRun": true
  }'
```

**响应:**
```json
{
  "success": true,
  "message": "Dry run completed. Found 150 matching codes",
  "matchedCount": 150,
  "deletedCount": 0,
  "matchedCodes": [
    "TEST-CODE-001",
    "TEST-CODE-002",
    "..."
  ],
  "wasDryRun": true
}
```

### 2. 确认无误后，执行实际删除

```bash
curl -X POST http://api.lovetest.com.cn/api/admin/codes/batch-delete \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pattern": "^TEST-.*",
    "dryRun": false
  }'
```

---

## 常用正则表达式模式

### 1. 按前缀删除

**删除所有TEST开头的激活码:**
```json
{
  "pattern": "^TEST-",
  "dryRun": false
}
```

**匹配示例:**
- ✅ TEST-001
- ✅ TEST-ABC-123
- ✅ TEST-DEMO
- ❌ DEMO-TEST
- ❌ MYTEST-001

---

### 2. 按后缀删除

**删除所有以2024结尾的激活码:**
```json
{
  "pattern": "-2024$",
  "dryRun": false
}
```

**匹配示例:**
- ✅ CODE-2024
- ✅ PROD-2024
- ✅ TEST-2024
- ❌ CODE-2024-01
- ❌ 2024-CODE

---

### 3. 按特定格式删除

**删除格式为 DEMO-数字 的激活码:**
```json
{
  "pattern": "^DEMO-\\d+$",
  "dryRun": false
}
```

**匹配示例:**
- ✅ DEMO-123
- ✅ DEMO-456789
- ❌ DEMO-ABC
- ❌ DEMO-123-ABC
- ❌ TEST-123

**注意:** 在JSON中，反斜杠需要转义，所以 `\d` 要写成 `\\d`

---

### 4. 包含特定文字

**删除包含"OLD"的激活码:**
```json
{
  "pattern": ".*OLD.*",
  "dryRun": false
}
```

**匹配示例:**
- ✅ OLD-CODE-001
- ✅ CODE-OLD-123
- ✅ TESTOLD
- ✅ OLDTEST
- ❌ CODE-123

---

### 5. 多个前缀（OR条件）

**删除TEST或DEMO开头的激活码:**
```json
{
  "pattern": "^(TEST|DEMO)-",
  "dryRun": false
}
```

**匹配示例:**
- ✅ TEST-001
- ✅ DEMO-123
- ✅ TEST-ABC
- ✅ DEMO-XYZ
- ❌ PROD-001
- ❌ CODE-123

---

### 6. 特定长度

**删除前缀后正好4位数字的激活码:**
```json
{
  "pattern": "^CODE-\\d{4}$",
  "dryRun": false
}
```

**匹配示例:**
- ✅ CODE-0001
- ✅ CODE-9999
- ❌ CODE-001 (3位)
- ❌ CODE-12345 (5位)
- ❌ CODE-ABC4

---

### 7. 日期格式

**删除包含2024年1月日期的激活码:**
```json
{
  "pattern": ".*202401\\d{2}.*",
  "dryRun": false
}
```

**匹配示例:**
- ✅ CODE-20240101
- ✅ PROD-20240115-ABC
- ✅ 20240131-TEST
- ❌ CODE-20240201 (2月)
- ❌ CODE-202401 (不完整)

---

### 8. 排除特定模式（使用负向预查）

**删除不以PROD开头的激活码:**
```json
{
  "pattern": "^(?!PROD-).*",
  "dryRun": false
}
```

**匹配示例:**
- ✅ TEST-001
- ✅ DEMO-123
- ✅ CODE-ABC
- ❌ PROD-001
- ❌ PROD-XYZ

---

## 实际场景示例

### 场景1: 清理测试数据

**需求:** 删除所有测试激活码（TEST、DEMO、SAMPLE前缀）

```bash
# 1. 试运行查看
curl -X POST http://api.lovetest.com.cn/api/admin/codes/batch-delete \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pattern": "^(TEST|DEMO|SAMPLE)-",
    "dryRun": true
  }'

# 2. 确认后删除
curl -X POST http://api.lovetest.com.cn/api/admin/codes/batch-delete \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pattern": "^(TEST|DEMO|SAMPLE)-",
    "dryRun": false
  }'
```

---

### 场景2: 清理旧批次

**需求:** 删除2023年生成的所有激活码

```bash
curl -X POST http://api.lovetest.com.cn/api/admin/codes/batch-delete \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pattern": ".*-2023\\d{4}$",
    "dryRun": false
  }'
```

---

### 场景3: 清理特定客户的激活码

**需求:** 删除为客户"ACME"生成的激活码

```bash
curl -X POST http://api.lovetest.com.cn/api/admin/codes/batch-delete \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pattern": "^ACME-",
    "dryRun": false
  }'
```

---

### 场景4: 清理错误批次

**需求:** 删除错误生成的激活码（包含"ERROR"或"INVALID"）

```bash
curl -X POST http://api.lovetest.com.cn/api/admin/codes/batch-delete \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "pattern": ".*(ERROR|INVALID).*",
    "dryRun": false
  }'
```

---

## Python示例

```python
import requests
import json

BASE_URL = "http://api.lovetest.com.cn"
TOKEN = "your-jwt-token"

headers = {
    "Authorization": f"Bearer {TOKEN}",
    "Content-Type": "application/json"
}

def batch_delete_codes(pattern, dry_run=True):
    """批量删除激活码"""
    url = f"{BASE_URL}/api/admin/codes/batch-delete"
    
    payload = {
        "pattern": pattern,
        "dryRun": dry_run
    }
    
    response = requests.post(url, headers=headers, json=payload)
    return response.json()

# 示例1: 试运行
result = batch_delete_codes("^TEST-.*", dry_run=True)
print(f"匹配到 {result['matchedCount']} 个激活码")
print(f"匹配的激活码: {result['matchedCodes'][:5]}")  # 显示前5个

# 示例2: 确认后删除
if input("确认删除? (yes/no): ").lower() == "yes":
    result = batch_delete_codes("^TEST-.*", dry_run=False)
    print(f"已删除 {result['deletedCount']} 个激活码")
```

---

## JavaScript/Node.js示例

```javascript
const axios = require('axios');

const BASE_URL = 'http://api.lovetest.com.cn';
const TOKEN = 'your-jwt-token';

async function batchDeleteCodes(pattern, dryRun = true) {
  try {
    const response = await axios.post(
      `${BASE_URL}/api/admin/codes/batch-delete`,
      {
        pattern: pattern,
        dryRun: dryRun
      },
      {
        headers: {
          'Authorization': `Bearer ${TOKEN}`,
          'Content-Type': 'application/json'
        }
      }
    );
    
    return response.data;
  } catch (error) {
    console.error('Error:', error.response?.data || error.message);
    throw error;
  }
}

// 示例使用
async function main() {
  // 1. 试运行
  const dryRunResult = await batchDeleteCodes('^TEST-.*', true);
  console.log(`匹配到 ${dryRunResult.matchedCount} 个激活码`);
  console.log('匹配的激活码:', dryRunResult.matchedCodes.slice(0, 5));
  
  // 2. 实际删除
  const deleteResult = await batchDeleteCodes('^TEST-.*', false);
  console.log(`已删除 ${deleteResult.deletedCount} 个激活码`);
}

main().catch(console.error);
```

---

## 安全建议

### ⚠️ 重要提示

1. **始终先使用试运行模式** (`dryRun: true`)
   - 查看会删除哪些激活码
   - 确认匹配结果符合预期

2. **谨慎使用通配符**
   - `.*` 会匹配所有激活码
   - 确保正则表达式足够具体

3. **备份重要数据**
   - 删除前备份数据库
   - 无法恢复已删除的激活码

4. **测试正则表达式**
   - 使用在线工具测试正则表达式
   - 推荐: [regex101.com](https://regex101.com)

5. **记录删除操作**
   - 保存删除的激活码列表
   - 记录删除原因和时间

---

## 常见错误

### 错误1: 无效的正则表达式

**错误信息:**
```json
{
  "success": false,
  "message": "Invalid regex pattern: parsing \"[\" - Unterminated [] set."
}
```

**原因:** 正则表达式语法错误

**解决:** 检查正则表达式语法，使用在线工具验证

---

### 错误2: JSON转义问题

**错误:** 在JSON中使用 `\d` 导致解析错误

**正确写法:**
```json
{
  "pattern": "^CODE-\\d+$"
}
```

**说明:** JSON中反斜杠需要转义

---

### 错误3: 匹配过多

**问题:** 不小心匹配了所有激活码

**预防:**
1. 始终使用试运行模式
2. 使用更具体的模式
3. 添加前缀或后缀限制

---

## 正则表达式速查表

| 符号 | 含义 | 示例 |
|------|------|------|
| `^` | 字符串开始 | `^TEST` 匹配以TEST开头 |
| `$` | 字符串结束 | `2024$` 匹配以2024结尾 |
| `.` | 任意字符 | `A.C` 匹配ABC, A1C |
| `*` | 0次或多次 | `AB*` 匹配A, AB, ABB |
| `+` | 1次或多次 | `AB+` 匹配AB, ABB |
| `?` | 0次或1次 | `AB?` 匹配A, AB |
| `\d` | 数字 | `\d+` 匹配123, 456 |
| `\w` | 字母数字下划线 | `\w+` 匹配abc, 123 |
| `[abc]` | a或b或c | `[ABC]` 匹配A, B, C |
| `[^abc]` | 非a非b非c | `[^ABC]` 匹配D, E, 1 |
| `{n}` | 正好n次 | `\d{4}` 匹配1234 |
| `{n,}` | 至少n次 | `\d{2,}` 匹配12, 123 |
| `{n,m}` | n到m次 | `\d{2,4}` 匹配12, 123, 1234 |
| `(a\|b)` | a或b | `(TEST\|DEMO)` 匹配TEST或DEMO |

---

## 相关文档

- [API文档](API_DOCUMENTATION.md)
- [管理员指南](README.md)
- [故障排查](TROUBLESHOOTING.md)
