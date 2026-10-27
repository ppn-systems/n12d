# Nalix.Dashboard.Controls

Hand-written Blazor components and styles for the n12d dashboard. No third-party packages and no JavaScript.

## Host setup

Link both stylesheets in the host page (before the app stylesheet):

```html
<link rel="stylesheet" href="_content/Nalix.Dashboard.Controls/tokens.css" />
<link rel="stylesheet" href="_content/Nalix.Dashboard.Controls/ui.css" />
```

Dark mode follows `prefers-color-scheme`; set `data-theme="light"` or `data-theme="dark"` on `<html>` to override.

## Components

| Component | Purpose |
| :--- | :--- |
| `UiCard` | Surface with optional title, subtitle and actions |
| `UiStat` | Label, large value, hint and optional sparkline; a button when `OnClick` is set |
| `UiSparkline` | Inline SVG trend line with optional area fill |
| `UiLineChart` | Multi-series time chart with gridlines, time axis and legend (`UiChartSeries`) |
| `UiBadge` | Small status pill |
| `UiButton` | Button with `Variant`, `Busy` spinner |
| `UiField` | Label, control slot, hint and error text |
| `UiTextInput` | Text input (`Value` / `ValueChanged`) |
| `UiNumberInput` | Integer input clamped to `Min` / `Max` on commit |
| `UiSwitch` | Accessible on/off switch (`role="switch"`) |
| `UiSelect` | Select over `KeyValuePair<string, string>` options |
| `UiEmpty` | Centred empty state |
| `UiIcon` | Line icons by name (`overview`, `process`, `chevron-down`, ...) |
| `UiTable` | Sortable table over `ReportNode` rows, capped at `MaxRows` |
| `UiReportView` | Renders a `ReportNode` object generically: tiles, nested sections and tables |

Supporting types: `UiTone`, `UiButtonVariant`, `UiChartSeries`, and `UiFormat` (number, byte, duration, percent, rate, time and label formatting).

## Stylesheets

- `_content/Nalix.Dashboard.Controls/tokens.css`: colour, radius, spacing and font tokens as CSS custom properties.
- `_content/Nalix.Dashboard.Controls/ui.css`: reset, layout utilities (`.ui-grid`, `.ui-grid-wide`, `.ui-stack`, `.ui-row`), text and tone helpers, and shared form control styles.

Component styles are scoped (`*.razor.css`) and ship with the component.
