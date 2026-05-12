---
name: scottplot-winforms
description: Generate C# WinForms chart/plot code using ScottPlot 5 (v5.1.58). Covers Signal, Scatter, Bar, Pie, Heatmap, Candlestick, OHLC, Box, Polar, Radar, DataLogger, DataStreamer, Annotation, Legend, Axis, Grid, Tick customization, and styling. Use when user requests charts, plots, graphs, data visualization, or mentions ScottPlot in a C# WinForms project.
---

# ScottPlot 5 for C# WinForms

## Overview

ScottPlot 5.1.58 is a free .NET plotting library. In WinForms, use the `FormsPlot` control (`formsPlot1`).

**NuGet packages required:**
- `ScottPlot` (core library)
- `ScottPlot.WinForms` (WinForms control)

**Essential using:**
```cs
using System.Linq;      // for Enumerable.Range / .Select in math-generated data
using ScottPlot;
using ScottPlot.WinForms;
```

**Every code block must end with `formsPlot1.Refresh();` to render the plot.**

## Quick Reference

### Scatter vs Signal vs SignalConst
- **Scatter**: Paired X/Y data, hundreds of points, flexible X spacing
- **Signal**: Y data + fixed sample period, millions of points, high framerate
- **SignalConst**: Constant data, hundreds of millions of points

## Plot Types & Code Examples

> All examples below have been **compiled and visually verified** on ScottPlot 5.1.58.

### Signal Plot
```cs
// Basic signal
double[] values = Generate.Sin(51);
formsPlot1.Plot.Add.Signal(values);
formsPlot1.Refresh();
```

Signal with offset, color, legend, and axis labels (verified):
```cs
var sig1 = formsPlot1.Plot.Add.Signal(Generate.Sin(51));
sig1.LegendText = "Sin";
sig1.LineWidth = 2;
sig1.Color = Colors.Blue;

var sig2 = formsPlot1.Plot.Add.Signal(Generate.Cos(51));
sig2.LegendText = "Cos";
sig2.LineWidth = 2;
sig2.Color = Colors.Red;
sig2.Data.YOffset = 0.5;
sig2.Data.XOffset = 5;

formsPlot1.Plot.Title("Signal Plot Demo");
formsPlot1.Plot.XLabel("Sample Index");
formsPlot1.Plot.YLabel("Amplitude");
formsPlot1.Plot.ShowLegend(Alignment.UpperRight);
formsPlot1.Refresh();
```

### SignalXY Plot
```cs
double[] xs = Generate.Consecutive(100);
double[] ys = Generate.RandomWalk(100);
formsPlot1.Plot.Add.SignalXY(xs, ys);
formsPlot1.Refresh();
```

### Scatter Plot
```cs
// Basic scatter
double[] xs = { 1, 2, 3, 4, 5 };
double[] ys = { 1, 4, 9, 16, 25 };
formsPlot1.Plot.Add.Scatter(xs, ys);
formsPlot1.Refresh();
```

Scatter with Smooth, ScatterPoints, custom ticks & rotated labels (verified):
```cs
double[] xs = Generate.Consecutive(51);
double[] ys1 = Generate.Sin(51);
double[] ys2 = Generate.Cos(51);

var sp1 = formsPlot1.Plot.Add.Scatter(xs, ys1);
sp1.LegendText = "Smooth Sin";
sp1.Smooth = true;
sp1.LineWidth = 2;

var sp2 = formsPlot1.Plot.Add.ScatterPoints(xs, ys2);
sp2.LegendText = "Cos Points";
sp2.MarkerSize = 6;

// Custom tick labels
double[] tickPos = { 0, 10, 20, 30, 40, 50 };
string[] tickLabels = { "Start", "P1", "P2", "P3", "P4", "End" };
formsPlot1.Plot.Axes.Bottom.SetTicks(tickPos, tickLabels);

// Rotated tick labels
formsPlot1.Plot.Axes.Bottom.TickLabelStyle.Rotation = -30;
formsPlot1.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;

formsPlot1.Plot.Title("Scatter Plot with Smooth & Custom Ticks");
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

Scatter with lines only:
```cs
var sp = formsPlot1.Plot.Add.ScatterLine(xs, ys);
formsPlot1.Refresh();
```

### Bar Plot
```cs
// Simple bar chart
double[] values = { 5, 10, 7, 13, 25, 60 };
formsPlot1.Plot.Add.Bars(values);
formsPlot1.Plot.Axes.Margins(bottom: 0);
formsPlot1.Refresh();
```

Bar[] with Palette colors, error bars, manual ticks & rotated labels (verified):
```cs
ScottPlot.Palettes.Category10 palette = new();

ScottPlot.Bar[] bars = new ScottPlot.Bar[]
{
    new() { Position = 1, Value = 25, FillColor = palette.GetColor(0), Error = 3 },
    new() { Position = 2, Value = 40, FillColor = palette.GetColor(1), Error = 5 },
    new() { Position = 3, Value = 30, FillColor = palette.GetColor(2), Error = 2 },
    new() { Position = 4, Value = 55, FillColor = palette.GetColor(3), Error = 4 },
    new() { Position = 5, Value = 20, FillColor = palette.GetColor(4), Error = 3 },
};

formsPlot1.Plot.Add.Bars(bars);

Tick[] ticks = new Tick[]
{
    new(1, "Alpha"), new(2, "Beta"), new(3, "Gamma"),
    new(4, "Delta"), new(5, "Epsilon"),
};
formsPlot1.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
formsPlot1.Plot.Axes.Bottom.TickLabelStyle.Rotation = 45;
formsPlot1.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;
formsPlot1.Plot.Axes.Margins(bottom: 0);

formsPlot1.Plot.Title("Bar Plot with Error Bars & Custom Ticks");
formsPlot1.Plot.YLabel("Value");
formsPlot1.Refresh();
```

Horizontal bars:
```cs
var barPlot = formsPlot1.Plot.Add.Bars(values);
barPlot.Horizontal = true;
formsPlot1.Refresh();
```

### Pie Chart (verified)
```cs
double[] values = { 3, 2, 8, 4, 8, 2 };
var pie = formsPlot1.Plot.Add.Pie(values);
pie.ExplodeFraction = 0.1;

formsPlot1.Plot.Title("Pie Chart Demo");
formsPlot1.Plot.Axes.Frameless();
formsPlot1.Plot.HideGrid();
formsPlot1.Refresh();
```

### Heatmap (verified)
```cs
double[,] data = Generate.Sin2D(width: 40, height: 30);
var hm = formsPlot1.Plot.Add.Heatmap(data);
hm.Colormap = new ScottPlot.Colormaps.Turbo();

formsPlot1.Plot.Add.ColorBar(hm);
formsPlot1.Plot.Title("Heatmap with Turbo Colormap");
formsPlot1.Refresh();
```

### Candlestick / OHLC
```cs
List<ScottPlot.OHLC> prices = new();
// populate OHLC data...
var candle = formsPlot1.Plot.Add.Candlestick(prices);
formsPlot1.Plot.Axes.DateTimeTicksBottom();
formsPlot1.Refresh();
```

### Box Plot
```cs
List<ScottPlot.Box> boxes = new()
{
    new() { Position = 1, BoxMin = 3, BoxMax = 7, WhiskerMin = 1, WhiskerMax = 9, BoxMiddle = 5 },
    new() { Position = 2, BoxMin = 5, BoxMax = 8, WhiskerMin = 3, WhiskerMax = 10, BoxMiddle = 6 },
};
var bp = formsPlot1.Plot.Add.Boxes(boxes);
formsPlot1.Refresh();
```

### DataLogger — real-time growing data (verified)

`DataLogger` accumulates an **ever-growing** list of (X, Y) pairs. Best for data that arrives
continuously and needs to be reviewed in full (think oscilloscope / data recorder).

> In WinForms, call `logger.Add(...)` inside a `Timer.Tick` event handler and call
> `formsPlot1.Refresh()` to update the display.

**Quickstart — add Y values (X auto-increments by 1):**
```cs
var logger = formsPlot1.Plot.Add.DataLogger();
logger.ViewFull();       // auto-expand axis to show all data

// In Timer.Tick:
logger.Add(newValue);    // X auto-increments; or: logger.Add(x, y)
formsPlot1.Refresh();
```

**Add data with explicit X/Y coordinates:**
```cs
var logger = formsPlot1.Plot.Add.DataLogger();
logger.ViewFull();

// Add by coordinate (explicit values)
logger.Add(new Coordinates(1.5, 0.7));
// Or with named args
logger.Add(x: 1.5, y: 0.7);
// Or bulk arrays
logger.Add(xs: new double[]{1, 2, 3}, ys: new double[]{0.1, 0.5, 0.3});
// Or a collection of Y values (X auto-increments)
logger.Add(ys: new double[]{0.1, 0.5, 0.3});
formsPlot1.Refresh();
```

**Period — set X spacing to represent real time:**
```cs
var logger = formsPlot1.Plot.Add.DataLogger();
logger.Period = 0.01;    // 100 Hz: each sample = 0.01 s apart
logger.ViewFull();
formsPlot1.Plot.XLabel("Time (s)");

