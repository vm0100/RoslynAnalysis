using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Core;

public static class ObjectExtensions
{
    public static bool IsNull(this object o)
    {
        return o == null;
    }

    public static bool IsNotNull(this object o)
    {
        return o != null;
    }
}