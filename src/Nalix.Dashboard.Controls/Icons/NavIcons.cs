// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Controls.Icons;

/// <summary>
/// The navigation icon set, stored as SVG <b>bodies</b> (24x24 grid, stroke-2, round caps) and wrapped at
/// render time by <see cref="Svg(int, string)"/>. Ported verbatim from AtmosOS AdminNavIcons. Lookups use
/// kebab-case names (for example <c>shield-check</c>); see <see cref="TryGetBody(string?, out string)"/>.
/// </summary>
public static class NavIcons
{
    /// <summary>Wraps an icon body in a sized, stroke-inheriting, decorative <c>&lt;svg&gt;</c>.</summary>
    /// <param name="size">The width and height in pixels.</param>
    /// <param name="body">The inner SVG markup of the icon.</param>
    /// <returns>The complete SVG element as a string.</returns>
    public static string Svg(int size, string body) =>
        $"<svg width=\"{size}\" height=\"{size}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" " +
        $"stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">{body}</svg>";

    // ── Icon bodies (verbatim from AtmosOS) ──────────────────────────────────
    private const string Dashboard = "<rect width=\"7\" height=\"7\" x=\"3\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"7\" x=\"14\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"7\" x=\"14\" y=\"14\" rx=\"1\"/><rect width=\"7\" height=\"7\" x=\"3\" y=\"14\" rx=\"1\"/>";

    private const string Clipboard = "<rect x=\"8\" y=\"2\" width=\"8\" height=\"4\" rx=\"1\" ry=\"1\"/><path d=\"M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2\"/><path d=\"m9 14 2 2 4-4\"/>";

    private const string Register = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M3 10h18M9 10v12M15 10v12\"/>";

    private const string Tasks = "<path d=\"M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2\"/><rect x=\"9\" y=\"3\" width=\"6\" height=\"4\" rx=\"1\"/><path d=\"m9 12 2 2 4-4\"/>";

    private const string Clock = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>";

    private const string Board = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M3 11h18\"/><path d=\"M8 7h5M8 15h8M8 18h5\"/>";

    private const string Face = "<path d=\"M7 3H5a2 2 0 0 0-2 2v2M17 3h2a2 2 0 0 1 2 2v2M7 21H5a2 2 0 0 1-2-2v-2M17 21h2a2 2 0 0 0 2-2v-2\"/><path d=\"M9 10v1a3 3 0 0 0 6 0v-1\"/>";

    private const string Sunrise = "<path d=\"M12 2v3M4.9 7.9 7 10M2 15h3M19 15h3M17 10l2.1-2.1\"/><path d=\"M8 15a4 4 0 0 1 8 0\"/><path d=\"M3 19h18\"/>";

    private const string Planner = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"17\" rx=\"2\"/><path d=\"M3 9h18M8 2v4M16 2v4\"/>";

    private const string TeachingPlan = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M16 2v4M8 2v4M3 10h18M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h.01\"/>";

    private const string Activities = "<rect x=\"2\" y=\"7\" width=\"20\" height=\"14\" rx=\"2\"/><path d=\"M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2\"/><line x1=\"12\" y1=\"12\" x2=\"12\" y2=\"16\"/><line x1=\"10\" y1=\"14\" x2=\"14\" y2=\"14\"/>";

    private const string MediaLibrary = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><circle cx=\"8.5\" cy=\"8.5\" r=\"1.5\"/><path d=\"M21 15l-5-5L5 21\"/>";

    private const string Grid = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/>";

    private const string Taxonomy = "<path d=\"M4 6h16M4 12h10M4 18h6\"/>";

    private const string Eye = "<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>";

    private const string Portfolio = "<path d=\"M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z\"/>";

    private const string UserPlus = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><line x1=\"19\" y1=\"8\" x2=\"19\" y2=\"14\"/><line x1=\"22\" y1=\"11\" x2=\"16\" y2=\"11\"/>";

    private const string Occupancy = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M3 9h18M9 21V9\"/>";

