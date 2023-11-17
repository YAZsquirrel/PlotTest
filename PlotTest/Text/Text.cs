using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;
using OpenTK.Mathematics;
using Plot.Shader;
using Plot.Viewport;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using ShaderType = OpenTK.Graphics.OpenGL4.ShaderType;

#pragma warning disable CA1416

namespace OpenTK.Text;

[Flags]
public enum TextAlignment 
{
   Center = 0b1111,
   HorizontalCenter = 0b0011,
   VerticalCenter = 0b1100,
   Right = 0b00001, 
   Left = 0b0010, 
   Top = 0b0100, 
   Bottom = 0b1000
}

static class TextSettings
{
   //public static int GlyphHeight = 64;
   //public static int GlyphWidth = 64;
   public static int GlyphsPerLine = 16;
   public static int GlyphLineCount = 16;
   public static int AtlasOffsetX = -3, AtlasOffsetY = -4;
}

public class TextParams
{
   public required FontFamily TextFontFamily;
   public FontStyle TextFontStyle;
   public TextAlignment Alignment;
   public Color4 Color = Color4.Black;
   public int FontSize;
   public int CharXSpacing = 11;

   public int GlyphWidth => (int)MathF.Pow(MathF.Ceiling(MathF.Log(FontSize, 2)), 2);
   public int GlyphHeight => GlyphWidth;
   public string FontFileName =>
      new StringBuilder()
         .Append("../../../Text/FontAtlases/")
         .Append(TextFontFamily.Name).Append(" ")
         .Append(TextFontStyle.ToString()).Append(" ")
         .Append(FontSize.ToString()).Append(" ")
         .Append(GlyphWidth).Append("x").Append(GlyphHeight)
         .Append(".bmp")
         .ToString();
}

public class Text : IDisposable
{
   Camera2D camera = Camera2D.Instance;

   private int _vaoBox = 0, _vboBox = 0, _eboBox = 0;
   private int _vaoText = 0, _vboText = 0, _eboText = 0, _vboUV = 0, _vboMat = 0;
   private ShaderProgram _shaderTextBox;
   private readonly ShaderProgram _shaderText;
   private float[] _vertices;
   private int _textureID;
   
   private string _text;
   private TextParams _params;
   public TextParams Params => _params;

   public int WidthInPixels => (_text.Length - 1) * _params.CharXSpacing + _text.Length * TextSettings.AtlasOffsetX;
   public int HeightInPixels => TextSettings.AtlasOffsetY;

   private int _textureWidth, _textureHeight;
   private Vector2 _coords = default;
   private Matrix4[] _textMats;
   private Vector2[] _textUVs;
   public Text(string? text = null, TextParams @params = null)
   {
      _shaderText = new ShaderProgram(new[] { @"Text/Shaders/text.vert", @"Text/Shaders/text.frag" },
                                         new[] { ShaderType.VertexShader, ShaderType.FragmentShader });
      _shaderText.LinkShaders();

      if (@params is not null)
      {
         _params = @params;
         if (!File.Exists(_params.FontFileName))
            GenerateFontImage();
         SetupFont();
      }

      if (text is not null)
      {
         _text = text;
         _coords = Vector2.One * 50f;
         PrepareText();
      }

   }

   public Text SetTextBox(Box2 box, (float top, float down, float left, float right) padding, Color4 backgroundColor)
   {
      _shaderTextBox = new ShaderProgram(new[] { @"Text/Shaders/textBox.vert", @"Text/Shaders/textBox.frag" },
                                         new[] { ShaderType.VertexShader, ShaderType.FragmentShader });
      _shaderTextBox.LinkShaders();

      var ur = box.Max - _coords;
      var ld = box.Min - _coords;
      _vertices = new[]
      {
         ld.X, ld.Y,
         ur.X, ld.Y,
         ld.X, ur.Y,
         ur.X, ur.Y
      };

      SetupTextBox(backgroundColor);

      return this;

   }
   private void SetupFont()
   {
      using var bitmap = new Bitmap(_params.FontFileName);

      _textureID = GL.GenTexture();
      GL.BindTexture(TextureTarget.Texture2D, _textureID);
      BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), 
         ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      
      GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
      GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
      //GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, 4, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, true);
      bitmap.UnlockBits(data);
      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
      GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
      _textureWidth = bitmap.Width; _textureHeight = bitmap.Height;
      GL.BindTexture(TextureTarget.Texture2D, 0);

