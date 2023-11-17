using OpenTK.Mathematics;
using Plot.Shader;
using Plot.Viewport;

namespace Plot.Function;

public class Function2D : IFunction
{
    private int _vao = 0, _vbo = 0;
    private Vector2[] _points;
    private static ShaderProgram _shader;
    private static bool _shaderInitialized = false;
    public IEnumerable<Vector2> Points => _points.OrderBy(p => p.X);

    public Function2D()
    {

    }
    public Box2 Domain
    {
        get
        {
            if (_points == null)
                throw new Exception("Function2D was not defined!");

            _points = _points.OrderBy(p => p.X).ToArray();
            var pointsByY = _points.OrderBy(p => p.Y).ToArray();

            return new Box2 { Min = (_points[0].X, pointsByY[0].Y), Max = (_points[^1].X, pointsByY[^1].Y) };
        }
    }

    public void FillPoints(float[] x, float[] y)
    {
        if (x == null || y == null)
            throw new Exception($"Point set {(x == null ? nameof(x) : nameof(y)).ToUpper()} was empty!");
        if (x.Length != y.Length)
            throw new Exception("Length of set X was not equal to length of set Y");

        _points = new Vector2[x.Length];
        for (int i = 0; i < x.Length; i++)
            _points[i] = (x[i], y[i]);

    }

    public void FillPoints(Vector2[] points)
    {
        _points = points;
    }

    public void Prepare()
    {
        if (!_shaderInitialized)
        {
            _shaderInitialized = true;
            _shader = new ShaderProgram(new[] { @"Function/Shaders/func.vert", @"Function/Shaders/func.frag" },
              new[] { ShaderType.VertexShader, ShaderType.FragmentShader });
            _shader.LinkShaders();

        }

        float[] pointsFloats = new float[2 * _points.Length];
        for (int i = 0; i < _points.Length * 2; i += 2)
        {
            pointsFloats[i] = _points[i / 2].X;
            pointsFloats[i + 1] = _points[i / 2].Y;
        }

        if (_vao == 0)
            _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        if (_vbo == 0)
            _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.BufferData(BufferTarget.ArrayBuffer, pointsFloats.Length * sizeof(float), pointsFloats, BufferUsageHint.StreamDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);

        GL.GetFloat(GetPName.AliasedLineWidthRange, new float[] { 2, 10 });

    }
    public void Draw(Color4 color, Box2 drawArea)
    {
        Vector2 Skew = drawArea.Size / Domain.Size;

        _shader.UseShaders();
        var ortho = Camera2D.Instance.GetOrthoMatrix();
        var model = Matrix4.CreateScale(Skew.X, Skew.Y, 1) *
                    Matrix4.CreateTranslation(-Domain.Center.X * Skew.X, -Domain.Center.Y * Skew.Y, 0);

        _shader.SetMatrix4("projection", ref ortho);
        _shader.SetMatrix4("model", ref model);
        _shader.SetVec4("color", ref color);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.LineStrip, 0, _points.Length);
    }
}