    private const string Finance = "<path d=\"M4 3h16v18l-3-2-3 2-3-2-3 2z\"/><line x1=\"8\" y1=\"8\" x2=\"16\" y2=\"8\"/><line x1=\"8\" y1=\"12\" x2=\"14\" y2=\"12\"/>";

    private const string FeeSchedule = "<path d=\"M20.6 13.4 12 22l-9-9V3h10z\"/><circle cx=\"7.5\" cy=\"7.5\" r=\"1.5\"/>";

    private const string ExpenseTag = "<path d=\"M20.6 13.4 12 22l-9-9V3h10z\"/><path d=\"M7.5 4.5v4.5m-1.5-1.5 1.5 1.5 1.5-1.5\"/>";

    private const string PercentAdjustment = "<line x1=\"19\" y1=\"5\" x2=\"5\" y2=\"19\"/><circle cx=\"6.5\" cy=\"6.5\" r=\"2.5\"/><circle cx=\"17.5\" cy=\"17.5\" r=\"2.5\"/>";

    private const string ReliefPrograms = "<path d=\"M4 8a4 4 0 0 1 4-4h8a4 4 0 0 1 4 4\"/><circle cx=\"7\" cy=\"14\" r=\"2\"/><circle cx=\"12\" cy=\"16\" r=\"2\"/><circle cx=\"17\" cy=\"14\" r=\"2\"/>";

    private const string LegalEntity = "<rect x=\"3\" y=\"3\" width=\"13\" height=\"18\" rx=\"1\"/><line x1=\"6\" y1=\"9\" x2=\"12\" y2=\"9\"/><circle cx=\"17\" cy=\"16\" r=\"4\"/><path d=\"m15 19-1 3 3-1.5 3 1.5-1-3\"/>";

    private const string Payer = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><rect x=\"16\" y=\"8\" width=\"6\" height=\"5\" rx=\"1\"/><line x1=\"17.5\" y1=\"10.5\" x2=\"20.5\" y2=\"10.5\"/>";

    private const string RuleChecklist = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><rect x=\"7\" y=\"7\" width=\"2\" height=\"2\"/><line x1=\"11\" y1=\"8\" x2=\"16\" y2=\"8\"/><rect x=\"7\" y=\"12\" width=\"2\" height=\"2\"/><line x1=\"11\" y1=\"13\" x2=\"16\" y2=\"13\"/>";

    private const string TuitionGenerate = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><line x1=\"8\" y1=\"14\" x2=\"16\" y2=\"14\"/><line x1=\"12\" y1=\"6\" x2=\"12\" y2=\"11\"/><line x1=\"9.5\" y1=\"8.5\" x2=\"14.5\" y2=\"8.5\"/>";

    private const string Expense = "<rect x=\"2\" y=\"6\" width=\"20\" height=\"14\" rx=\"2\"/><path d=\"M2 10h20\"/><path d=\"M12 17V13\"/><path d=\"m9.5 15.5 2.5 2.5 2.5-2.5\"/>";

    private const string IncomeOther = "<rect x=\"2\" y=\"6\" width=\"20\" height=\"14\" rx=\"2\"/><path d=\"M2 10h20\"/><path d=\"M12 13v4\"/><path d=\"m9.5 14.5 2.5-2.5 2.5 2.5\"/>";

    private const string Summary = "<path d=\"M3 21h18\"/><rect x=\"5\" y=\"9\" width=\"4\" height=\"9\"/><rect x=\"13\" y=\"4\" width=\"4\" height=\"14\"/>";

    private const string CashierJournal = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"1\"/><line x1=\"8\" y1=\"8\" x2=\"16\" y2=\"8\"/><line x1=\"8\" y1=\"12\" x2=\"16\" y2=\"12\"/><line x1=\"8\" y1=\"16\" x2=\"12\" y2=\"16\"/>";

    private const string Refund = "<rect x=\"3\" y=\"8\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 8V5a2 2 0 0 1 2-2h3\"/><path d=\"m10 1-3 2 3 2\"/>";

