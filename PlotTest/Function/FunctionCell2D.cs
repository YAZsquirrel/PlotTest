using OpenTK.Mathematics;
using Plot.Shader;
using Plot.Viewport;
using System;
using System.Linq;

namespace Plot.Function;

public class FunctionCell2D : IFunction
{
    private Box2[] _cells;
    private float[] _values;
    private Matrix4[] _mats;
    private Vector3[] _colors;

    public float Min => _values.Min();
    public float Max => _values.Max();

    private static ShaderProgram _shader, _shaderLine;
    private static bool _shadersInitialized = false;

    private int _vaoCell, _vaoBorder;


    public FunctionCell2D(Box2[] cells, float[] values)
    {
        SetCells(cells, values);
    }

    public FunctionCell2D()
    {
    }
    public Box2 Domain
    {
        get
        {
            if (_cells == null)
                throw new Exception("FunctionCell2D was not defined!");

            float minx = _cells.MinBy(x => x.Min.X).Min.X;
            float miny = _cells.MinBy(x => x.Min.Y).Min.Y;
            float maxx = _cells.MaxBy(x => x.Max.X).Max.X;
            float maxy = _cells.MaxBy(x => x.Max.Y).Max.Y;

            return new Box2 { Min = (minx, miny), Max = (maxx, maxy) };
        }
    }

    public void SetCells(Box2[] cells, float[] values)
    {
        if (cells == null || values == null)
            throw new Exception($"{(cells == null ? nameof(cells) : nameof(values)).ToUpper()} was empty!");
        if (cells.Length != values.Length)
            throw new Exception("Length cells array was not equal to length of values array");

        _cells = cells;
        _values = values;
    }

    public void FillValues(float[] values)
    {
        if (_cells.Length != values.Length)
            throw new Exception("Length cells array was not equal to length of values array");

        _values = values;
    }

    public void Prepare()
    {
        if (!_shadersInitialized)
        {
            _shader = new ShaderProgram(new[] { @"Function/Shaders/func2d.vert", @"Function/Shaders/func2d.frag" },
               new[] { ShaderType.VertexShader, ShaderType.FragmentShader });
            _shader.LinkShaders();

            _shaderLine = new ShaderProgram(new[] { @"Function/Shaders/border.vert", @"Function/Shaders/border.frag" },
                                             new[] { ShaderType.VertexShader, ShaderType.FragmentShader });
            _shaderLine.LinkShaders();

            _shadersInitialized = true;
        }
        int _vboColors, _vboCellMat, _vboCell, _vboBorder, _vboBorderMat;
        int _eboCell, _eboBorder;
        {
            _vaoCell = GL.GenVertexArray();
            GL.BindVertexArray(_vaoCell);

            _vboCell = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboCell);
            GL.BufferData(BufferTarget.ArrayBuffer, 8 * sizeof(float),
               new[]
               {
                  //x   y     
                   -0.5f, -0.5f,
                    0.5f, -0.5f,
                   -0.5f,  0.5f,
                    0.5f,  0.5f,
               },
               BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, true, 2 * sizeof(float), 0); // x y
        }

        float scale = Max - Min, min = Min;
        if (scale == 0)
            scale = 1f;
        _colors = new Vector3[_values.Length];
        for (int i = 0; i < _values.Length; i++)
        {
            float t = (_values[i] - min) / scale;
            _colors[i] = (1 - t) * Color0 + t * Color1;
        }

        _mats = new Matrix4[_values.Length];

        for (int i = 0; i < _mats.Length; i++)
        {
            (float scalex, float scaley) = _cells[i].Size;
            (float tx, float ty) = _cells[i].Center;

            _mats[i] = Matrix4.CreateScale(scalex, scaley, 1f) * Matrix4.CreateTranslation(tx, ty, 0);
        }

