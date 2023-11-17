using OpenTK.Mathematics;
using Plot.Viewport;

namespace Plot;

internal class PlotManager
{
    PlotView[] _plots;
    private PlotManager() { }

    private static PlotManager instance = null;
    public static PlotManager Instance => instance = instance ?? new PlotManager();
    public Vector2i ViewportSize { get; set; }

    int _nRows, _nColumns;
    public void SetupPlots(PlotView[] plotView, int nCols, int nRows)
    {
        _plots = plotView;
        _nRows = nRows;
        _nColumns = nCols;


        foreach (var plot in _plots)
            plot.SetAxes();

        ResetPlots(ViewportSize);
    }

    public void ResetPlots(Vector2i viewportSize)
    {
        ViewportSize = viewportSize;
        if (_plots is null) return;

        int nPlots = _plots.Length;
        for (int i = 0; i < _nRows; i++)
        {
            for (int j = 0; j < _nColumns; j++)
            {
                int plot_n = i * _nColumns + j;
                if (nPlots == plot_n) return;
                PlotView plot = _plots[plot_n];

                var area = CalcPlotArea(i, j);

                if (nPlots == plot_n + 1)
                {
                    area.Min = ( viewportSize.X / _nColumns * j, area.Min.Y);
                    area.Max = ( viewportSize.X, area.Max.Y);
                    plot.SetPlot(area);
                    plot.SetAxes();
                    break;
                }
                plot.SetPlot(area);
                plot.SetAxes();
                
            }
        }
    }

    public void DrawPlots()
    {
        if (_plots is null) return;
        foreach (var plot in _plots)
            plot.DrawPlotView(ViewportSize);
        Camera2D.Instance.Size = ViewportSize;
    }

    Box2i CalcPlotArea(int i, int j)
    {
        Box2i x = new Box2i();
        (int xSize, int ySize) size = (ViewportSize.X / _nColumns, ViewportSize.Y / _nRows);
        x.Max = (size.xSize * (j + 1), size.ySize * (i + 1));
        x.Min = (size.xSize * j, size.ySize * i);
        return x;
    }

}