    private const string PayrollRuns = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><circle cx=\"19\" cy=\"11\" r=\"3\"/><line x1=\"19\" y1=\"9.5\" x2=\"19\" y2=\"12.5\"/>";

    private const string Calendar = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>";

    private const string Announcement = "<path d=\"M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9\"/><path d=\"M13.73 21a2 2 0 0 1-3.46 0\"/>";

    private const string Conversations = "<path d=\"M8 13h6M8 9h8\"/><path d=\"M21 15a2 2 0 0 1-2 2H9l-4 4V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2z\"/>";

    private const string Holiday = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/><path d=\"m9 16 2 2 4-4\"/>";

    private const string Inbox = "<path d=\"M22 12h-6l-2 3h-4l-2-3H2\"/><path d=\"M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z\"/>";

    private const string Survey = "<path d=\"M9 2h6a2 2 0 0 1 2 2v1h2a2 2 0 0 1 2 2v13a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2h2V4a2 2 0 0 1 2-2Z\"/><path d=\"M9 12h6M9 16h4\"/>";

    private const string Reports = "<line x1=\"18\" y1=\"20\" x2=\"18\" y2=\"10\"/><line x1=\"12\" y1=\"20\" x2=\"12\" y2=\"4\"/><line x1=\"6\" y1=\"20\" x2=\"6\" y2=\"14\"/>";

    private const string FacilitiesHub = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M9 3v18M3 9h18\"/>";

    private const string PeopleHub = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75\"/>";

    private const string ShieldCheck = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/><path d=\"m9 12 2 2 4-4\"/>";

    private const string Tv = "<rect width=\"20\" height=\"14\" x=\"2\" y=\"3\" rx=\"2\"/><line x1=\"8\" y1=\"21\" x2=\"16\" y2=\"21\"/><line x1=\"12\" y1=\"17\" x2=\"12\" y2=\"21\"/>";

    private const string Tablet = "<rect x=\"4\" y=\"2\" width=\"16\" height=\"20\" rx=\"2\"/><line x1=\"10\" y1=\"19\" x2=\"14\" y2=\"19\"/>";

    private const string Speaker = "<path d=\"M3 11v2a1 1 0 0 0 1 1h3l4 4V6L7 10H4a1 1 0 0 0-1 1z\"/><path d=\"M15.5 8.5a4 4 0 0 1 0 7\"/><path d=\"M18.5 6a8 8 0 0 1 0 12\"/>";

    private const string Microphone = "<path d=\"M12 2a3 3 0 0 0-3 3v6a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3z\"/><path d=\"M19 10v1a7 7 0 0 1-14 0v-1M12 18v4M8 22h8\"/>";

    private const string Camera = "<path d=\"m23 7-7 5 7 5V7z\"/><rect width=\"15\" height=\"14\" x=\"1\" y=\"5\" rx=\"2\" ry=\"2\"/>";

    private const string Building = "<path d=\"M3 21h18M5 21V7l7-4 7 4v14M9 21v-6h6v6\"/>";

    private const string Gauge = "<path d=\"M4 19a9 9 0 1 1 16 0\"/><path d=\"m12 15 4-5\"/><circle cx=\"12\" cy=\"15\" r=\"1.5\"/>";

    private const string Addons = "<path d=\"M20 7h-9M14 17H5\"/><circle cx=\"17\" cy=\"17\" r=\"3\"/><circle cx=\"7\" cy=\"7\" r=\"3\"/>";

    private const string Key = "<path d=\"M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0 3 3L22 7l-3-3m-3.5 3.5L19 4\"/>";

    private const string AuditLog = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z\"/><path d=\"M14 2v6h6M16 13H8M16 17H8M10 9H8\"/>";

    private const string Trash = "<path d=\"M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2m3 0-1 14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2L4 6\"/>";

    private const string PlatformOrgs = "<path d=\"M3 21h18M6 21V8l6-4 6 4v13M9 21v-6h6v6\"/>";