        GL.BindVertexArray(_vaoCell);
        _vboColors = GL.GenBuffer();
        _vboCellMat = GL.GenBuffer();
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboColors);
            GL.BufferData(BufferTarget.ArrayBuffer, _colors.Length * sizeof(float) * 3, _colors, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
            GL.VertexAttribDivisor(2, 1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboCellMat);
            GL.BufferData(BufferTarget.ArrayBuffer, _mats.Length * 16 * sizeof(float), _mats,
               BufferUsageHint.StaticDraw);

            int vec4Size = 4 * sizeof(float);

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, 0);
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (1 * vec4Size));
            GL.EnableVertexAttribArray(5);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (2 * vec4Size));
            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (3 * vec4Size));

            GL.VertexAttribDivisor(3, 1);
            GL.VertexAttribDivisor(4, 1);
            GL.VertexAttribDivisor(5, 1);
            GL.VertexAttribDivisor(6, 1);

            _eboCell = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboCell);
            GL.BufferData(BufferTarget.ElementArrayBuffer, 4 * sizeof(uint), new uint[] { 0, 1, 2, 3 },
               BufferUsageHint.StaticDraw);

        }
        GL.BindVertexArray(0);

        {
            _vaoBorder = GL.GenVertexArray();
            GL.BindVertexArray(_vaoBorder);

            _vboBorder = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboBorder);
            GL.BufferData(BufferTarget.ArrayBuffer, 8 * sizeof(float),
               new[]
               {
                  //x   y     
                   -0.5f, -0.5f,
                    0.5f, -0.5f,
                   -0.5f,  0.5f,
                    0.5f,  0.5f,
               },
               BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, true, 2 * sizeof(float), 0); // x y

            _vboBorderMat = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboBorderMat);
            GL.BufferData(BufferTarget.ArrayBuffer, _mats.Length * 16 * sizeof(float), _mats,
               BufferUsageHint.StaticDraw);

            int vec4Size = 4 * sizeof(float);

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, 0);
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (1 * vec4Size));
            GL.EnableVertexAttribArray(5);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (2 * vec4Size));
            GL.EnableVertexAttribArray(6);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (3 * vec4Size));

            GL.VertexAttribDivisor(3, 1);
            GL.VertexAttribDivisor(4, 1);
            GL.VertexAttribDivisor(5, 1);
            GL.VertexAttribDivisor(6, 1);

            _eboBorder = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboBorder);
            GL.BufferData(BufferTarget.ElementArrayBuffer, 4 * sizeof(uint), new uint[] { 0, 1, 3, 2 },
               BufferUsageHint.StaticDraw);
        }

        //GL.DeleteBuffers(7, new[] { _vboColors, _vboCellMat, _vboCell, _vboBorder, _vboBorderMat, _eboBorder, _eboCell });
        GL.BindVertexArray(0);
    }

    public Vector3 Color1 { get; set; }
    public Vector3 Color0 { get; set; }

    public void Draw(Color4 color, Box2 drawArea)
    {
        // Cells
        Vector2 Skew = drawArea.Size / Domain.Size;

        var ortho = Camera2D.Instance.GetOrthoMatrix();
        var model = Matrix4.CreateScale(Skew.X, Skew.Y, 1)
                  * Matrix4.CreateTranslation(-Domain.Center.X * Skew.X, -Domain.Center.Y * Skew.Y, 0);
        // inside
        {
            _shader.UseShaders();
            _shader.SetMatrix4("projection", ref ortho);
            _shader.SetMatrix4("model", ref model);

            GL.BindVertexArray(_vaoCell);
            GL.DrawElementsInstanced(PrimitiveType.TriangleStrip, 4, DrawElementsType.UnsignedInt, IntPtr.Zero, _cells.Length);
            GL.BindVertexArray(0);
        }
        //border
        {
            _shaderLine.UseShaders();
            _shaderLine.SetMatrix4("projection", ref ortho);
            _shaderLine.SetMatrix4("model", ref model);
            _shaderLine.SetVec4("aColor", ref color);

            GL.BindVertexArray(_vaoBorder);
            //GL.BindVertexArray(_vaoCell);
            GL.DrawElementsInstanced(PrimitiveType.LineLoop, 4, DrawElementsType.UnsignedInt, IntPtr.Zero, _cells.Length);
            GL.BindVertexArray(0);
        }
    }
}