// In Timer.Tick:
logger.Add(newSensorValue);
formsPlot1.Refresh();
```

**View modes — controlling which data is visible:**
```cs
var logger = formsPlot1.Plot.Add.DataLogger();

// Show all data (auto-expand axis)
logger.ViewFull();

// Jump: axis jumps when new data runs off screen
logger.ViewJump(width: 100, paddingFraction: 0.1);

// Slide: axis slides continuously as new data arrives
logger.ViewSlide(width: 100);
formsPlot1.Refresh();
```

**Styling:**
```cs
var logger = formsPlot1.Plot.Add.DataLogger();
logger.ViewFull();

logger.Color      = Colors.Crimson;
logger.LineWidth  = 2;
logger.LegendText = "Sensor A";
logger.MarkerSize  = 4;
logger.MarkerShape = MarkerShape.FilledCircle;

formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**Multiple DataLoggers (multi-channel):**
```cs
var logger1 = formsPlot1.Plot.Add.DataLogger();
logger1.LegendText = "Channel A";
logger1.Color = Colors.Blue;
logger1.ViewFull();

var logger2 = formsPlot1.Plot.Add.DataLogger();
logger2.LegendText = "Channel B";
logger2.Color = Colors.Red;
logger2.ViewFull();

// In Timer.Tick:
logger1.Add(valueA);
logger2.Add(valueB);
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**DataLogger key properties & methods:**

| Member | Description |
|--------|-------------|
| `Add(double y)` | Add Y value (X auto-increments by `Period`) |
| `Add(double x, double y)` | Add point with explicit X |
| `Add(double[] xs, double[] ys)` | Add bulk data |
| `Add(Coordinates)` | Add a coordinate struct |
| `Add(IEnumerable<double> ys)` | Add a collection of Y values (X auto-increments) |
| `Clear()` | Remove all data |
| `ViewFull()` | Auto-expand to show all data |
| `ViewJump(width, paddingFraction)` | Jump axis when data runs off screen |
| `ViewSlide(width)` | Sliding window view |
| `Period` | X spacing between consecutive samples |
| `Color` | Line and marker color |
| `LineWidth` | Line thickness |
| `LegendText` | Label in legend |
| `MarkerSize / MarkerShape` | Marker style |
| `ManageAxisLimits` | Enable automatic axis limit management |

---

### DataStreamer — real-time fixed-length buffer (verified)

`DataStreamer` maintains a **fixed-length circular buffer**. New data shifts in from one end and
old data is discarded. Best for oscilloscope-style displays where only the latest N samples matter.

> In WinForms, call `streamer.Add(...)` in a `Timer.Tick` handler and call
> `formsPlot1.Refresh()`. Check `streamer.HasNewData` to skip unnecessary refreshes.

**Quickstart:**
```cs
var streamer = formsPlot1.Plot.Add.DataStreamer(200); // buffer: 200 points
streamer.ViewScrollLeft(); // newest data on the right

// In Timer.Tick:
streamer.Add(newValue);
formsPlot1.Refresh();
```

**View modes:**
```cs
var streamer = formsPlot1.Plot.Add.DataStreamer(200);

// Newest data on right (continuously shifts left)
streamer.ViewScrollLeft();

// Newest data on left (continuously shifts right)
streamer.ViewScrollRight();

// Oscilloscope wipe — new data overwrites old data left-to-right
streamer.ViewWipeRight(blankFraction: 0.1); // 10% blank gap at write head

// Wipe right-to-left
streamer.ViewWipeLeft();

formsPlot1.Refresh();
```

**FillY — fill above/below a baseline:**
```cs
var streamer = formsPlot1.Plot.Add.DataStreamer(200);
streamer.ViewScrollLeft();

streamer.FillY           = true;
streamer.FillYValue      = 0;                           // baseline Y
streamer.FillYAboveColor = Colors.Green.WithAlpha(.3);  // above baseline
streamer.FillYBelowColor = Colors.Red.WithAlpha(.3);    // below baseline

formsPlot1.Refresh();
```

**Period — display real-time X axis:**
```cs
var streamer = formsPlot1.Plot.Add.DataStreamer(200);
streamer.Period = 0.005;  // 200 Hz: 5 ms per sample
streamer.ViewScrollLeft();
formsPlot1.Plot.XLabel("Time (s)");
formsPlot1.Refresh();
```

**Multiple DataStreamers:**
```cs
var s1 = formsPlot1.Plot.Add.DataStreamer(200);
s1.LegendText = "Sin"; s1.Color = Colors.Blue; s1.ViewScrollLeft();

var s2 = formsPlot1.Plot.Add.DataStreamer(200);
s2.LegendText = "Cos"; s2.Color = Colors.Red; s2.ViewScrollLeft();

// In Timer.Tick:
s1.Add(sinValue);
s2.Add(cosValue);
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**HasNewData — skip refresh when no new data:**
```cs
// In Timer.Tick (optimization for high-frequency timers):
// Always Add first, then conditionally Refresh:
streamer.Add(newValue);
if (streamer.HasNewData)   // true until next Render clears the flag
    formsPlot1.Refresh();

// Or, if data is produced by a background thread:
// (background thread calls streamer.Add; Timer only refreshes)
if (streamer.HasNewData)
    formsPlot1.Refresh();
```

**DataStreamer key properties & methods:**

| Member | Description |
|--------|-------------|
| `Add(double value)` | Shift in one new value |
| `Add(double[] ys)` | Shift in an array of values |
| `AddRange(IEnumerable<double>)` | Shift in a collection |
| `Clear(double value)` | Fill buffer with a constant |
| `ViewScrollLeft()` | Newest data on right (default) |
| `ViewScrollRight()` | Newest data on left |
| `ViewWipeRight(blankFraction)` | Overwrite left-to-right (oscilloscope style) |
| `ViewWipeLeft()` | Overwrite right-to-left |
| `Count` | Current buffer fill count |
| `Period` | X spacing per sample |
| `Color` | Line color |
| `LineWidth` | Line thickness |
| `LegendText` | Label in legend |
| `FillY` | Enable above/below fill |
| `FillYValue` | Baseline Y for fill |
| `FillYAboveColor / FillYBelowColor` | Fill colors |
| `HasNewData` | True if data added since last render |
| `ManageAxisLimits` | Auto adjust axis limits |
| `ContinuouslyAutoscale` | If true, auto-scale axis limits before every render |

### Lines & Markers (verified)
```cs
// Horizontal / Vertical lines
var hLine = formsPlot1.Plot.Add.HorizontalLine(0.5);
hLine.Color = Colors.Red;
hLine.LineWidth = 2;

var vLine = formsPlot1.Plot.Add.VerticalLine(25);
vLine.Color = Colors.Green;
vLine.LineWidth = 2;

// Horizontal / Vertical spans
var hSpan = formsPlot1.Plot.Add.HorizontalSpan(10, 20);
hSpan.FillColor = Colors.Yellow.WithAlpha(40);
formsPlot1.Plot.Add.VerticalSpan(10, 30);

// Circle / Ellipse
var circle = formsPlot1.Plot.Add.Circle(25, 0, 5);
circle.FillColor = Colors.Magenta.WithAlpha(50);
circle.LineColor = Colors.Magenta;
formsPlot1.Plot.Add.Ellipse(0, 0, radiusX: 5, radiusY: 3);

// Polygon
Coordinates[] pts = { new(0,0), new(1,2), new(2,0) };
formsPlot1.Plot.Add.Polygon(pts);

// Arrow
formsPlot1.Plot.Add.Arrow(new Coordinates(0, 0), new Coordinates(1, 1));

formsPlot1.Refresh();
```

### Text & Annotation (verified)
```cs
// Text (positioned in data coordinates)
var txt = formsPlot1.Plot.Add.Text("Hello!", x: 5, y: 0.8);
txt.LabelStyle.FontSize = 20;

// Annotation (positioned in fractional figure space, stays fixed when panning)
var ann = formsPlot1.Plot.Add.Annotation("Top-Left Note");
ann.Alignment = Alignment.UpperLeft;

formsPlot1.Refresh();
```

### Tooltip (verified)

A tooltip displays a text bubble with a tail pointing to a specific coordinate.
`Add.Tooltip(tip, text, label)` — `tip` is the pointed coordinate, `label` is where the bubble sits.

**Quickstart:**
```cs
double[] ys = Generate.Sin(50);
var sig = formsPlot1.Plot.Add.Signal(ys);
sig.MaximumMarkerSize = 20;

Coordinates tip   = new(25, ys[25]);      // point to highlight
Coordinates label = tip.WithDelta(8, .7); // bubble offset from tip
formsPlot1.Plot.Add.Tooltip(tip, "Special Point", label);
formsPlot1.Refresh();
```

**Font & color styling:**
```cs
Coordinates tip   = new(25, 0.5);         // use a fixed coordinate
Coordinates label = tip.WithDelta(8, .7);
var tt = formsPlot1.Plot.Add.Tooltip(tip, "Special Point", label);

// Font
tt.LabelFontSize  = 18;
tt.LabelBold      = true;
tt.LabelFontColor = Colors.Yellow;

// Background & border
tt.FillColor = Colors.Blue;
tt.LineColor = Colors.Navy;
tt.LineWidth = 3;
formsPlot1.Refresh();
```