    private const string PlatformDefaults = "<rect x=\"2\" y=\"2\" width=\"20\" height=\"8\" rx=\"2\"/><rect x=\"2\" y=\"14\" width=\"20\" height=\"8\" rx=\"2\"/><line x1=\"6\" y1=\"6\" x2=\"6.01\" y2=\"6\"/><line x1=\"6\" y1=\"18\" x2=\"6.01\" y2=\"18\"/>";

    private const string Support = "<path d=\"M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z\"/>";

    private const string Staff = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>";

    private const string Shield = "<path d=\"M12 2 3 7v6c0 5 4 9 9 9s9-4 9-9V7z\"/>";

    private const string PlatformActivity = "<path d=\"M12 8v4l3 3\"/><circle cx=\"12\" cy=\"12\" r=\"9\"/>";

    private const string Settings = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1Z\"/>";

    private const string Menu = "<path d=\"M3 2v7a3 3 0 0 0 3 3 3 3 0 0 0 3-3V2\"/><path d=\"M6 12v10\"/><path d=\"M17 2c-1.7 1.4-2.5 3.4-2.5 6s.8 4 2.5 4h1V2z\"/><path d=\"M18 12v10\"/>";

    private const string MenuTemplate = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><line x1=\"8\" y1=\"8\" x2=\"16\" y2=\"8\"/><line x1=\"8\" y1=\"12\" x2=\"16\" y2=\"12\"/><line x1=\"8\" y1=\"16\" x2=\"13\" y2=\"16\"/>";

    private const string Serving = "<path d=\"M3 18h18\"/><path d=\"M4 18a8 8 0 0 1 16 0\"/><path d=\"M12 6V4\"/><circle cx=\"12\" cy=\"3\" r=\"1\"/>";

    private const string Recipe = "<path d=\"M4 10h16v4a6 6 0 0 1-6 6h-4a6 6 0 0 1-6-6z\"/><path d=\"M2 10h20\"/><path d=\"M12 4v3\"/><path d=\"M10 4h4\"/>";

    private const string Ingredient = "<path d=\"M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10z\"/><path d=\"M2 21c0-3 1.85-5.36 5.08-6\"/>";

    private const string Allergen = "<path d=\"M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z\"/><line x1=\"12\" y1=\"9\" x2=\"12\" y2=\"13\"/><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"/>";

    private const string NutritionLibrary = "<path d=\"M2 5a2 2 0 0 1 2-2h5a3 3 0 0 1 3 3 3 3 0 0 1 3-3h5a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-5a3 3 0 0 0-3 3 3 3 0 0 0-3-3H4a2 2 0 0 1-2-2z\"/><path d=\"M12 6v15\"/>";

    private const string FoodSafety = "<path d=\"M9 3h6a1 1 0 0 1 1 1v2H8V4a1 1 0 0 1 1-1z\"/><path d=\"M16 5h2a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2h2\"/><path d=\"m9 14 2 2 4-4\"/>";

    private const string Health = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 8v8M8 12h8\"/>";

    private const string Automation = "<rect x=\"3\" y=\"4\" width=\"6\" height=\"5\" rx=\"1\"/><rect x=\"3\" y=\"15\" width=\"6\" height=\"5\" rx=\"1\"/><rect x=\"15\" y=\"9.5\" width=\"6\" height=\"5\" rx=\"1\"/><path d=\"M9 6.5h2.5a2 2 0 0 1 2 2V12M9 17.5h2.5a2 2 0 0 0 2-2V12h1.5\"/>";

    private const string AutomationRuns = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M10 8.5v7l6-3.5z\"/>";

    private const string AutomationCredits = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v10M9.5 9.8c0-1 1-1.8 2.5-1.8s2.5.8 2.5 1.8-1 1.7-2.5 2.2-2.5 1.2-2.5 2.2 1 1.8 2.5 1.8 2.5-.8 2.5-1.8\"/>";

