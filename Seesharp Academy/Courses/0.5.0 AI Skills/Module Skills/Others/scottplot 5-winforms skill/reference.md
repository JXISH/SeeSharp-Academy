# ScottPlot 5 WinForms API Reference

## NuGet Package Setup

Target framework: .NET 8.0+ (or .NET Framework 4.6.2+)

```xml
<PackageReference Include="ScottPlot" Version="5.1.58" />
<PackageReference Include="ScottPlot.WinForms" Version="5.1.58" />
```

DLL files are located at:
- `ScottPlot.5.1.58/lib/net8.0/ScottPlot.dll`
- `ScottPlot.WinForms.5.1.58/lib/net8.0-windows7.0/ScottPlot.WinForms.dll`

Dependencies: SkiaSharp (rendering engine)

## FormsPlot Control

The `FormsPlot` control is the main WinForms widget. Drag it from the Toolbox or create programmatically:

```cs
var formsPlot1 = new ScottPlot.WinForms.FormsPlot();
formsPlot1.Dock = DockStyle.Fill;
this.Controls.Add(formsPlot1);
```

Key property: `formsPlot1.Plot` — the `ScottPlot.Plot` object that manages all data and rendering.

Always call `formsPlot1.Refresh()` after modifying the plot to trigger a re-render.

## Plot.Add — Plottable Factory Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Add.Signal(double[])` | Signal | Y values with fixed X spacing |
| `Add.SignalXY(double[], double[])` | SignalXY | Signal with custom X values |
| `Add.Scatter(double[], double[])` | Scatter | Paired X/Y data points |
| `Add.Scatter(DateTime[], double[])` | Scatter | DateTime X axis scatter |
| `Add.ScatterPoints(double[], double[])` | Scatter | Markers only, no lines |
| `Add.ScatterLine(double[], double[])` | Scatter | Lines only, no markers |
| `Add.Bars(double[])` | BarPlot | Simple bar chart from values |
| `Add.Bars(Bar[])` | BarPlot | Custom bars with individual styling |
| `Add.Pie(double[])` | Pie | Pie chart |
| `Add.Heatmap(double[,])` | Heatmap | 2D heatmap |
| `Add.ColorBar(Heatmap)` | ColorBar | Color scale bar for heatmap |
| `Add.Candlestick(List<OHLC>)` | CandlestickPlot | Financial candlestick |
| `Add.OHLC(List<OHLC>)` | OhlcPlot | Financial OHLC |
| `Add.Boxes(List<Box>)` | BoxPlot | Box-and-whisker plot |
| `Add.RadialGaugePlot(double[])` | RadialGaugePlot | Radial gauge chart; `GaugeMode`, `MaximumAngle`, `Labels`, `Colors` |
| `Add.DataLogger()` | DataLogger | Growing real-time data; use `Add(y)`, `ViewFull/Jump/Slide` |
| `Add.DataStreamer(int count)` | DataStreamer | Fixed circular buffer; use `Add(y)`, `ViewScrollLeft/WipeRight` |
| `Add.HorizontalLine(double)` | HorizontalLine | Horizontal line at Y |
| `Add.VerticalLine(double)` | VerticalLine | Vertical line at X |
| `Add.HorizontalSpan(double, double)` | HorizontalSpan | Shaded horizontal range |
| `Add.VerticalSpan(double, double)` | VerticalSpan | Shaded vertical range |
| `Add.Circle(double, double, double)` | Ellipse | Circle at (x,y) with radius |
| `Add.Ellipse(double, double, double, double)` | Ellipse | Ellipse at (x,y) with radii |
| `Add.Polygon(Coordinates[])` | Polygon | Closed polygon; `FillColor`, `FillHatch`, `LineColor` |
| `Add.Text(string, double, double)` | Text | Text label in data coordinates |
| `Add.Annotation(string)` | Annotation | Text label in figure space (fixed position) |
| `Add.Tooltip(Coordinates tip, string text, Coordinates label)` | Tooltip | Text bubble pointing at a coordinate |
| `Add.Marker(double, double)` | Marker | Single marker at (x,y) |
| `Add.Markers(double[], double[], MarkerShape, float, Color)` | Markers | Batch markers with uniform style |
| `Add.Arrow(CoordinateLine)` | Arrow | Coordinate-space arrow (base→tip); `ArrowFillColor`, `ArrowWidth`, `ArrowheadLength` |
| `Add.FillY(double[], double[], double[])` | FillY | Filled area between curves |
| `Add.PolarAxis(radius?)` | PolarAxis | Polar coordinate system with spokes & circles |
| `Add.Radar()` | Radar | Radar/spider chart; configure via `radar.PolarAxis` + `radar.Series` |
| `Add.Plottable(IPlottable)` | IPlottable | Add custom plottable |
| `Add.Rectangle(CoordinateRect)` | Rectangle | Rectangle in data space |
| `Add.Bracket(double,double,double,double)` | Bracket | Bracket annotation |
| `Add.Callout(string,double,double)` | Callout | Callout annotation |
| `Add.Tooltip(double tipX, double tipY, string text, double labelX, double labelY)` | Tooltip | Text bubble (numeric coords); tip at `(tipX,tipY)`, label at `(labelX,labelY)` |