**Tail width control:**
```cs
// TailWidthPercentage: 0.0 (needle) → 1.0 (full-width base)
Coordinates tip   = new(25, 0.5);
Coordinates label = tip.WithDelta(8, .7);
var tt = formsPlot1.Plot.Add.Tooltip(tip, "Wide Tail", label);
tt.TailWidthPercentage = 0.5;  // default ~0.3
formsPlot1.Refresh();
```

**Auto-angle — bubble direction adjusts automatically:**
```cs
// Tooltip tail direction is computed from the relative position of tip vs. label.
// Place the label at any offset and the tail will point correctly.
for (int i = 0; i < 360; i += 30)
{
    Coordinates tip   = new(0, 0);
    PolarCoordinates polar = new(1, Angle.FromDegrees(i));
    Coordinates label = polar.ToCartesian();
    var tt = formsPlot1.Plot.Add.Tooltip(tip, $"{i}°", label);
    tt.FillColor     = Colormap.Default.GetColor(i, 360).Lighten(0.5);
    tt.LineColor     = Colormap.Default.GetColor(i, 360);
    tt.LabelBold     = true;
    tt.LabelFontColor = Colormap.Default.GetColor(i, 360).Darken(0.5);
}
formsPlot1.Plot.Axes.SetLimits(-1.5, 1.5, -1.5, 1.5);
formsPlot1.Refresh();
```

**Multiple tooltips on a scatter chart:**
```cs
double[] xs = Generate.Consecutive(20);
double[] ys = Generate.Sin(20);
formsPlot1.Plot.Add.Scatter(xs, ys);

int[] highlightIdx    = { 5, 10, 15 };
string[] messages     = { "Min", "Zero", "Max" };
for (int i = 0; i < highlightIdx.Length; i++)
{
    int idx           = highlightIdx[i];
    Coordinates tip   = new(xs[idx], ys[idx]);
    Coordinates label = tip.WithDelta(1.5, .3);
    var tt = formsPlot1.Plot.Add.Tooltip(tip, messages[i], label);
    tt.FillColor = Colors.White;
    tt.LineColor = Colors.DarkGray;
    tt.LineWidth = 1.5f;
}
formsPlot1.Refresh();
```

**Tooltip key properties:**

| Property | Type | Description |
|----------|------|-------------|
| `TipLocation` | `Coordinates` | Coordinate of the tail tip (the pointed location) |
| `LabelLocation` | `Coordinates` | Coordinate where the text bubble sits |
| `TailWidthPercentage` | `double` | Base width of tail (0.0 = needle, 1.0 = full body width) |
| `PixelPadding` | `PixelPadding` | Space between text and bubble edge |
| `FillColor` | `Color` | Bubble background color |
| `LineColor` | `Color` | Bubble border color |
| `LineWidth` | `float` | Bubble border thickness |
| `LabelFontSize` | `float` | Text font size |
| `LabelBold` | `bool` | Bold text |
| `LabelFontColor` | `Color` | Text color |
| `LabelFontName` | `string` | Font family name |

> `tip.WithDelta(dx, dy)` is a convenience method on `Coordinates` that returns `new Coordinates(tip.X + dx, tip.Y + dy)` — useful for placing the bubble near the tip with a fixed offset.

### Marker (verified)

Markers are symbols placed at a specific location in coordinate space.

Single marker:
```cs
// Place individual markers at specific coordinates
formsPlot1.Plot.Add.Marker(25, .5);
formsPlot1.Plot.Add.Marker(35, .6);
formsPlot1.Plot.Add.Marker(45, .7);
formsPlot1.Refresh();
```

Many markers (batch) — `Add.Markers` returns a `Markers` plottable for further configuration:
```cs
double[] xs = Generate.Consecutive(51);
double[] sin = Generate.Sin(51);
double[] cos = Generate.Cos(51);

var sinMarkers = formsPlot1.Plot.Add.Markers(xs, sin, MarkerShape.OpenCircle, 15, Colors.Green);
sinMarkers.LegendText = "Sin Markers";

var cosMarkers = formsPlot1.Plot.Add.Markers(xs, cos, MarkerShape.FilledDiamond, 10, Colors.Magenta);
cosMarkers.LegendText = "Cos Markers";

formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

Marker with legend:
```cs
var marker = formsPlot1.Plot.Add.Marker(25, .5);
marker.LegendText = "Marker";
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

MarkerStyle customization (FillColor, OutlineColor, LineColor):
```cs
var mp = formsPlot1.Plot.Add.Marker(x: 5, y: 3);
mp.MarkerStyle.Shape = MarkerShape.FilledDiamond;
mp.MarkerStyle.Size = 20;

// Filled shape styling
mp.MarkerStyle.FillColor = Colors.Gold.WithAlpha(.5);
mp.MarkerStyle.OutlineColor = Colors.DarkGoldenrod;
mp.MarkerStyle.OutlineWidth = 2;

// Line-based shape styling
mp.MarkerStyle.LineWidth = 2f;
mp.MarkerStyle.LineColor = Colors.DarkGoldenrod;
formsPlot1.Refresh();
```

Scatter with custom MarkerShape:
```cs
double[] xs = Generate.Consecutive(51);
double[] ys = Generate.Sin(51);

var sp = formsPlot1.Plot.Add.Scatter(xs, ys);
sp.MarkerSize = 15;
sp.MarkerShape = MarkerShape.OpenTriangleUp;
sp.Color = Colors.Navy;
formsPlot1.Refresh();
```

**Available MarkerShape values:**
`None`, `FilledCircle`, `OpenCircle`, `FilledSquare`, `OpenSquare`,
`FilledTriangleUp`, `OpenTriangleUp`, `FilledTriangleDown`, `OpenTriangleDown`,
`FilledDiamond`, `OpenDiamond`, `Eks`, `Cross`, `VerticalBar`, `HorizontalBar`,
`TriUp`, `TriDown`, `Asterisk`, `HashTag`, `OpenCircleWithDot`,
`OpenCircleWithCross`, `OpenCircleWithEks`, `CircleWithLineLeft`,
`CircleWithLineRight`, `TriangleWithLineLeft`, `TriangleWithLineRight`

> **填充型 vs 线条型属性说明：**
> - **填充型**（`FilledCircle`, `FilledSquare`, `FilledTriangleUp/Down`, `FilledDiamond`）：使用 `FillColor`、`OutlineColor`、`OutlineWidth`。
> - **线条型**（`OpenCircle`, `OpenSquare`, `OpenTriangleUp/Down`, `Eks`, `Cross`, `VerticalBar`, `HorizontalBar` 等）：使用 `LineColor`、`LineWidth`，`FillColor` 不起作用。

### FillY — Area Between Curves (verified)
```cs
double[] xs = Generate.Consecutive(51);
double[] ys1 = Generate.Sin(51);
double[] ys2 = Generate.Cos(51);
var fill = formsPlot1.Plot.Add.FillY(xs, ys1, ys2);
fill.FillColor = Colors.Blue.WithAlpha(50);
fill.LegendText = "Filled Area";

formsPlot1.Plot.Title("FillY: Area Between Sin & Cos");
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

### Polar Axis (verified)

A polar axis uses spokes and concentric circles to render a polar coordinate system.
Use `polarAxis.GetCoordinates(radius, degrees)` to convert polar → Cartesian, then plot normally.

> **Important:** `PolarAxisSpoke` and `PolarAxisCircle` are in the `ScottPlot` namespace (not `ScottPlot.Plottables`).

**Quickstart — scatter data on polar axis:**
```cs
var polarAxis = formsPlot1.Plot.Add.PolarAxis();

IColormap colormap = new ScottPlot.Colormaps.Turbo();
foreach (double degrees in Generate.Range(0, 360, 10))
{
    double radius = degrees / 360.0;  // normalized 0..1
    Coordinates pt = polarAxis.GetCoordinates(radius, degrees);
    var marker = formsPlot1.Plot.Add.Marker(pt);
    marker.Color = colormap.GetColor(radius);
}
formsPlot1.Refresh();
```

**Rotation and Clockwise direction:**
```cs
var polarAxis = formsPlot1.Plot.Add.PolarAxis();
polarAxis.Rotation = Angle.FromDegrees(90);  // offset 0° spoke to top

// Clockwise (e.g. compass/bearing displays)
polarAxis.Clockwise = true;
polarAxis.Rotation = Angle.FromDegrees(-90);
formsPlot1.Refresh();
```

**Arrows from center to polar coordinates:**
```cs
PolarCoordinates[] points = new[]
{
    new PolarCoordinates(10, Angle.FromDegrees(15)),
    new PolarCoordinates(20, Angle.FromDegrees(120)),
    new PolarCoordinates(30, Angle.FromDegrees(240)),
};

var polarAxis = formsPlot1.Plot.Add.PolarAxis(radius: 30);
polarAxis.Circles.ForEach(x => x.LinePattern = LinePattern.Dotted);
polarAxis.Spokes.ForEach(x => x.LinePattern = LinePattern.Dotted);

