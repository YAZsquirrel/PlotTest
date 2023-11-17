using OpenTK.Mathematics;
using OpenTK.Text;
using Plot.Shader;

namespace Plot.Viewport;

public class Axis
{
    public static int Margin = 20;
    public static int TickMaxSize = 20;
    public static bool IsInside = false;


    public (float X0, float X1) _range = default;
    public (float X0, float X1) Range
    {
        get { return _range; }
        set { _range = (MathF.Min(value.X0, value.X1), MathF.Max(value.X0, value.X1)); }
    }
    public int Width
    {
        get
        {
            int max_text_width = 0;
            switch (_pos)
            {
                case Position.Vertical:
                    foreach (var text in _texts)
                        max_text_width = (int)MathF.Max(text.HeightInPixels, max_text_width);
                    break;
                case Position.Horizontal:
                    foreach (var text in _texts)
                        max_text_width = (int)MathF.Max(text.WidthInPixels, max_text_width);
                    break;
             
            }
            max_text_width -= 5;
            return Margin + TickMaxSize + max_text_width;
        }
    }

    private int _vbo, _vao;
    private static ShaderProgram _shader;
    private static bool _isShaderInitialized = false;

    public enum Position
    {
        Vertical, Horizontal
    }

    private float[] _ticks;
    private Text[] _texts;
    private int _axis_position;
    private int _p0, _p1;

    private int _ticksNum;
    private Position _pos;

    //private Matrix4 _transform; 

    public Axis(int point0, int point1, int axis_position, Position position, int ticksNum = 41)
    {
        _p0 = point0;
        _p1 = point1;
        _axis_position = axis_position;
        _ticksNum = ticksNum;
        _pos = position;
        _texts = new Text[ticksNum / 5 + 1];

        if (!_isShaderInitialized)
        {
            _shader = new ShaderProgram(new[] { @"Viewport/Shaders/axis.frag", @"Viewport/Shaders/axis.vert" },
               new[] { ShaderType.FragmentShader, ShaderType.VertexShader });
            _shader.LinkShaders();
            _isShaderInitialized = true;
        }


        var tparams = new TextParams()
        {
            Color = Color4.Black,
            FontSize = 8,
            TextFontFamily = System.Drawing.FontFamily.GenericMonospace,
            CharXSpacing = 3,
        };

        for (int i = 0; i < _texts.Length; i++)
            _texts[i] = new Text("0.0E-00", tparams);

        CalcTicksAndText();
        Prepare();
    }

    private void Prepare()
    {
        _shader.UseShaders();

        if (_vao == 0)
            _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        if (_vbo == 0)
            _vbo = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _ticks.Length * sizeof(float), _ticks, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);


    }
    public void Draw()
    {
        _shader.UseShaders();
        var ortho = Camera2D.Instance.GetOrthoMatrix();
        _shader.SetMatrix4("projection", ref ortho);
        //_shader.SetMatrix4("transform", ref _transform);
        GL.LineWidth(2);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, 2 * _ticksNum);
        GL.BindVertexArray(0);

        foreach (var text in _texts)
        {
            text.DrawText(false);
        }
    }

    public void OnViewChange(int point0, int point1, int position)
    {
        _p0 = point0;
        _p1 = point1;
        _axis_position = position;
        CalcTicksAndText();
        Prepare();
    }

    private void CalcTicksAndText()
    {
        //_transform = _isX ? Matrix4.CreateTranslation(0, _position, 0) 
        //                  : Matrix4.CreateTranslation(_position, 0, 0);


        float v = (_p1 - _p0) / ((float)_ticksNum - 1);
        _ticks = new float[_ticksNum * 4];
        for (int i = 0; i < _ticksNum * 4; i += 4)
        {
            int k = i / 4;
            int mul = IsInside ? -1 : 1;

            float u = mul * TickMaxSize;
            if (k % 5 != 0)
                u /= 1.5f;
            else
            {
                var text = _texts[k / 5];
                text.SetCoordinates(
                    (
                      _pos == Position.Horizontal ? _p0 + k * v - 10 : _axis_position - TickMaxSize - (text.Params.FontSize - text.Params.CharXSpacing + 1.3f) * "0.0E-0".Length,
                      _pos == Position.Horizontal ? _axis_position - TickMaxSize - text.Params.FontSize - 5 : _p0 + k * v
                    ));

            }

            if (k % 10 != 0)
                u /= 1.5f;
            _ticks[i]     = _pos == Position.Horizontal ? _p0 + k * v : _axis_position;     // x // \ point on axis
            _ticks[i + 1] = _pos == Position.Horizontal ? _axis_position : _p0 + k * v;     // y // / point on axis
            _ticks[i + 2] = _pos == Position.Horizontal ? _p0 + k * v : _axis_position - u; // x
            _ticks[i + 3] = _pos == Position.Horizontal ? _axis_position - u : _p0 + k * v; // y
        }

        float dx = (Range.X1 - Range.X0) / ((float)_texts.Length - 1);
        for (int i = 0; i < _texts.Length; i++)
        {
            float ix = Range.X0 + dx * i;
            _texts[i].SetText(ix.ToString("g4"));
        }
    }
}