      //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, 0, 0);
   }
   void GenerateFontImage()
   {
      int bitmapWidth = TextSettings.GlyphsPerLine * _params.GlyphWidth;
      int bitmapHeight = TextSettings.GlyphLineCount * _params.GlyphHeight;

      using Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      Font font;
      font = new Font(_params.TextFontFamily, _params.FontSize, _params.TextFontStyle);

      using (var g = System.Drawing.Graphics.FromImage(bitmap))
      {
         g.SmoothingMode = SmoothingMode.HighQuality;
         g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

         for (int p = 0; p < TextSettings.GlyphLineCount; p++)
         {
            for (int n = 0; n < TextSettings.GlyphsPerLine; n++)
            {
               char c = (char)(n + p * TextSettings.GlyphsPerLine);
               g.DrawString(c.ToString(), font, Brushes.White,
                  n * _params.GlyphWidth + TextSettings.AtlasOffsetX,
                  p * _params.GlyphHeight + _params.GlyphHeight - (int)(_params.FontSize) + TextSettings.AtlasOffsetY);// - 4 ); // + Settings.AtlasOffsetY);
            }
         }
      }
      bitmap.Save(_params.FontFileName);
   }
   void SetupTextBox(Color4 color4)
   {
      _shaderTextBox.SetVec4("backColor", ref color4);

      _vaoBox = GL.GenVertexArray();
      _vboBox = GL.GenBuffer();
      GL.BindVertexArray(_vaoBox);
      GL.BindBuffer(BufferTarget.ArrayBuffer, _vboBox);
      GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

      GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0); // x y
      GL.EnableVertexAttribArray(0);

      _eboBox = GL.GenBuffer();
      GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboBox);
      GL.BufferData(BufferTarget.ElementArrayBuffer, 6 * sizeof(uint), new[] { 0, 1, 2, 1, 2, 3 },
         BufferUsageHint.StaticDraw);

      GL.BindVertexArray(0);
      _shaderTextBox.UseShaders();

   }

   public Text SetTextAndParam(string text, TextParams textParams)
   {
      _text = text;
      _params = textParams;
      if (!File.Exists(_params.FontFileName))
         GenerateFontImage();

      SetupFont();

      PrepareText();
      return this;
   }
   public Text SetText(string text)
   {
      _text = text;
      if (_params is not null)
        PrepareText();
      return this;
   }
   public Text SetCoordinates(Vector2 coords)
   {
      _coords = coords;
      if (!string.IsNullOrEmpty(_text) && _params is not null)
         PrepareText();
      return this;
   }
   public Text SetColor(Color4 color)
   {
      _params.Color = color;
      return this;
   }
   public void DrawText(bool isContrastColor)
   {

      _shaderText.UseShaders();
      var ortho = camera.GetOrthoMatrix();
      _shaderText.SetMatrix4("projection", ref ortho);
      Color4 color = isContrastColor ? Color4.White : _params.Color;
      _shaderText.SetVec4("textColor", ref color);

      if (isContrastColor)
         GL.BlendFunc(BlendingFactor.OneMinusDstColor, BlendingFactor.OneMinusSrcAlpha);

      GL.BindVertexArray(_vaoText);
      GL.ActiveTexture(TextureUnit.Texture0);
      //GL.BindTexture(TextureTarget.Texture2DMultisample, _textureID);
      GL.BindTexture(TextureTarget.Texture2D, _textureID);
      GL.DrawElementsInstanced(PrimitiveType.TriangleStrip, 4, DrawElementsType.UnsignedInt, IntPtr.Zero, _text.Length);
      GL.BindVertexArray(0);

      if (isContrastColor)
         GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
   }

   public void Dispose()
   {
      GL.DeleteBuffers(6, new []{_vboBox, _eboBox, _eboText, _vboText, _vboUV, _vboMat });
      GL.DeleteVertexArray(_vaoBox);
      GL.DeleteVertexArray(_vaoText);
      GL.DeleteTexture(_textureID);
      _shaderTextBox?.Dispose();
      GC.SuppressFinalize(this);
   }

   void PrepareText()
   {
      float u_step = _params.GlyphWidth/ (float)_textureWidth;
      float v_step = _params.GlyphHeight/ (float)_textureHeight;

      var c = _coords;
      _textMats = new Matrix4[_text.Length];
      _textUVs = new Vector2[_text.Length];

      for (int n = 0; n < _text.Length; n++)
      {
         char idx = _text[n];
         float u = (idx % TextSettings.GlyphsPerLine) * u_step;
         float v = (idx / TextSettings.GlyphsPerLine) * v_step;

         _textMats[n] = Matrix4.CreateScale(_params.GlyphHeight)//(_params.FontSize) 
                        * Matrix4.CreateTranslation(c.X + (_params.GlyphWidth - _params.CharXSpacing) * n, c.Y, 0);//_params.CharXSpacing * n, c.Y, 0);
         _textUVs[n] = new Vector2(u, v);

      }

      _shaderText.UseShaders();
      var uvs = new Vector2(u_step, v_step);

      // prepare instance
      if (_vaoText == 0 && _eboText == 0 && _vboText == 0)
      {
         _vaoText = GL.GenVertexArray();
         GL.BindVertexArray(_vaoText);

         _vboText = GL.GenBuffer();
         GL.BindBuffer(BufferTarget.ArrayBuffer, _vboText);
         GL.BufferData(BufferTarget.ArrayBuffer, 16 * sizeof(float), 
            new []
            {//x  y  us      vs
               0, 0, 0,      v_step,
               1, 0, u_step, v_step,
               0, 1, 0,      0,
               1, 1, u_step, 0 
            },
            BufferUsageHint.StaticDraw);

         GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0); // x y
         GL.EnableVertexAttribArray(0);

         GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float)); // x y
         GL.EnableVertexAttribArray(1);

         _eboText = GL.GenBuffer();
         GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboText);
         GL.BufferData(BufferTarget.ElementArrayBuffer, 4 * sizeof(uint), new uint[] { 0, 1, 2, 3 },
            BufferUsageHint.StreamDraw);

         GL.BindVertexArray(0);
      }

      if (_vboUV == 0 && _vboMat == 0)
      {
         _vboUV = GL.GenBuffer();
         _vboMat = GL.GenBuffer();
      }

      // prepare per instance
      {
         GL.BindBuffer(BufferTarget.ArrayBuffer, _vboUV);
         GL.BufferData(BufferTarget.ArrayBuffer, _textUVs.Length * 2 * sizeof(float), _textUVs, BufferUsageHint.StreamDraw);

         GL.BindVertexArray(_vaoText);
         GL.EnableVertexAttribArray(3);
         GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

         GL.VertexAttribDivisor(3, 1);

         GL.BindBuffer(BufferTarget.ArrayBuffer, _vboMat);
         GL.BufferData(BufferTarget.ArrayBuffer, _textMats.Length * 16 * sizeof(float), _textMats,
            BufferUsageHint.StaticDraw);

         int vec4Size = 4 * sizeof(float);

         GL.EnableVertexAttribArray(4);
         GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, 0);
         GL.EnableVertexAttribArray(5);
         GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (1 * vec4Size));
         GL.EnableVertexAttribArray(6);
         GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (2 * vec4Size));
         GL.EnableVertexAttribArray(7);
         GL.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, 4 * vec4Size, (3 * vec4Size));
         
         GL.VertexAttribDivisor(4, 1);
         GL.VertexAttribDivisor(5, 1);
         GL.VertexAttribDivisor(6, 1);
         GL.VertexAttribDivisor(7, 1);

         GL.BindVertexArray(0);
      }
   }
}