IPalette palette = new ScottPlot.Palettes.Category10();
Coordinates center = polarAxis.GetCoordinates(0, 0);
for (int i = 0; i < points.Length; i++)
{
    Coordinates tip = polarAxis.GetCoordinates(points[i]);
    var arrow = formsPlot1.Plot.Add.Arrow(center, tip);
    arrow.ArrowLineWidth = 0;
    arrow.ArrowFillColor = palette.GetColor(i).WithAlpha(.7);
}
formsPlot1.Refresh();
```

**Styling spokes and circles:**
```cs
var polarAxis = formsPlot1.Plot.Add.PolarAxis();

// Style spokes (straight lines from center)
var radialPalette = new ScottPlot.Palettes.Category10();
for (int i = 0; i < polarAxis.Spokes.Count; i++)
{
    polarAxis.Spokes[i].LineColor = radialPalette.GetColor(i).WithAlpha(.5);
    polarAxis.Spokes[i].LineWidth = 4;
    polarAxis.Spokes[i].LabelStyle.ForeColor = radialPalette.GetColor(i);
    polarAxis.Spokes[i].LabelStyle.FontSize = 16;
    polarAxis.Spokes[i].LabelStyle.Bold = true;
}

// Style circles (concentric rings)
var circularColormap = new ScottPlot.Colormaps.Rain();
for (int i = 0; i < polarAxis.Circles.Count; i++)
{
    double fraction = (double)i / (polarAxis.Circles.Count - 1);
    polarAxis.Circles[i].LineColor = circularColormap.GetColor(fraction).WithAlpha(.5);
    polarAxis.Circles[i].LineWidth = 2;
    polarAxis.Circles[i].LinePattern = LinePattern.Dashed;
}
formsPlot1.Refresh();
```

**Background fill:**
```cs
var polarAxis = formsPlot1.Plot.Add.PolarAxis();
polarAxis.FillColor = Colors.Blue.WithAlpha(.2);
formsPlot1.Refresh();
```

**Custom spoke labels:**
```cs
var polarAxis = formsPlot1.Plot.Add.PolarAxis();
string[] labels = { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };
polarAxis.SetSpokes(labels, length: 1.1);  // length is fraction of radius
formsPlot1.Refresh();
```

**Tick (circle) labels + custom spoke count:**
```cs
var polarAxis = formsPlot1.Plot.Add.PolarAxis();
polarAxis.Rotation = Angle.FromDegrees(-90);

double[] tickPositions = { 5, 10, 15, 20 };
string[] tickLabels = { "A", "B", "C", "D" };
polarAxis.SetCircles(tickPositions, tickLabels);
polarAxis.SetSpokes(count: 5, length: 22, degreeLabels: false);
formsPlot1.Refresh();
```

**Manual spoke/circle geometry with individual styling:**
```cs
var polarAxis = formsPlot1.Plot.Add.PolarAxis();

// PolarAxisSpoke and PolarAxisCircle are in ScottPlot namespace
polarAxis.Spokes.Clear();
polarAxis.Spokes.Add(new ScottPlot.PolarAxisSpoke(Angle.FromDegrees(0),  0.5));
polarAxis.Spokes.Add(new ScottPlot.PolarAxisSpoke(Angle.FromDegrees(45), 0.75));
polarAxis.Spokes.Add(new ScottPlot.PolarAxisSpoke(Angle.FromDegrees(90), 1.0));

polarAxis.Circles.Clear();
polarAxis.Circles.Add(new ScottPlot.PolarAxisCircle(0.5));
polarAxis.Circles.Add(new ScottPlot.PolarAxisCircle(0.75));
polarAxis.Circles.Add(new ScottPlot.PolarAxisCircle(1.0));

ScottPlot.Palettes.Category10 pal = new();
for (int i = 0; i < 3; i++)
{
    polarAxis.Spokes[i].LineColor  = pal.GetColor(i).WithAlpha(.5);
    polarAxis.Circles[i].LineColor = pal.GetColor(i).WithAlpha(.5);
    polarAxis.Spokes[i].LineWidth  = 2 + i * 2;
    polarAxis.Circles[i].LineWidth = 2 + i * 2;
}
formsPlot1.Refresh();
```

**Radar chart using PolarAxis + Polygon:**
```cs
string[] labels = { "Speed", "Power", "Accuracy", "Stamina", "Defense" };
var polarAxis = formsPlot1.Plot.Add.PolarAxis();
polarAxis.SetSpokes(labels, 1.1);
polarAxis.SetCircles(5, 4);  // (maximumRadius, count)

double maxRadius = 5.0;
double[] p1Values = { 4.2, 3.8, 4.5, 3.0, 4.0 };
double[] p2Values = { 3.5, 4.8, 3.2, 4.5, 3.8 };
int n = labels.Length;

var p1Coords = new List<Coordinates>();
var p2Coords = new List<Coordinates>();
for (int i = 0; i < n; i++)
{
    double deg = 360.0 / n * i;
    p1Coords.Add(polarAxis.GetCoordinates(p1Values[i] / maxRadius, deg));
    p2Coords.Add(polarAxis.GetCoordinates(p2Values[i] / maxRadius, deg));
}
p1Coords.Add(p1Coords[0]);  // close polygon
p2Coords.Add(p2Coords[0]);

var poly1 = formsPlot1.Plot.Add.Polygon(p1Coords.ToArray());
poly1.FillColor = Colors.Blue.WithAlpha(40);
poly1.LineColor = Colors.Blue;
poly1.LineWidth = 2;

var poly2 = formsPlot1.Plot.Add.Polygon(p2Coords.ToArray());
poly2.FillColor = Colors.Red.WithAlpha(40);
poly2.LineColor = Colors.Red;
poly2.LineWidth = 2;

formsPlot1.Plot.Title("Radar Chart");
formsPlot1.Refresh();
```

**PolarAxis key properties & methods:**

| Member | Description |
|--------|-------------|
| `Add.PolarAxis(radius?)` | Add polar axis; optional max radius |
| `polarAxis.Rotation` | `Angle.FromDegrees(deg)` — rotate the 0° spoke |
| `polarAxis.Clockwise` | `true` = clockwise angle direction |
| `polarAxis.FillColor` | Background fill color |
| `polarAxis.Spokes` | `List<PolarAxisSpoke>` — straight lines from center |
| `polarAxis.Circles` | `List<PolarAxisCircle>` — concentric rings |
| `polarAxis.SetSpokes(count, length, degreeLabels?)` | Replace spokes evenly spaced |
| `polarAxis.SetSpokes(string[], length)` | Replace spokes with custom labels |
| `polarAxis.SetCircles(maxRadius, count)` | Replace circles evenly spaced |
| `polarAxis.SetCircles(double[], string[])` | Replace circles with custom labels |
| `polarAxis.GetCoordinates(radius, degrees)` | Convert polar → `Coordinates` |
| `polarAxis.GetCoordinates(PolarCoordinates)` | Convert polar struct → `Coordinates` |
| `spoke.LinePattern` | `LinePattern.Solid / Dashed / Dotted` |
| `spoke.LabelStyle.ForeColor / FontSize / Bold` | Spoke angle label style |
| `circle.LinePattern / LineColor / LineWidth` | Ring appearance |

### Radar Plot (verified)

`Add.Radar()` creates a dedicated radar/spider chart plottable backed by an internal `PolarAxis`.

> **Critical rule:** `Add.Radar()` starts with **0 spokes and 0 circles**.
> You **must** call `SetSpokes` before adding any `RadarSeries`,
> and the `Values` array length must equal the spoke count.
> Use `SetCircles(maxValue, N)` to add concentric ring ticks.

**Quickstart — single series:**
```cs
var radar = formsPlot1.Plot.Add.Radar();
// SetSpokes: count + length (= max data value); SetCircles: same max + ring count
radar.PolarAxis.SetSpokes(count: 5, length: 5.0);
radar.PolarAxis.SetCircles(5.0, 5);

radar.Series.Add(new ScottPlot.RadarSeries()
{
    Values    = new double[] { 5, 3, 4, 2, 5 },
    LegendText = "Score",
    FillColor = Colors.Blue.WithAlpha(.4),
    LineColor = Colors.Blue,
    LineWidth = 2,
});

formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**Multiple series with custom spoke labels:**
```cs
var radar = formsPlot1.Plot.Add.Radar();
string[] labels = { "Speed", "Power", "Accuracy", "Stamina", "Defense" };
radar.PolarAxis.SetSpokes(labels, length: 5.0);
radar.PolarAxis.SetCircles(5.0, 5);

radar.Series.Add(new ScottPlot.RadarSeries()
{
    Values    = new double[] { 4.2, 3.8, 4.5, 3.0, 4.0 },
    LegendText = "Player 1",
    FillColor = Colors.Blue.WithAlpha(.3),
    LineColor = Colors.Blue,
    LineWidth = 2,
});
radar.Series.Add(new ScottPlot.RadarSeries()
{
    Values    = new double[] { 3.5, 4.8, 3.2, 4.5, 3.8 },
    LegendText = "Player 2",
    FillColor = Colors.Red.WithAlpha(.3),
    LineColor = Colors.Red,
    LineWidth = 2,
});

formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**Styling spokes and circles via PolarAxis:**
```cs
var radar = formsPlot1.Plot.Add.Radar();
string[] labels = { "Speed", "Power", "Accuracy", "Stamina", "Defense" };
radar.PolarAxis.SetSpokes(labels, length: 5.0);
radar.PolarAxis.SetCircles(5.0, 5);

