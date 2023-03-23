using System;
using System.Linq;
using System.Text;

using RoslynAnalysis.Convert.ToJava;

namespace RoslynAnalysis.Convert;

public class ConvertFactory
{
    public static IAnalysisConvert Create(ConvertEnum convertEnum)
    {
        if (convertEnum == ConvertEnum.Java)
        {
            return new ConvertJava();
        }

        throw new NotImplementedException($"未实现{convertEnum}的转换");
    }
}