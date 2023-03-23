using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Convert;

public interface IAnalysisConvert
{
    string GenerateCode(string code);
}