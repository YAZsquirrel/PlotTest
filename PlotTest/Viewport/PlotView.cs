using OpenTK.Mathematics;
using OpenTK.Text;
using Plot.Shader;
using System;

namespace Plot.Viewport;

public class PlotView
{
    private readonly int _vao, _vbo;
    private static ShaderProgram _shader;
    private static bool _shaderInitialized = false;

    private Axis _yAxis, _xAxis;
    private Text[] _axisNames;
    public Action DrawFunc { set; private get; }
    public Func<Box2, Box2> DrawFunction { set; private get; }

    public (float X0, float X1) XRange;
    public (float Y0, float Y1) YRange;


    private (int x0, int x1, int y0, int y1) _margin = (10, 10, 10, 10);
    public (int x0, int x1, int y0, int y1) Margin
    {
        set => _margin = value;
        get => _margin;
    }
    public PlotView(string xAxisName, string yAxisName)
    {
        if (!_shaderInitialized)
        {
            _shader = new ShaderProgram(new[] { @"Viewport/Shaders/plotbox.vert", @"Viewport/Shaders/plotbox.frag" },
                                        new[] { ShaderType.VertexShader, ShaderType.FragmentShader });
            _shader.LinkShaders();
            _shaderInitialized = true;
        }
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.BufferData(BufferTarget.ArrayBuffer, 8 * sizeof(float), new[]
        {
         -1f, -1f,
         1f, -1f,
         1f, 1f,
         -1f, 1f
        }, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);

        GL.LineWidth(1);

        TextParams axistparams = new()
        {
            Color = Color4.Black,
            FontSize = 14,
            TextFontFamily = System.Drawing.FontFamily.GenericMonospace,
            CharXSpacing = 6
        };

        _axisNames = new[]
        {
            new Text(xAxisName, axistparams),
            new Text(yAxisName, axistparams)
        };

    }
    private (int x0, int x1, int y0, int y1) _plotBox;
    public void SetPlot(Box2i DrawArea)
    {
        _plotBox.x0 = DrawArea.Min.X + _margin.x0 + _xAxis.Width;
        _plotBox.x1 = DrawArea.Max.X - _margin.x1 * 2;
        _plotBox.y0 = DrawArea.Min.Y + _margin.y0 + _yAxis.Width;
        _plotBox.y1 = DrawArea.Max.Y - _margin.y1 * 2;
    }
    public void DrawPlotView(Vector2i ViewportSize)
    {
        if (_plotBox.x1 - _plotBox.x0 <= 0 || _plotBox.y1 - _plotBox.y0 <= 0)
            return;
        //x0 =  _margin.x0 + Axis.Margin + Axis.TickMaxSize;
        //x1 =  _margin.x1 - Axis.Margin - Axis.TickMaxSize;
        //y0 =  _margin.y0 + Axis.Margin + Axis.TickMaxSize;
        //y1 =  ViewportSize.Y - (_margin.y1 * 2 + Axis.Margin) - Axis.TickMaxSize;
        GL.ClearColor(1f, 1f, 1f, 1f);

        GL.Viewport(_plotBox.x0, _plotBox.y0, _plotBox.x1 - _plotBox.x0, _plotBox.y1 - _plotBox.y0);
        GL.Scissor(_plotBox.x0, _plotBox.y0, _plotBox.x1 - _plotBox.x0, _plotBox.y1 - _plotBox.y0);
        GL.Enable(EnableCap.ScissorTest);

        Camera2D.Instance.Size = (_plotBox.x1 - _plotBox.x0, _plotBox.y1 - _plotBox.y0);
        
        { 
            Box2 area = new Box2((_plotBox.x0, _plotBox.y0), (_plotBox.x1, _plotBox.y1));

            Box2 domain = DrawFunction?.Invoke(area) ?? default;
            XRange = (domain.Min.X, domain.Max.X);
            YRange = (domain.Min.Y, domain.Max.Y);

            DrawFunc?.Invoke();
        }

        _shader.UseShaders();
        GL.LineWidth(2);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);

        // Stop clipping
        GL.Viewport(0, 0, ViewportSize.X, ViewportSize.Y);
        GL.Disable(EnableCap.ScissorTest);
        Camera2D.Instance.Size = ViewportSize;

        //Camera2D.Instance.Position = (ViewportSize.X / 2f, ViewportSize.Y / 2f, -1f);
        _yAxis?.Draw();
        _xAxis?.Draw();

        _axisNames[0].DrawText(false);
        _axisNames[1].DrawText(false);
    }

    public void SetAxes()
    {
        int x0 = _plotBox.x0 - Camera2D.Instance.Size.X / 2,
            x1 = _plotBox.x1 - Camera2D.Instance.Size.X / 2,

            y0 = _plotBox.y0 - Camera2D.Instance.Size.Y / 2,
            y1 = _plotBox.y1 - Camera2D.Instance.Size.Y / 2;

        if (_xAxis is null)
            _xAxis = new Axis(x0, x1, y0, Axis.Position.Horizontal);
        else
            _xAxis.OnViewChange(x0, x1, y0);

        _xAxis.Range = XRange;

        _axisNames[0].SetCoordinates((x1 + 5f, y0 - _axisNames[1].Params.FontSize / 2));

        if (_yAxis is null)
            _yAxis = new Axis(y0, y1, x0, Axis.Position.Vertical);
        else
            _yAxis.OnViewChange(y0, y1, x0);
        
        _yAxis.Range = YRange;

        _axisNames[1].SetCoordinates((x0 - 2f, y1 + 5f));

    }
}