    private const string MedicationRequest = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><path d=\"M9 8v4M7 10h4\"/><path d=\"m9 16 2 2 4-4\"/>";

    private const string Dose = "<path d=\"m10.5 20.5 10-10a4.95 4.95 0 1 0-7-7l-10 10a4.95 4.95 0 1 0 7 7Z\"/><line x1=\"8.5\" y1=\"8.5\" x2=\"15.5\" y2=\"15.5\"/>";

    private const string GrowthChart = "<line x1=\"4\" y1=\"3\" x2=\"4\" y2=\"21\"/><line x1=\"4\" y1=\"7\" x2=\"6\" y2=\"7\"/><line x1=\"4\" y1=\"13\" x2=\"6\" y2=\"13\"/><line x1=\"4\" y1=\"19\" x2=\"6\" y2=\"19\"/><path d=\"M4 19 12 11l4 4 4-8\"/>";

    private const string GroupTeaching = "<path d=\"M12 7v14M3 18a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h5a4 4 0 0 1 4 4 4 4 0 0 1 4-4h5a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1h-6a3 3 0 0 0-3 3 3 3 0 0 0-3-3z\"/>";

    private const string GroupCare = "<path d=\"M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8z\"/>";

    private const string GroupAssessment = "<path d=\"M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2\"/><rect x=\"8\" y=\"2\" width=\"8\" height=\"4\" rx=\"1\" ry=\"1\"/><path d=\"m9 14 2 2 4-4\"/>";

    private const string GroupPeople = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M22 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>";

    private const string GroupGovernance = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/>";

    private const string GroupPlatform = "<ellipse cx=\"12\" cy=\"5\" rx=\"9\" ry=\"3\"/><path d=\"M3 5v14a9 3 0 0 0 18 0V5\"/><path d=\"M3 12a9 3 0 0 0 18 0\"/>";

    private const string GroupNutrition = "<path d=\"M6 14a4 4 0 0 1-1-7.87A3.5 3.5 0 0 1 12 5a3.5 3.5 0 0 1 7 1.13A4 4 0 0 1 18 14z\"/><path d=\"M6 14v4a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2v-4\"/><path d=\"M6 17h12\"/>";

    private const string GroupPlatformSystem = "<rect x=\"4\" y=\"4\" width=\"16\" height=\"16\" rx=\"2\"/><path d=\"M9 9h6v6H9z\"/><path d=\"M4 9h2M4 15h2M18 9h2M18 15h2M9 4v2M15 4v2M9 18v2M15 18v2\"/>";

    private const string GroupCatalogue = "<path d=\"M4 19.5A2.5 2.5 0 0 1 6.5 17H20\"/><path d=\"M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z\"/>";

    private const string Storefront = "<path d=\"M3 9h18l-1.5-5.4A1 1 0 0 0 18.5 3h-13a1 1 0 0 0-1 .6z\"/><path d=\"M4 9v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V9\"/><path d=\"M10 21v-6h4v6\"/>";

    private const string Globe = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M3 12h18\"/><path d=\"M12 3a14 14 0 0 1 0 18 14 14 0 0 1 0-18z\"/>";

    private const string More = "<circle cx=\"5\" cy=\"12\" r=\"1\"/><circle cx=\"12\" cy=\"12\" r=\"1\"/><circle cx=\"19\" cy=\"12\" r=\"1\"/>";

    private const string ClassDebt = "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"1\"/><path d=\"M9 21V12h6v9\"/><circle cx=\"12\" cy=\"8\" r=\"2.25\"/>";