// Style each spoke individually
var pal = new ScottPlot.Palettes.Category10();
for (int i = 0; i < radar.PolarAxis.Spokes.Count; i++)
{
    radar.PolarAxis.Spokes[i].LineColor = pal.GetColor(i).WithAlpha(.7);
    radar.PolarAxis.Spokes[i].LabelStyle.ForeColor = pal.GetColor(i);
    radar.PolarAxis.Spokes[i].LabelStyle.Bold = true;
}

// Style circles
foreach (var circle in radar.PolarAxis.Circles)
{
    circle.LinePattern = LinePattern.Dashed;
    circle.LineColor = Colors.Gray.WithAlpha(.5);
}
formsPlot1.Refresh();
```

**Hatch fill (pattern fill for a series):**
```cs
var radar = formsPlot1.Plot.Add.Radar();
radar.PolarAxis.SetSpokes(count: 5, length: 5.0);
radar.PolarAxis.SetCircles(5.0, 5);

radar.Series.Add(new ScottPlot.RadarSeries()
{
    Values     = new double[] { 4.0, 3.5, 4.5, 3.0, 5.0 },
    LegendText = "Filled",
    FillColor  = Colors.Blue.WithAlpha(.3),
    LineColor  = Colors.Blue,
    LineWidth  = 2,
});
radar.Series.Add(new ScottPlot.RadarSeries()
{
    Values         = new double[] { 3.5, 4.8, 3.2, 4.5, 3.0 },
    LegendText     = "Hatched",
    FillColor      = Colors.Transparent,
    FillHatch      = new ScottPlot.Hatches.Striped(),
    FillHatchColor = Colors.Orange.WithAlpha(.7),
    LineColor      = Colors.Orange,
    LineWidth      = 2,
});

formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**Axis above data (draw grid lines on top of fill):**
```cs
var radar = formsPlot1.Plot.Add.Radar();
radar.IsAxisAboveData = true;  // set BEFORE adding series
radar.PolarAxis.SetSpokes(count: 5, length: 5.0);
radar.PolarAxis.SetCircles(5.0, 5);

radar.Series.Add(new ScottPlot.RadarSeries()
{
    Values    = new double[] { 5, 4, 3, 4, 5 },
    FillColor = Colors.Purple.WithAlpha(.4),
    LineColor = Colors.Purple,
    LineWidth = 2,
});
formsPlot1.Refresh();
```

