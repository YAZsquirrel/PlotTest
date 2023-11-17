using OpenTK.Mathematics;

namespace Plot.Function;

public sealed class FunctionManager
{
    private static FunctionManager? _instance = null;
    public static FunctionManager Instance => _instance ??= new();
    private readonly List<IFunction> _functions = new();
    public IEnumerable<IFunction> Functions => _functions;

    private readonly Dictionary<IFunction, Color4> _colors = new();

    private FunctionManager() { }

    public void AddNewFunction(IFunction function)
    {
       _functions.Add(function);
       Random r = new Random(function.GetHashCode());

       float R = r.NextSingle();
       float G = (1f - R) * r.NextSingle();
       float B = 1f - R - G;
       Color4 color = new Color4(R, G, B, 1f);
       _colors.Add(function, color);
    }
    public void RemoveFunction(IFunction function)
    {
       _functions.Remove(function);
       _colors.Remove(function);
    }
    public void DrawFunctions(Box2 DrawArea)
    {
        // надо соотнести размеры графиков
       foreach (var function in _functions)
       {
          function.Draw(_colors[function], DrawArea);
       }

    }


}