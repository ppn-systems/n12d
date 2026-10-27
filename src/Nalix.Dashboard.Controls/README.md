# Nalix.Dashboard.Controls

The dashboard's design system, ported from the AtmosOS admin UI (tokens, typography, icons and control
look) and kept dependency-free: Razor components, scoped CSS, no JavaScript.

## Assets a host must link (in this order)

```html
<link rel="stylesheet" href="_content/Nalix.Dashboard.Controls/fonts.css" />
<link rel="stylesheet" href="_content/Nalix.Dashboard.Controls/tokens.css" />
<link rel="stylesheet" href="_content/Nalix.Dashboard.Controls/ui.css" />
```

- `tokens.css` — AtmosOS palette (navy accent, layered surfaces, status and `--chart-1..7` colours), light and
  dark, switched by `data-theme` on `<html>` or the OS preference. n12d aliases (`--line`, `--text`, …) at the end.
- `fonts.css` — IBM Plex Sans and Be Vietnam Pro (bundled woff2, OFL).
- `ui.css` — base styles, `.ad-page__head/__title/__hint`, layout utilities.
- `icons/` — AtmosOS monochrome SVG icons, painted with `currentColor` through a CSS mask.

## Components

| Component | Purpose |
| :--- | :--- |
| `UiCard` | Surface with optional title/subtitle/actions header and footer. |
| `UiStatGroup`, `UiStat` | KPI strip: label, big value, sub-line, delta, sparkline, palette slot, clickable. |
| `UiSparkline`, `UiLineChart` | SVG trend line and multi-series chart with hover readout (`UiChartSeries`). |
| `UiTable`, `UiReportView` | Generic rendering of a JSON report (`ReportNode`) with sortable tables. |
| `UiButton`, `UiBadge`, `UiSegmented` | Actions, status pills, segmented choice. |
| `UiField`, `UiTextInput`, `UiNumberInput`, `UiSelect`, `UiSwitch` | Form controls. |
| `UiEmpty`, `UiSkeleton` | Empty and loading states. |
| `UiIcon` | AtmosOS nav icon set (`Icons/NavIcons.cs`), shared SVG assets (`shared/x`) and n12d aliases. |
| `UiFormat` | Number, byte, duration, percent and time formatting. |