**RadarSeries key properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Values` | `IReadOnlyList<double>` | One value per spoke; must match spoke count |
| `LegendText` | `string` | Series name in legend |
| `FillColor` | `Color` | Polygon fill color |
| `FillHatch` | `IHatch` | Hatch pattern (e.g. `new Hatches.Striped()`) |
| `FillHatchColor` | `Color` | Color for hatch lines |
| `LineColor` | `Color` | Polygon outline color |
| `LineWidth` | `float` | Polygon outline width |
| `LinePattern` | `LinePattern` | Outline dash pattern |

**Radar plottable key properties:**

| Member | Description |
|--------|-------------|
| `radar.PolarAxis` | Underlying `PolarAxis` — configure spokes & circles here |
| `radar.Series` | `List<RadarSeries>` — add series objects here |
| `radar.IsAxisAboveData` | Draw axis grid on top of filled polygons |
| `radar.ManageAxisLimits` | Auto square axes so circles appear round |

---

### Radial Gauge Chart (verified)

A radial gauge chart displays **scalar values as arc-shaped gauges** arranged concentrically.
Commonly used for dashboards, KPIs, and progress indicators.

**Quickstart:**
```cs
double[] values = { 100, 80, 65, 45, 20 };
formsPlot1.Plot.Add.RadialGaugePlot(values);
formsPlot1.Refresh();
```

**Palette / Colors:**
```cs
// Change gauge colors via palette (applied before Add)
formsPlot1.Plot.Add.Palette = new ScottPlot.Palettes.Nord();
double[] values = { 100, 80, 65, 45, 20 };
formsPlot1.Plot.Add.RadialGaugePlot(values);
formsPlot1.Refresh();
```

**Negative values:**
```cs
formsPlot1.Plot.Add.Palette = new ScottPlot.Palettes.Nord();
double[] values = { 100, 80, -65, 45, -20 };  // negative arcs fill opposite direction
formsPlot1.Plot.Add.RadialGaugePlot(values);
formsPlot1.Refresh();
```

**GaugeMode — Sequential (each gauge starts at tip of previous):**
```cs
formsPlot1.Plot.Add.Palette = new ScottPlot.Palettes.Nord();
double[] values = { 100, 80, 65, 45, 50 };
var rg = formsPlot1.Plot.Add.RadialGaugePlot(values);
rg.GaugeMode = ScottPlot.RadialGaugeMode.Sequential;
formsPlot1.Refresh();
```

**GaugeMode — SingleGauge (semicircle KPI-style):**
```cs
formsPlot1.Plot.Add.Palette = new ScottPlot.Palettes.Nord();
double[] values = { 100, 80, 65, 45 };
var rg = formsPlot1.Plot.Add.RadialGaugePlot(values);
rg.GaugeMode    = ScottPlot.RadialGaugeMode.SingleGauge;
rg.MaximumAngle = 180;  // semicircle
rg.StartingAngle = 180; // start from West
formsPlot1.Refresh();
```

**Angular range and direction:**
```cs
formsPlot1.Plot.Add.Palette = new ScottPlot.Palettes.Nord();
double[] values = { 100, 80, 65, 45, 20 };
var rg = formsPlot1.Plot.Add.RadialGaugePlot(values);
rg.MaximumAngle  = 270;    // 270° arc instead of full circle
rg.StartingAngle = 135;    // 270 for North (default), 0 for East, 90 for South
rg.Clockwise     = false;  // counter-clockwise fill
formsPlot1.Refresh();
```

**Labels in legend:**
```cs
formsPlot1.Plot.Add.Palette = new ScottPlot.Palettes.Nord();
double[] values = { 100, 80, 65, 45, 20 };
var rg = formsPlot1.Plot.Add.RadialGaugePlot(values);
rg.Labels = new string[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**Fine-tuning display:**
```cs
formsPlot1.Plot.Add.Palette = new ScottPlot.Palettes.Nord();
double[] values = { 100, 80, 65, 45, 20 };
var rg = formsPlot1.Plot.Add.RadialGaugePlot(values);
rg.ShowLevels                  = true;   // show value text on each gauge
rg.LabelPositionFraction       = 0.5;    // 0 = near base, 1 = at tip
rg.FontSizeFraction            = 0.4;    // label font size relative to gauge width
rg.SpaceFraction               = 0.05;   // gap between gauges (0.2 = 20% of gauge width)
rg.BackgroundTransparencyFraction = 0.3; // 0 = opaque background ring, 1 = fully transparent
formsPlot1.Refresh();
```

**RadialGaugePlot key properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Levels` | `double[]` | Source values (read the passed-in data) |
| `GaugeMode` | `RadialGaugeMode` | `Stacked` (default), `Sequential`, `SingleGauge` |
| `MaximumAngle` | `double` | Total arc in degrees (360 = full circle, 180 = semi) |
| `StartingAngle` | `float` | Start angle: 270=North, 0=East, 90=South, 180=West |
| `Clockwise` | `bool` | Fill direction (default `true`) |
| `OrderInsideOut` | `bool` | Draw order: `true` = innermost first |
| `Labels` | `string[]` | Legend labels (must match gauge count) |
| `Colors` | `Color[]` | Override gauge colors (must match gauge count) |
| `ShowLevels` | `bool` | Show numeric value text inside each gauge |
| `LabelPositionFraction` | `double` | Label position along gauge (0.0–1.0) |
| `FontSizeFraction` | `double` | Label font size as fraction of gauge width |
| `SpaceFraction` | `double` | Space between gauges as fraction of gauge width |
| `BackgroundTransparencyFraction` | `double` | Background ring transparency (0=dim, 1=transparent) |
| `CircularBackground` | `bool` | Full-circle background vs. stops at MaximumAngle |

---

### Arrow (verified)

`Add.Arrow()` draws a coordinate-space arrow pointing from a base to a tip.
Ideal for annotating key data points.

**Quickstart:**
```cs
double[] ys = Generate.Sin(51);
var sig = formsPlot1.Plot.Add.Signal(ys);

Coordinates arrowTip  = new(25, ys[25]);
Coordinates arrowBase = new(35, 1.3);
formsPlot1.Plot.Add.Arrow(new CoordinateLine(arrowBase, arrowTip));
formsPlot1.Refresh();
```

**Styling (color, width):**
```cs
Coordinates tip   = new(0, 0);
Coordinates base1 = new(1, 1);
var arrow = formsPlot1.Plot.Add.Arrow(new CoordinateLine(base1, tip));
arrow.ArrowFillColor = Colors.Red;      // arrowhead fill
arrow.ArrowLineColor = Colors.DarkRed;  // shaft/outline color
arrow.ArrowLineWidth = 2;               // shaft line thickness
arrow.ArrowWidth = 8;                   // shaft width (pixels)
formsPlot1.Refresh();
```

**Arrowhead shape customization:**
```cs
// Skinny needle arrowhead
var skinny = formsPlot1.Plot.Add.Arrow(
    new CoordinateLine(new Coordinates(0, 1), new Coordinates(1, 1)));
skinny.ArrowFillColor   = Colors.Green;
skinny.ArrowLineWidth   = 0;   // no shaft line
skinny.ArrowWidth       = 3;
skinny.ArrowheadLength  = 20;
skinny.ArrowheadWidth   = 7;

// Fat broad arrowhead
var fat = formsPlot1.Plot.Add.Arrow(
    new CoordinateLine(new Coordinates(0, 0), new Coordinates(1, 0)));
fat.ArrowFillColor  = Colors.Blue;
fat.ArrowLineWidth  = 0;
fat.ArrowWidth      = 18;
fat.ArrowheadLength = 20;
fat.ArrowheadWidth  = 30;
formsPlot1.Refresh();
```

**WithDelta helper — offset arrow base from tip:**
```cs
Coordinates tip   = new(25, 0.9);
Coordinates base1 = tip.WithDelta(5, 0.3);  // 5 units right, 0.3 units up
var arrow = formsPlot1.Plot.Add.Arrow(new CoordinateLine(base1, tip));
arrow.ArrowOffset = 10;  // pull tip back 10 pixels from arrowTip coordinate
formsPlot1.Refresh();
```

**Arrow key properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Tip` / `Base` | `Coordinates` | Arrow tip and base positions |
| `ArrowFillColor` | `Color` | Arrowhead fill color |
| `ArrowLineColor` | `Color` | Shaft / arrowhead outline color |
| `ArrowLineWidth` | `float` | Shaft line thickness |
| `ArrowWidth` | `float` | Shaft width in pixels |
| `ArrowheadLength` | `float` | Length of arrowhead |
| `ArrowheadWidth` | `float` | Width of arrowhead |
| `ArrowheadAxisLength` | `float` | Axis (back-notch) length of arrowhead |
| `ArrowOffset` | `float` | Pull arrowhead back from tip by N pixels |
| `ArrowMinimumLength` | `float` | Minimum total arrow length |
| `LegendText` | `string` | Label in legend |

---

### Polygon (verified)

`Add.Polygon()` draws a **closed filled shape** from an array of coordinate vertices.
Useful for highlighting regions, custom shapes, and geometric annotations.

**Quickstart:**
```cs
Coordinates[] coords = {
    new(0, 0), new(2, 4), new(4, 0)
};
formsPlot1.Plot.Add.Polygon(coords);
formsPlot1.Refresh();
```

**Fill color and border:**
```cs
Coordinates[] coords = {
    new(0, 0), new(1, 3), new(3, 4), new(5, 2), new(4, 0)
};
var poly = formsPlot1.Plot.Add.Polygon(coords);
poly.FillColor = Colors.Blue.WithAlpha(0.3);  // semi-transparent fill
poly.LineColor = Colors.Blue;
poly.LineWidth = 2;
formsPlot1.Refresh();
```

**Hatch fill:**
```cs
Coordinates[] coords = {
    new(0, 0), new(1, 3), new(3, 4), new(5, 2), new(4, 0)
};
var poly = formsPlot1.Plot.Add.Polygon(coords);
poly.FillColor      = Colors.LightBlue.WithAlpha(0.3);
poly.FillHatch      = new ScottPlot.Hatches.Striped(
    ScottPlot.Hatches.StripeDirection.DiagonalUp);
poly.FillHatchColor = Colors.Blue.WithAlpha(0.6);
poly.LineColor      = Colors.Blue;
formsPlot1.Refresh();
```

**Outline only (no fill):**
```cs
Coordinates[] coords = {
    new(0, 0), new(2, 4), new(4, 0)
};
var poly = formsPlot1.Plot.Add.Polygon(coords);
poly.FillColor = Colors.Transparent;  // no fill
poly.LineColor = Colors.DarkGreen;
poly.LineWidth = 3;
formsPlot1.Refresh();
```

**Generate polygon from math (regular N-gon):**
```cs
// Pentagon centered at (4, 1) with radius 1
int n = 5;
var pts = Enumerable.Range(0, n).Select(i => {
    double angle = Math.PI * 2 * i / n - Math.PI / 2;
    return new Coordinates(4 + Math.Cos(angle), 1 + Math.Sin(angle));
}).ToArray();
var pent = formsPlot1.Plot.Add.Polygon(pts);
pent.FillColor = Colors.Blue.WithAlpha(0.3);
pent.LineColor = Colors.Blue;
pent.LegendText = "Pentagon";
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

**Polygon key properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Coordinates` | `Coordinates[]` | Vertex coordinates (auto-closed) |
| `FillColor` | `Color` | Fill color (`Colors.Transparent` for no fill) |
| `FillHatch` | `IHatch` | Hatch pattern (e.g. `new Hatches.Striped(...)`) |
| `FillHatchColor` | `Color` | Hatch foreground color |
| `LineColor` | `Color` | Border line color |
| `LineWidth` | `float` | Border line thickness |
| `LinePattern` | `LinePattern` | Border line pattern |
| `LegendText` | `string` | Label in legend |
| `MarkerShape / MarkerSize` | — | Vertex markers |
| `PointCount` | `int` | Number of vertices (read-only) |



### Title & Labels
```cs
formsPlot1.Plot.Title("My Title");
formsPlot1.Plot.XLabel("X Axis");
formsPlot1.Plot.YLabel("Y Axis");
formsPlot1.Refresh();
```

### Axis Limits
```cs
formsPlot1.Plot.Axes.SetLimits(xMin: -10, xMax: 60, yMin: -2, yMax: 2);
formsPlot1.Plot.Axes.AutoScale();
formsPlot1.Plot.Axes.SetLimitsY(bottom: -2, top: 2);
formsPlot1.Refresh();
```

### DateTime Axis (verified)
```cs
DateTime[] dates = Generate.ConsecutiveDays(100);
double[] ys = Generate.RandomWalk(100);

var sp = formsPlot1.Plot.Add.Scatter(dates, ys);
sp.LineWidth = 2;
sp.LegendText = "Random Walk";

formsPlot1.Plot.Axes.DateTimeTicksBottom();
formsPlot1.Plot.Title("DateTime Axis Demo");
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

### Multiple Y Axes (verified)
```cs
var sig1 = formsPlot1.Plot.Add.Signal(Generate.Sin(51, mult: 0.01));
var sig2 = formsPlot1.Plot.Add.Signal(Generate.Cos(51, mult: 100));
sig1.Axes.YAxis = formsPlot1.Plot.Axes.Left;
sig2.Axes.YAxis = formsPlot1.Plot.Axes.Right;

formsPlot1.Plot.Axes.Left.Label.Text = "Left Axis (small)";
formsPlot1.Plot.Axes.Left.Label.ForeColor = sig1.Color;
formsPlot1.Plot.Axes.Right.Label.Text = "Right Axis (large)";
formsPlot1.Plot.Axes.Right.Label.ForeColor = sig2.Color;

formsPlot1.Plot.Title("Multi-Axis: Dual Y Scales");
formsPlot1.Plot.ShowLegend();
formsPlot1.Refresh();
```

### Add Extra Axes
```cs
var yAxis2 = formsPlot1.Plot.Axes.AddLeftAxis();
var sig = formsPlot1.Plot.Add.Signal(Generate.Cos(51, mult: 100));
sig.Axes.YAxis = yAxis2;
yAxis2.LabelText = "Secondary Y Axis";
formsPlot1.Refresh();
```

### Inverted Axis
```cs
formsPlot1.Plot.Axes.SetLimitsY(bottom: 1.5, top: -1.5);
// or auto-inverted:
formsPlot1.Plot.Axes.AutoScaler.InvertedY = true;
formsPlot1.Refresh();
```

### Square Units (circles stay circular)
```cs
formsPlot1.Plot.Axes.SquareUnits();
formsPlot1.Refresh();
```

### Frameless
```cs
formsPlot1.Plot.Axes.Frameless();
formsPlot1.Plot.HideGrid();
formsPlot1.Refresh();
```

## Legend
```cs
var sig1 = formsPlot1.Plot.Add.Signal(Generate.Sin());
sig1.LegendText = "Sin";
var sig2 = formsPlot1.Plot.Add.Signal(Generate.Cos());
sig2.LegendText = "Cos";
formsPlot1.Plot.ShowLegend();
// or position it:
formsPlot1.Plot.ShowLegend(Alignment.UpperRight);
formsPlot1.Refresh();
```

## Grid Customization (verified)
```cs
// Hide grid entirely
formsPlot1.Plot.HideGrid();

// Or customize grid with fill bands:
formsPlot1.Plot.Grid.MajorLineColor = Colors.Green.WithOpacity(.3);
formsPlot1.Plot.Grid.MajorLineWidth = 2;
formsPlot1.Plot.Grid.MinorLineColor = Colors.Gray.WithOpacity(.1);
formsPlot1.Plot.Grid.MinorLineWidth = 1;

// Alternating fill bands
formsPlot1.Plot.Grid.XAxisStyle.FillColor1 = Colors.Gray.WithOpacity(0.1);
formsPlot1.Plot.Grid.XAxisStyle.FillColor2 = Colors.Gray.WithOpacity(0.2);
formsPlot1.Plot.Grid.YAxisStyle.FillColor1 = Colors.Gray.WithOpacity(0.1);
formsPlot1.Plot.Grid.YAxisStyle.FillColor2 = Colors.Gray.WithOpacity(0.2);
formsPlot1.Refresh();
```

## Tick Customization

### Custom Tick Labels
```cs
double[] tickPositions = { 10, 25, 40 };
string[] tickLabels = { "Alpha", "Beta", "Gamma" };
formsPlot1.Plot.Axes.Bottom.SetTicks(tickPositions, tickLabels);
formsPlot1.Refresh();
```

### Manual Tick Generator (verified)
```cs
ScottPlot.TickGenerators.NumericManual ticks = new();
ticks.AddMajor(0, "zero");
ticks.AddMajor(20, "twenty");
ticks.AddMajor(50, "fifty");
ticks.AddMinor(10);
ticks.AddMinor(30);
ticks.AddMinor(40);
formsPlot1.Plot.Axes.Bottom.TickGenerator = ticks;

// Reduce Y-axis tick density
ScottPlot.TickGenerators.NumericAutomatic tickGenY = new();
tickGenY.TickDensity = 0.3;
formsPlot1.Plot.Axes.Left.TickGenerator = tickGenY;
formsPlot1.Refresh();
```

### Rotated Tick Labels
```cs
formsPlot1.Plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
formsPlot1.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
formsPlot1.Refresh();
```

### Custom Tick Formatter
```cs
static string CustomFormatter(double position) => $"{position:F1} units";
ScottPlot.TickGenerators.NumericAutomatic tickGen = new() { LabelFormatter = CustomFormatter };
formsPlot1.Plot.Axes.Bottom.TickGenerator = tickGen;
formsPlot1.Refresh();
```

### Log Scale Ticks (verified)
```cs
double[] xs = Generate.Consecutive(100);
double[] ys = Generate.NoisyExponential(100);
double[] logYs = ys.Select(Math.Log10).ToArray();
formsPlot1.Plot.Add.ScatterPoints(xs, logYs);

ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();
ScottPlot.TickGenerators.NumericAutomatic tickGen = new()
{
    MinorTickGenerator = minorTickGen,
    IntegerTicksOnly = true,
    LabelFormatter = (double y) => $"{Math.Pow(10, y):N0}",
};
formsPlot1.Plot.Axes.Left.TickGenerator = tickGen;

formsPlot1.Plot.Grid.MajorLineColor = Colors.Black.WithOpacity(.15);
formsPlot1.Plot.Grid.MinorLineColor = Colors.Black.WithOpacity(.05);
formsPlot1.Plot.Grid.MinorLineWidth = 1;

formsPlot1.Plot.Title("Log Scale Y Axis");
formsPlot1.Plot.XLabel("Sample");
formsPlot1.Plot.YLabel("Value (log)");
formsPlot1.Refresh();
```

### DateTime Fixed Interval Ticks
```cs
var dtAx = formsPlot1.Plot.Axes.DateTimeTicksBottom();
dtAx.TickGenerator = new ScottPlot.TickGenerators.DateTimeFixedInterval(
    new ScottPlot.TickGenerators.TimeUnits.Hour(), 6,
    new ScottPlot.TickGenerators.TimeUnits.Hour(), 1,
    dt => new DateTime(dt.Year, dt.Month, dt.Day));
formsPlot1.Refresh();
```

## Styling

### Colors
```cs
// Named colors
Colors.Red; Colors.Green; Colors.Blue;

// From RGB
new Color(red: 255, green: 0, blue: 0);

// From hex
new Color("#FF6600");

// From System.Drawing.Color
new Color(System.Drawing.Color.Green);

// Alpha / Opacity
Colors.Blue.WithAlpha(.5);
Colors.Red.WithOpacity(0.3);

// HSL
Color.FromHSL(hue: 0.5f, saturation: 1f, luminosity: 0.5f);

// Lighten / Darken / Mix
color.Lighten(.2);
color.Darken(.2);
color1.MixedWith(color2, fraction: 0.5);
Color.RandomHue();
```

### Colormaps
```cs
IColormap colormap = new ScottPlot.Colormaps.Viridis();
Color[] colors = colormap.GetColors(20);
```

### Dark Mode (verified)
```cs
var sig = formsPlot1.Plot.Add.Signal(Generate.SquareWaveFromSines());
sig.LineWidth = 3;
sig.Color = new Color("#2b9433");
sig.AlwaysUseLowDensityMode = true;

// Dark backgrounds
formsPlot1.Plot.FigureBackground.Color = new Color("#1c1c1e");
formsPlot1.Plot.DataBackground.Color = new Color("#2c2c2e");
formsPlot1.Plot.Axes.Color(new Color("#888888"));

// Grid fill + subtle lines
formsPlot1.Plot.Grid.XAxisStyle.FillColor1 = new Color("#888888").WithAlpha(10);
formsPlot1.Plot.Grid.YAxisStyle.FillColor1 = new Color("#888888").WithAlpha(10);
formsPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
formsPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Color = Colors.White.WithAlpha(15);
formsPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);
formsPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Color = Colors.White.WithAlpha(5);
formsPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width = 1;
formsPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Width = 1;

