using OpenTK.Mathematics;

namespace Plot.Viewport;

public class Camera2D
{
   private static Camera2D _instance = null;
   public static Camera2D Instance { get; } = _instance = _instance ?? new Camera2D();

   public float Scale = 1;
   public Vector3 Position;
   public Vector2i Size;
   protected Camera2D()
   {
      Position = Vector3.Zero;
   }

   public Matrix4 GetOrthoMatrix(float width, float height) =>
      Matrix4.CreateOrthographic(width, height, -10, 10);

   public Matrix4 GetOrthoMatrix() => GetOrthoMatrix(Size.X, Size.Y);
   public Matrix4 GetTransformMatrix() =>
      Matrix4.CreateScale(Scale) * 
      Matrix4.CreateTranslation(Position);

}