## Plot Properties & Methods

### Plot Title & Labels
```cs
formsPlot1.Plot.Title("Title");
formsPlot1.Plot.XLabel("X");
formsPlot1.Plot.YLabel("Y");
formsPlot1.Plot.Axes.Title.Label.FontSize = 24;
formsPlot1.Plot.Axes.Title.Label.Bold = true;
```

### Background
```cs
formsPlot1.Plot.FigureBackground.Color = Colors.White;
formsPlot1.Plot.DataBackground.Color = Colors.WhiteSmoke;
```

### Legend
```cs
formsPlot1.Plot.ShowLegend();
formsPlot1.Plot.ShowLegend(Alignment.UpperRight);
formsPlot1.Plot.Legend.FontSize = 14;
formsPlot1.Plot.Legend.FontName = "Arial";
```

### Save to Image
```cs
formsPlot1.Plot.SavePng("output.png", 800, 600);
formsPlot1.Plot.SaveJpeg("output.jpg", 800, 600);
formsPlot1.Plot.SaveSvg("output.svg", 800, 600);
```

## Axis System

### AxisManager (`formsPlot1.Plot.Axes`)

| Property/Method | Description |
|-----------------|-------------|
| `Axes.Bottom` | Primary bottom X axis |
| `Axes.Left` | Primary left Y axis |
| `Axes.Top` | Secondary top X axis |
| `Axes.Right` | Secondary right Y axis |
| `Axes.SetLimits(xMin, xMax, yMin, yMax)` | Set all axis limits |
| `Axes.SetLimitsX(left, right)` | Set X limits only |
| `Axes.SetLimitsY(bottom, top)` | Set Y limits only |
| `Axes.GetLimits()` | Returns `AxisLimits` |
| `Axes.AutoScale()` | Auto-fit data |
| `Axes.AutoScaleX()` | Auto-fit X only |
| `Axes.AutoScaleY()` | Auto-fit Y only |
| `Axes.Margins(horizontal, vertical)` | Whitespace fraction around data |
| `Axes.TightMargins()` | Zero whitespace |
| `Axes.SquareUnits()` | 1:1 pixel aspect ratio |
| `Axes.Frameless()` | Hide all axis panels |
| `Axes.DateTimeTicksBottom()` | Enable DateTime X ticks |
| `Axes.AddLeftAxis()` | Add extra left Y axis |
| `Axes.AddRightAxis()` | Add extra right Y axis |
| `Axes.AddBottomAxis()` | Add extra bottom X axis |
| `Axes.AddTopAxis()` | Add extra top X axis |
| `Axes.Color(Color)` | Color all axes |
| `Axes.AntiAlias(bool)` | Anti-alias axes/grid |
| `Axes.InvertX()` / `Axes.InvertY()` | Invert axis direction |
| `Axes.Link(Plot, bool, bool)` | Link axes with another plot |

### Axis Properties
```cs
formsPlot1.Plot.Axes.Bottom.Label.Text = "My Label";
formsPlot1.Plot.Axes.Bottom.Label.FontSize = 16;
formsPlot1.Plot.Axes.Bottom.Label.ForeColor = Colors.Blue;
formsPlot1.Plot.Axes.Bottom.TickLabelStyle.IsVisible = false;
formsPlot1.Plot.Axes.Bottom.MajorTickStyle.Length = 10;
formsPlot1.Plot.Axes.Bottom.MinorTickStyle.Length = 5;
formsPlot1.Plot.Axes.Bottom.FrameLineStyle.Width = 1;
formsPlot1.Plot.Axes.Bottom.MinimumSize = 50;
```

### Multi-Axis Pattern
```cs
var sig1 = formsPlot1.Plot.Add.Signal(data1);
var sig2 = formsPlot1.Plot.Add.Signal(data2);
sig1.Axes.YAxis = formsPlot1.Plot.Axes.Left;
sig2.Axes.YAxis = formsPlot1.Plot.Axes.Right;
// or create new axes:
var yAxis2 = formsPlot1.Plot.Axes.AddLeftAxis();
sig2.Axes.YAxis = yAxis2;
```