formsPlot1.Plot.Title("Dark Mode with Grid Fill");
formsPlot1.Plot.Axes.Title.Label.ForeColor = Colors.White;
formsPlot1.Refresh();
```

### Color Mixing & Interpolation (verified)
```cs
// Color interpolation: Blue → Green
for (int i = 0; i <= 10; i++)
{
    double fraction = (double)i / 10;
    double x = i;
    double y = Math.Sin(Math.PI * 2 * fraction);
    var circle = formsPlot1.Plot.Add.Circle(x, y, 0.4);
    circle.FillColor = Colors.Blue.MixedWith(Colors.Green, fraction);
    circle.LineColor = Colors.Black.WithAlpha(128);
}
formsPlot1.Plot.Axes.SquareUnits();
formsPlot1.Plot.Title("Color Mixing: Blue → Green");
formsPlot1.Refresh();
```

### Background Images
```cs
Image bgImage = new("background.png");
formsPlot1.Plot.DataBackground.Image = bgImage;
// or figure background:
formsPlot1.Plot.FigureBackground.Image = bgImage;
formsPlot1.Refresh();
```

## Plottable Management
```cs
// Clear all plottables
formsPlot1.Plot.Clear();

// Remove specific plottable
formsPlot1.Plot.Remove(sig1);

// Remove all of a type
formsPlot1.Plot.Remove<ScottPlot.Plottables.Signal>();

// Move plottable to front
formsPlot1.Plot.MoveToTop(rect1);
```

## Business Scenario Examples (Verified)

> The following examples demonstrate real-world business chart patterns, all compiled and visually verified.

### Stock Candlestick Chart with Volume
```cs
// Generate OHLC data
Random rand = new(42);
DateTime startDate = new DateTime(2025, 1, 2);
List<ScottPlot.OHLC> ohlcList = new();
double price = 150.0;
for (int i = 0; i < 60; i++)
{
    DateTime date = startDate.AddDays(i * 1.5);
    double open = price + rand.NextDouble() * 4 - 2;
    double close = open + rand.NextDouble() * 8 - 4;
    double high = Math.Max(open, close) + rand.NextDouble() * 3;
    double low = Math.Min(open, close) - rand.NextDouble() * 3;
    price = close;
    ohlcList.Add(new ScottPlot.OHLC(open, high, low, close, date, TimeSpan.FromDays(1)));
}

// Candlestick on left axis
var candle = formsPlot1.Plot.Add.Candlestick(ohlcList);
candle.Axes.YAxis = formsPlot1.Plot.Axes.Left;

// Volume bars on right axis with red/green coloring
var rightAxis = formsPlot1.Plot.Axes.Right;
ScottPlot.Bar[] volumeBars = new ScottPlot.Bar[ohlcList.Count];
for (int i = 0; i < ohlcList.Count; i++)
{
    double volume = rand.Next(500000, 3000000);
    bool isUp = ohlcList[i].Close >= ohlcList[i].Open;
    volumeBars[i] = new ScottPlot.Bar()
    {
        Position = ohlcList[i].DateTime.ToOADate(),
        Value = volume,
        Size = 0.6,
        FillColor = isUp
            ? new Color(38, 166, 91).WithAlpha(120)
            : new Color(220, 53, 69).WithAlpha(120),
    };
}
var barPlot = formsPlot1.Plot.Add.Bars(volumeBars);
barPlot.Axes.YAxis = rightAxis;

