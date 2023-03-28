using System;
using System.Linq;
using System.Text;

namespace RoslynAnalysis.Convert.AnalysisToJava
{
    public abstract class RewriterBase<T>
    {
        protected T _declaration;

        public RewriterBase(T declaration)
        {
            _declaration = declaration;
        }

        public virtual T Rewriter() => _declaration;
    }
}