## Grid System

```cs
formsPlot1.Plot.HideGrid();

formsPlot1.Plot.Grid.MajorLineColor = Colors.Black.WithOpacity(.15);
formsPlot1.Plot.Grid.MajorLineWidth = 1;
formsPlot1.Plot.Grid.MinorLineColor = Colors.Black.WithOpacity(.05);
formsPlot1.Plot.Grid.MinorLineWidth = 1;
formsPlot1.Plot.Grid.IsBeneathPlottables = true; // false = grid on top

// Per-axis grid styling
formsPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Color = Colors.Magenta;
formsPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Width = 2;
formsPlot1.Plot.Grid.YAxisStyle.IsVisible = false;

// Grid fill bands
formsPlot1.Plot.Grid.XAxisStyle.FillColor1 = Colors.Gray.WithOpacity(0.1);
formsPlot1.Plot.Grid.XAxisStyle.FillColor2 = Colors.Gray.WithOpacity(0.2);
```

## Tick Generators

### NumericAutomatic (default)
```cs
ScottPlot.TickGenerators.NumericAutomatic tickGen = new()
{
    MinimumTickSpacing = 50,       // min pixel distance between ticks
    TickDensity = 0.5,             // fraction of default density
    TargetTickCount = 5,           // aim for N ticks
    IntegerTicksOnly = true,       // whole numbers only
    LabelFormatter = (double v) => $"{v:F2}",
};
formsPlot1.Plot.Axes.Bottom.TickGenerator = tickGen;
```

### NumericManual
```cs
ScottPlot.TickGenerators.NumericManual ticks = new();
ticks.AddMajor(0, "zero");
ticks.AddMajor(50, "fifty");
ticks.AddMinor(25);
formsPlot1.Plot.Axes.Bottom.TickGenerator = ticks;
```

### NumericFixedInterval
```cs
formsPlot1.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(10);
```

### DateTimeAutomatic
```cs
var axis = formsPlot1.Plot.Axes.DateTimeTicksBottom();
var tickGen = (ScottPlot.TickGenerators.DateTimeAutomatic)axis.TickGenerator;
tickGen.LabelFormatter = (DateTime dt) => dt.ToString("MMM dd");
```

### DateTimeManual
```cs
ScottPlot.TickGenerators.DateTimeManual ticks = new();
ticks.AddMajor(someDate, "Label");
formsPlot1.Plot.Axes.Bottom.TickGenerator = ticks;
```

### DateTimeFixedInterval
```cs
var dtAx = formsPlot1.Plot.Axes.DateTimeTicksBottom();
dtAx.TickGenerator = new ScottPlot.TickGenerators.DateTimeFixedInterval(
    new ScottPlot.TickGenerators.TimeUnits.Hour(), 6,    // major every 6 hours
    new ScottPlot.TickGenerators.TimeUnits.Hour(), 1,    // minor every 1 hour
    dt => new DateTime(dt.Year, dt.Month, dt.Day));      // snap to midnight
```

### Log Scale Minor Ticks
```cs
ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();
// or decimal distribution:
IMinorTickGenerator minorTickGen = new ScottPlot.TickGenerators.LogDecadeMinorTickGenerator();
```

### EvenlySpacedMinorTickGenerator
```cs
ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator minorTickGen = new(10);
```

## Color System

### ScottPlot.Color
```cs
// Named colors
Colors.Red; Colors.Blue; Colors.Green; Colors.Black; Colors.White;
Colors.Transparent;

// Constructors
new Color(255, 0, 0);             // RGB
new Color(255, 0, 0, 128);        // RGBA
new Color("#FF6600");              // Hex
new Color(System.Drawing.Color.Green);

// Methods
color.WithAlpha(.5);              // set alpha 0-1
color.WithOpacity(0.3);           // set opacity 0-1
color.Lighten(.2);
color.Darken(.2);
color.MixedWith(otherColor, 0.5);
Color.FromHSL(0.5f, 1f, 0.5f);
Color.RandomHue();
Color.InterpolateRgbArray(c1, c2, steps: 20);
```

### Colormaps
Available colormaps: `Viridis`, `Turbo`, `Inferno`, `Magma`, `Plasma`, `MellowRainbow`, etc.

```cs
IColormap cmap = new ScottPlot.Colormaps.Viridis();
Color[] colors = cmap.GetColors(count);
Color c = cmap.GetColor(fraction); // 0 to 1
```

### Palettes
```cs
ScottPlot.Palettes.Category10 palette = new();
Color c = palette.GetColor(index);
```

## Data Generation (for testing)