formsPlot1.Plot.Axes.DateTimeTicksBottom();
formsPlot1.Plot.Axes.Left.Label.Text = "Price (USD)";
formsPlot1.Plot.Axes.Right.Label.Text = "Volume";
formsPlot1.Plot.Title("AAPL Stock — Daily Candlestick + Volume");
formsPlot1.Plot.Axes.Right.Min = 0;
formsPlot1.Plot.Axes.Right.Max = 10000000;
formsPlot1.Refresh();
```

### Sensor Temperature Monitoring (Dark Industrial Theme)
```cs
// Simulate 500 temperature readings (1 sample/sec)
Random rand = new(123);
double[] temperatures = new double[500];
double temp = 45.0;
for (int i = 0; i < temperatures.Length; i++)
{
    temp += rand.NextDouble() * 2 - 0.95;
    if (i > 300) temp += 0.15; // heat event
    temperatures[i] = Math.Round(temp, 1);
}

var sig = formsPlot1.Plot.Add.Signal(temperatures);
sig.Data.Period = 1.0;
sig.LineWidth = 2;
sig.Color = new Color("#00BFFF");
sig.LegendText = "Sensor T1";

// Warning threshold at 55°C
var warnLine = formsPlot1.Plot.Add.HorizontalLine(55);
warnLine.Color = Colors.Orange;
warnLine.LineWidth = 2;
warnLine.LinePattern = LinePattern.Dashed;
warnLine.LegendText = "Warning (55°C)";

// Critical threshold at 65°C
var critLine = formsPlot1.Plot.Add.HorizontalLine(65);
critLine.Color = Colors.Red;
critLine.LineWidth = 2;
critLine.LinePattern = LinePattern.Dashed;
critLine.LegendText = "Critical (65°C)";

// Danger zone
var dangerZone = formsPlot1.Plot.Add.HorizontalSpan(65, 80);
dangerZone.FillColor = Colors.Red.WithAlpha(25);
dangerZone.LegendText = "Danger Zone";

// Dark industrial theme
formsPlot1.Plot.FigureBackground.Color = new Color("#1a1a2e");
formsPlot1.Plot.DataBackground.Color = new Color("#16213e");
formsPlot1.Plot.Axes.Color(new Color("#a0a0a0"));
formsPlot1.Plot.Grid.MajorLineColor = Colors.White.WithAlpha(15);

formsPlot1.Plot.Title("Industrial Sensor — Temperature Monitor");
formsPlot1.Plot.Axes.Title.Label.ForeColor = Colors.White;
formsPlot1.Plot.XLabel("Time (seconds)");
formsPlot1.Plot.YLabel("Temperature (°C)");
formsPlot1.Plot.ShowLegend(Alignment.UpperLeft);
formsPlot1.Refresh();
```

### Monthly Sales Year-over-Year Comparison
```cs
string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
double[] sales2024 = { 32, 28, 45, 38, 52, 48, 55, 61, 58, 63, 72, 85 };
double[] sales2025 = { 38, 35, 50, 42, 58, 55, 62, 70, 65, 71, 80, 95 };

Color color2024 = new Color("#5B9BD5");
Color color2025 = new Color("#ED7D31");
double barWidth = 0.35;

List<ScottPlot.Bar> bars = new();
for (int i = 0; i < 12; i++)
{
    bars.Add(new ScottPlot.Bar()
    {
        Position = i - barWidth / 2, Value = sales2024[i],
        Size = barWidth, FillColor = color2024,
    });
    bars.Add(new ScottPlot.Bar()
    {
        Position = i + barWidth / 2, Value = sales2025[i],
        Size = barWidth, FillColor = color2025,
    });
}
formsPlot1.Plot.Add.Bars(bars.ToArray());

// Month labels
Tick[] ticks = months.Select((m, i) => new Tick(i, m)).ToArray();
formsPlot1.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
formsPlot1.Plot.Axes.Margins(bottom: 0);

// Legend via invisible scatter
var leg1 = formsPlot1.Plot.Add.Scatter(new double[]{-100}, new double[]{-100});
leg1.Color = color2024; leg1.MarkerShape = MarkerShape.FilledSquare;
leg1.MarkerSize = 12; leg1.LineWidth = 0; leg1.LegendText = "2024";
var leg2 = formsPlot1.Plot.Add.Scatter(new double[]{-100}, new double[]{-100});
leg2.Color = color2025; leg2.MarkerShape = MarkerShape.FilledSquare;
leg2.MarkerSize = 12; leg2.LineWidth = 0; leg2.LegendText = "2025";

formsPlot1.Plot.Title("Monthly Sales Comparison — 2024 vs 2025");
formsPlot1.Plot.YLabel("Revenue (10K USD)");
formsPlot1.Plot.ShowLegend(Alignment.UpperLeft);
formsPlot1.Plot.Axes.SetLimitsX(-1, 12);
formsPlot1.Refresh();
```

### Server CPU & Memory Dashboard (Dual Y Axes)
```cs
Random rand = new(99);
int points = 300;

// CPU: 20-90% with spikes
double[] cpu = new double[points];
double cpuBase = 35;
for (int i = 0; i < points; i++)
{
    cpuBase += rand.NextDouble() * 6 - 3;
    cpuBase = Math.Clamp(cpuBase, 15, 90);
    if (i > 100 && i < 140) cpuBase += 15;
    if (i > 200 && i < 220) cpuBase += 25;
    cpu[i] = Math.Round(cpuBase, 1);
}

// Memory: 4-12 GB with gradual increase (leak simulation)
double[] memory = new double[points];
double memBase = 5.5;
for (int i = 0; i < points; i++)
{
    memBase += rand.NextDouble() * 0.1 - 0.04;
    memBase = Math.Clamp(memBase, 4, 14);
    if (i > 150) memBase += 0.02;
    memory[i] = Math.Round(memBase, 2);
}

var cpuSig = formsPlot1.Plot.Add.Signal(cpu);
cpuSig.Data.Period = 1.0;
cpuSig.LineWidth = 2;
cpuSig.Color = new Color("#FF6B35");
cpuSig.LegendText = "CPU (%)";
cpuSig.Axes.YAxis = formsPlot1.Plot.Axes.Left;

var memSig = formsPlot1.Plot.Add.Signal(memory);
memSig.Data.Period = 1.0;
memSig.LineWidth = 2;
memSig.Color = new Color("#4ECDC4");
memSig.LegendText = "Memory (GB)";
memSig.Axes.YAxis = formsPlot1.Plot.Axes.Right;

// CPU danger threshold
var cpuThreshold = formsPlot1.Plot.Add.HorizontalLine(80);
cpuThreshold.Color = Colors.Red.WithAlpha(150);
cpuThreshold.LinePattern = LinePattern.Dotted;
cpuThreshold.Axes.YAxis = formsPlot1.Plot.Axes.Left;

formsPlot1.Plot.Axes.Left.Label.Text = "CPU Usage (%)";
formsPlot1.Plot.Axes.Left.Label.ForeColor = new Color("#FF6B35");
formsPlot1.Plot.Axes.Right.Label.Text = "Memory (GB)";
formsPlot1.Plot.Axes.Right.Label.ForeColor = new Color("#4ECDC4");

var ann = formsPlot1.Plot.Add.Annotation("CPU Spike @ 200s");
ann.Alignment = Alignment.UpperRight;

formsPlot1.Plot.Title("Server Performance Dashboard — Real-Time");
formsPlot1.Plot.XLabel("Time (seconds)");
formsPlot1.Plot.ShowLegend(Alignment.UpperLeft);
formsPlot1.Plot.Grid.MajorLineColor = Colors.Gray.WithOpacity(.15);
formsPlot1.Refresh();
```

### Product Category Pie Chart with Custom Colors & Legend
```cs
double[] values = { 35, 25, 18, 12, 7, 3 };
var pie = formsPlot1.Plot.Add.Pie(values);
pie.ExplodeFraction = 0.05;

Color[] colors = {
    new Color("#4472C4"), new Color("#ED7D31"), new Color("#A5A5A5"),
    new Color("#FFC000"), new Color("#5B9BD5"), new Color("#70AD47"),
};
string[] labels = {
    "Electronics 35%", "Clothing 25%", "Food & Bev 18%",
    "Home 12%", "Sports 7%", "Other 3%",
};
for (int i = 0; i < pie.Slices.Count; i++)
{
    pie.Slices[i].FillColor = colors[i];
    pie.Slices[i].LegendText = labels[i];
}

formsPlot1.Plot.Title("Product Category Revenue Distribution — Q1 2025");
formsPlot1.Plot.Axes.Frameless();
formsPlot1.Plot.HideGrid();
formsPlot1.Plot.ShowLegend(Alignment.MiddleRight);
formsPlot1.Refresh();
```

## Additional Resources

For complete API reference and advanced examples, see [reference.md](reference.md).