    private static readonly System.Collections.Generic.Dictionary<string, string> s_byName =
        new(System.StringComparer.Ordinal)
        {
            ["dashboard"] = Dashboard,
            ["clipboard"] = Clipboard,
            ["register"] = Register,
            ["tasks"] = Tasks,
            ["clock"] = Clock,
            ["board"] = Board,
            ["face"] = Face,
            ["sunrise"] = Sunrise,
            ["planner"] = Planner,
            ["teaching-plan"] = TeachingPlan,
            ["activities"] = Activities,
            ["media-library"] = MediaLibrary,
            ["grid"] = Grid,
            ["taxonomy"] = Taxonomy,
            ["eye"] = Eye,
            ["portfolio"] = Portfolio,
            ["user-plus"] = UserPlus,
            ["occupancy"] = Occupancy,
            ["finance"] = Finance,
            ["fee-schedule"] = FeeSchedule,
            ["expense-tag"] = ExpenseTag,
            ["percent-adjustment"] = PercentAdjustment,
            ["relief-programs"] = ReliefPrograms,
            ["legal-entity"] = LegalEntity,
            ["payer"] = Payer,
            ["rule-checklist"] = RuleChecklist,
            ["tuition-generate"] = TuitionGenerate,
            ["expense"] = Expense,
            ["income-other"] = IncomeOther,
            ["summary"] = Summary,
            ["cashier-journal"] = CashierJournal,
            ["refund"] = Refund,
            ["payroll-runs"] = PayrollRuns,
            ["calendar"] = Calendar,
            ["announcement"] = Announcement,
            ["conversations"] = Conversations,
            ["holiday"] = Holiday,
            ["inbox"] = Inbox,
            ["survey"] = Survey,
            ["reports"] = Reports,
            ["facilities-hub"] = FacilitiesHub,
            ["people-hub"] = PeopleHub,
            ["shield-check"] = ShieldCheck,
            ["tv"] = Tv,
            ["tablet"] = Tablet,
            ["speaker"] = Speaker,
            ["microphone"] = Microphone,
            ["camera"] = Camera,
            ["building"] = Building,
            ["gauge"] = Gauge,
            ["addons"] = Addons,
            ["key"] = Key,
            ["audit-log"] = AuditLog,
            ["trash"] = Trash,
            ["platform-orgs"] = PlatformOrgs,
            ["platform-defaults"] = PlatformDefaults,
            ["support"] = Support,
            ["staff"] = Staff,
            ["shield"] = Shield,
            ["platform-activity"] = PlatformActivity,
            ["settings"] = Settings,
            ["food-menu"] = Menu,
            ["food-menu-template"] = MenuTemplate,
            ["serving"] = Serving,
            ["recipe"] = Recipe,
            ["ingredient"] = Ingredient,
            ["allergen"] = Allergen,
            ["nutrition-library"] = NutritionLibrary,
            ["food-safety"] = FoodSafety,
            ["health"] = Health,
            ["automation"] = Automation,
            ["automation-runs"] = AutomationRuns,
            ["automation-credits"] = AutomationCredits,
            ["medication-request"] = MedicationRequest,
            ["dose"] = Dose,
            ["growth-chart"] = GrowthChart,
            ["group-teaching"] = GroupTeaching,
            ["group-care"] = GroupCare,
            ["group-assessment"] = GroupAssessment,
            ["group-people"] = GroupPeople,
            ["group-governance"] = GroupGovernance,
            ["group-platform"] = GroupPlatform,
            ["group-nutrition"] = GroupNutrition,
            ["group-platform-system"] = GroupPlatformSystem,
            ["group-catalogue"] = GroupCatalogue,
            ["storefront"] = Storefront,
            ["globe"] = Globe,
            ["more"] = More,
            ["class-debt"] = ClassDebt,
        };

    /// <summary>
    /// Looks an icon body up by its kebab-case name, for example <c>shield-check</c>.
    /// </summary>
    /// <param name="name">The kebab-case icon name. Case-sensitive. Null returns false.</param>
    /// <param name="body">The SVG body when found; an empty string otherwise.</param>
    /// <returns><see langword="true"/> when the name is known; otherwise <see langword="false"/>.</returns>
    public static bool TryGetBody(string? name, out string body)
    {
        if (name is not null && s_byName.TryGetValue(name, out string? found))
        {
            body = found;
            return true;
        }

        body = string.Empty;
        return false;
    }
}