```cs
Generate.Sin(count);                     // sine wave
Generate.Cos(count);                     // cosine wave
Generate.RandomWalk(count);              // random walk
Generate.RandomSample(count, min, max);  // random values
Generate.Consecutive(count, spacing, offset);
Generate.ConsecutiveDays(count);         // DateTime[]
Generate.ConsecutiveHours(count);        // DateTime[]
Generate.ConsecutiveMinutes(count);      // DateTime[]
Generate.Sin2D(width, height);           // double[,] for heatmaps
Generate.Ramp2D(width, height);          // double[,] gradient
Generate.NoisyExponential(count);
Generate.SquareWaveFromSines(count);
```

## Common Plottable Properties

### Signal / Scatter
```cs
plottable.LineWidth = 2;
plottable.LineColor = Colors.Blue;
plottable.Color = Colors.Red;           // shorthand for LineColor
plottable.MarkerSize = 10;
plottable.MarkerShape = MarkerShape.FilledCircle;
plottable.LegendText = "My Series";
plottable.IsVisible = true;
```

### Scatter-specific
```cs
scatter.Smooth = true;
scatter.SmoothTension = 0.5;
scatter.ConnectStyle = ConnectStyle.StepHorizontal; // or StepVertical, Straight
```

### Signal-specific
```cs
signal.Data.YOffset = 5;
signal.Data.XOffset = 10;
signal.Data.Period = 0.5;
signal.AlwaysUseLowDensityMode = true;   // better anti-alias, lower performance
signal.MaximumMarkerSize = 10;
```

### Bar
```cs
bar.Position = 1;           // X position
bar.Value = 10;             // height
bar.ValueBase = 0;          // where bar starts
bar.Size = 0.8;             // width
bar.Error = 1.5;            // error bar size
bar.ErrorSize = 0.3;        // error bar whisker width
bar.FillColor = Colors.Blue;
bar.Label = "10";           // value label text
```

### Heatmap
```cs
heatmap.Colormap = new ScottPlot.Colormaps.Viridis();
heatmap.Smooth = true;
heatmap.FlipRows = true;
heatmap.FlipColumns = false;
heatmap.Opacity = 0.8;
heatmap.NaNCellColor = Colors.Transparent;
heatmap.Rectangle = new CoordinateRect(0, 10, 0, 10);
heatmap.Update();  // call after modifying Intensities
```

## Interactive Features

### DataLogger (growing real-time data)
```cs
var logger = formsPlot1.Plot.Add.DataLogger();
logger.Add(new Coordinates(1.5, 0.7));  // explicit X, Y
logger.Add(y: 0.5);                     // auto-increment X

logger.ViewFull();               // show all data
logger.ViewSlide(width: 100);    // sliding window
logger.ViewJump(width: 100, paddingFraction: 0.1);  // jump when data overflows
formsPlot1.Refresh();
```

### DataStreamer (fixed-length circular buffer)
```cs
var streamer = formsPlot1.Plot.Add.DataStreamer(500);
streamer.Add(value);
streamer.AddRange(values);
streamer.ManageAxisLimits = true;

streamer.ViewScrollLeft();       // newest data on right
streamer.ViewScrollRight();      // newest data on left
streamer.ViewWipeRight();        // overwrite left to right
streamer.ViewWipeLeft();         // overwrite right to left
```

## Floating Axis
```cs
ScottPlot.Plottables.FloatingAxis floatingX = new(formsPlot1.Plot.Axes.Bottom);
ScottPlot.Plottables.FloatingAxis floatingY = new(formsPlot1.Plot.Axes.Left);
formsPlot1.Plot.Axes.Frameless();
formsPlot1.Plot.HideGrid();
formsPlot1.Plot.Add.Plottable(floatingX);
formsPlot1.Plot.Add.Plottable(floatingY);
```

## Axis Rules

Axis rules constrain axis behavior:
```cs
// Lock horizontal axis
formsPlot1.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedHorizontal(formsPlot1.Plot.Axes.Bottom));

// Lock vertical axis  
formsPlot1.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.LockedVertical(formsPlot1.Plot.Axes.Left));

// Minimum/Maximum span
formsPlot1.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.MinimumSpan(formsPlot1.Plot.Axes.Bottom, 10));
```

## Render Events
```cs
formsPlot1.Plot.RenderManager.RenderStarting += (s, e) =>
{
    // Modify ticks before render
    Tick[] ticks = formsPlot1.Plot.Axes.Bottom.TickGenerator.Ticks;
    for (int i = 0; i < ticks.Length; i++)
    {
        DateTime dt = DateTime.FromOADate(ticks[i].Position);
        string label = $"{dt:MMM} '{dt:yy}";
        ticks[i] = new Tick(ticks[i].Position, label);
    }
};
```
