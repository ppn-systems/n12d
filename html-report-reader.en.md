---
name: html-report-reader
description: >
  Use this skill ANY TIME the user uploads or refers to an HTML file
  that is a report — including analytics reports, performance reports,
  diagnostic reports, dashboard exports, or any .html file containing data
  that needs to be read and understood.
  This skill strips all CSS/JS/style content and keeps only meaningful content,
  helping the AI read quickly without being overwhelmed by bundled code.
  Trigger this skill even when the user only says "read this HTML file",
  "summarize this report", "what does this report file say",
  or asks any question about the content of an HTML file.
---

# HTML Report Reader Skill

This skill helps quickly read HTML report files by removing all noise
(CSS, JS bundles, Tailwind, minified code) and keeping only meaningful content.

## When to use

- The HTML file is a report/dashboard export: analytics, performance, diagnostics, etc.
- The HTML file contains large `<script>` blocks or minified JS bundles.
- The user asks about the content, summary, or analysis of an HTML file.

## Workflow

### Step 1: Extract clean content using Python

Run the following script to strip HTML and extract plain text:

```python
import re
from html.parser import HTMLParser

with open('<FILE_PATH>', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove all <script> and <style> blocks
content_clean = re.sub(r'<script[^>]*>.*?</script>', '', content, flags=re.DOTALL)
content_clean = re.sub(r'<style[^>]*>.*?</style>', '', content_clean, flags=re.DOTALL)

# Extract text
class TextExtractor(HTMLParser):
    def __init__(self):
        super().__init__()
        self.text = []

    def handle_data(self, data):
        stripped = data.strip()
        if stripped:
            self.text.append(stripped)

extractor = TextExtractor()
extractor.feed(content_clean)
result = '\n'.join(extractor.text)
print(result[:8000])  # Increase the limit if needed
```

User-uploaded files are located at:

```text
/mnt/user-data/uploads/<file_name>
```

### Step 2: Read and analyze the content

After getting the clean text:

1. **Identify the report type**: analytics, performance, security, network, etc.
2. **Find the main sections**: usually numbered, such as `1. Executive Summary`, `2. Metrics`, etc.
3. **Extract important metrics**: numbers, timestamps, warnings, anomalies.
4. **Summarize based on the user's context**.

### Step 3: Respond

Reply in the same language used by the user. Prioritize:

- Specific numbers: values, timestamps, percentages.
- Warnings, anomalies, or incidents.
- Conclusions or recommendations, if available.

## Technical notes

- HTML report files often contain very large JS bundles: 5,000–15,000+ lines — **do not read them directly**.
- Use `view_range` only when you need to confirm the structure; do not read the whole file directly.
- If `print(result[:8000])` is not enough, increase it to `[:15000]` or print by section.
- Encoding is usually UTF-8; if an error occurs, try `errors='ignore'`.

## Common report types

| Type | Identification signs |
| ---- | -------------------- |
| Performance/Diagnostic | "Peak Connections", "Latency", "Memory", "Threads" |
| Security/DDoS | "Attack Detected", "Suspected Attacker", "Z-score" |
| Analytics | "Sessions", "Users", "Bounce Rate", "Conversion" |
| Network | "TCP", "WebSocket", "Pool", "Cache Hit Rate" |

## Quick summary template

After extracting the clean content, summarize using this structure:

```text
**Report type**: [name]
**Time**: [generated at / duration]

**Key metrics**:
- [Metric 1]: [value]
- [Metric 2]: [value]

**Incidents / Warnings**:
- [timestamp] [severity]: [short description]

**Conclusion**: [1-2 sentences]
```
