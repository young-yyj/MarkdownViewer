# Markdown 格式测试

这是**加粗文字**，这是*斜体文字*，这是***加粗斜体***，这是~~删除线~~。

## 文本格式

- 行内代码：`var x = 42;`
- 链接：[GitHub](https://github.com)
- 自动链接：https://example.com

## 列表测试

### 无序列表

- 第一项
- 第二项
  - 嵌套子项 A
  - 嵌套子项 B
    - 三级嵌套
- 第三项

### 有序列表

1. 第一步
2. 第二步
   1. 子步骤 2.1
   2. 子步骤 2.2
3. 第三步

### 任务列表

- [x] 已完成的任务
- [x] 已完成的第二项
- [ ] 待完成的任务
- [ ] 另一项待完成

## 引用块

> 这是一段引用文字。引用可以包含多个段落。
>
> 这是引用的第二段，前面有空行。
>
> > 这是嵌套引用。

## 代码块

```csharp
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, Markdown!");
        var config = new { Name = "test", Value = 42 };
        Console.WriteLine(config.Name);
    }
}
```

```python
def fibonacci(n):
    if n <= 1:
        return n
    return fibonacci(n - 1) + fibonacci(n - 2)

print(fibonacci(10))  # Output: 55
```

```bash
#!/bin/bash
echo "Deploying..."
docker build -t myapp .
docker run -d -p 8080:80 myapp
```

## 表格

| 功能 | 状态 | 优先级 |
|------|------|--------|
| 渲染引擎 | 已完成 | 高 |
| 目录导航 | 已完成 | 高 |
| 搜索功能 | 进行中 | 中 |
| 导出 PDF | 未开始 | 低 |
| 主题切换 | 计划中 | 低 |

### 对齐表格

| 左对齐 | 居中对齐 | 右对齐 |
|:-------|:--------:|-------:|
| 内容 A | 内容 B | 100 |
| 长内容 Longer | 短 | 2000 |

## 分隔线

上面是分隔线。

---

下面也是分隔线。

***

另一个分隔线。

___

## HTML 内联元素

Markdig 支持的 HTML：<kbd>Ctrl</kbd> + <kbd>C</kbd> 复制。

上标：E = mc<sup>2</sup>，下标：H<sub>2</sub>O

## 图片（如有本地图片）

![示例图片](https://via.placeholder.com/600x200/252525/dcdcdc?text=Markdown+Viewer+Test)

---

**测试结束** — 以上覆盖了加粗、斜体、删除线、行内代码、链接、列表（无序/有序/任务/嵌套）、引用（含嵌套）、代码块（3种语言）、表格（含对齐）、分隔线、HTML 内联元素、图片。
